using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// A greedy algorithm for minimizing the weighted sum of completion times.
//
// Jobs as read from a text file with the format:
// [number_of_jobs]
// [job_1_weight] [job_1_length]
// [job_2_weight] [job_2_length]

namespace ScheduleJobsToMinimizeWeightedSumOfCompletionTime
{
    public class Job
    {
        public int Weight { get; private set; }
        public int Length { get; private set; }
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
        public Job Job { get; private set; }
        public int CompletionTime { get; private set; }
        public int WeightedCompletionTime { get { return Job.Weight * CompletionTime; } }

        public CompletedJob(Job job, int completionTime)
        {
            Job = job;
            CompletionTime = completionTime;
        }
    }

    public class Program 
    {
        //static void Main(string[] args)
        //{
        //    var data = ReadFile();

        //    var jobs = ParseJobsFromFile(data);

        //    var completedJobs = SimulateRunningJobs(jobs);

        //    foreach(var job in completedJobs)
        //    {
        //        Console.WriteLine("Weight: " + job.Job.Weight + 
        //            " Lenth: " + job.Job.Length + 
        //            " Diff: " + job.Job.Difference +
        //            " Ratio: " + Math.Round(job.Job.Ratio, 4) +
        //            " CT: " + job.CompletionTime +
        //            " WCT: " + job.WeightedCompletionTime
        //            );
        //    }

        //    var sumOfWeightedCompletionTimes = completedJobs.Sum(x => (long)x.WeightedCompletionTime);

        //    Console.WriteLine("\n\nWeighted Sum of Completion Times: " + sumOfWeightedCompletionTimes);

        //    Console.WriteLine("Done");
        //    Console.ReadKey();
        //}

        private static List<CompletedJob> SimulateRunningJobs(IEnumerable<Job> jobs)
        {
            var currentTime = 0;
            var completedJobs = jobs

                // Method 1 - Schedule jobs by decreasing order of difference.  Break ties by picking the
                // job with the higher weight first.
                //.OrderByDescending(x => x.Difference).ThenByDescending(x => x.Weight)

                // Method 2 - Schedule jobs by decreasing order of ratio.  Break ties by picking the
                // job with the higher weight first.
                .OrderByDescending(x => x.Ratio).ThenByDescending(x => x.Weight)

                .Select(job =>
                {
                    currentTime += job.Length;
                    return new CompletedJob(job, currentTime);
                })
                .ToList(); // Materialize so only evaluated a single time.

            return completedJobs;
        }

        private static IEnumerable<Job> ParseJobsFromFile(string data)
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
                throw new Exception("Number of jobs processed does not match number of jobs listed in file header.");
            }

            return jobs;
        }

        private static string ReadFile()
        {
            var data = string.Empty;
            try
            {
                using (var sr = new StreamReader("../../jobs.txt"))
                {
                    data = sr.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
            }
            return data;
        }
    }
}
