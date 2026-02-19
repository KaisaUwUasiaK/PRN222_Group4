using Group4_ReadingComicWeb.Models;
using System.Collections.Generic;

namespace Group4_ReadingComicWeb.ViewModels;

public class PublicProfileViewModel
{
    public User User { get; set; } = null!;
    public IEnumerable<Comic> AuthoredComics { get; set; } = new List<Comic>();
    public int TotalComicsCount { get; set; }
    public int TotalViewsCount { get; set; }
    public string JoinedDate => User != null ? "Joined 2024" : ""; // Placeholder for now

    // Pagination
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
}
