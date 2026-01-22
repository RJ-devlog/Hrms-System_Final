using HRMS_System.Data;
using HRMS_System.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HRMS_System.Pages.Hrms.EmployeeManagement
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
        public List<SelectListItem> JobRoleOptions { get; set; } = new();
        public List<SelectListItem> DepartmentOptions { get; set; } = new();
        public void OnGet()
        {
            Employee.EmployeeNumber = GenerateNextEmployeeNumber();
            Employee.StartDate = DateTime.Today;
            Employee.BirthDate = new DateTime(2000,01,01);
            Employee.EmploymentStatus = "Probationary";
            Employee.Status = "Active";
            DepartmentOptions = EmployeeCatalog.Departments;
            JobRoleOptions = EmployeeCatalog.JobRoles;
        }

        public IActionResult OnPost()
        {
            JobRoleOptions = EmployeeCatalog.JobRoles;
            DepartmentOptions = EmployeeCatalog.Departments;
            if (!ModelState.IsValid)
                return Page();
            try
            {
                Employee.EmployeeNumber = GenerateNextEmployeeNumber();
                _context.UserInformation.Add(Employee);
                _context.SaveChanges();
                return RedirectToPage("./Index");
            }
            catch
            {
                // duplicate key / constraint error
                ModelState.AddModelError(string.Empty, "Unable to save employee. Possible duplicate or invalid data.");
                return Page();
            }
        }
        private string GenerateNextEmployeeNumber()
        {
            var lastEmp = _context.UserInformation
                .OrderByDescending(e => e.id)
                .Select(e => e.EmployeeNumber)
                .FirstOrDefault();

            int nextNumber = 1;

            if (!string.IsNullOrEmpty(lastEmp))
            {
                var digits = int.Parse(
                    System.Text.RegularExpressions.Regex.Replace(lastEmp, @"\D", "")
                );
                nextNumber = digits + 1;
            }

            return $"EMP-{nextNumber:D6}";
        }

    }
}
