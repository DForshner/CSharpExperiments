using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// Minimize the weighted sum of completion times
// for a set of jobs with length and weight (value/priority).
//
// Two different greedy algorithms are included.
// 1) Take the job with the lowest difference between the weight and the length each pass.
// 2) Take the job with the lowest ratio between the weight and the length each pass.
//
// Jobs as read from a text file with the format:
// [number_of_jobs]
// [job_1_weight] [job_1_length]
// [job_2_weight] [job_2_length]

namespace ScheduleJobsToMinimizeWeightedSumOfCompletionTime
{
    public class Job
    {
        public readonly int Weight;

        public readonly int Length;

        public int Difference { get { return Weight - Length; } }

        public decimal Ratio { get { return (decimal)Weight / (decimal)Length; } }

        public Job(int weight, int length)
        {
            Weight = weight;
            Length = length;
        }
    }

    public class CompletedJob
    {
        public readonly Job Job;

        public readonly int CompletionTime;

        public int WeightedCompletionTime { get { return Job.Weight * CompletionTime; } }

        public CompletedJob(Job job, int completionTime)
        {
            Job = job;
            CompletionTime = completionTime;
        }
    }

    public class Program 
    {
        static void Main(string[] args)
        {
            var jobs = ParseJobsFromFile(ReadFile());

            SimulateRunningJobsByDecreasingDifference(jobs);

            SimulateRunningJobsByDecreasingRatio(jobs);

            Console.WriteLine("Done");
            Console.ReadKey();
        }

        private static void SimulateRunningJobsByDecreasingDifference(IReadOnlyCollection<Job> jobs)
        {
            var completeJobs = ScheduleJobsByDecreasingDifference(jobs);

            foreach (var job in completeJobs)
            {
                Console.WriteLine("Weight: " + job.Job.Weight +
                    " Length: " + job.Job.Length +
                    " Difference: " + job.Job.Difference +
                    " Ratio: " + Math.Round(job.Job.Ratio, 4) +
                    " CT: " + job.CompletionTime +
                    " WCT: " + job.WeightedCompletionTime
                    );
            }

            var sumOfWeightedCompletionTimes = completeJobs.Sum(x => (long)x.WeightedCompletionTime);
            Console.WriteLine("\n\nWeighted Sum of Completion Times (Difference Method): " + sumOfWeightedCompletionTimes);
        }

        /// <summary>
        /// Schedule jobs by decreasing order of difference.  Break ties by picking the
        /// job with the higher weight first.
        /// </summary>
        private static List<CompletedJob> ScheduleJobsByDecreasingDifference(IEnumerable<Job> jobs)
        {
            var currentTime = 0;
            return jobs
                .OrderByDescending(x => x.Difference).ThenByDescending(x => x.Weight)
                .Select(job =>
                {
                    currentTime += job.Length;
                    return new CompletedJob(job, currentTime);
                })
                .ToList(); // Materialize so only evaluated a single time.
        }

        private static void SimulateRunningJobsByDecreasingRatio(IReadOnlyCollection<Job> jobs)
        {
            var completeJobs = ScheduleJobsByDecreasingRatio(jobs);

            foreach (var job in completeJobs)
            {
                Console.WriteLine("Weight: " + job.Job.Weight +
                    " Length: " + job.Job.Length +
                    " Difference: " + job.Job.Difference +
                    " Ratio: " + Math.Round(job.Job.Ratio, 4) +
                    " CT: " + job.CompletionTime +
                    " WCT: " + job.WeightedCompletionTime
                    );
            }

            var sumOfWeightedCompletionTimes = completeJobs.Sum(x => (long)x.WeightedCompletionTime);
            Console.WriteLine("\n\nWeighted Sum of Completion Times (Ratio Method): " + sumOfWeightedCompletionTimes);
        }

        /// <summary>
        /// Schedule jobs by decreasing order of ratio.  Break ties by picking the
        /// job with the higher weight first.
        /// </summary>
        private static List<CompletedJob> ScheduleJobsByDecreasingRatio(IEnumerable<Job> jobs)
        {
            var currentTime = 0;
            return jobs
                .OrderByDescending(x => x.Ratio).ThenByDescending(x => x.Weight)

                .Select(job =>
                {
                    currentTime += job.Length;
                    return new CompletedJob(job, currentTime);
                })
                .ToList(); // Materialize so only evaluated a single time.
        }

        private static IReadOnlyCollection<Job> ParseJobsFromFile(string data)
        {
            var lines = data.Split('\n');

            // First line is file is the number of jobs
            var numJobs = Int32.Parse(lines.First());

            var jobs = lines
                .Select((x, i) => new { JobDetails = x, Index = i })
                // Include all non-empty lines after the first line
                .Where(x => x.Index != 0 && x.JobDetails != "")
                .Select(x =>
                {
                    var details = x.JobDetails.Split(' ');
                    return new Job(Int32.Parse(details[0]), Int32.Parse(details[1]));
                });

            if (jobs.Count() != numJobs)
            {
                throw new Exception("The number of jobs processed does not match number of jobs specified in the file header.");
            }

            return jobs.ToList().AsReadOnly();
        }

        private static string ReadFile()
        {
            try
            {
                using (var sr = new StreamReader("../../../jobs.txt"))
                {
                    return sr.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The file could not be read.");
                Console.WriteLine(e.Message);
            }
            return string.Empty;
        }
    }
}
