using Insightly.Models;
using Insightly.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Insightly.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;

        public ProfileController(UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return NotFound();
            }
            
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            // Get user's articles
            var articles = await _unitOfWork.Articles.GetByAuthorIdAsync(userId);
            var followersCount = await _unitOfWork.Follows.GetFollowersCountAsync(user.Id);
            var followingCount = await _unitOfWork.Follows.GetFollowingCountAsync(user.Id);

            ViewBag.Articles = articles;
            ViewBag.FollowersCount = followersCount;
            ViewBag.FollowingCount = followingCount;
            return View(user);
        }

        public async Task<IActionResult> Edit()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return NotFound();
            }
            
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var model = new EditProfileViewModel
            {
                Name = user.Name,
                ProfilePicture = user.ProfilePicture,
                Bio = user.Bio,
                Email = user.Email ?? string.Empty,
                Gender = user.Gender
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return NotFound();
            }
            
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            try
            {
                // Update only the profile fields
                user.Name = model.Name;
                user.Bio = model.Bio;

                // Handle profile picture upload
                if (model.ProfilePictureFile != null && model.ProfilePictureFile.Length > 0)
                {
                    // Validate file type
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var fileExtension = Path.GetExtension(model.ProfilePictureFile.FileName).ToLowerInvariant();
                    
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        ModelState.AddModelError(nameof(model.ProfilePictureFile), "Only JPG, JPEG, PNG, and GIF files are allowed.");
                        return View(model);
                    }

                    // Validate file size (5MB max)
                    if (model.ProfilePictureFile.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError(nameof(model.ProfilePictureFile), "File size must be less than 5MB.");
                        return View(model);
                    }

                    // Generate unique filename
                    var fileName = $"{userId}_{DateTime.Now:yyyyMMddHHmmss}{fileExtension}";
                    var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
                    
                    // Ensure directory exists
                    Directory.CreateDirectory(uploadsPath);
                    
                    var filePath = Path.Combine(uploadsPath, fileName);
                    
                    // Save file
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ProfilePictureFile.CopyToAsync(stream);
                    }
                    
                    // Update profile picture path
                    user.ProfilePicture = $"/uploads/profiles/{fileName}";
                }

                var result = await _userManager.UpdateAsync(user);
                
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "Profile updated successfully!";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"An error occurred while updating your profile: {ex.Message}");
            }

            return View(model);
        }
        public async Task<IActionResult> ViewProfile(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            var articles = await _unitOfWork.Articles.GetByAuthorIdAsync(id);
            var followersCount = await _unitOfWork.Follows.GetFollowersCountAsync(id);
            var followingCount = await _unitOfWork.Follows.GetFollowingCountAsync(id);

            var currentUser = await _userManager.GetUserAsync(User);
            var isFollowing = false;

            if (currentUser != null)
            {
                isFollowing = await _unitOfWork.Follows.ExistsAsync(currentUser.Id, id);
            }

            ViewBag.Articles = articles;
            ViewBag.FollowersCount = followersCount;
            ViewBag.FollowingCount = followingCount;
            ViewBag.IsFollowing = isFollowing;

            return View(user);
        }


    }
}
