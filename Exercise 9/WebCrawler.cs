using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

namespace Exercise_9
{
    class WebCrawler
    {
        private static ConcurrentDictionary<Uri, bool> visitedUrls =
            new ConcurrentDictionary<Uri, bool>();

        private static BlockingCollection<Uri> resultUrls =
            new BlockingCollection<Uri>(new ConcurrentQueue<Uri>());

        private static BlockingCollection<KeyValuePair<Uri, int>> frontier =
            new BlockingCollection<KeyValuePair<Uri, int>>(new ConcurrentQueue<KeyValuePair<Uri, int>>());

        private static int maxLevel;
        private static string searchTerm;

        private Thread crawler;
        private WebClient wc;
        private bool done;

        public WebCrawler(string aSearchTerm, Queue<Uri> urls, int maximumLevel)
           : this()
        {
            searchTerm = aSearchTerm;
            maxLevel = maximumLevel;
            foreach (Uri url in urls)
                frontier.Add(new KeyValuePair<Uri, int>(url, 0));
        }

        public WebCrawler()
        {
            wc = new WebClient();
            wc.Headers.Add(HttpRequestHeader.UserAgent, "User-Agent");
            //wc.Headers.Add(HttpRequestHeader.From, "bhp@easv.dk");
            crawler = new Thread(new ThreadStart(crawl));
            crawler.Start();
        }

        private void crawl()
        {
            done = false;
            while (!done)
            {
                try
                {
                    KeyValuePair<Uri, int> keyValue = frontier.Take();
                    Uri page = keyValue.Key;
                    int level = keyValue.Value;
                    resolvePage(page, level);
                }
                catch (ThreadInterruptedException)
                {
                    // nothing special to do, just wake up !
                }
            }

        }

        private void resolvePage(Uri page, int level)
        {
            if (visitedUrls.ContainsKey(page)) return;

            visitedUrls[page] = true;

            try
            {
                // try to download the webpage. Throws exception if the url is bad.
                string webPage = wc.DownloadString(page.ToString());

                // If the webpage does not contain the search term, just skip it.
                if (!webPage.ToLower().Contains(searchTerm.ToLower())) return;

                // Page contains search term, so add it to the results.
                resultUrls.Add(page);

                if (level < maxLevel)
                {
                    // look for links in the webpage
                    var urlTagPattern = new Regex(@"<a.*?href\s*=\s*[""'](?<url>.*?)[""'].*?</a>", RegexOptions.IgnoreCase);

                    var urls = urlTagPattern.Matches(webPage);

                    Uri baseUrl = new UriBuilder(page.Host).Uri;

                    foreach (Match url in urls)
                    {
                        try
                        {
                            string newUrl = url.Groups["url"].Value;
                            Uri result;
                            Uri.TryCreate(baseUrl, newUrl, out result);


                            // if the url is for a webpage, add it to the frontier for crawling
                            if (result.HostNameType == UriHostNameType.Dns && !visitedUrls.ContainsKey(result))
                            {
                                frontier.Add(new KeyValuePair<Uri, int>(result, level + 1));
                            }
                        }
                        catch
                        {
                            //just continue with the next found link...
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Unable to load page
                visitedUrls[page] = false;
            }
        }

        public void Stop()
        {
            done = true;
            if (crawler.ThreadState == ThreadState.WaitSleepJoin)
                crawler.Interrupt();
        }

        public int getFrontierSize()
        {
            return frontier.Count;
        }

        public Dictionary<Uri, bool> GetVisitedUrls()
        {
            return new Dictionary<Uri, bool>(visitedUrls);
        }

        public Queue<Uri> GetResultUrls()
        {
            return new Queue<Uri>(resultUrls);
        }
    }
}
