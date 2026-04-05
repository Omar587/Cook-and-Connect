using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RecipeFinder.Models;
using RecipeFinder.Services.Forum;
using RecipeFinder.ViewModels.Forum;

namespace RecipeFinder.Controllers;

[Authorize]
public class ForumCommentController : Controller
{
    private readonly IForumCommentService _comments;
    private readonly UserManager<Customer> _userManager;

    public ForumCommentController(
        IForumCommentService comments,
        UserManager<Customer> userManager)
    {
        _comments = comments;
        _userManager = userManager;
    }

    private int? CurrentCustomerId
    {
        get
        {
            var userId = _userManager.GetUserId(User);
            return int.TryParse(userId, out var id) ? id : null;
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(AddCommentViewModel vm)
    {
        if (!ModelState.IsValid || CurrentCustomerId == null)
            return RedirectToAction("Details", "Forum", new { id = vm.ForumPostId });

        await _comments.AddCommentAsync(vm, CurrentCustomerId.Value);

        return RedirectToAction("Details", "Forum",
            new { id = vm.ForumPostId }, fragment: "comments");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int commentId, int postId, string body)
    {
        if (!string.IsNullOrWhiteSpace(body) && CurrentCustomerId != null)
            await _comments.EditCommentAsync(commentId, body, CurrentCustomerId.Value);

        return RedirectToAction("Details", "Forum",
            new { id = postId }, fragment: $"comment-{commentId}");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int commentId, int postId)
    {
        if (CurrentCustomerId != null)
            await _comments.DeleteCommentAsync(commentId, CurrentCustomerId.Value);

        return RedirectToAction("Details", "Forum", new { id = postId });
    }

    [HttpPost]
    public async Task<IActionResult> Vote(int commentId, int value)
    {
        if (CurrentCustomerId == null)
            return Unauthorized();

        var result = await _comments.VoteAsync(commentId, CurrentCustomerId.Value, value);
        return Json(result);
    }
}