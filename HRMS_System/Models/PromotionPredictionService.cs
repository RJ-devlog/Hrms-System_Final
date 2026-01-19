using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.ML;
using Microsoft.ML.Data;
using HRMS_System.Models.PromotionML;


namespace HRMS_System.Services
{
    // This service handles:
    // 1. Training the Random Forest model
    // 2. Saving the model
    // 3. Loading the model
    // 4. Making predictions
    public class PromotionPredictionService
    {
        // MLContext is required by ML.NET
        // Seed ensures reproducible results (important for thesis)
        private readonly MLContext _ml = new(seed: 69);

        // File path where the trained model is saved
        private readonly string _modelPath;

        // Constructor: inject where the model will be stored
        public PromotionPredictionService(string modelPath)
        {
            _modelPath = modelPath;
        }

        /* ===================== TRAINING ===================== */

        // This method trains the Random Forest model and saves it
        public void TrainAndSaveModel(IEnumerable<PromotionTrainingRow> rows)
        {
            // Convert C# list into ML.NET data format
            var data = _ml.Data.LoadFromEnumerable(rows);

            // Build the ML pipeline
            var pipeline =
                // Combine all numeric inputs into one vector
                _ml.Transforms.Concatenate(
                    "Features",
                    nameof(PromotionTrainingRow.TenureMonths),
                    nameof(PromotionTrainingRow.AbsenceRate),
                    nameof(PromotionTrainingRow.LateRate),
                    nameof(PromotionTrainingRow.TrainingCount),
                    nameof(PromotionTrainingRow.CertificationCount),
                    nameof(PromotionTrainingRow.AvgEvaluationScore)
                )


                // Apply Random Forest (FastForest)
                .Append(_ml.BinaryClassification.Trainers.FastForest(
                    labelColumnName: nameof(PromotionTrainingRow.WasPromoted),
                    featureColumnName: "Features",
                    numberOfTrees: 200,              // More trees = more accuracy
                    numberOfLeaves: 20,              // Controls complexity
                    minimumExampleCountPerLeaf: 10   // Prevents overfitting
                ));

            // Train the model
            var model = pipeline.Fit(data);

            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(_modelPath)!);

            // Save the trained model to disk
            _ml.Model.Save(model, data.Schema, _modelPath);
        }

        /* ===================== PREDICTION ===================== */

        // This method predicts promotion for ONE employee
        public PromotionPredictionOutput Predict(PromotionTrainingRow input)
        {
            // If model doesn't exist, prediction is impossible
            if (!File.Exists(_modelPath))
                throw new FileNotFoundException("Model not found. Train the model first.");

            // Load trained model
            var model = _ml.Model.Load(_modelPath, out _);

            // Create prediction engine (single prediction)
            var engine = _ml.Model.CreatePredictionEngine
                <PromotionTrainingRow, PromotionPredictionOutput>(model);

            // Run prediction
            return engine.Predict(input);
        }
    }
}
