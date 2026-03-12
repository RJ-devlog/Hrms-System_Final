using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS_System.Models
{
    public class PerformanceRecord
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserInformationId { get; set; }

        [ForeignKey(nameof(UserInformationId))]
        public UserInformationModel UserInformation { get; set; } = null!;

    }
}
