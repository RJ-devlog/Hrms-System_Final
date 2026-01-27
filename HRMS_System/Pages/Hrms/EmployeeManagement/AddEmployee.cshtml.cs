using HRMS_System.Data;
using HRMS_System.Models;
using HRMS_System.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HRMS_System.Pages.Hrms.EmployeeManagement
{
    public class AddEmployeeModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly DepartmentSelectService _deptService;

        public AddEmployeeModel(ApplicationDbContext context, DepartmentSelectService deptService)
        {
            _context = context;
            _deptService = deptService;
        }

        [BindProperty]
        public string? NewDepartmentName { get; set; }

        [BindProperty]
        public UserInformationModel Employee { get; set; } = new();

        public List<SelectListItem> JobRoleOptions { get; set; } = new();
        public List<SelectListItem> DepartmentOptions { get; set; } = new();


        public async Task OnGetAsync()
        {
            Employee.EmployeeNumber = GenerateNextEmployeeNumber();
            Employee.StartDate = DateTime.Today;
            Employee.BirthDate = new DateTime(2000, 01, 01);
            Employee.EmploymentStatus = "Probationary";
            Employee.Status = "Active";

            JobRoleOptions = EmployeeCatalog.JobRoles;

            // Load from DB (with Add new department...)
            DepartmentOptions = await _deptService.GetDepartmentOptionsAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            JobRoleOptions = EmployeeCatalog.JobRoles;
            DepartmentOptions = await _deptService.GetDepartmentOptionsAsync();

            if (!ModelState.IsValid)
                return Page();

            // handle Add new department
            if (Employee.DepartmentId == DepartmentSelectService.AddNewValue)
            {
                if (string.IsNullOrWhiteSpace(NewDepartmentName))
                {
                    ModelState.AddModelError(nameof(NewDepartmentName), "Please enter a department name.");
                    return Page();
                }

                Employee.DepartmentId = await _deptService.GetOrCreateDepartmentIdAsync(NewDepartmentName);
            }

            try
            {
                Employee.EmployeeNumber = GenerateNextEmployeeNumber();

                _context.UserInformation.Add(Employee);
                await _context.SaveChangesAsync();

                return RedirectToPage("./Index");
            }
            catch
            {
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
                var digits = int.Parse(System.Text.RegularExpressions.Regex.Replace(lastEmp, @"\D", ""));
                nextNumber = digits + 1;
            }

            return $"EMP-{nextNumber:D6}";
        }
    }
}
