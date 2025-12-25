using HRMS_System.Data;
using HRMS_System.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

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

        public IActionResult OnPost()
        {
            if (string.IsNullOrEmpty(Users.Username) || string.IsNullOrEmpty(Users.Password))
            {
                ModelState.AddModelError("", "Username and Password are required");
                return Page();
            }
            
            var user = _context.Users
              .FirstOrDefault(u => u.Username == Users.Username
                    && u.Password == Users.Password);

            if (user != null)
            {
                // SUCCESS LOGIN
                return Redirect("/Dashboard/EmployeeManagement");
            }
            ModelState.AddModelError("", "Invalid username or password");
       
            return Page();
        }
    }
}
