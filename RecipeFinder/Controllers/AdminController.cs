using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecipeFinder.Data;
using RecipeFinder.Models;

namespace RecipeFinder.Controllers;

public class AdminController : Controller
{
    private readonly AppDbContext          _db;
    private readonly UserManager<Customer> _userManager;

    public AdminController(AppDbContext db, UserManager<Customer> userManager)
    {
        _db          = db;
        _userManager = userManager;
    }

    // ── Only admins can access this controller ─────────────────────
    private async Task<bool> IsAdminAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        Console.WriteLine(
            JsonSerializer.Serialize(user, new JsonSerializerOptions
            {
                WriteIndented = true
            }));
        
        Console.WriteLine(user?.IsAdmin == true);
        return user?.IsAdmin == true;
    }

    // ── GET /Admin/Posts ───────────────────────────────────────────
    public async Task<IActionResult> Posts()
    {
        if (!await IsAdminAsync()) return NotFound();

        var pending = await _db.ForumPosts
            .Where(p => !p.IsApproved && !p.IsDeleted)
            .Include(p => p.Customer)
            .Include(p => p.Flair)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new AdminPostViewModel
            {
                Id         = p.Id,
                Title      = p.Title,
                Body       = p.Body,
                AuthorName = p.Customer.UserName ?? "",
                FlairName  = p.Flair != null ? p.Flair.Name : null,
                CreatedAt  = p.CreatedAt
            })
            .ToListAsync();

        var approved = await _db.ForumPosts
            .Where(p => p.IsApproved && !p.IsDeleted)
            .Include(p => p.Customer)
            .Include(p => p.Flair)
            .OrderByDescending(p => p.CreatedAt)
            .Take(20)
            .Select(p => new AdminPostViewModel
            {
                Id         = p.Id,
                Title      = p.Title,
                Body       = p.Body,
                AuthorName = p.Customer.UserName ?? "",
                FlairName  = p.Flair != null ? p.Flair.Name : null,
                CreatedAt  = p.CreatedAt
            })
            .ToListAsync();

        var vm = new AdminPostsViewModel
        {
            PendingPosts  = pending,
            ApprovedPosts = approved
        };

        return View(vm);
    }

    // ── POST /Admin/Approve ────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int postId)
    {
        if (!await IsAdminAsync()) return NotFound();

        var post = await _db.ForumPosts.FindAsync(postId);
        if (post is null) return NotFound();

        post.IsApproved = true;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Post approved and is now live.";
        return RedirectToAction(nameof(Posts));
    }

    // ── POST /Admin/Reject ─────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int postId)
    {
        if (!await IsAdminAsync()) return NotFound();

        var post = await _db.ForumPosts.FindAsync(postId);
        if (post is null) return NotFound();

        post.IsDeleted = true;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Post has been rejected and removed.";
        return RedirectToAction(nameof(Posts));
    }

    // ── POST /Admin/Revoke ─────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Revoke(int postId)
    {
        if (!await IsAdminAsync()) return NotFound();

        var post = await _db.ForumPosts.FindAsync(postId);
        if (post is null) return NotFound();

        post.IsApproved = false;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Post approval has been revoked.";
        return RedirectToAction(nameof(Posts));
    }
}

// ── ViewModels ────────────────────────────────────────────────────

public class AdminPostViewModel
{
    public int      Id         { get; set; }
    public string   Title      { get; set; } = "";
    public string   Body       { get; set; } = "";
    public string   AuthorName { get; set; } = "";
    public string?  FlairName  { get; set; }
    public DateTime CreatedAt  { get; set; }
}

public class AdminPostsViewModel
{
    public List<AdminPostViewModel> PendingPosts  { get; set; } = new();
    public List<AdminPostViewModel> ApprovedPosts { get; set; } = new();
}