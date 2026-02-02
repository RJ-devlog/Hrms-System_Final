using HRMS_System.Data;
using HRMS_System.Models;
using HRMS_System.Services;
using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;


namespace HRMS_System.Pages.Hrms.EmployeeManagement
{

    public class EditEmployeeInfoModel : PageModel
    {

        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        public List<SelectListItem> JobRoleOptions { get; set; } = new();
        public List<SelectListItem> DepartmentOptions { get; set; } = new();
        private readonly DepartmentSelectService _deptService;
        public EditEmployeeInfoModel(ApplicationDbContext context,
                                     IWebHostEnvironment environment,
                                     DepartmentSelectService deptService)
        {
            _context = context;
            _environment = environment;
            _deptService = deptService;
        }

        [BindProperty]
        public IFormFile? ProfileImage { get; set; }

        [BindProperty]
        public UserInformationModel Employee { get; set; } = new();

        [BindProperty]
        public string? NewDepartmentName { get; set; }
        public async Task<IActionResult> OnGetAsync(int id)
        {
            //Load dropdowns
            JobRoleOptions = EmployeeCatalog.JobRoles;
            await _deptService.EnsureCatalogSeededAsync();
            DepartmentOptions = await _deptService.GetDepartmentOptionsAsync();
            // If user selected "+ Add new..."
            if (Employee.DepartmentId == DepartmentSelectService.AddNewValue)
            {
                if (string.IsNullOrWhiteSpace(NewDepartmentName))
                {
                    ModelState.AddModelError(nameof(NewDepartmentName), "Please enter a department name.");
                    return Page();
                }

                Employee.DepartmentId = await _deptService.GetOrCreateDepartmentIdAsync(NewDepartmentName);
                Employee.Department = NewDepartmentName.Trim();
            }
            else
            {
                if (Employee.DepartmentId.HasValue)
                {
                    Employee.Department = await _context.Departments
                        .Where(d => d.Id == Employee.DepartmentId.Value)
                        .Select(d => d.Name)
                        .FirstOrDefaultAsync();
                }
            }
            Employee = await _context.UserInformation.FirstOrDefaultAsync(e => e.id == id);

            if (Employee == null)
                return NotFound();
            return Page();
        }
        /* ========================= POST ========================= */
        public async Task<IActionResult> OnPostAsync()
        {
            //Load dropdowns
            JobRoleOptions = EmployeeCatalog.JobRoles;
            await _deptService.EnsureCatalogSeededAsync();
            DepartmentOptions = await _deptService.GetDepartmentOptionsAsync();
            // Handle "+ Add new department..."
            if (Employee.DepartmentId == DepartmentSelectService.AddNewValue)
            {
                if (string.IsNullOrWhiteSpace(NewDepartmentName))
                {
                    ModelState.AddModelError(nameof(NewDepartmentName), "Please enter a department name.");
                    return Page();
                }

                Employee.DepartmentId = await _deptService.GetOrCreateDepartmentIdAsync(NewDepartmentName);
                Employee.Department = NewDepartmentName.Trim();
            }
            else
            {
                // If existing department selected, set the name from DB
                if (Employee.DepartmentId.HasValue)
                {
                    Employee.Department = await _context.Departments
                        .Where(d => d.Id == Employee.DepartmentId.Value)
                        .Select(d => d.Name)
                        .FirstOrDefaultAsync();
                }
            }
            if (!ModelState.IsValid)
                return Page();

            var employeeInDb = await _context.UserInformation
                .FirstOrDefaultAsync(e => e.id == Employee.id);

            if (employeeInDb == null)
                return NotFound();

            /* ========= PROFILE IMAGE UPLOAD ========= */

            if (ProfileImage != null && ProfileImage.Length > 0)
            {
                var extension = Path.GetExtension(ProfileImage.FileName).ToLower();

                if (extension != ".jpg" && extension != ".jpeg" && extension != ".png")
                {
                    ModelState.AddModelError("", "Only JPG and PNG files are allowed.");
                    return Page();
                }

                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");

                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileName = Guid.NewGuid() + extension;
                var filePath = Path.Combine(uploadsFolder, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await ProfileImage.CopyToAsync(stream);

                employeeInDb.ProfileImagePath = "/uploads/" + fileName;
            }

            /* ========= UPDATE FIELDS ========= */
            employeeInDb.FirstName = Employee.FirstName;
            employeeInDb.MiddleName = Employee.MiddleName;
            employeeInDb.LastName = Employee.LastName;
            employeeInDb.Email = Employee.Email;
            employeeInDb.PhoneNumber = Employee.PhoneNumber;
            employeeInDb.BirthDate = Employee.BirthDate;
            employeeInDb.Gender = Employee.Gender;
            employeeInDb.CivilStatus = Employee.CivilStatus;
            employeeInDb.Address = Employee.Address;
            employeeInDb.JobRole = Employee.JobRole;
            employeeInDb.Department = Employee.Department;
            employeeInDb.DepartmentId = Employee.DepartmentId;
            employeeInDb.EmploymentStatus = Employee.EmploymentStatus;
            employeeInDb.Status = Employee.Status;
            employeeInDb.StartDate = Employee.StartDate;
            employeeInDb.TenureMonths = CalculateTenureMonths(employeeInDb.StartDate);
            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState)
                {
                    foreach (var err in error.Value.Errors)
                    {
                        Console.WriteLine($"❌ {error.Key}: {err.ErrorMessage}");
                    }
                }
                return Page();
            }
            await _context.SaveChangesAsync();
            return RedirectToPage("./Index");
        }
        private int CalculateTenureMonths(DateTime startDate)
        {
            int tenureMonth = 0;
            var today = DateTime.Today;

            tenureMonth = Math.Max(0,(today.Year - startDate.Year) * 12 + (today.Month - startDate.Month));
            return tenureMonth;
        }

    }

}
