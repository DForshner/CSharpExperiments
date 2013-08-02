using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

// The main class is SingleLayerPerceptron and there are simple unit tests in the SingleLayerPerceptronTests class.
namespace SingleLayerPerceptron
{
    [TestClass]
    public class SingleLayerPerceptronTests
    {
        SingleLayerPerceptron testPreceptron;

        [TestInitialize]
        public void TestInitialize()
        {
            this.testPreceptron = new SingleLayerPerceptron();
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))] // Assert
        public void Classify_WhenClassifyPreceptronThatHasNotBeenTrained_ExpectException()
        {
            // Act
            testPreceptron.Classify(new TestCase(new int[] {}));     
        }

        [TestMethod]
        public void GetError_WhenTrainSimpleSet_ExpectZeroTrainingError()
        {
            //Arrange
            var trainingSet = new List<TrainingCase>()
                {
                    new TrainingCase(new int[] { 0 }, 0),
                    new TrainingCase(new int[] { 1 }, 1),
                };
            
            testPreceptron.Train(trainingSet);
            // Act
            double error = testPreceptron.CalculateTotalError(trainingSet);
            // Assert
            Assert.AreEqual(0, error);
        }

        [TestMethod]
        public void GetError_WhenTrainSqaure_ExpectErrorToBeZero()
        {
            //Arrange
            testPreceptron.Train(GetSquareShapeTrainingSet());
            var trainingSet = GetSquareShapeTrainingSet();
            // Act
            double error = testPreceptron.CalculateTotalError(trainingSet);
            // Assert
            Assert.AreEqual(0, error);
        }

        [TestMethod]
        public void Classify_WhenTrainSquareAndTestSquare_ExpectPredictTrue()
        {
            //Arrange
            testPreceptron.Train(GetSquareShapeTrainingSet());
            var testCase = GetSquareTestCase();
            // Act
            bool result = testPreceptron.Classify(testCase);
            // Assert
            Assert.AreEqual(true, result);
        }

        [TestMethod]
        public void Classify_WhenTrainSquareAndLearningRateIsTooSmall_ExpectPredictFalse()
        {
            //Arrange
            testPreceptron.LearningRate = 0.0000000000001;
            testPreceptron.Train(GetSquareShapeTrainingSet());
            var testCase = GetSquareTestCase();
            // Act
            bool result = testPreceptron.Classify(testCase);
            // Assert
            Assert.AreEqual(false, result);
        }

        private static TestCase GetSquareTestCase()
        {
            var testCase = new TestCase(new int[]{
                0, 0, 0, 0, 0, 
                0, 1, 1, 0, 0, 
                0, 1, 0, 1, 0, 
                0, 1, 1, 1, 0,
                0, 0, 0, 0, 0
            });
            return testCase;
        }

        private static List<TrainingCase> GetSquareShapeTrainingSet()
        {
            return new List<TrainingCase>()
                {
                    new TrainingCase(new int[] { // Small Square
                        0, 0, 0, 0, 0, 
                        0, 1, 1, 1, 0, 
                        0, 1, 0, 1, 0, 
                        0, 1, 1, 1, 0,
                        0, 0, 0, 0, 0
                    }, 1),
                    new TrainingCase(new int[] { // Big Square
                        1, 1, 1, 1, 1, 
                        1, 0, 0, 0, 1, 
                        1, 0, 0, 0, 1, 
                        1, 0, 0, 0, 1,
                        1, 1, 1, 1, 1
                    }, 1),
                    new TrainingCase(new int[] { // Small Triangle
                        0, 0, 0, 0, 0, 
                        0, 0, 0, 0, 0,
                        0, 0, 1, 0, 0, 
                        0, 1, 1, 1, 0,
                        0, 0, 0, 0, 0
                    }, 0),
                    new TrainingCase(new int[] { // Big Triangle
                        0, 0, 1, 0, 0, 
                        0, 0, 1, 0, 0, 
                        0, 1, 0, 1, 0, 
                        0, 1, 0, 1, 0,
                        1, 1, 1, 1, 1
                    }, 0),
                    new TrainingCase(new int[] { // diamond
                        0, 0, 1, 0, 0, 
                        0, 1, 0, 1, 0, 
                        1, 0, 0, 0, 1, 
                        0, 1, 0, 1, 0,
                        0, 0, 1, 0, 0
                    }, 0),
                };
        }

        [TestMethod]
        public void Classify_WhenTrainBAndTestA_ExpectFalse()
        {
            //Arrange
            testPreceptron.Train(GetLetterBTrainingSet());
            var testCase = new TestCase(new int[] { // A
                0, 1, 1, 0, 
                1, 0, 0, 1, 
                1, 1, 1, 1, 
                1, 0, 0, 1, 
                1, 0, 0, 1 
            });
            // Act
            bool result = testPreceptron.Classify(testCase);
            // Assert
            Assert.AreEqual(false, result);
        }

        // Semi Complete Classify 
        [TestMethod]
        public void Classify_WhenTrainBAndTestSemiCompleteB_ExpectTrue()
        {
            //Arrange
            testPreceptron.Train(GetLetterBTrainingSet());
            var testCase = new TestCase(new int[] { // B
                0, 1, 1, 0, 
                0, 0, 0, 1, 
                1, 1, 1, 0, 
                1, 0, 0, 1, 
                1, 1, 1, 0 
            });
            // Act
            bool result = testPreceptron.Classify(testCase);
            // Assert
            Assert.AreEqual(true, result);
        }

        private static List<TrainingCase> GetLetterBTrainingSet()
        {
            return new List<TrainingCase>()
                {
                    new TrainingCase(new int[] { // A
                        0, 1, 1, 0, 
                        1, 0, 0, 1, 
                        1, 1, 1, 1, 
                        1, 0, 0, 1, 
                        1, 0, 0, 1
                    }, 0),
                    new TrainingCase(new int[] { // B
                        1, 1, 1, 0, 
                        1, 0, 0, 1, 
                        1, 1, 1, 0, 
                        1, 0, 0, 1, 
                        1, 1, 1, 0
                    }, 1),
                    new TrainingCase(new int[] { // C
                        0, 1, 1, 1, 
                        1, 0, 0, 0, 
                        1, 0, 0, 0, 
                        1, 0, 0, 0, 
                        0, 1, 1, 1
                    }, 0),
                    new TrainingCase(new int[] { // D
                        1, 1, 1, 0, 
                        1, 0, 0, 1, 
                        1, 0, 0, 1, 
                        1, 0, 0, 1, 
                        1, 1, 1, 0
                    }, 0),
                    new TrainingCase(new int[] { // E
                        1, 1, 1, 1, 
                        1, 0, 0, 0, 
                        1, 1, 1, 0, 
                        1, 0, 0, 0, 
                        1, 1, 1, 1
                    }, 0),
                    new TrainingCase(new int[] { // F
                        1, 1, 1, 1, 
                        1, 0, 0, 0, 
                        1, 1, 1, 0, 
                        1, 0, 0, 0, 
                        1, 0, 0, 0
                    }, 0),
                };
        }
    }

    public struct TrainingCase
    {
        /// <summary>
        /// The training pattern.
        /// </summary>
        public int[] TrainingVector { get; private set; }

        /// <summary>
        /// Indicates if the training pattern represents the classification or not.
        /// </summary>
        public int DesiredValue { get; private set; }

        public TrainingCase(int[] trainingData, int desiredValue)
            : this()
        {
            this.TrainingVector = trainingData;
            this.DesiredValue = desiredValue;
        }
    }

    public struct TestCase
    {
        public int[] TestData { get; private set; }

        public TestCase(int[] testData)
            : this()
        {
            this.TestData = testData;
        }
    }

    /// <summary>
    /// A Rosenblatt style single layer perceptron.
    /// A simple feed forward neural network best used for binary classification problems.
    /// Linear classifiers can only be used on linearly separable problems.
    /// </summary>
    public class SingleLayerPerceptron
    {
        // The weight and bias values that define the behaviour of the perceptron.
        private double[] optimalWeights;
        private double optimalBias = 0.0;

        /// <summary>
        /// The amount that weights and bias are changed during each training epoch.
        /// If too small training may stop before finding the best optimal weight and bias.
        /// If too large the weight and bias may overshoot and then undershoot the optimal values.
        /// </summary>
        public double LearningRate { get; set; }

        /// <summary>
        /// Stop training after this many iterations.
        /// </summary>
        public int MaxTrainingEpochs { get; set; }

        /// <summary>
        /// Stop training if the training set error generated by the current weight/bias values is under this.
        /// </summary>
        public double TrainingErrorTarget { get; set; }

        public SingleLayerPerceptron()
        {
            this.LearningRate = 0.05;
            this.MaxTrainingEpochs = 500;
            this.TrainingErrorTarget = 0.001;
        }

        /// <summary>
        /// The method that computes the optimal values for the weights and the bias using training data.
        /// The training loop executes MaxTrainingEpochs times or until the desired error error target has been achieved. 
        /// </summary>
        public void Train(List<TrainingCase> trainingSet)
        {
            if (trainingSet == null)
                throw new Exception();

            if (!trainingSet.Any())
                throw new Exception();

            this.optimalWeights = new double[trainingSet[0].TrainingVector.Length];


            for (int currentTrainingEpoch = 0; currentTrainingEpoch < MaxTrainingEpochs; ++currentTrainingEpoch )
            {
                // In the online learning version the optimal weights and bias are updated after computing the output for a single training vector
                // instead of updating the weights after the outputs of all training vectors have been computed (batch learning).
                foreach (var trainingCase in trainingSet)
                {
                    // -1 (if output too large), 0 (output correct), or +1 (output too small)
                    int desiredVsComputedValueDelta = trainingCase.DesiredValue - CalculateOutput(trainingCase.TrainingVector);

                    /// TODO: Gradually decrease learning rate based on the current currentTrainingEpoch to prevent over/undershooting the optimal value.
                    CorrectWeight(trainingCase, desiredVsComputedValueDelta);
                    CorrectBias(desiredVsComputedValueDelta);
                }

                if (CalculateTotalError(trainingSet) < TrainingErrorTarget)
                    break; // The total error generated by the current optimal weights and bias is below the target error.
            }
        }

        /// <summary>
        /// Increase the weights by the learning rate * delta * the input if positive (the output was too large).
        /// Decrease the weights by the learning rate * delta * the input if negative (the output was too small).
        /// Do nothing if the delta is zero.
        /// </summary>
        private void CorrectWeight(TrainingCase trainingCase, int desiredVsComputedValueDelta)
        {
            for (int j = 0; j < this.optimalWeights.Length; ++j)
                this.optimalWeights[j] = this.optimalWeights[j] + (this.LearningRate * desiredVsComputedValueDelta * trainingCase.TrainingVector[j]);
        }

        /// <summary>
        /// Increase the bias by the learning rate * delta if positive (the output was too large).
        /// Decrease the bias by the learning rate * delta if negative (the output was too small).
        /// Do nothing if the delta is zero.
        /// </summary>
        private void CorrectBias(int desiredVsComputedValueDelta)
        {
            this.optimalBias = this.optimalBias + (this.LearningRate * desiredVsComputedValueDelta);
        }

        /// <summary>
        /// Performs a classification using the optimal weights and bias.
        /// </summary>
        public bool Classify(TestCase testCase)
        {
            if (optimalWeights == null)
                throw new Exception("Perceptron has not been trained");

            return (CalculateOutput(testCase.TestData) == 1);
        }

        /// <summary>
        /// Then the output (0 or 1) of the perceptron is computed using the current weights and bias.
        /// If the dot product is greater than 0.5, the output Y is 1; otherwise the output is 0.
        /// dotProduct = Sum(x[i] * weights[i] + bias
        /// </summary>
        private int CalculateOutput(int[] trainingVector)
        {
            double dotProduct = 0.0;

            for (int j = 0; j < trainingVector.Length; ++j)
                dotProduct += (trainingVector[j] * this.optimalWeights[j]); // Product of all inputs times their associated weights

            dotProduct += this.optimalBias;

            return StepFunction(dotProduct);
        }

        /// <summary>
        /// Simple method that computes the total error generated when the current optimal weights and bias 
        /// are used to compute the output values for a training case.
        /// Error = 0.5 * Sum( (desiredValue[i] - calculatedOutput[i] )^2 )
        /// If this is 0.0 it means that the training cases has been classified correctly.
        /// If this is 1 it means the training case was not predicted correctly.
        /// </summary>
        public double CalculateTotalError(IList<TrainingCase> trainingCases)
        {
            double SumOfSquaredError = 0.0;

            foreach (var trainingCase in trainingCases)
            {
                int output = CalculateOutput(trainingCase.TrainingVector);

                // Abs(DesiredValue - output) because (desired - output)^2 will always be 0, +1 or -1.
                SumOfSquaredError += (trainingCase.DesiredValue - output) * (trainingCase.DesiredValue - output);
            }

            return 0.5 * SumOfSquaredError;
        }

        /// <summary>
        /// The activation function.
        /// </summary>
        public int StepFunction(double dotProduct)
        {
            const double thresholdValue = 0.5;

            return (dotProduct > thresholdValue) ? 1 : 0;
        }
    }
}