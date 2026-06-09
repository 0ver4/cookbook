using CookBook.Models;

namespace CookBook.ViewModels;

public class PublicProfileViewModel
{
    public ApplicationUser User { get; }
    public IReadOnlyList<Recipe> Recipes { get; }
    public IReadOnlyList<Review> Reviews { get; }

    public string DisplayName => string.IsNullOrWhiteSpace(User.FirstName)
        ? User.Email!.Split('@')[0]
        : $"{User.FirstName} {User.LastName}";

    public PublicProfileViewModel(ApplicationUser user, IReadOnlyList<Recipe> recipes, IReadOnlyList<Review> reviews)
    {
        User = user;
        Recipes = recipes;
        Reviews = reviews;
    }
}
