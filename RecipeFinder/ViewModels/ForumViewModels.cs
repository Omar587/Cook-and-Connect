using System.ComponentModel.DataAnnotations;
using RecipeFinder.Models;
using RecipeFinder.Models.FourmModel;

namespace RecipeFinder.ViewModels.Forum;

// ─── LIST PAGE ───────────────────────────────────────────────────────────────

public class ForumIndexViewModel
{
    public IEnumerable<ForumPostListItemViewModel> Posts { get; set; } = [];
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public string Sort { get; set; } = "hot";
    public IEnumerable<ForumFlair> Flairs { get; set; } = [];
    public int? FilterFlairId { get; set; }
}

public class ForumPostListItemViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string BodyPreview { get; set; } = "";
    public string AuthorName { get; set; } = "";
    public int AuthorId { get; set; }
    public DateTime CreatedAt { get; set; }
    public int VoteScore { get; set; }
    public int CommentCount { get; set; }
    public string? FlairName { get; set; }
    public string? FlairColorHex { get; set; }
    public string? RecipeName { get; set; }
    public int? RecipeId { get; set; }
    public int? UserVote { get; set; }  // +1, -1, or null if not voted / not logged in
}

// ─── DETAIL PAGE ─────────────────────────────────────────────────────────────

public class ForumPostDetailViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public string AuthorName { get; set; } = "";
    public int AuthorId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int VoteScore { get; set; }
    public int? UserVote { get; set; }
    public string? FlairName { get; set; }
    public string? FlairColorHex { get; set; }
    public string? RecipeName { get; set; }
    public int? RecipeId { get; set; }
    public bool CanEdit { get; set; }
    public List<ForumCommentViewModel> Comments { get; set; } = [];
    public AddCommentViewModel NewComment { get; set; } = new();
}

// ─── COMMENT ─────────────────────────────────────────────────────────────────

public class ForumCommentViewModel
{
    public int Id { get; set; }
    public string Body { get; set; } = "";
    public string AuthorName { get; set; } = "";
    public int AuthorId { get; set; }
    public DateTime CreatedAt { get; set; }
    public int VoteScore { get; set; }
    public int? UserVote { get; set; }
    public int? ParentCommentId { get; set; }
    public bool IsDeleted { get; set; }
    public bool CanEdit { get; set; }
    public List<ForumCommentViewModel> Replies { get; set; } = [];
}

public class AddCommentViewModel
{
    public int ForumPostId { get; set; }
    public int? ParentCommentId { get; set; }

    [Required(ErrorMessage = "Comment cannot be empty.")]
    [MaxLength(5000)]
    public string Body { get; set; } = "";
}

// ─── CREATE / EDIT POST ───────────────────────────────────────────────────────

public class CreatePostViewModel
{
    [Required(ErrorMessage = "Please add a title.")]
    [MaxLength(300, ErrorMessage = "Title must be 300 characters or fewer.")]
    public string Title { get; set; } = "";

    [Required(ErrorMessage = "Post body cannot be empty.")]
    [MaxLength(40000)]
    public string Body { get; set; } = "";

    public int? FlairId { get; set; }
    public int? RecipeId { get; set; }

    // Populated by the service for the form dropdowns
    public List<ForumFlair> AvailableFlairs { get; set; } = [];
    public List<Recipe> AvailableRecipes { get; set; } = [];
}

public class EditPostViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Please add a title.")]
    [MaxLength(300)]
    public string Title { get; set; } = "";

    [Required(ErrorMessage = "Post body cannot be empty.")]
    [MaxLength(40000)]
    public string Body { get; set; } = "";
}

// ─── VOTE RESPONSE (JSON) ─────────────────────────────────────────────────────

public class VoteResultViewModel
{
    public bool Success { get; set; }
    public int NewScore { get; set; }
    public int UserVote { get; set; }   // what value is now stored for this user
}
