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

        private static readonly Dictionary<string, string> GroupPrefixMap = new()
        {
            ["Operations / Port Operations"] = "OPE",
            ["Technical / Engineering / Maintenance"] = "TEM",
            ["Administrative / Office"] = "ADM",
            ["IT / MIS / Systems"] = "ITM",
            ["Drivers / Transport"] = "DRI",
            ["Safety / Medical / Security"] = "SMS",
            ["Logistics / Warehouse / Support"] = "LWS",
            ["Finance / Claims / Insurance"] = "FCI",
            ["Misc / Support Roles"] = "MSC",
            // If you add Management/Executive later:
            ["Management / Executive"] = "MEX",
        };

        private string GetPrefixFromJobRole(string? jobRole)
        {
            if (string.IsNullOrWhiteSpace(jobRole)) return "EMP";

            var item = EmployeeCatalog.JobRoles.FirstOrDefault(x => x.Value == jobRole);
            var groupName = item?.Group?.Name;

            if (groupName != null && GroupPrefixMap.TryGetValue(groupName, out var prefix))
                return prefix;

            return "EMP"; // fallback
        }
        public async Task OnGetAsync()
        {
            Employee.EmployeeNumber = ""; // generated after selecting job role
            Employee.StartDate = DateTime.Today;
            Employee.BirthDate = new DateTime(2000, 01, 01);
            Employee.EmploymentStatus = "Probationary";
            Employee.Status = "Active";

            JobRoleOptions = EmployeeCatalog.JobRoles;

            await _deptService.EnsureCatalogSeededAsync();
            DepartmentOptions = await _deptService.GetDepartmentOptionsAsync();

            var prefix = GetPrefixFromJobRole(Employee.JobRole);
            Employee.EmployeeNumber = await GenerateNextEmployeeNumberAsync(prefix);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            JobRoleOptions = EmployeeCatalog.JobRoles;
            await _deptService.EnsureCatalogSeededAsync();
            DepartmentOptions = await _deptService.GetDepartmentOptionsAsync();

            Employee.Department = await _context.Departments
                .Where(d => d.Id == Employee.DepartmentId)
                .Select(d => d.Name)
                .FirstOrDefaultAsync();

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

            if (!ModelState.IsValid)
                return Page();

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _context.UserInformation.Add(Employee);
                await _context.SaveChangesAsync();

                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == Employee.EmployeeNumber);

                if (existingUser == null)
                {
                    _context.Users.Add(new User
                    {
                        Username = Employee.EmployeeNumber,
                        Password = Employee.EmployeeNumber,
                        Role = UserRole.HR
                    });
                }
                else
                {
                    existingUser.Password = Employee.EmployeeNumber;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return RedirectToPage("./Index");
            }
            catch
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError(string.Empty, "Unable to save employee and login account.");
                return Page();
            }
        }
        private async Task<string> GenerateNextEmployeeNumberAsync(string prefix)
        {
            // Find the latest employee number for that prefix
            var last = await _context.UserInformation
                .AsNoTracking()
                .Where(e => e.EmployeeNumber.StartsWith(prefix + "-"))
                .OrderByDescending(e => e.id)
                .Select(e => e.EmployeeNumber)
                .FirstOrDefaultAsync();

            int nextNumber = 1;

            if (!string.IsNullOrWhiteSpace(last))
            {
                // last looks like "OPE-000123"
                var parts = last.Split('-', 2);
                if (parts.Length == 2 && int.TryParse(parts[1], out var n))
                    nextNumber = n + 1;
            }

            return $"{prefix}-{nextNumber:D6}";
        }
        public async Task<JsonResult> OnGetGenerateEmployeeNumberAsync(string jobRole)
        {
            var prefix = GetPrefixFromJobRole(jobRole);
            var empNo = await GenerateNextEmployeeNumberAsync(prefix);
            return new JsonResult(new { employeeNumber = empNo, prefix });
        }
    }
}
