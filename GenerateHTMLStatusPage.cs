using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GenerateHTMLStatusPage
{
	/// <summary>
	/// Takes in a list of items and generates an HTML page that is stored
	/// in the program's subdirectory.
	/// </summary>
    public sealed class ItemStatusPageGenerator
    {
        private static string Encode(string input)
        {
            StringBuilder sb = new StringBuilder(input);

            sb.Replace("&", "&amp;");
            sb.Replace("<", "&lt;");
            sb.Replace(">", "&gt;");
            sb.Replace("\"", "&quot;");
            sb.Replace("'", "&apos;");

            return sb.ToString();
        }

        public void Update(IEnumerable<Item> items)
        {
            if (!Directory.Exists("web"))
                Directory.CreateDirectory("web");

            using (StreamWriter op = new StreamWriter("web/item-status.html"))
            {
                op.WriteLine("<html>");
                op.WriteLine("   <head>");
                op.WriteLine("      <title>Item Status</title>");
                op.WriteLine("   </head>");
                op.WriteLine("   <body bgcolor=\"white\">");
                op.WriteLine("      <h1>Item Status</h1>");
                op.WriteLine("      <table width=\"100%\">");
                op.WriteLine("         <tr>");
                op.WriteLine("            <td bgcolor=\"black\"><font color=\"white\">Name</font></td><td bgcolor=\"black\"><font color=\"white\">Description</font></td><td bgcolor=\"black\"><font color=\"white\">Quantity</font></td>");
                op.WriteLine("         </tr>");

                foreach (var item in items)
                {
                    op.Write("         <tr><td>");
                    op.Write(Encode(item.Name));
                    op.Write("</td><td>");
                    op.Write(Encode(item.Description));
                    op.Write("</td><td>");
                    op.Write(item.Quantity);
                    op.WriteLine("</td></tr>");
                }

                op.WriteLine("         <tr>");
                op.WriteLine("      </table>");
                op.WriteLine("   </body>");
                op.WriteLine("</html>");
            }
        }
    }

    public class Item
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Quantity { get; set; }
    }

    public static class Program
    {
        public static void Main()
        {
            var generator = new ItemStatusPageGenerator();

            var items = new List<Item>() { 
                new Item() { Name = "A", Description = "Red A", Quantity = 2 }, 
                new Item() { Name = "B", Description = "Green B", Quantity = 4 },
                new Item() { Name = "C", Description = "Purple 'ish' C", Quantity = 8 },
                new Item() { Name = "D", Description = "Teal & Orange D", Quantity = 16 },
            };

            generator.Update(items);
        }
    }
}