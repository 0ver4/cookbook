using System.ComponentModel.DataAnnotations;

namespace CookBook.Models;

public class Comment
{
    public int Id { get; set; }

    public int RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!;

    public int UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    [Required, MaxLength(2000)]
    public string Content { get; set; } = null!;

    public int? ImageId { get; set; }
    public Image? Image { get; set; }

    public int? ReplyToId { get; set; }
    public Comment? ReplyTo { get; set; }
    public ICollection<Comment> Replies { get; set; } = new List<Comment>();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<CommentReaction> Reactions { get; set; } = new List<CommentReaction>();
}
