using System;
using System.IO;

// Watches files in the temp directory for changes.
// TODO: Rework to work correctly with the GUI thread in WPF.

namespace DirectoryWatcher
{
    public class Program
    {
        static void Main()
        {
            var watcher = new ConsoleDirectoryWatcher("C:\\Temp");
            Console.ReadKey();
        }
    }

    public class ConsoleDirectoryWatcher : DirectoryWatcher 
    {
        public ConsoleDirectoryWatcher(String pathToMonitor) : base(pathToMonitor) {}
        public override void createdHandler() { Console.WriteLine("Created"); }
        public override void modifiedHandler() { Console.WriteLine("Modified"); }
        public override void deletedHandler() { Console.WriteLine("Deleted"); }
    }

    public class DirectoryWatcher
    {
        public DirectoryWatcher(String pathToMonitor)
        {
            var watcher = new FileSystemWatcher(pathToMonitor);
            watcher.Created += createdEvent;
            watcher.Changed += modifiedEvent;
            watcher.Deleted += deletedEvent;

            watcher.EnableRaisingEvents = true;
        }

        private void createdEvent(Object sender, FileSystemEventArgs e) 
        {
            var callback = new createdHandlerCallback(createdHandler);
            callback.Invoke();
        }
       
        delegate void createdHandlerCallback();
        public virtual void createdHandler() {}

        private void modifiedEvent(Object sender, FileSystemEventArgs e) 
        {
            var callback = new createdHandlerCallback(modifiedHandler);
            callback.Invoke();
        }

        delegate void modifiedHandlerCallback();
        public virtual void modifiedHandler() {}

        private void deletedEvent(Object sender, FileSystemEventArgs e) 
        {
            var callback = new createdHandlerCallback(deletedHandler);
            callback.Invoke();
        }

        delegate void deletedHandlerCallback();
        public virtual void deletedHandler() {}
    }
}