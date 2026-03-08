using HRMS_System.Data;
using HRMS_System.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HRMS_System.Pages.Account
{
    public class LoginPageModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public LoginPageModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public User Users { get; set; } = new User();

        [BindProperty]
        public string? SelectedModule { get; set; }

        // STEP 1: Validate credentials only (AJAX call)
        public async Task<JsonResult> OnPostValidateCredentialsAsync([FromBody] LoginRequest request)
        {
            if (request == null ||
                string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "Username and Password are required."
                });
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username && u.Password == request.Password);

            if (user == null)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "Invalid username or password."
                });
            }

            var roleValue = user.Role.ToString();

            return new JsonResult(new
            {
                success = true,
                role = roleValue
            });
        }

        // STEP 2: Actual login after modal selection
        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Users.Username) || string.IsNullOrWhiteSpace(Users.Password))
            {
                ModelState.AddModelError("", "Username and Password are required.");
                return Page();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == Users.Username && u.Password == Users.Password);

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid username or password.");
                return Page();
            }

            var roleValue = user.Role.ToString();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, roleValue),
                new Claim("UserRole", roleValue)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            // HR chooses destination from modal
            if (roleValue == "HR")
            {
                if (SelectedModule == "AttendanceDailyLogs")
                    return RedirectToPage("/Hrms/DailyLogsAttendance/Index");

                if (SelectedModule == "EmployeeManagement")
                    return RedirectToPage("/Hrms/EmployeeManagement/Index");

                return RedirectToPage("/Hrms/EmployeeManagement/Index");
            }

            if (roleValue == "Supervisor")
                return RedirectToPage("/Hrms/Evaluation/Index");

            if (roleValue == "Manager")
                return RedirectToPage("/Hrms/AttendanceTracking/Index");

            if (roleValue == "CEO")
                return RedirectToPage("/Hrms/AttendanceTracking/Index");

            return RedirectToPage("/Account/LoginPage");
        }

        public class LoginRequest
        {
            public string? Username { get; set; }
            public string? Password { get; set; }
        }
    }
}