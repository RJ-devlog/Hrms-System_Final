using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS_System.Models
{
    public class TrainingandSeminar : IValidatableObject
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Please select an employee.")]
        [Display(Name = "Employee")]
        public int UserInformationId { get; set; }

        [ForeignKey(nameof(UserInformationId))]
        public UserInformationModel? UserInfo { get; set; }

        [Required(ErrorMessage = "Please enter the seminar or training title.")]
        [StringLength(150, ErrorMessage = "The title cannot exceed 150 characters.")]
        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select the date accomplished.")]
        [DataType(DataType.Date)]
        [Display(Name = "Date Accomplished")]
        public DateTime DateAccomplished { get; set; } = DateTime.Today;

        [Display(Name = "Points")]
        public int Points { get; set; }

        [Display(Name = "Certificate Count")]
        public int CertificateCount { get; set; }

        [NotMapped]
        public string EmployeeDisplay =>
            UserInfo == null
                ? string.Empty
                : $"{UserInfo.EmployeeNumber} - {UserInfo.FirstName} {UserInfo.LastName}".Trim();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Points != 2 && Points != 4 && Points != 6)
            {
                yield return new ValidationResult(
                    "Points must be 2, 4, or 6.",
                    new[] { nameof(Points) });
            }
        }
    }
}