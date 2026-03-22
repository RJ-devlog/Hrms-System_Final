using HRMS_System.Data;
using HRMS_System.Enums;
using HRMS_System.Models;
using HRMS_System.Services;
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
        private readonly DepartmentSelectService _deptService;

        public EditEmployeeInfoModel(
            ApplicationDbContext context,
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

        public List<SelectListItem> JobRoleOptions { get; set; } = new();
        public List<SelectListItem> DepartmentOptions { get; set; } = new();

        private static readonly Dictionary<EmployeeCategory, string> GroupPrefixMap = new()
        {
            [EmployeeCategory.OperationsPortOperations] = "OPE",
            [EmployeeCategory.TechnicalEngineeringMaintenance] = "TEM",
            [EmployeeCategory.AdministrativeOffice] = "ADM",
            [EmployeeCategory.ITMISSystems] = "ITM",
            [EmployeeCategory.DriversTransport] = "DRI",
            [EmployeeCategory.SafetyMedicalSecurity] = "SMS",
            [EmployeeCategory.LogisticsWarehouseSupport] = "LWS",
            [EmployeeCategory.FinanceClaimsInsurance] = "FCI",
            [EmployeeCategory.MiscSupportRoles] = "MSC",
            [EmployeeCategory.ManagementExecutive] = "MEX",
        };

        public async Task<IActionResult> OnGetAsync(int id)
        {
            JobRoleOptions = EmployeeCatalog.JobRoles;
            await _deptService.EnsureCatalogSeededAsync();
            DepartmentOptions = await _deptService.GetDepartmentOptionsAsync();

            Employee = await _context.UserInformation.FirstOrDefaultAsync(e => e.Id == id);
            if (Employee == null)
                return NotFound();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            JobRoleOptions = EmployeeCatalog.JobRoles;
            await _deptService.EnsureCatalogSeededAsync();
            DepartmentOptions = await _deptService.GetDepartmentOptionsAsync();

            var employeeInDb = await _context.UserInformation
                .FirstOrDefaultAsync(e => e.Id == Employee.Id);

            if (employeeInDb == null)
                return NotFound();
            var oldJobRole = employeeInDb.JobRole?.Trim();
            var newJobRole = Employee.JobRole?.Trim();

            // Department handling
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

            // Recalculate category from job role
            var oldCategory = employeeInDb.Category;
            Employee.Category = GetCategoryFromJobRole(Employee.JobRole);

            // Remove old validation error because Category is now assigned in backend
            ModelState.Remove("Employee.Category");
            ModelState.Remove("Employee.Category.Value");

            if (Employee.Category == null)
            {
                ModelState.AddModelError("Employee.JobRole", "Selected job role has no assigned category.");
            }

            if (!ModelState.IsValid)
                return Page();

            // Regenerate employee number only if category changed
            if (oldCategory != Employee.Category)
            {
                var prefix = GetPrefixFromCategory(Employee.Category);
                Employee.EmployeeNumber = await GenerateNextEmployeeNumberAsync(prefix, Employee.Id);
            }
            else
            {
                Employee.EmployeeNumber = employeeInDb.EmployeeNumber;
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Upload image
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

                // Update employee fields
                employeeInDb.EmployeeNumber = Employee.EmployeeNumber;
                employeeInDb.FirstName = Employee.FirstName;
                employeeInDb.MiddleName = Employee.MiddleName;
                employeeInDb.LastName = Employee.LastName;
                employeeInDb.Email = Employee.Email;
                employeeInDb.PhoneNumber = Employee.PhoneNumber;
                employeeInDb.BirthDate = Employee.BirthDate;
                employeeInDb.Gender = Employee.Gender;
                employeeInDb.CivilStatus = Employee.CivilStatus;
                employeeInDb.Address = Employee.Address;
                employeeInDb.JobRole = newJobRole;
                employeeInDb.Category = Employee.Category;
                employeeInDb.Department = Employee.Department;
                employeeInDb.DepartmentId = Employee.DepartmentId;
                employeeInDb.EmploymentStatus = Employee.EmploymentStatus;
                employeeInDb.Status = Employee.Status;
                employeeInDb.StartDate = Employee.StartDate;
                employeeInDb.TenureMonths = CalculateTenureMonths(employeeInDb.StartDate);
                if (!string.Equals(oldJobRole, newJobRole, StringComparison.OrdinalIgnoreCase))
                {
                    var employeeDisplay = $"{Employee.EmployeeNumber} - {Employee.FirstName} {Employee.LastName}".Trim();

                    _context.PromotionNotifications.Add(new PromotionNotificationModel
                    {
                        EmployeeId = Employee.Id,
                        EmployeeName = employeeDisplay,
                        Title = "New Job Role Assigned",
                        Message = $"{employeeDisplay} has been updated to the job role: {newJobRole}. Previous job role: {oldJobRole ?? "—"}.",
                        StatusKey = "job_role_updated",
                        IsRead = false,
                        IsArchived = false,
                        CreatedAt = DateTime.Now
                    });

                    TempData["NewNotif"] = true;
                }
                await _context.SaveChangesAsync();

                // Sync login table
                var accessRole = GetAccessRoleFromJobRole(employeeInDb.JobRole);

                var loginInDb = await _context.Login
                    .FirstOrDefaultAsync(l => l.UserInformationId == employeeInDb.Id);

                if (accessRole == AccessRole.Employee)
                {
                    // Unauthorized/basic role -> remove login if exists
                    if (loginInDb != null)
                    {
                        _context.Login.Remove(loginInDb);
                    }
                }
                else
                {
                    if (loginInDb == null)
                    {
                        _context.Login.Add(new LoginModel
                        {
                            UserInformationId = employeeInDb.Id,
                            EmployeeNumber = employeeInDb.EmployeeNumber,
                            Password = employeeInDb.EmployeeNumber,
                            AccessRole = accessRole
                        });
                    }
                    else
                    {
                        loginInDb.UserInformationId = employeeInDb.Id;
                        loginInDb.EmployeeNumber = employeeInDb.EmployeeNumber;
                        loginInDb.Password = employeeInDb.EmployeeNumber;
                        loginInDb.AccessRole = accessRole;
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError(string.Empty, ex.InnerException?.Message ?? ex.Message);
                return Page();
            }
        }

        private int CalculateTenureMonths(DateTime startDate)
        {
            var today = DateTime.Today;
            return Math.Max(0, (today.Year - startDate.Year) * 12 + (today.Month - startDate.Month));
        }

        private string GetPrefixFromCategory(EmployeeCategory? category)
        {
            if (category.HasValue && GroupPrefixMap.TryGetValue(category.Value, out var prefix))
                return prefix;

            return "EMP";
        }

        private async Task<string> GenerateNextEmployeeNumberAsync(string prefix, int? excludeEmployeeId = null)
        {
            var query = _context.UserInformation
                .AsNoTracking()
                .Where(e => e.EmployeeNumber.StartsWith(prefix + "-"));

            if (excludeEmployeeId.HasValue)
            {
                query = query.Where(e => e.Id != excludeEmployeeId.Value);
            }

            var last = await query
                .OrderByDescending(e => e.Id)
                .Select(e => e.EmployeeNumber)
                .FirstOrDefaultAsync();

            int nextNumber = 1;

            if (!string.IsNullOrWhiteSpace(last))
            {
                var parts = last.Split('-', 2);
                if (parts.Length == 2 && int.TryParse(parts[1], out var n))
                    nextNumber = n + 1;
            }

            return $"{prefix}-{nextNumber:D6}";
        }

        private AccessRole GetAccessRoleFromJobRole(string? jobRole)
        {
            var role = jobRole?.Trim();

            return role switch
            {
                "IT Development Personnel" => AccessRole.Supervisor,
                "General Manager" => AccessRole.Manager,
                "HR Clerk" => AccessRole.HR,
                "HR Staff" => AccessRole.HR,
                "CEO" => AccessRole.CEO,
                _ => AccessRole.Employee
            };
        }
        public async Task<JsonResult> OnGetPreviewEmployeeNumberAsync(string jobRole, int employeeId)
        {
            var employeeInDb = await _context.UserInformation
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == employeeId);

            if (employeeInDb == null)
            {
                return new JsonResult(new { employeeNumber = "", prefix = "" });
            }

            var newCategory = GetCategoryFromJobRole(jobRole);

            if (newCategory == null)
            {
                return new JsonResult(new { employeeNumber = employeeInDb.EmployeeNumber, prefix = "" });
            }

            // keep same employee number if category did not change
            if (employeeInDb.Category == newCategory)
            {
                return new JsonResult(new
                {
                    employeeNumber = employeeInDb.EmployeeNumber,
                    prefix = GetPrefixFromCategory(newCategory)
                });
            }

            var prefix = GetPrefixFromCategory(newCategory);
            var empNo = await GenerateNextEmployeeNumberAsync(prefix, employeeId);

            return new JsonResult(new { employeeNumber = empNo, prefix });
        }
        private EmployeeCategory? GetCategoryFromJobRole(string? jobRole)
        {
            var role = jobRole?.Trim();

            return role switch
            {
                // Operations / Port Operations
                "Port Worker" => EmployeeCategory.OperationsPortOperations,
                "Extra Port Worker" => EmployeeCategory.OperationsPortOperations,
                "Port Worker / RTG" => EmployeeCategory.OperationsPortOperations,
                "Port Worker / Timekeeper" => EmployeeCategory.OperationsPortOperations,
                "Gantry Operator" => EmployeeCategory.OperationsPortOperations,
                "Quay Crane Operator" => EmployeeCategory.OperationsPortOperations,
                "QC Crane Operator" => EmployeeCategory.OperationsPortOperations,
                "RTG Operator" => EmployeeCategory.OperationsPortOperations,
                "RTG Trainee" => EmployeeCategory.OperationsPortOperations,
                "Reach Stacker Operator" => EmployeeCategory.OperationsPortOperations,
                "Prime Mover Operator" => EmployeeCategory.OperationsPortOperations,
                "Extra Prime Mover" => EmployeeCategory.OperationsPortOperations,
                "PM Operator" => EmployeeCategory.OperationsPortOperations,
                "Transtrainer Operator" => EmployeeCategory.OperationsPortOperations,
                "Sidelifter Operator" => EmployeeCategory.OperationsPortOperations,
                "Forklift Operator" => EmployeeCategory.OperationsPortOperations,
                "Winchman" => EmployeeCategory.OperationsPortOperations,
                "Winchman – Pooling" => EmployeeCategory.OperationsPortOperations,
                "Gang Boss" => EmployeeCategory.OperationsPortOperations,
                "Foreman" => EmployeeCategory.OperationsPortOperations,
                "Gear Locker" => EmployeeCategory.OperationsPortOperations,
                "Gatekeeper" => EmployeeCategory.OperationsPortOperations,
                "Gate Control Checker" => EmployeeCategory.OperationsPortOperations,
                "Assigning Checker" => EmployeeCategory.OperationsPortOperations,
                "CY Checker" => EmployeeCategory.OperationsPortOperations,
                "Dock Checker" => EmployeeCategory.OperationsPortOperations,
                "Document Verifier" => EmployeeCategory.OperationsPortOperations,
                "Checker" => EmployeeCategory.OperationsPortOperations,
                "Tireman" => EmployeeCategory.OperationsPortOperations,

                // Technical / Engineering / Maintenance
                "Mechanic" => EmployeeCategory.TechnicalEngineeringMaintenance,
                "Senior Mechanic" => EmployeeCategory.TechnicalEngineeringMaintenance,
                "Junior Mechanic" => EmployeeCategory.TechnicalEngineeringMaintenance,
                "Heavy Equipment Mechanic" => EmployeeCategory.TechnicalEngineeringMaintenance,
                "Junior Heavy Equipment Mechanic" => EmployeeCategory.TechnicalEngineeringMaintenance,
                "Senior Heavy Equipment Mechanic" => EmployeeCategory.TechnicalEngineeringMaintenance,
                "Bulk Handling Mechanic" => EmployeeCategory.TechnicalEngineeringMaintenance,
                "Industrial Electrician" => EmployeeCategory.TechnicalEngineeringMaintenance,
                "Junior Industrial Electrician" => EmployeeCategory.TechnicalEngineeringMaintenance,
                "Automotive Electrician" => EmployeeCategory.TechnicalEngineeringMaintenance,
                "Electronics Technician" => EmployeeCategory.TechnicalEngineeringMaintenance,
                "Junior Electronics Technician" => EmployeeCategory.TechnicalEngineeringMaintenance,
                "Assistant Electronics Technician" => EmployeeCategory.TechnicalEngineeringMaintenance,
                "Reefer Container Technician" => EmployeeCategory.TechnicalEngineeringMaintenance,
                "Reefer Monitoring Technician" => EmployeeCategory.TechnicalEngineeringMaintenance,
                "Welder" => EmployeeCategory.TechnicalEngineeringMaintenance,
                "Industrial Welder" => EmployeeCategory.TechnicalEngineeringMaintenance,
                "Carpenter" => EmployeeCategory.TechnicalEngineeringMaintenance,
                "Painter" => EmployeeCategory.TechnicalEngineeringMaintenance,
                "Sewer" => EmployeeCategory.TechnicalEngineeringMaintenance,
                "Toolkeeper" => EmployeeCategory.TechnicalEngineeringMaintenance,
                "Maintenance Utility" => EmployeeCategory.TechnicalEngineeringMaintenance,
                "Technical & Maintenance Staff" => EmployeeCategory.TechnicalEngineeringMaintenance,

                // Administrative / Office
                "Office Staff" => EmployeeCategory.AdministrativeOffice,
                "Office Assistant to the OM" => EmployeeCategory.AdministrativeOffice,
                "Record Keeper" => EmployeeCategory.AdministrativeOffice,
                "Timekeeper" => EmployeeCategory.AdministrativeOffice,
                "Head Timekeeper" => EmployeeCategory.AdministrativeOffice,
                "Payroll Clerk" => EmployeeCategory.AdministrativeOffice,
                "Billing Clerk" => EmployeeCategory.AdministrativeOffice,
                "Billing Staff" => EmployeeCategory.AdministrativeOffice,
                "Accounting Staff" => EmployeeCategory.AdministrativeOffice,
                "Subsidiary Ledger Clerk" => EmployeeCategory.AdministrativeOffice,
                "Subsidiary Ledger In-Charge" => EmployeeCategory.AdministrativeOffice,
                "Credit & Collection Staff" => EmployeeCategory.AdministrativeOffice,
                "Purchasing Clerk" => EmployeeCategory.AdministrativeOffice,
                "HR Clerk" => EmployeeCategory.AdministrativeOffice,
                "HR Staff" => EmployeeCategory.AdministrativeOffice,
                "Paymaster" => EmployeeCategory.AdministrativeOffice,
                "Audit Staff" => EmployeeCategory.AdministrativeOffice,

                // IT / MIS / Systems
                "MIS Staff" => EmployeeCategory.ITMISSystems,
                "MIS Programmer" => EmployeeCategory.ITMISSystems,
                "Junior MIS Programmer" => EmployeeCategory.ITMISSystems,
                "IT Development Personnel" => EmployeeCategory.ITMISSystems,
                "Junior Technical Specialist" => EmployeeCategory.ITMISSystems,
                "MIS Junior Technical Specialist" => EmployeeCategory.ITMISSystems,

                // Drivers / Transport
                "Service Driver" => EmployeeCategory.DriversTransport,
                "Shuttle Driver" => EmployeeCategory.DriversTransport,
                "Executive Driver" => EmployeeCategory.DriversTransport,
                "Ambulance Driver" => EmployeeCategory.DriversTransport,

                // Safety / Medical / Security
                "Company Nurse" => EmployeeCategory.SafetyMedicalSecurity,
                "First Aider" => EmployeeCategory.SafetyMedicalSecurity,
                "Safety Officer" => EmployeeCategory.SafetyMedicalSecurity,
                "Security Aide" => EmployeeCategory.SafetyMedicalSecurity,
                "SSHEMO Staff" => EmployeeCategory.SafetyMedicalSecurity,
                "Assistant Pollution Control Officer" => EmployeeCategory.SafetyMedicalSecurity,

                // Logistics / Warehouse / Support
                "Warehouse Staff" => EmployeeCategory.LogisticsWarehouseSupport,
                "Warehouse Clerk" => EmployeeCategory.LogisticsWarehouseSupport,
                "CFS Staff" => EmployeeCategory.LogisticsWarehouseSupport,
                "ISO Staff" => EmployeeCategory.LogisticsWarehouseSupport,
                "Utility Personnel" => EmployeeCategory.LogisticsWarehouseSupport,
                "General Services Utility" => EmployeeCategory.LogisticsWarehouseSupport,
                "General Services Driver" => EmployeeCategory.LogisticsWarehouseSupport,
                "General Services Painter" => EmployeeCategory.LogisticsWarehouseSupport,
                "General Services Carpenter" => EmployeeCategory.LogisticsWarehouseSupport,

                // Finance / Claims / Insurance
                "Insurance & Claims Staff" => EmployeeCategory.FinanceClaimsInsurance,
                "Insurance & Claims In-Charge" => EmployeeCategory.FinanceClaimsInsurance,

                // Misc / Support Roles
                "Cashier" => EmployeeCategory.MiscSupportRoles,
                "Satellite Cashier" => EmployeeCategory.MiscSupportRoles,
                "Augmentation Cashier" => EmployeeCategory.MiscSupportRoles,
                "Extra" => EmployeeCategory.MiscSupportRoles,
                "Operations Staff" => EmployeeCategory.MiscSupportRoles,
                "Operations & Monitoring Staff" => EmployeeCategory.MiscSupportRoles,
                "OPN / Monitoring Staff" => EmployeeCategory.MiscSupportRoles,
                "VOS" => EmployeeCategory.MiscSupportRoles,

                // Management / Executive
                "General Manager" => EmployeeCategory.ManagementExecutive,
                "CEO" => EmployeeCategory.ManagementExecutive,

                _ => null
            };
        }
    }
}