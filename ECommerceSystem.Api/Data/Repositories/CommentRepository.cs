using ECommerceSystem.Shared.Entities;
using Microsoft.EntityFrameworkCore;
using ECommerceSystem.Api.Data;
    

namespace EcommerceSystem.API.Data.Repositories
{
    public class CommentRepository
    {
        private readonly WebDBContext _context;

        public CommentRepository(WebDBContext context)
        {
            _context = context;
        }

        public async Task<List<Comment>> GetCommentsByProductIdAsync(int productId)
        {
            return await _context.Comments
                .Where(c => c.ProductId == productId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task AddCommentAsync(Comment comment)
        {
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();
        }
    }
}
