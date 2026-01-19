namespace HRMS_System.Models.PromotionML
{
    // This class represents the RESULT of the ML prediction
    public class PromotionPredictionOutput
    {
        // Final classification result
        // TRUE  = predicted promotable
        // FALSE = predicted not promotable
        public bool PredictedLabel { get; set; }

        // Confidence level (0.0 - 1.0)
        // Example: 0.82 means 82% confidence
        public float Probability { get; set; }

        // Raw score from the model (used internally)
        public float Score { get; set; }
    }
}
