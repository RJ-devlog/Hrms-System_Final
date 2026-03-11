using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using HRMS_System.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HRMS_System.Pages.Account
{
    public class ChangePasswordModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ChangePasswordModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public class ChangePasswordInput
        {
            [Required]
          
            public string CurrentPassword { get; set; } = "";

            [Required, MinLength(6, ErrorMessage = "New password must be at least 6 characters.")]
            public string NewPassword { get; set; } = "";

            [Required, Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
            public string ConfirmPassword { get; set; } = "";
        }

        [BindProperty]
        public ChangePasswordInput Input { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
                return RedirectToPage("/Account/LoginPage");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
                return RedirectToPage("/Account/LoginPage");

            if (!ModelState.IsValid)
                return Page();

            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idStr, out var userId))
                return RedirectToPage("/Account/LoginPage");

            var user = await _context.loginModels.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return RedirectToPage("/Account/LoginPage");

            if (user.Password != Input.CurrentPassword)
            {
                ModelState.AddModelError("", "Current password is incorrect.");
                return Page();
            }

            if (Input.CurrentPassword == Input.NewPassword)
            {
                ModelState.AddModelError("", "New password must be different from the current password.");
                return Page();
            }

            user.Password = Input.NewPassword;
            await _context.SaveChangesAsync();

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToPage("/Account/LoginPage");
        }
    }
}