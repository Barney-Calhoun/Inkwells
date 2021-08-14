using static Globals.ConsoleMethods;
using static Globals.Constants;
using static Globals.WebMethods;
using Globals;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MostBrutalNoReply
{
    class Program
    {
        const string ForumHeader = "Forum";
        const string MostBrutalNoReplyThreadsHeader = "Most Brutal No-Reply Threads";
        const string AllForumsName = "All Forums";
        const string ThreadCacheFile = "ThreadCache.json";
        const string ResultFile = "MostBrutalNoReply.txt";

        static ChromeDriver Driver;

        static void Main()
        {
            var domain = ReadDomain();

            var userNameOrEmail = ReadStringInput("Enter user name or email (leave blank to skip):");

            var userPassword = string.IsNullOrEmpty(userNameOrEmail)
                ? string.Empty
                : ReadPassword();

            InitChromeDriver(out Driver);

            ExitSignal.InitSignal(Driver);

            PrintResults(FindMostBrutalNoReply(domain, userNameOrEmail, userPassword));

            Driver.Quit();

            Console.WriteLine(SuccessMessage);
            
            WaitForExit();
        }

        static List<string> GetForumUrls(string parentForumUrl, List<string> forumUrls = null)
        {
            forumUrls ??= new List<string>();

            Driver.GoToUrlWithRetries(parentForumUrl, DefaultRefreshBy);

            var forumLinks = Driver
                .FindElementsByXPath("//h3[@class='node-title']/a")
                .Select(link => link.GetAttribute("href"))
                .Where(link => !link.Contains("rules-faq.27"))
                .ToArray();

            foreach (var forumLink in forumLinks)
            {
                forumUrls.Add(forumLink);
                forumUrls = GetForumUrls(forumLink, forumUrls);
            }

            return forumUrls;
        }

        static int GetIdFromUrl(string url)
        {
            int startIndex = url.LastIndexOf('.') + 1;

            return int.Parse(url.TrimEnd('/')[startIndex..]);
        }

        static ThreadCache FindMostBrutalNoReply(string domain, string userNameOrEmail, string userPassword)
        {
            var baseUrl = $"https://incels.{domain}";

            Driver.ExecuteScript("window.open('');");
            var windowHandle1 = Driver.WindowHandles.ElementAt(0);
            var windowHandle2 = Driver.WindowHandles.ElementAt(1);
            Driver.SwitchTo().Window(windowHandle1);

            if (!string.IsNullOrEmpty(userNameOrEmail) && !string.IsNullOrEmpty(userPassword))
            {
                Driver.LoginWithRetries(
                    $"{baseUrl}/login/login",
                    userNameOrEmail,
                    userPassword,
                    By.Name("login"),
                    By.Name("password"),
                    By.ClassName("button--icon--login"),
                    DefaultRefreshBy);
            }

            var threadCache = File.Exists(ThreadCacheFile)
                ? JsonConvert.DeserializeObject<ThreadCache>(File.ReadAllText(ThreadCacheFile))
                : new ThreadCache();
            threadCache.ResetForumsById();

            var forumUrls = GetForumUrls(baseUrl);

            foreach (var forumUrl in forumUrls)
            {
                Driver.GoToUrlWithRetries(forumUrl + "?order=reply_count&direction=desc", DefaultRefreshBy);

                var forumId = GetIdFromUrl(forumUrl);
                var forumName = Driver.FindElementByClassName("p-title-value").Text;

                threadCache.ForumsById[forumId] = new Forum(forumId, forumName);

                var page = 0;

                while (true)
                {
                    page++;

                    var threadUrls = Driver
                        .FindElementsByXPath("//li[@class='structItem-startDate']/a")
                        .Select(url => url.GetAttribute("href"))
                        .ToArray();

                    Driver.SwitchTo().Window(windowHandle2);

                    var zeroReplies = false;

                    foreach (var threadUrl in threadUrls)
                    {
                        var threadId = GetIdFromUrl(threadUrl);

                        if (!threadCache.ThreadIds.Contains(threadId))
                        {
                            Console.WriteLine($"{forumName} | Page {page} | {threadUrl.TrimEnd('/')}");

                            Driver.GoToUrlWithRetries(threadUrl, DefaultRefreshBy);

                            var postDateTimes = Driver
                                .FindElementsByXPath("//ul[contains(@class,'message-attribution-main')]//time[@class='u-dt']")
                                .Select(dateTime => DateTime.Parse(dateTime.GetAttribute("datetime")))
                                .ToArray();

                            if (postDateTimes.Length > 1)
                            {
                                var threadDate = postDateTimes[0];
                                var firstReplyDate = postDateTimes[1];
                                var difference = firstReplyDate - threadDate;
                                var comparison = difference
                                    .CompareTo(threadCache.ForumsById[forumId].MaxThreadAndFirstReplyDifference);
                                if (comparison > 0)
                                {
                                    threadCache.ForumsById[forumId].MaxThreadAndFirstReplyDifference = difference;
                                    threadCache.ForumsById[forumId].MostBrutalNoReplyThreadUrls = new List<string>
                                    {
                                        threadUrl
                                    };
                                }
                                else if (comparison == 0)
                                {
                                    threadCache.ForumsById[forumId].MostBrutalNoReplyThreadUrls.Add(threadUrl);
                                }

                                threadCache.ThreadIds.Add(threadId);
                            }
                            else if (page > 1)
                            {
                                zeroReplies = true;
                                break;
                            }
                        }
                    }

                    Driver.SwitchTo().Window(windowHandle1);

                    var nextButtons = Driver.FindElementsByClassName("pageNav-jump--next");

                    if (!zeroReplies && nextButtons.Count > 0)
                    {
                        Driver.GoToUrlWithRetries(
                            nextButtons.First().GetAttribute("href"),
                            DefaultRefreshBy);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            threadCache.UpdatetMostBrutalNoReplyThreadUrls();

            File.WriteAllText(ThreadCacheFile, JsonConvert.SerializeObject(threadCache, Formatting.Indented));

            return threadCache;
        }

        static void PrintResults(ThreadCache threadCache)
        {
            var forums = threadCache.ForumsById.Values;

            var maxForumNameLength = forums.Max(f => f.Name.Length);
            if (AllForumsName.Length > maxForumNameLength || ForumHeader.Length > maxForumNameLength)
            {
                maxForumNameLength = AllForumsName.Length > ForumHeader.Length
                    ? AllForumsName.Length
                    : ForumHeader.Length;
            }

            var maxThreadUrlLength = forums.Max(f => f.MostBrutalNoReplyThreadUrls.Max(t => t.Length));
            if (MostBrutalNoReplyThreadsHeader.Length > maxThreadUrlLength)
            {
                maxThreadUrlLength = MostBrutalNoReplyThreadsHeader.Length;
            }

            var hyphens = new string('-', maxForumNameLength + maxThreadUrlLength + 3) + Environment.NewLine;

            var resultFormat = $"{{0, -{maxForumNameLength}}} | {{1}}{Environment.NewLine}";

            var result = string.Empty;
            result += hyphens;
            result += string.Format(resultFormat, ForumHeader, MostBrutalNoReplyThreadsHeader);

            for (int i = 0; i < threadCache.MostBrutalNoReplyThreadUrls.Count; i++)
            {
                if (i == 0)
                {
                    result += hyphens;
                }
                result += string.Format(
                    resultFormat,
                    i == 0 ? AllForumsName : string.Empty,
                    threadCache.MostBrutalNoReplyThreadUrls[i]);
            }

            foreach (var forum in forums)
            {
                result += hyphens;
                for (int i = 0; i < forum.MostBrutalNoReplyThreadUrls.Count; i++)
                {
                    result += string.Format(
                        resultFormat,
                        i == 0 ? forum.Name : string.Empty,
                        forum.MostBrutalNoReplyThreadUrls[i]);
                }
            }

            result += hyphens.TrimEnd();

            Console.WriteLine(result);

            File.WriteAllText(ResultFile, result);
        }
    }
}
