using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS_System.Models
{
    public class EmployeeAuditLog
    {
        [Key]
        public int Id { get; set; }

        public int UserInformationId { get; set; }

        [ForeignKey(nameof(UserInformationId))]
        public UserInformationModel UserInformation { get; set; } = null!;

        [Required]
        [StringLength(30)]
        public string ActionType { get; set; } = string.Empty;

        public DateTime ActionDate { get; set; }

        public int? PerformedByUserId { get; set; }
    }
}