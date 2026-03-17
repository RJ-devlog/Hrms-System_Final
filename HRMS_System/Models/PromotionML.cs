namespace HRMS_System.Models.PromotionML
{
    public class PromotionWeightedScoreResult
    {
        public float AttendanceScore { get; set; }
        public float TrainingScore { get; set; }
        public float PerformanceScore { get; set; }
        public float OverallScore { get; set; }
        public bool IsRecommended { get; set; }
    }
}