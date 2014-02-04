using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

// Scaffolding code for a timer and timer manager class.
// The timer manager class uses a dedicated thread that periodically checks
// collections of timers stored with priorities to see which ones have elapsed.
//
// TODO: 
// -- De-couple the timer and timer manager classes.
// -- The TimerChangeEntryQueue should be factored out into a generic class.
// -- There is a bug that is preventing timers from being GCed correctly.
//
// Compiled: Visual Studio 2013

namespace MultiThreadedPriorityTimerManager
{
	public enum TimerPriority : int
	{
		None = -1,
		EveryTick = 0,
		OneSecond = 1,
		TenSeconds = 2,
		ThirtySeconds = 3,
		OneMinute = 4 
	}

	public abstract class Timer
	{
		public DateTime Next { get; private set; }
		public TimeSpan Delay {get; set;}
		public TimeSpan Interval {get; set;}

		public bool IsRunning {get; private set;}
		public int TimesToRun { get; set; }
		public int TimesRun { get; set;}

		public bool CanBeScheduledAgain
		{
			get { return this.TimesToRun != 0 && (this.TimesRun < this.TimesToRun); }
		}

		public bool IsReadyForProcessing(DateTime now)
		{
			return !this.IsQueuedForProcessing && now > this.Next;
		}

		public bool IsQueuedForProcessing { get; private set; }

		public void SetQueuedForProcessing()
		{ 
			Debug.Assert(!IsQueuedForProcessing, "Expected not queued for processing.");
			IsQueuedForProcessing = true;
			++TimesRun;
		}

		public void ClearQueuedForProcessing()
		{
			Debug.Assert(IsQueuedForProcessing, "Expected queued for processing.");
			IsQueuedForProcessing = false;
		}

		private TimerManager manager;

		/// <summary>
		/// Single shot
		/// </summary>
		public Timer(TimerManager manager, TimeSpan delay) 
			: this(manager, delay, TimeSpan.Zero, 1) {}

		/// <summary>
		/// Multiple times with an interval 
		/// </summary>
		public Timer(TimerManager manager, TimeSpan interval, int timesToRun) 
			: this(manager, interval, interval, timesToRun - 1) {}

		public Timer(TimerManager manager, TimeSpan delay, TimeSpan interval, int timesToRun)
		{
			Debug.Assert(timesToRun > 0, "Expected at least one times to run.");
			Debug.Assert(delay != TimeSpan.Zero, "Expected non zero delay.");
			Debug.Assert(!(delay == TimeSpan.Zero && timesToRun == 1), "Expected delay when running a single time.");

			this.manager = manager;
			this.Delay = delay;
			this.Interval = interval;
			this.TimesToRun = timesToRun;

			if (timesToRun == 1)
				DesiredPriority = ComputePriority(delay);
			else
				DesiredPriority = ComputePriority(interval);

			Debug.Assert(DesiredPriority != TimerPriority.None, "Expected priority to be set.");
		}

		/// <summary>
		/// Determine how often to check if a timer has elapsed.
		/// Increasing the granularity makes the timer more accurate.
		/// </summary>
		public static TimerPriority ComputePriority(TimeSpan ts)
		{
			if (ts >= TimeSpan.FromMinutes(10.0))
				return TimerPriority.OneMinute; // +/- 10%

			if (ts >= TimeSpan.FromMinutes(5.0))
				return TimerPriority.ThirtySeconds; // +/- 10%

			if (ts >= TimeSpan.FromMinutes(1.0))
				return TimerPriority.TenSeconds; // +/- 16%

			if (ts >= TimeSpan.FromSeconds(10.0)) // +/- 10%
				return TimerPriority.OneSecond;

			return TimerPriority.EveryTick;
		}

		public void SetupInitialTriggerTime(DateTime now)
		{
			Next = now + Delay;
			TimesRun = 0;
		}

		public void UpdateNextTriggerTime(DateTime now)
		{
			Next = now + Interval;
		}

		public TimerPriority CurrentPriority {get; set;}

