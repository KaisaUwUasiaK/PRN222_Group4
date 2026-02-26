using Group4_ReadingComicWeb.Models;
using Group4_ReadingComicWeb.Services.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Group4_ReadingComicWeb.Services.Implementations
{
    public class CommentService : ICommentService
    {
        private AppDbContext _context;

        public CommentService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> AddCommentAsync(int chapterId, int userId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return false;
            }

            var comment = new Comment
            {
                ChapterId = chapterId,
                UserId = userId,
                Content = content,
                CreatedAt = DateTime.Now 
            };

            _context.Comments.Add(comment);
            var result = await _context.SaveChangesAsync();

            return result > 0;
        }

        public async Task<bool> DeleteCommentAsync(int commentId, int userId)
        {
            var comment = await _context.Comments.FindAsync(commentId);

            if (comment == null)
                return false;

         
            if (comment.UserId != userId)
                return false;

            _context.Comments.Remove(comment);
            var result = await _context.SaveChangesAsync();

            return result > 0;
        }
    }
}
