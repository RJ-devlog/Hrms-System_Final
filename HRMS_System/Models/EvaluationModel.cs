using System;
using System.ComponentModel.DataAnnotations;

namespace HRMS_System.Models.Evaluation
{
    public class EvaluationModel
    {
        [Required]
        public int? UserId { get; set; }

        [Required]
        [StringLength(20)]
        public string? Period { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime EvaluationDate { get; set; } = DateTime.Today;

        /* ===== PERFORMANCE RATINGS ===== */

        [Range(1, 5, ErrorMessage = "Work Quality rating is required.")]
        public int? WorkQuality { get; set; }

        [Range(1, 5, ErrorMessage = "Productivity rating is required.")]
        public int? Productivity { get; set; }

        [Range(1, 5, ErrorMessage = "Teamwork rating is required.")]
        public int? Teamwork { get; set; }

        [Range(1, 5, ErrorMessage = "Attendance rating is required.")]
        public int? Attendance { get; set; }

        [Range(1, 5, ErrorMessage = "Communication rating is required.")]
        public int? Communication { get; set; }

        /* ===== COMMENTS ===== */

        [StringLength(1000)]
        public string? Strengths { get; set; }

        [StringLength(1000)]
        public string? Improvements { get; set; }

        [StringLength(1000)]
        public string? Comments { get; set; }

        /* ===== OVERALL ===== */
        [StringLength(10)]
        public string? OverallRating { get; set; }
    }
}