		private TimerPriority desiredPriority;
		public TimerPriority DesiredPriority
		{
			get
			{
				return desiredPriority;
			}
			set
			{
				desiredPriority = value;

				if (desiredPriority != CurrentPriority)
				{
					if (IsRunning)
						manager.ChangeTimerPriority(this, desiredPriority);
				}
			}
		}

		public void Start()
		{
			if (!IsRunning)
			{
				manager.AddTimer(this);
				IsRunning = true;
			}
		}

		public void Stop()
		{
			if (IsRunning)
			{
				manager.RemoveTimer(this);
				IsRunning = false;
			}
		}

		public abstract void OnTick();
	}

	public class TimerManager
	{
        private Thread thread;
        private TimerChangeEntryQueue priorityChangeQueue = new TimerChangeEntryQueue();
		private List<PriorityLevel> PriorityLevels;

        /// <summary>
        /// Callback action when timers have elapsed.
        /// </summary>
        private Action notifyTimersHaveElapsed;

		private AutoResetEvent checkIfTimersHaveElapsedSignal = new AutoResetEvent(false);
		private bool enabled = true;
		private int maxTimersPerSlice;
		private Queue<Timer> elapsedTimers = new Queue<Timer>();

        #region NESTED PRIVATE CLASSES

        private class PriorityLevel
		{
			public DateTime Next { get; set; }
			public TimeSpan Delay { get; set; }
			public List<Timer> Timers { get; set; }

			public PriorityLevel()
			{
				Timers = new List<Timer>();
			}

			public void SetNextTriggerTime(DateTime now)
			{
				Debug.Assert(now >= Next);
				Next = now + Delay;
			}

			// If the last priority level exceeded its alloted time then this time slice is done.
			public bool IsNextTriggerTimeStillInFuture(DateTime now)
			{
				return (now < Next);
			}
		}

	    private class TimerChangeEntry
	    {
		    public Timer timer { get; set; }
		    public TimerPriority newPriority { get; set; }
		    public bool isAdd { get; set; }
	    }

	    /// <summary>
	    /// Thread safe queue that uses object pooling.
	    /// </summary>
	    private class TimerChangeEntryQueue
	    {
		    private Object synclock = new Object();
		    private Queue<TimerChangeEntry> m_ChangeQueue = new Queue<TimerChangeEntry>();
		    private Stack<TimerChangeEntry> instancePool = new Stack<TimerChangeEntry>();

		    public void Change(Timer t, TimerPriority priority, bool isAdd)
		    {
			    lock (synclock)
				    m_ChangeQueue.Enqueue(Get(t, priority, isAdd));
		    }

		    public TimerChangeEntry Dequeue()
		    {
			    TimerChangeEntry result;

			    lock (synclock)
				    result = m_ChangeQueue.Dequeue();

			    return result;
		    }

		    public bool Empty { get { return m_ChangeQueue.Count == 0; } }

		    public void Free(TimerChangeEntry entry)
		    {
                entry.isAdd = false;
                entry.timer = null;
                entry.newPriority = TimerPriority.None;

			    instancePool.Push(entry);
		    }

		    private TimerChangeEntry Get(Timer timer, TimerPriority newPriority, bool isAdd)
		    {
			    TimerChangeEntry entry;

			    if (instancePool.Count > 0)
				    entry = instancePool.Pop();
			    else
				    entry = new TimerChangeEntry();

			    entry.timer = timer;
			    entry.newPriority = newPriority;
			    entry.isAdd = isAdd;

			    return entry;
		    }
	    }

        #endregion

		public TimerManager(int maxTimersPerSlice, Action notifyTimersToProcess)
		{
			this.maxTimersPerSlice = maxTimersPerSlice;
			this.notifyTimersHaveElapsed = notifyTimersToProcess;

			// The priority levels are stored in list corresponding to the TimerPriority Enum
			this.PriorityLevels = new List<PriorityLevel>() {
				new PriorityLevel() { Delay = TimeSpan.Zero }, // TimerPriority.EveryTick 
				new PriorityLevel() { Delay = TimeSpan.FromSeconds( 1.0 ) },
				new PriorityLevel() { Delay = TimeSpan.FromSeconds( 10.0 ) },
				new PriorityLevel() { Delay = TimeSpan.FromSeconds( 30.0 ) },
				new PriorityLevel() { Delay = TimeSpan.FromMinutes( 1.0 ) } // TimerPriority.OneMinute 
			};
		}

