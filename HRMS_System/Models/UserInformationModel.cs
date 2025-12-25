using System;
using System.ComponentModel.DataAnnotations;

namespace HRMS_System.Models
{
    public class UserInformationModel
    {
        [Key]
        public int id { get; set; }
        [Required(ErrorMessage = "Id Number is required.")]
        public int UserIdNumber { get; set; }
     //   public ICollection<AttendanceTrackingModel>? AttendanceTrackings { get; set; }

        [Required(ErrorMessage = "First Name is required.")]
        [StringLength(50)]
        public string? FirstName { get; set; }

        [StringLength(50)]
        public string? MiddleName { get; set; }

        [Required(ErrorMessage = "Last Name is required.")]
        [StringLength(50)]
        public string? LastName { get; set; }

        /* ===================== WORK INFORMATION ===================== */

        [Required(ErrorMessage = "Job Role is required.")]
        [StringLength(50)]
        public string? JobRole { get; set; }

        [Required(ErrorMessage = "Department is required.")]
        [StringLength(50)]
        public string? Department { get; set; }

        // Full-time / Part-time
        [Required(ErrorMessage = "Work hours type is required.")]
        [StringLength(20)]
        public string? WorkHoursType { get; set; }


        ///Mon – Fri / Shifting
        [Required(ErrorMessage = "Schedule is required.")]
        [StringLength(50)]
        public string? Schedule { get; set; }

        // Example: Employee / Contractual / Intern
        [Required(ErrorMessage = "Employee type is required.")]
        [StringLength(30)]
        public string? EmployeeType { get; set; }

        [Required(ErrorMessage = "Employment status is required.")]
        [StringLength(20)]
        public string? EmploymentStatus { get; set; }

        [Required(ErrorMessage = "Account status is required.")]
        [StringLength(20)]
        public string? Status { get; set; }

        [Required(ErrorMessage = "Start Date is required.")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        /* ===================== PERSONAL INFORMATION ===================== */

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

        /* ===================== OPTIONAL / UI SUPPORT ===================== */

        // Used to display avatar image in profile panel
        [StringLength(255)]
        public string? ProfileImagePath { get; set; }

        // Cached tenure in months (optional)
        [Range(0, 600)]
        public int? TenureMonths { get; set; }
    }
}
