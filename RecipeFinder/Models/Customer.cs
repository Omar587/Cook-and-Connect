using System.ComponentModel.DataAnnotations.Schema;

namespace RecipeFinder.Models;

using Microsoft.AspNetCore.Identity;

public class Customer : IdentityUser<int>
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public DateTime CreatedAt { get; set; }
    [Column("IsAdmin")]
    public bool IsAdmin { get; set; } = false;
    
    // Navigation properties
    public List<FavoriteRecipe> FavoriteRecipes { get; set; } = new();
    public List<RecipeRating> RecipeRatings { get; set; } = new();
    public List<RecipeNote> RecipeNotes { get; set; } = new();
}