using NationStatesSharp;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Example
{
    internal class Program
    {
        static List<string> dispatchIds = new List<string>();
        static List<string> dispatchResult = new List<string>();
        static RequestDispatcher requestDispatcher;

        private static async Task Main()
        {
            try
            {
                await ConsoleUI();
            }
            finally
            {
                requestDispatcher?.Shutdown();
            }
        }

        public static async Task ConsoleUI()
        {
            string uAgent = "";
            string nName = "";
            Console.Title = "Dispatch Backup Tool";
            // Introduction.
            Console.WriteLine("\n\nNationStates Dispatch Backup Tool\nVersion 0.0 (Development)\n--------\n\nPress Enter to continue...");
            Console.ReadKey();
            Console.WriteLine("\n\n");

            // Request uAgent and Nation Name
            Console.WriteLine("Enter the User Agent:");
            uAgent = Console.ReadLine();

            Console.WriteLine("Enter the Nation Name:");
            nName = Console.ReadLine();

            // Send Request
            ConfigureLogging();
            requestDispatcher = new RequestDispatcher(uAgent, Log.Logger);
            requestDispatcher.Start();
            await IdsRequest(uAgent, nName);
            await TextRequest();
            await File.WriteAllTextAsync("dispatches.txt", string.Join("", dispatchResult));
        }

        private static async Task IdsRequest(string uAgent, string nName)
        {
            /* --- Request ID's --- */
            // Requests 
            var request = new Request($"nation={nName}&q=dispatchlist", ResponseFormat.Xml);
            requestDispatcher.Dispatch(request);
            await request.WaitForResponseAsync();
            XDocument xml = request.GetResponseAsXml();

            /* --- Filter ID's --- */
            var nodes = xml.Descendants().Skip(1).First().Nodes();
            foreach (XNode node in nodes)
            {
                var element = node as XElement;
                var id = element?.FirstAttribute?.Value;
                dispatchIds.Add(id);
            };

        }

        private static async Task TextRequest()
        {
            /* --- GET Requests for ID's --- */
            for (var i = 0; i < dispatchIds.Count; i++)
            {
                var request = new Request($"q=dispatch;dispatchid={dispatchIds[i]}", ResponseFormat.Xml);
                requestDispatcher.Dispatch(request);
                await request.WaitForResponseAsync();
                XDocument xml = request.GetResponseAsXml();

                var dispatchContent = xml.Descendants().Skip(1).First().Elements().Where(e => e.Name == "TEXT").FirstOrDefault()?.Value;
                dispatchResult.Add($"\nDispatch {i} / {dispatchIds.Count}:\n");
                dispatchResult.Add(dispatchContent);
            }
        }


        private static void ConfigureLogging()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console(theme: SystemConsoleTheme.Literate).CreateLogger();
        }
    }
}