		public void AddTimer(Timer t)
		{
			Debug.Assert(t.DesiredPriority != TimerPriority.None);
			priorityChangeQueue.Change(t, t.DesiredPriority, true);
			checkIfTimersHaveElapsedSignal.Set();
		}

		public void RemoveTimer(Timer t)
		{
			priorityChangeQueue.Change(t, TimerPriority.None, false);
			checkIfTimersHaveElapsedSignal.Set();
		}

		public void ChangeTimerPriority(Timer t, TimerPriority newPriority)
		{
			priorityChangeQueue.Change(t, newPriority, false);
			checkIfTimersHaveElapsedSignal.Set();
		}

        /// <summary>
        /// Disables main loop and blocks until thread to exist.
        /// Returns number of unfinished timers.
        /// </summary>
		public int ShutDown() 
        { 
            enabled = false; 
			thread.Join();

            var count = 0;
            foreach (var priority in PriorityLevels)
                foreach (var timer in priority.Timers)
                    ++count;

            return count;
        }

        /// <summary>
        /// Starts main loop that monitors timers.
        /// </summary>
        public void Start()
        {
			this.thread = new Thread(new ThreadStart(QueueElapsedTimers));
			this.thread.Start();
        }

        /// <summary>
        /// Executes the OnTick() methods of any timers which have elapsed.
        /// </summary>
		public void Slice()
		{
			lock (elapsedTimers)
			{
				int i = 0;
				while (i < maxTimersPerSlice && elapsedTimers.Count != 0)
				{
					var timer = elapsedTimers.Dequeue();
					timer.OnTick();
					timer.ClearQueuedForProcessing();
					Debug.Assert(timer.TimesRun <= timer.TimesToRun, "Expected times run to be less than or equal to max times to run.");
					++i;
				}
			}
		}

        /// <summary>
        /// Processes timer priority changes which have been stored in the queue.
        /// </summary>
		private void ProcessPriorityChangeQueue()
		{
			while (!priorityChangeQueue.Empty)
			{
				var tce = priorityChangeQueue.Dequeue();
				var timer = tce.timer;

				// If the timer is already scheduled de-schedule it.
				if (timer.CurrentPriority != TimerPriority.None)
				{
					PriorityLevels[(int)tce.timer.CurrentPriority].Timers.Remove(timer);
					timer.CurrentPriority = TimerPriority.None;
				}

				// If the timer is being added update its trigger time and index.
				if (tce.isAdd)
					timer.SetupInitialTriggerTime(DateTime.Now);

				// If the timer has a new priority schedule it.
				if (tce.newPriority != TimerPriority.None)
				{
					PriorityLevels[(int)tce.newPriority].Timers.Add(timer);
					timer.CurrentPriority = tce.newPriority;
				}

				priorityChangeQueue.Free(tce);
			}
		}

		private void Set() { checkIfTimersHaveElapsedSignal.Set(); }

		/// <summary>
		/// Queue any timers that have elapsed.
		/// </summary>
		private void QueueElapsedTimers()
		{
			DateTime now;
			bool timersToProcessFlag;

			while (enabled)
			{
				ProcessPriorityChangeQueue();

				timersToProcessFlag = false;

				// For each priority
				foreach (var priority in PriorityLevels)
				{
					now = DateTime.Now;

					// If the next time this level will be triggered is in the future skip it.
					if (priority.IsNextTriggerTimeStillInFuture(now))
						break;

					Debug.Assert(now >= priority.Next, String.Format("Expected current time {0} to be after priority trigger time {1}.", now, priority.Next));

					priority.SetNextTriggerTime(now);

					// For each timer in the current priority level
					foreach (var timer in priority.Timers)
					{
						if (timer.IsReadyForProcessing(now))
						{
							timer.SetQueuedForProcessing();

							lock (elapsedTimers)
								elapsedTimers.Enqueue(timer);

							timersToProcessFlag = true;

							if (timer.CanBeScheduledAgain)
								timer.UpdateNextTriggerTime(now);
							else
								timer.Stop();

							Debug.Assert(timer.TimesRun <= timer.TimesToRun, "Expected times run to be less than or equal to max times to run.");
						}
					}
				}

				if (timersToProcessFlag)
					notifyTimersHaveElapsed();

				// Wait 10ms before checking if any timers have elapsed
				checkIfTimersHaveElapsedSignal.WaitOne(10, false);
			}
		}
	}

