using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;

// Example showing building XML and querying using LINQ to XML

namespace LINQToXMLExample 
{
    public class Program
    {
        static void Main()
        {
            var processes = QueryXML();

            var pids = ParsePIDS(processes);

            foreach(var pid in pids)
            {
                Console.WriteLine(pid);
            }
        }

        private static IEnumerable<int> ParsePIDS(XDocument processes)
        {
            return processes
                .Element("Processes")
                .Elements("Process") 
                // OR: .Descendants("Process")

                .Where(x => x.Attribute("Name").Value != "svchost")
                .OrderBy(x => x.Attribute("Name").Value)
                .Select(x => (int)x.Attribute("PID"));
        }

        private static XDocument QueryXML()
        {
            var processes = Process
                .GetProcesses()
                .Select(x =>
                    new XElement("Process",
                        new XAttribute("Name", x.ProcessName),
                        new XAttribute("PID", x.Id)
                    )
                );

            return new XDocument
            (
                new XElement("Processes", processes)
            );
        }
    }
}
