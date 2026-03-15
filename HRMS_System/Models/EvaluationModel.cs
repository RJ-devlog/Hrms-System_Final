using HRMS_System.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS_System.Models.Evaluation
{
    public class EvaluationModel
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Employee is required.")]
        public int? UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public UserInformationModel? User { get; set; }

        public int? EvaluatorUserId { get; set; }

        [StringLength(100)]
        public string? EvaluatorName { get; set; }

        public AccessRole? EvaluatorRole { get; set; }

        [Required(ErrorMessage = "Evaluation period is required."   )]
        [Range(1, 12, ErrorMessage = "Evaluation period (month) is invalid.")]
        public int Period { get; set; } = DateTime.Today.Month; // store month as integer (1-12)

        [Required(ErrorMessage = "Evaluation date is required.")]
        [DataType(DataType.Date)]
        public DateTime EvaluationDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Evaluation year is required.")]
        [Range(1990, 3000, ErrorMessage = "Evaluation year is invalid.")]
        public int EvaluationCurrentYear { get; set; } = DateTime.Today.Year;

        [Required(ErrorMessage = "Work Quality rating is required.")]
        [Range(1, 5)]
        public int? WorkQuality { get; set; }

        [Required(ErrorMessage = "Productivity rating is required.")]
        [Range(1, 5)]
        public int? Productivity { get; set; }

        [Required(ErrorMessage = "Teamwork rating is required.")]
        [Range(1, 5)]
        public int? Teamwork { get; set; }

        [Required(ErrorMessage = "Attendance rating is required.")]
        [Range(1, 5)]
        public int? Attendance { get; set; }

        [Required(ErrorMessage = "Communication rating is required.")]
        [Range(1, 5)]
        public int? Communication { get; set; }

        [StringLength(1000)]
        public string? Strengths { get; set; }

        [StringLength(1000)]
        public string? Improvements { get; set; }

        [StringLength(1000)]
        public string? Comments { get; set; }

        public string? OverallRating { get; set; }

        // Optional: list of months for dropdown (for Razor)
        [NotMapped]
        public List<string> Months { get; set; } =
            Enumerable.Range(1, 12)
                .Select(m => new DateTime(2000, m, 1).ToString("MMMM"))
                .ToList();
    }
}