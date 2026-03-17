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

        private string GetPrefixFromCategory(EmployeeCategory? category)
        {
            if (category.HasValue && GroupPrefixMap.TryGetValue(category.Value, out var prefix))
                return prefix;

            return "EMP";
        }

        public async Task OnGetAsync()
        {
            Employee.EmployeeNumber = "";
            Employee.StartDate = DateTime.Today;
            Employee.BirthDate = new DateTime(2000, 01, 01);
            Employee.EmploymentStatus = "Probationary";
            Employee.Status = "Active";

            JobRoleOptions = EmployeeCatalog.JobRoles;

            await _deptService.EnsureCatalogSeededAsync();
            DepartmentOptions = await _deptService.GetDepartmentOptionsAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            JobRoleOptions = EmployeeCatalog.JobRoles;
            await _deptService.EnsureCatalogSeededAsync();
            DepartmentOptions = await _deptService.GetDepartmentOptionsAsync();

            if (Employee.DepartmentId == DepartmentSelectService.AddNewValue)
            {
                if (string.IsNullOrWhiteSpace(NewDepartmentName))
                {
                    ModelState.AddModelError(nameof(NewDepartmentName), "Please enter a department name.");
                }
                else
                {
                    Employee.DepartmentId = await _deptService.GetOrCreateDepartmentIdAsync(NewDepartmentName);
                    Employee.Department = NewDepartmentName.Trim();
                }
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

            Employee.Category = GetCategoryFromJobRole(Employee.JobRole);

            // Clear the old model-binding validation error for Category
            ModelState.Remove("Employee.Category");
            ModelState.Remove("Employee.Category.Value");

            if (Employee.Category == null)
            {
                ModelState.AddModelError("Employee.JobRole", "Selected job role has no assigned category.");
            }
            else
            {
                var prefix = GetPrefixFromCategory(Employee.Category);
                Employee.EmployeeNumber = await GenerateNextEmployeeNumberAsync(prefix);
            }

            if (!ModelState.IsValid)
                return Page();

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _context.UserInformation.Add(Employee);
                await _context.SaveChangesAsync();

                var accessRole = GetAccessRoleFromJobRole(Employee.JobRole);

                var existingUser = await _context.Login
                    .FirstOrDefaultAsync(u => u.EmployeeNumber == Employee.EmployeeNumber);

                if (existingUser == null)
                {
                    _context.Login.Add(new LoginModel
                    {
                        UserInformationId = Employee.Id,
                        EmployeeNumber = Employee.EmployeeNumber,
                        Password = Employee.EmployeeNumber,      
                        AccessRole = accessRole
                    });
                }   
                else
                {
                    existingUser.UserInformationId = Employee.Id;
                    existingUser.EmployeeNumber = Employee.EmployeeNumber;
                    existingUser.Password = Employee.EmployeeNumber;
                    existingUser.AccessRole = accessRole;
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
        private async Task<string> GenerateNextEmployeeNumberAsync(string prefix)
        {
            var last = await _context.UserInformation
                .AsNoTracking()
                .Where(e => e.EmployeeNumber.StartsWith(prefix + "-"))
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

        public async Task<JsonResult> OnGetGenerateEmployeeNumberAsync(string jobRole)
        {
            var category = GetCategoryFromJobRole(jobRole);

            if (category == null)
            {
                return new JsonResult(new { employeeNumber = "", prefix = "" });
            }

            var prefix = GetPrefixFromCategory(category.Value);
            var empNo = await GenerateNextEmployeeNumberAsync(prefix);

            return new JsonResult(new { employeeNumber = empNo, prefix });
        }

        private AccessRole GetAccessRoleFromJobRole(string? jobRole)
        {
            var role = jobRole?.Trim();

            return role switch
            {
                "IT Development Personnel" => AccessRole.Admin,
                "General Manager" => AccessRole.Manager,
                "SUPERVISOR" => AccessRole.Supervisor,
                "HR Clerk" => AccessRole.HR,
                "HR Staff" => AccessRole.HR,
                "CEO" => AccessRole.CEO,
                _ => AccessRole.Employee
            };
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
                "SUPERVISOR" => EmployeeCategory.MiscSupportRoles,
                // Management / Executive
                "General Manager" => EmployeeCategory.ManagementExecutive,
                "CEO" => EmployeeCategory.ManagementExecutive,

                _ => null
            };
        }
    }
}