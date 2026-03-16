using System;
using System.ComponentModel.DataAnnotations;

namespace HRMS_System.Models
{
    public class PromotionNotificationModel
    {
        [Key]
        public int Id { get; set; }

        // Who this notification is about (employee)
        public int EmployeeId { get; set; }

        public UserInformationModel? EmployeeInfo { get; set; }


        [StringLength(120)]
        public string EmployeeName { get; set; } = "";

        [StringLength(200)]
        public string Title { get; set; } = "";

        [StringLength(600)]
        public string Message { get; set; } = "";

        // predicted_yes / predicted_no / created / approved / rejected
        [StringLength(30)]
        public string StatusKey { get; set; } = "created";

        public bool IsRead { get; set; } = false;

        public bool IsArchived { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
