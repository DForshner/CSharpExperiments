using System;

public class Program
{
    public static void Main()
    {
        MyEventClass myEvent = new MyEventClass();

        MyHandlerClass myHandler = new MyHandlerClass(myEvent);

        myEvent.InvokeEvent("Foo");
        myEvent.InvokeEvent("Bar");

        return;
    }
}

/// <summary>
/// This class is responsible for firing the event.
/// </summary>
public class MyEventClass
{
    public delegate void MyEventHandlerDelegate(object sender, MyEventArgs fe);

    // Public event based on the delegate. 
    public event MyEventHandlerDelegate MyEvent;

    public void InvokeEvent(string description)
    {
        // Create the EventArgs so parameters can be passed.
        MyEventArgs eventArgs = new MyEventArgs(description);

        MyEvent(this, eventArgs);
    }
}

/// <summary>
/// This class is responsible for handling the event after it's fired.
/// </summary>
public class MyHandlerClass
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="myEventClass">The class that will fire the event.</param>
    public MyHandlerClass(MyEventClass myEventClass)
    {
        // Configure a delegate with the function that will be executed when the event is raised.
        myEventClass.MyEvent += new MyEventClass.MyEventHandlerDelegate(RespondToTheEventBeingRaised);
    }

    void RespondToTheEventBeingRaised(object sender, MyEventArgs fe)
    {
        Console.WriteLine("Sender Class: {0}, Description: {1}", sender.ToString(), fe.description);
    }
}


/// <summary>
/// This class is responsible for passing information to the handler class.
/// </summary>
public class MyEventArgs : EventArgs
{
    public string description;

    public MyEventArgs(string description)
    {
        this.description = description;
    }
}