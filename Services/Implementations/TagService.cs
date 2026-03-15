using Group4_ReadingComicWeb.Models;
using Group4_ReadingComicWeb.Services.Contracts;
using Group4_ReadingComicWeb.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Group4_ReadingComicWeb.Services.Implementations
{
    public class TagService : ITagService
    {
        private readonly AppDbContext _context;

        public TagService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy tất cả tag cùng với số lượng truyện sử dụng tag đó, sắp xếp theo tên tag
        /// </summary>
        /// <returns></returns>
        public async Task<List<TagListItemViewModel>> GetAllWithUsageAsync()
        {
            return await _context.Tags
                .Select(t => new TagListItemViewModel
                {
                    TagId = t.TagId,
                    TagName = t.TagName,
                    ComicCount = t.ComicTags.Count
                })
                .OrderBy(t => t.TagName)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy tag theo ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<Tag?> GetByIdAsync(int id)
        {
            return await _context.Tags.FirstOrDefaultAsync(t => t.TagId == id);
        }

        /// <summary>
        /// Kiểm tra xem tên tag đã tồn tại hay chưa
        /// </summary>
        /// <param name="tagName"></param>
        /// <param name="excludeTagId"></param>
        /// <returns></returns>
        public async Task<bool> IsTagNameExistsAsync(string tagName, int? excludeTagId = null)
        {
            var normalized = tagName.Trim().ToLower();

            return await _context.Tags.AnyAsync(t =>
                t.TagName.ToLower() == normalized &&
                (!excludeTagId.HasValue || t.TagId != excludeTagId.Value));
        }

        /// <summary>
        /// Tạo mới một tag
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public async Task CreateAsync(Tag tag)
        {
            _context.Tags.Add(tag);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Cập nhật một tag
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public async Task<bool> UpdateAsync(Tag tag)
        {
            var existing = await _context.Tags.FirstOrDefaultAsync(t => t.TagId == tag.TagId);
            if (existing == null)
            {
                return false;
            }

            existing.TagName = tag.TagName;
            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Kiểm tra xem tag có đang được sử dụng hay không
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<bool> IsInUseAsync(int id)
        {
            return await _context.ComicTags.AnyAsync(ct => ct.TagId == id);
        }

        /// <summary>
        /// Lấy số lượng truyện sử dụng tag theo ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<int> GetComicUsageCountAsync(int id)
        {
            return await _context.ComicTags.CountAsync(ct => ct.TagId == id);
        }

        /// <summary>
        /// Lấy tổng số lượng tag
        /// </summary>
        /// <returns></returns>
        public async Task<int> GetTotalTagsCountAsync()
        {
            return await _context.Tags.CountAsync();
        }

        /// <summary>
        /// Xóa một tag theo ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<bool> DeleteAsync(int id)
        {
            var tag = await _context.Tags.FirstOrDefaultAsync(t => t.TagId == id);
            if (tag == null)
            {
                return false;
            }

            _context.Tags.Remove(tag);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}