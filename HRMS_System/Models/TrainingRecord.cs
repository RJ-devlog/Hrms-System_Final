using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRMS_System.Models;

namespace HRMS_System.Models.Training
{
    public class TrainingRecord
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public UserInformationModel? User { get; set; }

        [Required]
        public int TrainingSessionId { get; set; }

        [ForeignKey(nameof(TrainingSessionId))]
        public TrainingSession? Session { get; set; }

        [Required, StringLength(150)]
        public string Provider { get; set; } = "Internal";

        // Generated server-side only when training is completed
        [StringLength(80)]
        public string? CertificationId { get; set; }

        public DateTime? DateCompleted { get; set; }

        [StringLength(50)]
        public string? Duration { get; set; }

        [Required]
        public TrainingProgress Progress { get; set; } = TrainingProgress.NotStarted;

        public DateTime? ValidUntil { get; set; }
    }

    public enum TrainingProgress
    {
        NotStarted = 1,
        InProgress = 2,
        Completed = 3
    }
}