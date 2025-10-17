﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Insightly.Models
{
    public class Article
    {
        public int ArticleId { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        public string Content { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        
        public string? ImagePath { get; set; }
        
        [Required]
        public string AuthorId { get; set; } = string.Empty;
        
        // Navigation properties
        [ForeignKey("AuthorId")]
        public virtual ApplicationUser Author { get; set; } = null!;
        public virtual List<Comment> Comments { get; set; } = new List<Comment>();
        public virtual List<Vote> Votes { get; set; } = new List<Vote>();
        
        public Article() { }
        /*public List<string> Tags { get; set; }
        public bool IsPublished { get; set; }
        public int ViewCount { get; set; }
        public int Likes { get; set; }
        */
    }
}
