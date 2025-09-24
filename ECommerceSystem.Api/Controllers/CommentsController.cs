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

        // GET: api/comments/product/{productId} - Lấy tất cả comments của một sản phẩm
        [HttpGet("product/{productId}")]
        public async Task<IActionResult> GetCommentsByProductId(int productId)
        {
            try
            {
                var comments = await _commentRepo.GetCommentsByProductIdAsync(productId);

                if (comments == null || !comments.Any())
                {
                    return Ok(new List<Comment>()); // Trả về danh sách rỗng thay vì NotFound
                }

                return Ok(comments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET: api/comments/{id} - Lấy một comment theo ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCommentById(int id)
        {
            try
            {
                var comment = await _commentRepo.GetCommentByIdAsync(id);

                if (comment == null)
                {
                    return NotFound($"Comment with ID {id} not found.");
                }

                return Ok(comment);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // POST: api/comments - Thêm comment mới cho một sản phẩm
        [HttpPost]
        public async Task<IActionResult> AddComment([FromBody] Comment comment)
        {
            try
            {
                // Validation
                if (comment == null)
                {
                    return BadRequest("Comment data is required.");
                }

                if (comment.ProductId <= 0)
                {
                    return BadRequest("Valid ProductId is required.");
                }

                if (string.IsNullOrWhiteSpace(comment.Content))
                {
                    return BadRequest("Comment content is required.");
                }

                if (string.IsNullOrWhiteSpace(comment.UserId))
                {
                    return BadRequest("UserName is required.");
                }

                // Set timestamps
                comment.CreatedAt = DateTime.UtcNow;
                comment.UpdatedAt = DateTime.UtcNow;

                // Add comment to database
                var addedComment = await _commentRepo.AddCommentAsync(comment);

                return CreatedAtAction(
                    nameof(GetCommentById),
                    new { id = addedComment.Id },
                    addedComment
                );
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // PUT: api/comments/{id} - Cập nhật comment
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateComment(int id, [FromBody] Comment comment)
        {
            try
            {
                if (comment == null)
                {
                    return BadRequest("Comment data is required.");
                }

                if (id != comment.Id)
                {
                    return BadRequest("Comment ID mismatch.");
                }

                var existingComment = await _commentRepo.GetCommentByIdAsync(id);
                if (existingComment == null)
                {
                    return NotFound($"Comment with ID {id} not found.");
                }

                // Validation
                if (string.IsNullOrWhiteSpace(comment.Content))
                {
                    return BadRequest("Comment content is required.");
                }

                // Update timestamp
                comment.UpdatedAt = DateTime.UtcNow;
                // Preserve original creation time
                comment.CreatedAt = existingComment.CreatedAt;

                await _commentRepo.UpdateCommentAsync(comment);

                return Ok(comment);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // DELETE: api/comments/{id} - Xóa comment
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComment(int id)
        {
            try
            {
                var existingComment = await _commentRepo.GetCommentByIdAsync(id);
                if (existingComment == null)
                {
                    return NotFound($"Comment with ID {id} not found.");
                }

                await _commentRepo.DeleteCommentAsync(id);

                return Ok(new { message = $"Comment with ID {id} has been deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET: api/comments - Lấy tất cả comments (có thể dùng cho admin)
        [HttpGet]
        public async Task<IActionResult> GetAllComments()
        {
            try
            {
                var comments = await _commentRepo.GetAllCommentsAsync();
                return Ok(comments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}