    #region EXAMPLES OF TIMERS

    public class DisplayByTimer : Timer
	{
		public DisplayByTimer(TimerManager timerManager, TimeSpan delay, TimeSpan interval, int timesToRun) : 
			base(timerManager, delay, interval, timesToRun)
		{
			Start();
		}

		public override void OnTick()
		{
			Console.WriteLine("TimeSpan: {0} Priority: {1} TimesToRun: {2} TimesRun {3}", 
				Interval.ToString(), CurrentPriority.ToString(), TimesToRun, TimesRun);
		}
	}

	public delegate void TimerCallback();

	public class DelayCallTimer : Timer
	{
		private TimerCallback m_Callback;

		public TimerCallback Callback { get { return m_Callback; } }

		public DelayCallTimer(TimerManager manager, TimeSpan delay, TimeSpan interval, int count, TimerCallback callback)
			: base(manager, delay, interval, count)
		{
			m_Callback = callback;
		}

		public override void OnTick()
		{
			if (m_Callback != null)
				m_Callback();
		}

		/// <summary>
		/// Factory method to create delay call
		/// </summary>
		public static Timer Create(TimerManager manager, TimeSpan delay, TimeSpan interval, int count, TimerCallback callback)
		{
			var t = new DelayCallTimer(manager, delay, interval, count, callback);
			t.Start();
			return t;
		}
	}

    #endregion

    #region DEMO CODE

    public class DisplayByCallback 
	{
		public void DisplaySomething()
		{
			Console.WriteLine("Executing callback.");
		}
	}

	public static class Program 
	{
		private static AutoResetEvent activity = new AutoResetEvent(true);
		private static void NotifyActivityOccured() { activity.Set(); }
        private static bool enabled = true;

		public static void Main()
		{
			var timerManager = new TimerManager(100, NotifyActivityOccured);
            timerManager.Start();

			var timers = new List<Timer>();
			timers.Add(new DisplayByTimer(timerManager, new TimeSpan(0, 0, 1), new TimeSpan(0, 0, 1), 60));
			timers.Add(new DisplayByTimer(timerManager, new TimeSpan(0, 0, 10), new TimeSpan(0, 0, 10), 6));
			timers.Add(new DisplayByTimer(timerManager, new TimeSpan(0, 0, 30), new TimeSpan(0, 0, 30), 2));
			timers.Add(new DisplayByTimer(timerManager, new TimeSpan(0, 1, 0), new TimeSpan(0, 1, 0), 1));

			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine("Press ESC key to shut down the timer manager.");
			Console.WriteLine("Press SPACE key to trigger 5 second one shot.");
			Console.ForegroundColor = ConsoleColor.White;

            while (enabled)
			{
			    // Block for 100ms waiting for timer to elapse
                activity.WaitOne(100, false);

				timerManager.Slice();

				if (Console.KeyAvailable)
				{
					var key = Console.ReadKey(true).Key;

					if (key == ConsoleKey.Spacebar)
					{
						var display = new DisplayByCallback();
						Console.WriteLine("Registering callback");
						DelayCallTimer.Create(timerManager, TimeSpan.FromSeconds(5.0), TimeSpan.Zero, 1, 
                            new TimerCallback(display.DisplaySomething));
					}

					if (key == ConsoleKey.Escape)
					{
						var unfinished = timerManager.ShutDown();
                        Console.WriteLine("Shutting down timer manager with {0} incomplete timers", unfinished);

						timers.Clear();
                        enabled = false;
					}
				}
			}

			Console.ForegroundColor = ConsoleColor.Red;
            timerManager.ShutDown();

			Console.WriteLine("[Press enter to exit]");
			Console.ReadLine();
		}
	}

    #endregion
}