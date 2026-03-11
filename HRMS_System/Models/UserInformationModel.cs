using HRMS_System.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace HRMS_System.Models
{
    public class UserInformationModel
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Department is required.")]
        public int? DepartmentId { get; set; }

        [NotMapped]
        public string EmployeeNumberDigits =>
            Regex.Replace(EmployeeNumber ?? "", @"\D", "");

        [Required]
        [StringLength(20)]
        public string EmployeeNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "First Name is required.")]
        [StringLength(50)]
        public string? FirstName { get; set; }

        [StringLength(50)]
        public string? MiddleName { get; set; }

        [Required(ErrorMessage = "Last Name is required.")]
        [StringLength(50)]
        public string? LastName { get; set; }

        public EmployeeCategory? Category { get; set; }

        [Required(ErrorMessage = "Job Role is required.")]
        [StringLength(100)]
        public string? JobRole { get; set; }

        [StringLength(50)]
        public string? Department { get; set; }

        [Required(ErrorMessage = "Employment Status is required.")]
        [StringLength(20)]
        public string? EmploymentStatus { get; set; }

        [Required(ErrorMessage = "Account status is required.")]
        [StringLength(20)]
        public string? Status { get; set; }

        [Required(ErrorMessage = "Start Date is required.")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Birth Date is required.")]
        [DataType(DataType.Date)]
        public DateTime BirthDate { get; set; }

        [Required(ErrorMessage = "Gender is required.")]
        [StringLength(10)]
        public string? Gender { get; set; }

        [Required(ErrorMessage = "Civil Status is required.")]
        [StringLength(20)]
        public string? CivilStatus { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Phone Number is required.")]
        [Phone]
        public string? PhoneNumber { get; set; }

        [StringLength(150)]
        public string? Address { get; set; }

        [Range(0, 600)]
        public int? TenureMonths { get; set; }

        [StringLength(255)]
        public string? ProfileImagePath { get; set; }

        [StringLength(6)]
        public string? Pin { get; set; }

        public LoginModel? Login { get; set; }
    }
}