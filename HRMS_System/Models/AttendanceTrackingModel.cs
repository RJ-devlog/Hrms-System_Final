using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS_System.Models
{
    public class AttendanceTrackingModel
    {
        [Key]
        public int Id { get; set; }

        /* 🔗 RELATIONSHIP */
        [Required]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public UserInformationModel User { get; set; } = null!;

        /* 📅 ATTENDANCE */
        [Required]
        [DataType(DataType.Date)]
        public DateTime AttendanceDate { get; set; }

        [DataType(DataType.Time)]
        [Display(Name = "Time In")]
        public DateTime? TimeIn { get; set; }

        [DataType(DataType.Time)]
        [Display(Name = "Time Out")]
        public DateTime? TimeOut { get; set; }

        /* 📊 STATUS */
        [StringLength(20)]
        public string? AttendanceStatus { get; set; }
        // On-Time | Late | Absent

        /* 🖥 UI SUPPORT */
        [NotMapped]
        public string DisplayTime =>
            TimeIn.HasValue ? TimeIn.Value.ToString("hh:mm tt") :
            TimeOut.HasValue ? TimeOut.Value.ToString("hh:mm tt") : "-";
    }
}
