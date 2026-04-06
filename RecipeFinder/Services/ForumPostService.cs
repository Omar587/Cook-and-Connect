using Microsoft.EntityFrameworkCore;
using RecipeFinder.Data;                    // ← adjust to your DbContext namespace
using RecipeFinder.Models;
using RecipeFinder.Models.FourmModel;
using RecipeFinder.ViewModels.Forum;

namespace RecipeFinder.Services.Forum;

public class ForumPostService : IForumPostService
{
    private readonly AppDbContext _db;   // ← replace with your actual DbContext type

    public ForumPostService(AppDbContext db) => _db = db;

    // ─────────────────────────────────────────────────────────────────────────
    // INDEX
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<ForumIndexViewModel> GetIndexViewModelAsync(
        string sort, int page, int pageSize, int? flairId, int? currentCustomerId)
    {
        var query = _db.ForumPosts
            .Where(p => !p.IsDeleted)
            .Include(p => p.Customer)
            .Include(p => p.Flair)
            .Include(p => p.Recipe)
            .Include(p => p.Votes)
            .Include(p => p.Comments.Where(c => !c.IsDeleted))
            .AsQueryable();

        if (flairId.HasValue)
            query = query.Where(p => p.ForumFlairId == flairId);

        // Apply DB-translatable sorts first
        query = sort switch
        {
            "new" => query.OrderByDescending(p => p.CreatedAt),
            "top" => query.OrderByDescending(p => p.Votes.Sum(v => v.Value)),
            _     => query.OrderByDescending(p => p.CreatedAt) // hot: pre-sort by new, refine client-side
        };

        int total = await query.CountAsync();

        var posts = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Apply hot score client-side (after fetch, so no EF translation issue)
        if (sort == "hot")
        {
            posts = posts
                .OrderByDescending(p => p.VoteScore - (DateTime.UtcNow - p.CreatedAt).TotalHours * 0.5)
                .ToList();
        }

        var postVms = posts.Select(p => MapToListItem(p, currentCustomerId));
        var flairs  = await _db.ForumFlairs.OrderBy(f => f.Name).ToListAsync();

        return new ForumIndexViewModel
        {
            Posts         = postVms,
            TotalCount    = total,
            CurrentPage   = page,
            TotalPages    = (int)Math.Ceiling(total / (double)pageSize),
            Sort          = sort,
            Flairs        = flairs,
            FilterFlairId = flairId
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DETAIL
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<ForumPostDetailViewModel?> GetDetailViewModelAsync(int postId, int? currentCustomerId)
    {
        var post = await _db.ForumPosts
            .Where(p => p.Id == postId && !p.IsDeleted)
            .Include(p => p.Customer)
            .Include(p => p.Flair)
            .Include(p => p.Recipe)
            .Include(p => p.Votes)
            .Include(p => p.Comments)
            .ThenInclude(c => c.Customer)
            .Include(p => p.Comments)
            .ThenInclude(c => c.Votes)
            .Include(p => p.Comments)
            .ThenInclude(c => c.Replies)
            .ThenInclude(r => r.Customer)
            .Include(p => p.Comments)
            .ThenInclude(c => c.Replies)
            .ThenInclude(r => r.Votes)
            .Include(p => p.Comments)
            .ThenInclude(c => c.Replies)
            .ThenInclude(r => r.Replies)
            .ThenInclude(r => r.Customer)
            .Include(p => p.Comments)
            .ThenInclude(c => c.Replies)
            .ThenInclude(r => r.Replies)
            .ThenInclude(r => r.Votes)
            .FirstOrDefaultAsync();

        return new ForumPostDetailViewModel
        {
            Id          = post.Id,
            Title       = post.Title,
            Body        = post.Body,
            AuthorName  = post.Customer.FirstName,   // ← adjust to your Customer property
            AuthorId    = post.CustomerId,
            CreatedAt   = post.CreatedAt,
            UpdatedAt   = post.UpdatedAt,
            VoteScore   = post.VoteScore,
            UserVote    = currentCustomerId.HasValue
                            ? post.Votes.FirstOrDefault(v => v.CustomerId == currentCustomerId)?.Value
                            : null,
            FlairName     = post.Flair?.Name,
            FlairColorHex = post.Flair?.ColorHex,
            RecipeName    = post.Recipe?.Name,
            RecipeId      = post.RecipeId,
            CanEdit       = currentCustomerId == post.CustomerId,
            Comments      = post.Comments
                              .Where(c => c.ParentCommentId == null)
                              .OrderByDescending(c => c.Votes.Sum(v => v.Value))
                              .Select(c => MapComment(c, currentCustomerId))
                              .ToList(),
            NewComment    = new AddCommentViewModel { ForumPostId = postId }
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CREATE
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<CreatePostViewModel> GetCreateViewModelAsync()
    {
        return new CreatePostViewModel
        {
            AvailableFlairs   = await _db.ForumFlairs.OrderBy(f => f.Name).ToListAsync(),
            AvailableRecipes  = await _db.Recipes.OrderBy(r => r.Name).ToListAsync()
        };
    }

    public async Task<int> CreatePostAsync(CreatePostViewModel vm, int customerId)
    {
        var post = new ForumPost
        {
            Title       = vm.Title.Trim(),
            Body        = vm.Body.Trim(),
            CustomerId  = customerId,
            ForumFlairId = vm.FlairId,
            RecipeId    = vm.RecipeId,
            CreatedAt   = DateTime.UtcNow
        };

        _db.ForumPosts.Add(post);
        await _db.SaveChangesAsync();
        return post.Id;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // EDIT / DELETE
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<bool> EditPostAsync(EditPostViewModel vm, int customerId)
    {
        var post = await _db.ForumPosts
            .FirstOrDefaultAsync(p => p.Id == vm.Id && !p.IsDeleted);

        if (post is null || post.CustomerId != customerId) return false;

        post.Title     = vm.Title.Trim();
        post.Body      = vm.Body.Trim();
        post.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeletePostAsync(int postId, int customerId)
    {
        var post = await _db.ForumPosts
            .FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted);

        if (post is null || post.CustomerId != customerId) return false;

        post.IsDeleted = true;
        await _db.SaveChangesAsync();
        return true;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // VOTING
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<VoteResultViewModel> VoteAsync(int postId, int customerId, int value)
    {
        // value must be +1 or -1
        value = value >= 0 ? 1 : -1;

        var existing = await _db.ForumPostVotes
            .FirstOrDefaultAsync(v => v.ForumPostId == postId && v.CustomerId == customerId);

        if (existing is not null)
        {
            if (existing.Value == value)
            {
                // Same vote again → remove (toggle off)
                _db.ForumPostVotes.Remove(existing);
                value = 0;
            }
            else
            {
                // Flip vote
                existing.Value = value;
            }
        }
        else
        {
            _db.ForumPostVotes.Add(new ForumPostVote
            {
                ForumPostId = postId,
                CustomerId  = customerId,
                Value       = value
            });
        }

        await _db.SaveChangesAsync();

        int newScore = await _db.ForumPostVotes
            .Where(v => v.ForumPostId == postId)
            .SumAsync(v => v.Value);

        return new VoteResultViewModel { Success = true, NewScore = newScore, UserVote = value };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PRIVATE MAPPERS
    // ─────────────────────────────────────────────────────────────────────────

    private static ForumPostListItemViewModel MapToListItem(ForumPost p, int? currentCustomerId) =>
        new()
        {
            Id           = p.Id,
            Title        = p.Title,
            BodyPreview  = p.Body.Length > 220 ? string.Concat(p.Body.AsSpan(0, 220), "…") : p.Body,
            AuthorName   = p.Customer.FirstName,   // ← adjust
            AuthorId     = p.CustomerId,
            CreatedAt    = p.CreatedAt,
            VoteScore    = p.VoteScore,
            CommentCount = p.Comments.Count,
            FlairName     = p.Flair?.Name,
            FlairColorHex = p.Flair?.ColorHex,
            RecipeName    = p.Recipe?.Name,
            RecipeId      = p.RecipeId,
            UserVote      = currentCustomerId.HasValue
                              ? p.Votes.FirstOrDefault(v => v.CustomerId == currentCustomerId)?.Value
                              : null
        };

    private static ForumCommentViewModel MapComment(ForumComment c, int? currentCustomerId) =>
        new()
        {
            Id              = c.Id,
            Body            = c.IsDeleted ? "[deleted]" : c.Body,
            AuthorName      = c.IsDeleted ? "[deleted]" : c.Customer?.FirstName ?? "?",  // ← adjust
            AuthorId        = c.CustomerId,
            CreatedAt       = c.CreatedAt,
            VoteScore       = c.VoteScore,
            UserVote        = currentCustomerId.HasValue
                                ? c.Votes.FirstOrDefault(v => v.CustomerId == currentCustomerId)?.Value
                                : null,
            UpdatedAt = c.UpdatedAt,
            ParentCommentId = c.ParentCommentId,
            IsDeleted       = c.IsDeleted,
            CanEdit         = !c.IsDeleted && currentCustomerId == c.CustomerId,
            Replies         = c.Replies
                               .OrderBy(r => r.CreatedAt)
                               .Select(r => MapComment(r, currentCustomerId))
                               .ToList()
        };
}
