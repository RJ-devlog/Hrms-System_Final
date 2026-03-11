using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRMS_System.Enums;

namespace HRMS_System.Models
{
    public class LoginModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserInformationId { get; set; }

        [ForeignKey(nameof(UserInformationId))]
        public UserInformationModel UserInformation { get; set; } = null!;

        [Required(ErrorMessage = "Employee Number is required")]
        [StringLength(20)]
        public string EmployeeNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [Column("Passwordd")]
        [StringLength(100)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public AccessRole AccessRole { get; set; }
    }
}