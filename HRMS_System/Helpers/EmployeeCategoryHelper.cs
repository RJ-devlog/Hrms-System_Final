using HRMS_System.Enums;

namespace HRMS_System.Helpers
{
    public static class EmployeeCategoryHelper
    {
        public static EmployeeCategory FromText(string? category)
        {
            return category?.Trim() switch
            {
                "Operations / Port Operations" => EmployeeCategory.OperationsPortOperations,
                "Technical / Engineering / Maintenance" => EmployeeCategory.TechnicalEngineeringMaintenance,
                "Administrative / Office" => EmployeeCategory.AdministrativeOffice,
                "IT / MIS / Systems" => EmployeeCategory.ITMISSystems,
                "Drivers / Transport" => EmployeeCategory.DriversTransport,
                "Safety / Medical / Security" => EmployeeCategory.SafetyMedicalSecurity,
                "Logistics / Warehouse / Support" => EmployeeCategory.LogisticsWarehouseSupport,
                "Finance / Claims / Insurance" => EmployeeCategory.FinanceClaimsInsurance,
                "Misc / Support Roles" => EmployeeCategory.MiscSupportRoles,
                "Management / Executive" => EmployeeCategory.ManagementExecutive,
                _ => EmployeeCategory.OperationsPortOperations
            };
        }

        public static string ToDisplayText(EmployeeCategory category)
        {
            return category switch
            {
                EmployeeCategory.OperationsPortOperations => "Operations / Port Operations",
                EmployeeCategory.TechnicalEngineeringMaintenance => "Technical / Engineering / Maintenance",
                EmployeeCategory.AdministrativeOffice => "Administrative / Office",
                EmployeeCategory.ITMISSystems => "IT / MIS / Systems",
                EmployeeCategory.DriversTransport => "Drivers / Transport",
                EmployeeCategory.SafetyMedicalSecurity => "Safety / Medical / Security",
                EmployeeCategory.LogisticsWarehouseSupport => "Logistics / Warehouse / Support",
                EmployeeCategory.FinanceClaimsInsurance => "Finance / Claims / Insurance",
                EmployeeCategory.MiscSupportRoles => "Misc / Support Roles",
                EmployeeCategory.ManagementExecutive => "Management / Executive",
                _ => ""
            };
        }

    }
}