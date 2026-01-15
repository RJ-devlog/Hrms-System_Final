using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HRMS_System.Models.Training
{
    public class TrainingSession
    {
        [Key]
        public int Id { get; set; }

        [StringLength(150)]
        public string? Title { get; set; }

        [StringLength(2000)]
        public string? Description { get; set; } 

        [StringLength(300)]
        public string? TargetAudience { get; set; }

        [Required]
        public SessionType SessionType { get; set; } = SessionType.Mandatory;

        //NEW: schedule
        [Required, DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        //NEW: time window
        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        //NEW: provider & training type
        [Required, StringLength(150)]
        public string Provider { get; set; } = "Internal";

        [Required]
        public TrainingType TrainingType { get; set; } = TrainingType.Workshop;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<TrainingRecord> Records { get; set; } = new List<TrainingRecord>();
        [Required]
        public TrainingProgress Progress { get; set; } = TrainingProgress.NotStarted;
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
