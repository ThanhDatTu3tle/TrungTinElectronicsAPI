using BCrypt.Net;
using CloudinaryDotNet.Core;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TrungTinElectronicsAPI.Data;
using TrungTinElectronicsAPI.Models;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly ApplicationDbContext _context;

    public AuthController(IConfiguration config, ApplicationDbContext context)
    {
        _config = config;
        _context = context;
    }

    // =============================
    // 🔹 API ĐĂNG KÝ USER
    // =============================
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            return BadRequest(new
            {
                message = "error",
                statusCode = "400",
                result = "Tên đăng nhập đã tồn tại"
            });

        // ✅ Kiểm tra email đã tồn tại chưa
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            return BadRequest(new
            {
                message = "error",
                statusCode = "400",
                result = "Email đã tồn tại"
            });

        // ✅ Kiểm tra sđt đã tồn tại chưa
        if (!string.IsNullOrWhiteSpace(request.Phone) && await _context.Users.AnyAsync(u => u.Phone == request.Phone))
            return BadRequest(new
            {
                message = "error",
                statusCode = "400",
                result = "Số điện thoại đã tồn tại"
            });

        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var user = new User
        {
            Username = request.Username,
            PasswordHash = hashedPassword,
            Email = request.Email,
            FullName = request.FullName,
            Phone = request.Phone,
            Role = request.Role ?? "user"
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "success",
            statusCode = "200",
            result = "User created successfully"
        });
    }

    // =============================
    // 🔹 API ĐĂNG NHẬP
    // =============================
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var input = request.UsernameOrEmailOrPhone?.Trim();
        // var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (string.IsNullOrEmpty(input))
            return Ok(new
            {
                message = "error",
                statusCode = "warning",
                result = "Input required"
            });

        // 🔍 Tìm user theo username OR email OR phone
        var user = await _context.Users
            .FirstOrDefaultAsync(u =>
                u.Username == input ||
                u.Email == input ||
                u.Phone == input);

        if (user == null)
            return Ok(new
            {
                message = "error",
                statusCode = "warning",
                result = "Invalid username/email/phone"
            });

        // 🔑 Kiểm tra password
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Ok(new
            {
                message = "error",
                statusCode = "warning",
                result = "Invalid password"
            });

        // 🎫 Tạo JWT token
        var token = GenerateJwtToken(user);

        return Ok(new
        {
            message = "success",
            statusCode = "200",
            result = "Login successfully",
            token,
            user.Role
        });
    }

    // =============================
    // 🔹 API ĐĂNG NHẬP BẰNG TÀI KHOẢN GOOGLE
    // =============================
    [HttpPost("google-login")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        var settings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = new List<string> { _config["Google:ClientId"] }
        };

        var payload = await GoogleJsonWebSignature.ValidateAsync(request.Token, settings);

        var email = payload.Email;
        var name = payload.Name;
        var avatar = payload.Picture;

        // Kiểm tra user trong DB
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == email);

        if (user == null)
        {
            user = new User
            {
                Email = email,
                FullName = name,
                CreatedAt = DateTime.UtcNow,
                Role = "User" // default role
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        // tạo JWT
        var jwt = GenerateJwtToken(user);

        return Ok(new { token = jwt });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var user = await _context.Users
            .Where(u => u.Id == int.Parse(userId))
            .Select(u => new { u.Id, u.Username, u.Email, u.FullName, u.Phone, DateOfBirth = u.DateOfBirth.HasValue ? u.DateOfBirth.Value.ToString("yyyyMMdd") : null })
            .FirstOrDefaultAsync();

        if (user == null)
            return NotFound();

        return Ok(user);
    }

    [Authorize]
    [HttpPut("update-profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        // Lấy userId từ claim trong JWT (đã generate bằng GenerateJwtToken)
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim))
            return Unauthorized();

        if (!int.TryParse(userIdClaim, out int userId))
            return BadRequest("Invalid user id");

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return NotFound();

        // Cập nhật các trường nếu có gửi
        user.FullName = string.IsNullOrWhiteSpace(request.FullName) ? user.FullName : request.FullName;
        user.Phone = string.IsNullOrWhiteSpace(request.Phone) ? user.Phone : request.Phone;
        user.DateOfBirth = request.DateOfBirth ?? user.DateOfBirth;

        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Trả về thông tin user cập nhật luôn cho FE
        var result = new
        {
            user.Username,
            user.Email,
            user.FullName,
            user.Phone,
            DateOfBirth = user.DateOfBirth?.ToString("yyyy-MM-dd")
        };

        return Ok(result);
    }

    // =============================
    // 🔹 Generate JWT
    // =============================
    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // duy nhất
            new Claim(ClaimTypes.Name, user.FullName ?? user.Email),   // fallback nếu FullName null
            new Claim(ClaimTypes.Role, user.Role ?? "User")           // default role
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(6),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public class RegisterRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string? Role { get; set; }
    }

    public class LoginRequest
    {
        public string UsernameOrEmailOrPhone { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class GoogleLoginRequest
    {
        public string Token { get; set; } = string.Empty;
    }

    public class UpdateProfileRequest
    {
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public DateTime? DateOfBirth { get; set; }
    }
}
