using HRMS_System.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HRMS_System.Data
{
    public static class EmployeeCatalog
    {
        // Job Roles (with OptGroup)
        public static List<SelectListItem> JobRoles => BuildJobRoles();        
        public static List<SelectListItem> Departments => BuildDepartments();
        private static List<SelectListItem> BuildDepartments()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "-- Select Department --" },

                new SelectListItem { Value = "Chairman's Office", Text = "Chairman's Office" },
                new SelectListItem { Value = "Vice Chairman's Office", Text = "Vice Chairman's Office" },
                new SelectListItem { Value = "Administration", Text = "Administration" },
                new SelectListItem { Value = "Engineering", Text = "Engineering" },
                new SelectListItem { Value = "Finance", Text = "Finance" },
                new SelectListItem { Value = "General Manager's Office", Text = "General Manager's Office" },
                new SelectListItem { Value = "Legal", Text = "Legal" },
                new SelectListItem { Value = "....", Text = "...." }
            };
        }
        private static List<SelectListItem> BuildJobRoles()
        {
            // Create groups ONCE (important)
            var ops = new SelectListGroup { Name = "Operations / Port Operations" };
            var tech = new SelectListGroup { Name = "Technical / Engineering / Maintenance" };
            var admin = new SelectListGroup { Name = "Administrative / Office" };
            var it = new SelectListGroup { Name = "IT / MIS / Systems" };
            var drivers = new SelectListGroup { Name = "Drivers / Transport" };
            var safety = new SelectListGroup { Name = "Safety / Medical / Security" };
            var logistics = new SelectListGroup { Name = "Logistics / Warehouse / Support" };
            var finance = new SelectListGroup { Name = "Finance / Claims / Insurance" };
            var misc = new SelectListGroup { Name = "Misc / Support Roles" };
            var mngmt = new SelectListGroup { Name = "Management / Executive" };

            return new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "-- Select Job Role --" },

                // Operations / Port Operations
                new SelectListItem { Value = "Port Worker", Text = "Port Worker", Group = ops },
                new SelectListItem { Value = "Extra Port Worker", Text = "Extra Port Worker", Group = ops },
                new SelectListItem { Value = "Port Worker / RTG", Text = "Port Worker / RTG", Group = ops },
                new SelectListItem { Value = "Port Worker / Timekeeper", Text = "Port Worker / Timekeeper", Group = ops },
                new SelectListItem { Value = "Gantry Operator", Text = "Gantry Operator", Group = ops },
                new SelectListItem { Value = "Quay Crane Operator", Text = "Quay Crane Operator", Group = ops },
                new SelectListItem { Value = "QC Crane Operator", Text = "QC Crane Operator", Group = ops },
                new SelectListItem { Value = "RTG Operator", Text = "RTG Operator", Group = ops },
                new SelectListItem { Value = "RTG Trainee", Text = "RTG Trainee", Group = ops },
                new SelectListItem { Value = "Reach Stacker Operator", Text = "Reach Stacker Operator", Group = ops },
                new SelectListItem { Value = "Prime Mover Operator", Text = "Prime Mover Operator", Group = ops },
                new SelectListItem { Value = "Extra Prime Mover", Text = "Extra Prime Mover", Group = ops },
                new SelectListItem { Value = "PM Operator", Text = "PM Operator", Group = ops },
                new SelectListItem { Value = "Transtrainer Operator", Text = "Transtrainer Operator", Group = ops },
                new SelectListItem { Value = "Sidelifter Operator", Text = "Sidelifter Operator", Group = ops },
                new SelectListItem { Value = "Forklift Operator", Text = "Forklift Operator", Group = ops },
                new SelectListItem { Value = "Winchman", Text = "Winchman", Group = ops },
                new SelectListItem { Value = "Winchman – Pooling", Text = "Winchman – Pooling", Group = ops },
                new SelectListItem { Value = "Gang Boss", Text = "Gang Boss", Group = ops },
                new SelectListItem { Value = "Foreman", Text = "Foreman", Group = ops },
                new SelectListItem { Value = "Gear Locker", Text = "Gear Locker", Group = ops },
                new SelectListItem { Value = "Gatekeeper", Text = "Gatekeeper", Group = ops },
                new SelectListItem { Value = "Gate Control Checker", Text = "Gate Control Checker", Group = ops },
                new SelectListItem { Value = "Assigning Checker", Text = "Assigning Checker", Group = ops },
                new SelectListItem { Value = "CY Checker", Text = "CY Checker", Group = ops },
                new SelectListItem { Value = "Dock Checker", Text = "Dock Checker", Group = ops },
                new SelectListItem { Value = "Document Verifier", Text = "Document Verifier", Group = ops },
                new SelectListItem { Value = "Checker", Text = "Checker", Group = ops },
                new SelectListItem { Value = "Tireman", Text = "Tireman", Group = ops },

                // Technical / Engineering / Maintenance
                new SelectListItem { Value = "Mechanic", Text = "Mechanic", Group = tech },
                new SelectListItem { Value = "Senior Mechanic", Text = "Senior Mechanic", Group = tech },
                new SelectListItem { Value = "Junior Mechanic", Text = "Junior Mechanic", Group = tech },
                new SelectListItem { Value = "Heavy Equipment Mechanic", Text = "Heavy Equipment Mechanic", Group = tech },
                new SelectListItem { Value = "Junior Heavy Equipment Mechanic", Text = "Junior Heavy Equipment Mechanic", Group = tech },
                new SelectListItem { Value = "Senior Heavy Equipment Mechanic", Text = "Senior Heavy Equipment Mechanic", Group = tech },
                new SelectListItem { Value = "Bulk Handling Mechanic", Text = "Bulk Handling Mechanic", Group = tech },
                new SelectListItem { Value = "Industrial Electrician", Text = "Industrial Electrician", Group = tech },
                new SelectListItem { Value = "Junior Industrial Electrician", Text = "Junior Industrial Electrician", Group = tech },
                new SelectListItem { Value = "Automotive Electrician", Text = "Automotive Electrician", Group = tech },
                new SelectListItem { Value = "Electronics Technician", Text = "Electronics Technician", Group = tech },
                new SelectListItem { Value = "Junior Electronics Technician", Text = "Junior Electronics Technician", Group = tech },
                new SelectListItem { Value = "Assistant Electronics Technician", Text = "Assistant Electronics Technician", Group = tech },
                new SelectListItem { Value = "Reefer Container Technician", Text = "Reefer Container Technician", Group = tech },
                new SelectListItem { Value = "Reefer Monitoring Technician", Text = "Reefer Monitoring Technician", Group = tech },
                new SelectListItem { Value = "Welder", Text = "Welder", Group = tech },
                new SelectListItem { Value = "Industrial Welder", Text = "Industrial Welder", Group = tech },
                new SelectListItem { Value = "Carpenter", Text = "Carpenter", Group = tech },
                new SelectListItem { Value = "Painter", Text = "Painter", Group = tech },
                new SelectListItem { Value = "Sewer", Text = "Sewer", Group = tech },
                new SelectListItem { Value = "Toolkeeper", Text = "Toolkeeper", Group = tech },
                new SelectListItem { Value = "Maintenance Utility", Text = "Maintenance Utility", Group = tech },
                new SelectListItem { Value = "Technical & Maintenance Staff", Text = "Technical & Maintenance Staff", Group = tech },

                // Administrative / Office
                new SelectListItem { Value = "Office Staff", Text = "Office Staff", Group = admin },
                new SelectListItem { Value = "Office Assistant to the OM", Text = "Office Assistant to the OM", Group = admin },
                new SelectListItem { Value = "Record Keeper", Text = "Record Keeper", Group = admin },
                new SelectListItem { Value = "Timekeeper", Text = "Timekeeper", Group = admin },
                new SelectListItem { Value = "Head Timekeeper", Text = "Head Timekeeper", Group = admin },
                new SelectListItem { Value = "Payroll Clerk", Text = "Payroll Clerk", Group = admin },
                new SelectListItem { Value = "Billing Clerk", Text = "Billing Clerk", Group = admin },
                new SelectListItem { Value = "Billing Staff", Text = "Billing Staff", Group = admin },
                new SelectListItem { Value = "Accounting Staff", Text = "Accounting Staff", Group = admin },
                new SelectListItem { Value = "Subsidiary Ledger Clerk", Text = "Subsidiary Ledger Clerk", Group = admin },
                new SelectListItem { Value = "Subsidiary Ledger In-Charge", Text = "Subsidiary Ledger In-Charge", Group = admin },
                new SelectListItem { Value = "Credit & Collection Staff", Text = "Credit & Collection Staff", Group = admin },
                new SelectListItem { Value = "Purchasing Clerk", Text = "Purchasing Clerk", Group = admin },
                new SelectListItem { Value = "HR Clerk", Text = "HR Clerk", Group = admin },
                new SelectListItem { Value = "HR Staff", Text = "HR Staff", Group = admin },
                new SelectListItem { Value = "Paymaster", Text = "Paymaster", Group = admin },
                new SelectListItem { Value = "Audit Staff", Text = "Audit Staff", Group = admin },

                // IT / MIS / Systems
                new SelectListItem { Value = "MIS Staff", Text = "MIS Staff", Group = it },
                new SelectListItem { Value = "MIS Programmer", Text = "MIS Programmer", Group = it },
                new SelectListItem { Value = "Junior MIS Programmer", Text = "Junior MIS Programmer", Group = it },
                new SelectListItem { Value = "IT Development Personnel", Text = "IT Development Personnel", Group = it },
                new SelectListItem { Value = "Junior Technical Specialist", Text = "Junior Technical Specialist", Group = it },
                new SelectListItem { Value = "MIS Junior Technical Specialist", Text = "MIS Junior Technical Specialist", Group = it },

                // Drivers / Transport
                new SelectListItem { Value = "Service Driver", Text = "Service Driver", Group = drivers },
                new SelectListItem { Value = "Shuttle Driver", Text = "Shuttle Driver", Group = drivers },
                new SelectListItem { Value = "Executive Driver", Text = "Executive Driver", Group = drivers },
                new SelectListItem { Value = "Ambulance Driver", Text = "Ambulance Driver", Group = drivers },

                // Safety / Medical / Security
                new SelectListItem { Value = "Company Nurse", Text = "Company Nurse", Group = safety },
                new SelectListItem { Value = "First Aider", Text = "First Aider", Group = safety },
                new SelectListItem { Value = "Safety Officer", Text = "Safety Officer", Group = safety },
                new SelectListItem { Value = "Security Aide", Text = "Security Aide", Group = safety },
                new SelectListItem { Value = "SSHEMO Staff", Text = "SSHEMO Staff", Group = safety },
                new SelectListItem { Value = "Assistant Pollution Control Officer", Text = "Assistant Pollution Control Officer", Group = safety },

                // Logistics / Warehouse / Support
                new SelectListItem { Value = "Warehouse Staff", Text = "Warehouse Staff", Group = logistics },
                new SelectListItem { Value = "Warehouse Clerk", Text = "Warehouse Clerk", Group = logistics },
                new SelectListItem { Value = "CFS Staff", Text = "CFS Staff", Group = logistics },
                new SelectListItem { Value = "ISO Staff", Text = "ISO Staff", Group = logistics },
                new SelectListItem { Value = "Utility Personnel", Text = "Utility Personnel", Group = logistics },
                new SelectListItem { Value = "General Services Utility", Text = "General Services Utility", Group = logistics },
                new SelectListItem { Value = "General Services Driver", Text = "General Services Driver", Group = logistics },
                new SelectListItem { Value = "General Services Painter", Text = "General Services Painter", Group = logistics },
                new SelectListItem { Value = "General Services Carpenter", Text = "General Services Carpenter", Group = logistics },

                // Finance / Claims / Insurance
                new SelectListItem { Value = "Insurance & Claims Staff", Text = "Insurance & Claims Staff", Group = finance },
                new SelectListItem { Value = "Insurance & Claims In-Charge", Text = "Insurance & Claims In-Charge", Group = finance },

                // Misc / Support Roles
                new SelectListItem { Value = "Cashier", Text = "Cashier", Group = misc },
                new SelectListItem { Value = "Satellite Cashier", Text = "Satellite Cashier", Group = misc },
                new SelectListItem { Value = "Augmentation Cashier", Text = "Augmentation Cashier", Group = misc },
                new SelectListItem { Value = "Extra", Text = "Extra", Group = misc },
                new SelectListItem { Value = "Operations Staff", Text = "Operations Staff", Group = misc },
                new SelectListItem { Value = "Operations & Monitoring Staff", Text = "Operations & Monitoring Staff", Group = misc },
                new SelectListItem { Value = "OPN / Monitoring Staff", Text = "OPN / Monitoring Staff", Group = misc },
                new SelectListItem { Value = "VOS", Text = "VOS", Group = misc },
                new SelectListItem {Value = "SUPERVISOR", Text = "SUPERVISOR", Group = misc},
                // Management / Exucutive
                new SelectListItem { Value = "General Manager", Text = "General Manager", Group = mngmt },
                new SelectListItem { Value = "CEO", Text = "CEO", Group = mngmt }

            };
        }
    }
}
