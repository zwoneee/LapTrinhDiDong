using DocumentFormat.OpenXml.Spreadsheet;
using ECommerceSystem.Api.Data;
using ECommerceSystem.Api.Hubs;
using ECommerceSystem.Shared.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ECommerceSystem.Api.Controllers
{
    [ApiController]
    [Route("api/chat")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ChatController : ControllerBase
    {
        private readonly WebDBContext _db;
        private readonly IHubContext<ChatHub> _hub;
        private readonly ChatConnectionManager _connManager;

        public ChatController(WebDBContext db, IHubContext<ChatHub> hub, ChatConnectionManager connManager)
        {
            _db = db;
            _hub = hub;
            _connManager = connManager;
        }

        /// <summary>
        /// Lấy UserId từ JWT
        /// </summary>
        private int GetMyId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("nameid")?.Value;

            return int.TryParse(userIdClaim, out var id) ? id : 0;
        }


        // ====================== Upload file ======================
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null) return BadRequest("No file uploaded");

            var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);

            var fileName = Path.GetRandomFileName() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploads, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var fileUrl = $"{Request.Scheme}://{Request.Host}/uploads/{fileName}";
            var fileType = file.ContentType.StartsWith("image/") ? "image" :
                           file.ContentType.StartsWith("video/") ? "video" : "file";

            return Ok(new { url = fileUrl, fileName = file.FileName, fileType });
        }

        // ====================== Customer gửi message ======================
        [HttpPost("customer/send")]
        public async Task<IActionResult> CustomerSend([FromBody] ChatMessage message)
        {
            var me = GetMyId();
            if (me == 0) return Unauthorized();

            message.FromUserId = me;
            message.ToUserId = 1    ; // broadcast tới admin
            message.SentAt = DateTime.UtcNow;

            _db.ChatMessages.Add(message);
            await _db.SaveChangesAsync();

            // SignalR gửi tới Admins
            await _hub.Clients.Group("Admins").SendAsync(
                "ReceiveMessage",
                message.FromUserId,
                message.Content,
                message.SentAt,
                message.FileUrl,
                message.FileType,
                message.FileName
            );

            return Ok(message);
        }

        // ====================== Admin gửi message ======================
        [HttpPost("admin/send")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminSend([FromBody] ChatMessage message)
        {
            var adminId = GetMyId();
            if (adminId == 0) return Unauthorized();

            message.FromUserId = adminId;
            message.SentAt = DateTime.UtcNow;

            _db.ChatMessages.Add(message);
            await _db.SaveChangesAsync();

            // Gửi tới Customer
            foreach (var connId in _connManager.GetConnections(message.ToUserId))
            {
                await _hub.Clients.Client(connId).SendAsync(
                    "ReceiveMessage",
                    message.FromUserId,
                    message.Content,
                    message.SentAt,
                    message.FileUrl,
                    message.FileType,
                    message.FileName
                );
            }

            // Echo lại cho Admin
            foreach (var connId in _connManager.GetConnections(adminId))
            {
                await _hub.Clients.Client(connId).SendAsync(
                    "ReceiveMessage",
                    message.FromUserId,
                    message.Content,
                    message.SentAt,
                    message.FileUrl,
                    message.FileType,
                    message.FileName
                );
            }

            return Ok(message);
        }

        // ✅ API lấy danh sách tất cả người dùng (trừ admin)
        [HttpGet("users")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _db.Users
                .Where(u => !_db.UserRoles
                    .Any(ur => ur.UserId == u.Id && _db.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Admin")))
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.UserName
                })
                .ToListAsync();

            return Ok(users);
        }
        [HttpGet("history")]
        public async Task<IActionResult> GetChatHistory([FromQuery] int withUserId = 0)
        {
            var userId = GetMyId();
            if (userId == 0)
                return Unauthorized("Không xác định được người dùng.");

            var isAdmin = User.IsInRole("Admin");

            IQueryable<ChatMessage> query;

            if (isAdmin)
            {
                // 👑 Admin xem lịch sử với user cụ thể
                if (withUserId == 0)
                    return BadRequest("Thiếu userId cần xem lịch sử.");

                query = _db.ChatMessages.Where(m =>
                    (m.FromUserId == withUserId && m.ToUserId == 0) ||
                    (m.FromUserId == 0 && m.ToUserId == withUserId)
                );
            }
            else
            {
                // 👤 User xem lịch sử với admin (admin thật sự có ID=1)
                query = _db.ChatMessages.Where(m =>
                    (m.FromUserId == userId && m.ToUserId == 1) ||
                    (m.FromUserId == 1 && m.ToUserId == userId)
                );
            }
            var messages = await query.OrderBy(m => m.SentAt).ToListAsync();
            return Ok(messages);
        }
    }
}
