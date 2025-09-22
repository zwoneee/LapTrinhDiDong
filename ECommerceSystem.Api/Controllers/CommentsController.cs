using EcommerceSystem.API.Data.Repositories;
using ECommerceSystem.Shared.Entities;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommentsController : ControllerBase
    {
        private readonly CommentRepository _commentRepo;

        public CommentsController(CommentRepository commentRepo)
        {
            _commentRepo = commentRepo;
        }

        [HttpGet("product/{productId}")]
        public async Task<IActionResult> GetComments(int productId)
        {
            var comments = await _commentRepo.GetCommentsByProductIdAsync(productId);
            return Ok(comments);
        }

        [HttpPost]
        public async Task<IActionResult> AddComment([FromBody] Comment comment)
        {
            comment.CreatedAt = DateTime.UtcNow;
            await _commentRepo.AddCommentAsync(comment);
            return Ok(comment);
        }
    }
}
