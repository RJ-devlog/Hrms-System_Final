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

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Users.Username) || string.IsNullOrWhiteSpace(Users.Password))
            {
                ModelState.AddModelError("", "Username and Password are required");
                return Page();
            }

            // DB check
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == Users.Username && u.Password == Users.Password);

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid username or password");
                return Page();
            }

            // Build claims (Login + Role)
            // If your Role is enum: user.Role.ToString()
            // If your Role is string already: user.Role
            var roleValue = user.Role.ToString();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, roleValue)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal
            );

            //Redirect based on role
            if (roleValue == "HR") 
                return Redirect("/Hrms/EmployeeManagement");

            if (roleValue == "Supervisor") 
                return Redirect("/Hrms/DailyLogs/DailyLogsUpload"); 

            //fallback
            return Redirect("/Dashboard");
        }
    }
}
