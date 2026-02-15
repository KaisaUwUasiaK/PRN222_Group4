using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Group4_ReadingComicWeb.Models
{

    public class Comic
    {
        [Key]
        public int ComicId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = null!;

        [Column(TypeName = "ntext")]
        public string? Description { get; set; }

        public int AuthorId { get; set; }

        [ForeignKey("AuthorId")]
        public User Author { get; set; } = null!;

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Viewcount
        public int ViewCount { get; set; } = 0;

        // Navigation
        public ICollection<Chapter> Chapters { get; set; } = new List<Chapter>();
    }
}