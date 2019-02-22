using Google.Apis.Services;
using System;
using System.Collections.Generic;
using System.Net;

namespace Exercise_9
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Write("Enter search string: ");
            string searchString = Console.ReadLine().Trim();

            WebClient wc = new WebClient();
            Queue<Uri> initialQueue = InitialSearch(searchString);
            Console.WriteLine("Initial queue size = " + initialQueue.Count);

            // Initiate the first crawler
            List<WebCrawler> crawlers = new List<WebCrawler>();
            crawlers.Add(new WebCrawler(searchString, initialQueue, 6));

            // Initiate 9 more worker crawlers
            for (int i = 1; i < 10; i++)
            {
                crawlers.Add(new WebCrawler());
            }

            Console.WriteLine("\nCrawling...");

            bool done = false;
            while (!done)
            {

                Console.WriteLine("\nPress any key to see status (ESC to stop)...");

                // if ESC is pressed then stop
                if (Console.ReadKey().Key == ConsoleKey.Escape) done = true;

                // Prints out the size of the frontier queue
                Console.WriteLine("\nFrontier size = " + crawlers[0].getFrontierSize());
            }

            // Stop all crawlers
            for (int i = 0; i < 10; i++)
            {
                crawlers[i].Stop();
            }

            // Get all visited urls
            Dictionary<Uri, bool> visitedUrls = crawlers[0].GetVisitedUrls();

            PrintResults(crawlers[0].GetVisitedUrls(), crawlers[0].GetResultUrls());
        }

        private static void PrintResults(Dictionary<Uri, bool> visitedUrls, Queue<Uri> results)
        {
            // Print out the number of visited urls
            Console.WriteLine("\nNumber of visited urls = " + visitedUrls.Count);

            // Print out the result urls containing the search string found by the web crawlers 

            foreach (Uri url in results)
                Console.WriteLine(url);

            // Print out the number of results found by the crawlers
            Console.WriteLine("\nNumber of found results = " + results.Count);
        }

        private static Queue<Uri> InitialSearch(string searchString)
        {
            string apiKey = "AIzaSyDs1yfYiY1gVtAC-40B5rpNgFLQqmbuN04";
            string cx = "015351139142946479508:3lihax8uevs";
            string query = searchString;

            var svc = new Google.Apis.Customsearch.v1.CustomsearchService(new BaseClientService.Initializer { ApiKey = apiKey });
            var listRequest = svc.Cse.List(query);

            listRequest.Cx = cx;
            var search = listRequest.Execute();

            Queue<Uri> links = new Queue<Uri>();
            foreach (var result in search.Items)
            {
                links.Enqueue(new UriBuilder(result.Link).Uri);
            }
            return links;
        }
    }
}
