using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS_System.Models.Training
{
    public class TrainingSession
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required, StringLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Required, StringLength(300)]
        public string TargetAudience { get; set; } = string.Empty;

        [Required]
        public SessionType SessionType { get; set; } = SessionType.Mandatory;

        [Required, DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        [Required, StringLength(150)]
        public string Provider { get; set; } = "Internal";

        [Required]
        public TrainingType TrainingType { get; set; } = TrainingType.Workshop;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<TrainingRecord> Records { get; set; } = new List<TrainingRecord>();

        [Required]
        public TrainingProgress Progress { get; set; } = TrainingProgress.NotStarted;

        [NotMapped]
        public DateTime EndDateTime => StartDate.Date.Add(EndTime);

        [NotMapped]
        public bool CanBeCompleted => DateTime.Now >= EndDateTime;
    }

    public enum SessionType
    {
        Mandatory = 1,
        Optional = 2
    }

    public enum TrainingType
    {
        Workshop = 1,
        Online = 2,
        InPerson = 3
    }
}