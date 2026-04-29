using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrungTinElectronicsAPI.Services;

namespace TrungTinElectronicsAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "admin")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        // GET /api/Notification?page=1&pageSize=20
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            var (items, totalCount) = await _notificationService.GetAllAsync(page, pageSize);

            return Ok(new
            {
                message = "Lấy danh sách thông báo thành công",
                result = true,
                data = items,
                totalCount,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            });
        }

        // GET /api/Notification/unread-count
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var count = await _notificationService.GetUnreadCountAsync();

            return Ok(new
            {
                message = "Lấy số thông báo chưa đọc thành công",
                result = true,
                count
            });
        }

        // PUT /api/Notification/{id}/read
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var success = await _notificationService.MarkAsReadAsync(id);

            if (!success)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy thông báo",
                    result = false
                });
            }

            return Ok(new
            {
                message = "Đã đánh dấu đã đọc",
                result = true
            });
        }

        // PUT /api/Notification/read-all
        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            await _notificationService.MarkAllAsReadAsync();

            return Ok(new
            {
                message = "Đã đánh dấu tất cả đã đọc",
                result = true
            });
        }

        // DELETE /api/Notification/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _notificationService.DeleteAsync(id);

            if (!success)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy thông báo",
                    result = false
                });
            }

            return Ok(new
            {
                message = "Đã xoá thông báo",
                result = true
            });
        }
    }
}
