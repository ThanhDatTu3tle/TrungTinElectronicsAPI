using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TrungTinElectronicsAPI.Models;
using TrungTinElectronicsAPI.Repositories;

namespace TrungTinElectronicsAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PushSubscriptionController : ControllerBase
{
    private readonly PushSubscriptionRepository _repo;

    public PushSubscriptionController(PushSubscriptionRepository repo)
    {
        _repo = repo;
    }

    [HttpPost]
    public async Task<IActionResult> Subscribe([FromBody] PushSubscriptionRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _repo.SaveAsync(userId, request);
        return Ok(new { message = "Đăng ký thông báo thành công" });
    }

    [HttpDelete]
    public async Task<IActionResult> Unsubscribe([FromBody] UnsubscribeRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _repo.DeleteAsync(userId, request.Endpoint);
        return Ok(new { message = "Hủy đăng ký thông báo thành công" });
    }

    [HttpGet("vapid-key")]
    [AllowAnonymous]
    public IActionResult GetVapidKey([FromServices] IConfiguration config)
    {
        return Ok(new { publicKey = config["Vapid:PublicKey"] });
    }
}

public class UnsubscribeRequest
{
    public string Endpoint { get; set; } = string.Empty;
}
