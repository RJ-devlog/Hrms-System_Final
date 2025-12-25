using HRMS_System.Data;
using HRMS_System.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HRMS_System.Pages.Dashboard.EmployeeManagement
{
    public class EditEmployeeInfoModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public EditEmployeeInfoModel(
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [BindProperty]
        public IFormFile? ProfileImage { get; set; }

        [BindProperty]
        public UserInformationModel Employee { get; set; } = new();

        /* ========================= GET ========================= */

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Employee = await _context.UserInformation.FirstOrDefaultAsync(e => e.id == id);

            if (Employee == null)
                return NotFound();

            return Page();
        }

        /* ========================= POST ========================= */

        public async Task<IActionResult> OnPostAsync()
        {
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

            employeeInDb.UserIdNumber = Employee.UserIdNumber;
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
            employeeInDb.WorkHoursType = Employee.WorkHoursType;
            employeeInDb.Schedule = Employee.Schedule;
            employeeInDb.EmployeeType = Employee.EmployeeType;
            employeeInDb.EmploymentStatus = Employee.EmploymentStatus;
            employeeInDb.Status = Employee.Status;
            employeeInDb.StartDate = Employee.StartDate;

            employeeInDb.TenureMonths = CalculateTenureMonths(employeeInDb.StartDate);

            await _context.SaveChangesAsync();

            return RedirectToPage("/Dashboard/EmployeeManagement/Index");
        }

        private int CalculateTenureMonths(DateTime startDate)
        {
            var today = DateTime.Today;
            return Math.Max(0,
                (today.Year - startDate.Year) * 12 +
                (today.Month - startDate.Month));
        }
    }
}
