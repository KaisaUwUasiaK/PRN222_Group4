namespace Group4_ReadingComicWeb.Services.Contracts
{
    public interface ICommentService
    {
        Task<bool> AddCommentAsync(int chapterId, int userId, string content);

        Task<bool> DeleteCommentAsync(int commentId, int userId);
    }
}
