using HRMS_System.Models.PromotionML;

namespace HRMS_System.Services
{
    public class PromotionWeightedScoringService
    {
        public PromotionWeightedScoreResult Calculate(PromotionTrainingRow row)
        {
            var attendanceScore = CalculateAttendanceScore(row);
            var trainingScore = CalculateTrainingScore(row);
            var performanceScore = CalculatePerformanceScore(row);

            var overallScore =
                (attendanceScore * 0.20f) +
                (trainingScore * 0.30f) +
                (performanceScore * 0.50f);

            return new PromotionWeightedScoreResult
            {
                AttendanceScore = attendanceScore,
                TrainingScore = trainingScore,
                PerformanceScore = performanceScore,
                OverallScore = overallScore,
                IsRecommended = overallScore >= 92f
            };
        }

        private float CalculateAttendanceScore(PromotionTrainingRow row)
        {
            float score = 100f;

            // Penalty for absences and late records
            score -= row.AbsenceRate * 100f * 0.60f;
            score -= row.LateRate * 100f * 0.40f;

            // Tenure bonus: up to 12 bonus points
            var tenureBonus = Math.Min(row.TenureMonths, 24f) / 24f * 12f;
            score += tenureBonus;

            return Clamp(score, 0f, 100f);
        }

        private float CalculateTrainingScore(PromotionTrainingRow row)
        {
            // You can adjust these targets later
            float trainingPart = Math.Min(row.TrainingCount / 10f, 1f) * 40f;
            float certificationPart = Math.Min(row.CertificationCount / 15f, 1f) * 60f;

            return Clamp(trainingPart + certificationPart, 0f, 100f);
        }

        private float CalculatePerformanceScore(PromotionTrainingRow row)
        {
            // AvgEvaluationScore assumed from 0 to 5
            float score = (row.AvgEvaluationScore / 5f) * 100f;
            return Clamp(score, 0f, 100f);
        }

        private float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}