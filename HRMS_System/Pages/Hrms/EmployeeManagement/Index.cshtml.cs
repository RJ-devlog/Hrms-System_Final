using HRMS_System.Data;
using HRMS_System.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HRMS_System.Pages.Dashboard.EmployeeManagement
{
    public class EmployeeManagementModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EmployeeManagementModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<UserInformationModel> employeeInfo { get; set; } = new List<UserInformationModel>();

        // ?? Search term from URL (?SearchTerm=value)
        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }
        //---------------SEARCH-----------------------
        public async Task OnGetAsync()
        {
            IQueryable<UserInformationModel> query = _context.UserInformation;

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                query = query.Where(e =>
                    e.UserIdNumber.ToString().Contains(SearchTerm) ||
                    e.FirstName.Contains(SearchTerm) ||
                    e.LastName.Contains(SearchTerm) ||
                    e.Email.Contains(SearchTerm) ||
                    e.PhoneNumber.Contains(SearchTerm)
                );
            }
            employeeInfo = await query
                .OrderBy(e => e.LastName)
                .ToListAsync();
        }
        public async Task<IActionResult> OnGetEmployeeProfile(int id)
        {
            var emp = await _context.UserInformation
                .Where(e => e.id == id)
                .Select(e => new
                {
                    Id = e.id,
                    idNumber = e.UserIdNumber,
                    fullName = e.FirstName + " " + e.LastName,
                    email = e.Email,
                    phoneNumber = e.PhoneNumber,
                    gender = e.Gender,
                    civilStatus = e.CivilStatus,
                    address = e.Address,
                    jobRole = e.JobRole,
                    department = e.Department,
                    workHoursType = e.WorkHoursType,
                    schedule = e.Schedule,
                    employeeType = e.EmployeeType,
                    status = e.Status,
                    startDate = e.StartDate,
                    profileImagePath = e.ProfileImagePath
                })
                .FirstOrDefaultAsync();

            if (emp == null)
                return NotFound();

            return new JsonResult(emp);
        }


    }
}
