using HRMS_System.Data;
using HRMS_System.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HRMS_System.Pages.Dashboard.EmployeeManagement
{
    public class AddEmployeeModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public AddEmployeeModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public UserInformationModel Employee { get; set; } = new();

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.UserInformation.Add(Employee);
            _context.SaveChanges();
            return RedirectToPage("/Dashboard/EmployeeManagement/EmployeeManagementLayout");
        }
    }
}
