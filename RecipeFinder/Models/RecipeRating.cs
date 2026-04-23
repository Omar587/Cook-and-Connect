namespace RecipeFinder.Models;

public class RecipeRating
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int RecipeId { get; set; }
    public int Rating { get; set; } // 1-5 stars
    public DateTimeOffset RatedAt { get; set; }
    
    // Navigation properties
    public Customer Customer { get; set; }
    public Recipe Recipe { get; set; }
}