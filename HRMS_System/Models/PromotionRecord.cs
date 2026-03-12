using System;

namespace HRMS_System.Models
{
    public class PromotionRecord
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        // optional navigation property
        public UserInformationModel? Employee { get; set; }

        public string OldRole { get; set; } = "";

        public string NewRole { get; set; } = "";

        public DateTime PromotionDate { get; set; } = DateTime.Now;

        public string? ApprovedBy { get; set; }

        public string? Notes { get; set; }
    }
}