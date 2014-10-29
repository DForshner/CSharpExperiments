using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// One possible way of performing business rules with an imperative shell accessing the database
// and pushing the results down into business rules in a functional core.

namespace FunctionalCoreImperativeShellBusinessRules 
{
    #region IMPERATIVE SHELL - Single path (avoid conditional logic).  Wires together Services, Entities, and Value Objects

    public static class Program 
    {
        public static void Main()
        {
            var cts = new CancellationTokenSource();
            System.Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };
            MainAsync(cts.Token).Wait();
        }

        public static async Task MainAsync(CancellationToken token)
        {
            // Create with IoC
            var rules = new BusinessRules(new FakePhaseRepo(), new FakeTaskRepo());

            var project = new Project("A", "B", 100);

            Task.Run(async () => { 
                var results = rules.ValidateAll(project).Result;
                if (results.Any())
                    throw new Exception("Failed validation.");
            }, token);
        }
    }

    /// <summary>
    /// Pass data from services (I/O) to business logic (functional).
    /// </summary>
    public class BusinessRules
    {
        private readonly IPhaseRepo phaseRepo;
        private readonly ITaskRepo taskRepo;

        public BusinessRules(IPhaseRepo phaseRepe, ITaskRepo taskRepo)
        {
            this.phaseRepo = phaseRepo;
            this.taskRepo = taskRepo;
        }

        public async Task<IEnumerable<ValidationResult>> ValidateAll(Project project)
        {
            var rules = new List<Func<ValidationResult>>();

            var phases = await phaseRepo.GetPhasesFromFakeDb();
            var phaseRuleParams = new PhaseMustExistRuleParams(phases, project);
            rules.Add(() => new PhaseMustExistRule().Validate(phaseRuleParams));

            var tasks = taskRepo.GetTasksFromFakeDb();
            var taskRuleParams = new TaskMustExistRuleParams(tasks, project);
            rules.Add(() => new TaskMustExistRule().Validate(taskRuleParams));

            var valueRule = new EstimatedValueMustBePositive(project);
            rules.Add(() => valueRule.Validate());

            return rules.Select(x => x.Invoke()); 
        }
    }

    #endregion

    #region SERVICES - Interact with outside world (I/O)

    public interface IPhaseRepo
    {
        Task<string[]> GetPhasesFromFakeDb();
    }

    public class FakePhaseRepo : IPhaseRepo
    {
        public async Task<string[]> GetPhasesFromFakeDb()
        {
            return await Task.FromResult(new[] { "TaskA", "TaskB", "TaskC" });
        }
    }

    public interface ITaskRepo
    {
        string[] GetTasksFromFakeDb();
    }

    public class FakeTaskRepo : ITaskRepo
    {
        public string[] GetTasksFromFakeDb()
        {
            return new[] { "TaskA", "TaskB", "TaskC" };
        }
    }

    #endregion

    #region IMMUTABLE VALUE OBJECTS - Carry data around.  No mutating methods or setters.

    public class Project
    {
        public readonly string Task;
        public readonly string Phase;
        public readonly decimal EstimatedValue;

        public Project(string phase, string task, decimal estimatedValue)
        {
            Phase = phase;
            Task = task;
            EstimatedValue = estimatedValue;
        }
    }

    public class ValidationResult
    {
        public readonly bool IsValid;
        public readonly string Reason;

        private ValidationResult(bool isValid, string reason)
        {
            this.IsValid = isValid;
            this.Reason = reason;
        }

        public static ValidationResult Valid
        {
            get { return new ValidationResult(true, null); }
        }

        public static ValidationResult Invalid(string reason)
        {
            if (reason == null) { throw new ArgumentNullException(); }
            return new ValidationResult(false, reason);
        }
    }

    #endregion

    #region FUNCTIONAL CORE - Entities that makes decisions based on data passed in and pass out results.

    public interface IRule<TParam>
    {
        ValidationResult Validate(TParam parameters);
    }

    public class PhaseMustExistRuleParams
    {
        public readonly Project Project;
        public readonly IReadOnlyCollection<string> Phases;

        public PhaseMustExistRuleParams(IReadOnlyCollection<string> phases, Project project)
        {
            this.Project = project;
            this.Phases = phases;
        }
    }

    public class PhaseMustExistRule : IRule<PhaseMustExistRuleParams>
    {
        public ValidationResult Validate(PhaseMustExistRuleParams parameters)
        {
            if (parameters.Phases.Contains(parameters.Project.Phase))
                return ValidationResult.Valid;
                
            return ValidationResult.Invalid("Phase does not exist.");
        }
    }

    /// <summary>
    /// No need for mocks/stubs!
    /// </summary>
    [TestClass]
    public class PhaseMustExistRuleTests
    {
        [TestMethod]
        public void WhenDoesNotExist_ExpectInvalid()
        {
            var project = new Project("Phase", "Task", 100);
            var parameters = new PhaseMustExistRuleParams(new[] { "A", "B", "C" }, project);
            var sut = new PhaseMustExistRule();
            var result = sut.Validate(parameters);
            Assert.IsFalse(result.IsValid);
        }

        [TestMethod]
        public void WhenExists_ExpectValid()
        {
            var project = new Project("Phase", "Task", 100);
            var parameters = new PhaseMustExistRuleParams(new[] { "A", "Phase", "C" }, project);
            var sut = new PhaseMustExistRule();
            var result = sut.Validate(parameters);
            Assert.IsTrue(result.IsValid);
        }
    }

    public class TaskMustExistRuleParams
    {
        public readonly Project Project;
        public readonly IReadOnlyCollection<string> Tasks;

        public TaskMustExistRuleParams(IReadOnlyCollection<string> tasks, Project project)
        {
            this.Project = project;
            this.Tasks = tasks;
        }
    }

    public class TaskMustExistRule : IRule<TaskMustExistRuleParams>
    {
        public ValidationResult Validate(TaskMustExistRuleParams parameters)
        {
            if (parameters.Tasks.Contains(parameters.Project.Task))
                return ValidationResult.Valid;
            return ValidationResult.Invalid("Task does not exist.");
        }
    }

    /// <summary>
    /// No need for mocks/stubs!
    /// </summary>
    [TestClass]
    public class TaskMustExistRuleTests
    {
        [TestMethod]
        public void WhenDoesNotExist_ExpectInvalid()
        {
            var project = new Project("Phase", "Task", 100);
            var parameters = new TaskMustExistRuleParams(new[] { "A", "B", "C" }, project);
            var sut = new TaskMustExistRule();
            var result = sut.Validate(parameters);
            Assert.IsFalse(result.IsValid);
        }

        [TestMethod]
        public void WhenExists_ExpectValid()
        {
            var project = new Project("Phase", "Task", 100);
            var parameters = new TaskMustExistRuleParams(new[] { "A", "Task", "C" }, project);
            var sut = new TaskMustExistRule();
            var result = sut.Validate(parameters);
            Assert.IsTrue(result.IsValid);
        }
    }

    /// <summary>
    /// This example combines the rule and the parameter together.
    /// </summary>
    public class EstimatedValueMustBePositive
    {
        readonly Project _project;

        public EstimatedValueMustBePositive(Project project)
        {
            _project = project;
        }

        public ValidationResult Validate()
        {
            if (_project.EstimatedValue < 0)
                return ValidationResult.Invalid("Value must be positive.");
            return ValidationResult.Valid;
        }
    }

    /// <summary>
    /// No need for mocks/stubs!
    /// </summary>
    [TestClass]
    public class EstimatedValueMustBePositiveTests
    {
        [TestMethod]
        public void WhenNegative_ExpectInvalid()
        {
            var project = new Project("Phase", "Task", -1);
            var sut = new EstimatedValueMustBePositive(project);
            var result = sut.Validate();
            Assert.IsFalse(result.IsValid);
        }

        [TestMethod]
        public void WhenExists_ExpectValid()
        {
            var project = new Project("Phase", "Task", 100);
            var sut = new EstimatedValueMustBePositive(project);
            var result = sut.Validate();
            Assert.IsTrue(result.IsValid);
        }
    }

    #endregion
}