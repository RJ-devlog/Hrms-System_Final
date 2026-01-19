namespace HRMS_System.Models.PromotionML
{
    // This class represents ONE ROW of training data
    // Each instance = 1 employee record used by the ML model
    public class PromotionTrainingRow
    {
        /* ===================== INPUT FEATURES ===================== */

        // How long the employee has worked in the company (experience)
        public float TenureMonths { get; set; }

        // Percentage of absent days (0.0 - 1.0)
        public float AbsenceRate { get; set; }

        // Percentage of late days (0.0 - 1.0)
        public float LateRate { get; set; }

        // Total number of trainings attended
        public float TrainingCount { get; set; }

        // Total number of certifications earned
        public float CertificationCount { get; set; }

        // Average evaluation score (example: 4.2)
        public float AvgEvaluationScore { get; set; }

        /* ===================== OUTPUT / LABEL ===================== */

        // TRUE  = employee was promoted
        // FALSE = employee was not promoted
        // This is the TARGET VARIABLE that Random Forest learns
        public bool WasPromoted { get; set; }
    }
}
