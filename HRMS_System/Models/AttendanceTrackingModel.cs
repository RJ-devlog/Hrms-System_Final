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

        /*ATTENDANCE DATE*/
        [Required]
        [DataType(DataType.Date)]
        public DateTime AttendanceDate { get; set; }

        /*IME IN / TIME OUT*/
        [DataType(DataType.Time)]
        public DateTime? TimeIn { get; set; }

        [DataType(DataType.Time)]
        public DateTime? TimeOut { get; set; }

        /*STATUS */
        [StringLength(20)]
        public string? AttendanceStatus { get; set; }

    }
}
