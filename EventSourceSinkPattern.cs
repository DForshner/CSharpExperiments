using System;

// The event source/sink pattern decouples event sources from event handlers.  
// This allows event handlers to be defined at runtime and for multiple event handlers
// to be configured for a given event.

namespace EventSourceSinkPattern
{
    #region Event delegates

    public delegate void EventAEventHandler(EventAEventArgs e);
	public delegate void EventBEventHandler(EventBEventArgs e);

    #endregion

    #region Event arguments

    public class EventAEventArgs : EventArgs
	{
		public int Value {get; private set;}

        public Nullable<bool> Result {get; set;}

        public EventAEventArgs(int value)
		{
			this.Value = value;
		}
	}

	public class EventBEventArgs : EventArgs
	{
        public string Value {get; private set;}

		public EventBEventArgs(string value)
		{
			this.Value = value;
		}
	}

    #endregion

    /// <summary>
	/// Event Sink Singleton
	/// </summary>
	public static class EventSinkSingleton
	{
		public static event EventAEventHandler EventA;
		public static event EventBEventHandler EventB;

		public static void InvokeEventA(EventAEventArgs e)
		{
			if (EventA != null)
				EventA(e);
		}

		public static void InvokeEventB(EventBEventArgs e)
		{
			if (EventB != null)
				EventB(e);
		}

		public static void Reset()
		{
			EventA = null;
			EventB = null;
		}
	}

    #region Event handlers

    public class EventAHandler
    {
        // Connect the event to the event handler
        public static void Initialize()
        {
            EventSinkSingleton.EventA += new EventAEventHandler(Handle);
        }

        private static void Handle(EventAEventArgs e)
        {
            e.Result = (e.Value > 0);
        }
    }

    public class EventBHandler
    {
        // Connect the event to the event handler
        public static void Initialize()
        {
            EventSinkSingleton.EventB += new EventBEventHandler(Handle);
        }

        private static void Handle(EventBEventArgs e)
        {
            Console.WriteLine("EventBHandler - Value from event B source: " + e.Value);
        }
    }

    public class EventBHandler2
    {
        // Connect the event to the event handler
        public static void Initialize()
        {
            EventSinkSingleton.EventB += new EventBEventHandler(Handle);
        }

        private static void Handle(EventBEventArgs e)
        {
            Console.WriteLine("EventBHandler2 - I like : " + e.Value);
        }
    }

    #endregion

    #region Event emitters/sources

    public class EventASource
    {
        public void SimulateExternalEvent() { Emit(5); }

        private void Emit(int value)
        {
            var args = new EventAEventArgs(value);
            EventSinkSingleton.InvokeEventA(args);
            Console.WriteLine("EventASource - Result of event A handler: " + args.Result);
        }
    }

    public class EventBSource
    {
        public void SimulateExternalEvent() { Emit("Cats"); }

        private void Emit(string value)
        {
            var args = new EventBEventArgs(value);
            EventSinkSingleton.InvokeEventB(args);
        }
    }

    #endregion

    public static class Program
    {
        static void Main()
        {
            Console.WriteLine("\nEvent Source/Sink Pattern");

            var eventA = new EventASource();
            var eventB = new EventBSource();

            Console.WriteLine("\n1 - Events with no handlers configured\n");
            eventA.SimulateExternalEvent();
            eventB.SimulateExternalEvent();

            EventAHandler.Initialize();
            EventBHandler.Initialize();

            Console.WriteLine("\n2 - Events with handlers configured\n");
            eventA.SimulateExternalEvent();
            eventB.SimulateExternalEvent();

            EventBHandler2.Initialize();

            Console.WriteLine("\n3 - Events with multiple handlers configured for one event\n");
            eventB.SimulateExternalEvent();

            Console.WriteLine("\n4 - Events after event sink reset\n");
            EventSinkSingleton.Reset();
            eventA.SimulateExternalEvent();
            eventB.SimulateExternalEvent();
        }
    }
}
