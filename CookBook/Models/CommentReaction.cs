namespace CookBook.Models;

public class CommentReaction
{
    public int CommentId { get; set; }
    public Comment Comment { get; set; } = null!;

    public int UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public int ReactionId { get; set; }
    public Reaction Reaction { get; set; } = null!;
}
