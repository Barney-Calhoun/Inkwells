using static Globals.ConsoleMethods;
using static Globals.Constants;
using static Globals.FileMethods;
using static Globals.WebMethods;
using Globals;
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

        const int ActionRestart = 1;
        const int ActionContinueWithoutLogin = 2;
        const int ActionExit = 3;
        static readonly SortedDictionary<int, string> LoginErrorActions = new()
        {
            { ActionRestart, "Restart." },
            { ActionContinueWithoutLogin, "Continue without logging in." },
            { ActionExit, "Exit." }
        };

        static ChromeDriver Driver;

        static void Main()
        {
        Start:
            var domain = ReadDomain();

            InitChromeDriver(out Driver);

            ExitSignal.InitSignal(Driver);

            if (Driver.CaptchaDetected(domain.Value))
            {
                if (ReadConfirmation("Restart?", TrueAction))
                {
                    Console.Clear();
                    Driver.Quit();
                    goto Start;
                }
                else
                {
                    Driver.Quit();
                    return;
                }
            }

            var userNameOrEmail = ReadStringInput("Enter user name or email (leave blank to skip):");

            var userPassword = string.IsNullOrEmpty(userNameOrEmail)
                ? string.Empty
                : ReadPassword();

            if (!string.IsNullOrEmpty(userNameOrEmail))
            {
                var baseUrl = Driver.GetBaseForumUrl(domain.Value, DefaultRefreshBy);

                Driver.Login(
                    $"{baseUrl}?login",
                    userNameOrEmail,
                    userPassword,
                    By.Name("login"),
                    By.Name("password"),
                    By.ClassName("button--icon--login"),
                    DefaultRefreshBy);

                var loginErrors = Driver.FindElements(By.ClassName("blockMessage--error"));

                if (loginErrors.Count > 0)
                {
                    Console.WriteLine(loginErrors.First().Text);

                    var action = ReadSelection(LoginErrorActions);

                    switch (action)
                    {
                        case ActionRestart:
                            Console.Clear();
                            Driver.Quit();
                            goto Start;
                        case ActionExit:
                            Driver.Quit();
                            return;
                        default:
                            break;
                    }
                }
            }

            Directory.CreateDirectory(domain.Value);
            Directory.SetCurrentDirectory(domain.Value);

            PrintResults(FindMostBrutalNoReply(domain));

            Driver.Quit();

            Console.WriteLine(SuccessMessage);
            
            WaitForExit();
        }

        static int GetIdFromUrl(string url)
        {
            while (!char.IsDigit(url[^1]))
            {
                url = url[..^1];
            }

            var id = string.Empty;

            while (char.IsDigit(url[^1]))
            {
                id = url[^1] + id;
                url = url[..^1];
            }

            return int.Parse(id);
        }

        static ThreadCache FindMostBrutalNoReply(KeyValuePair<int, string> domain)
        {
            Driver.ExecuteScript("window.open('');");
            var windowHandle1 = Driver.WindowHandles.ElementAt(0);
            var windowHandle2 = Driver.WindowHandles.ElementAt(1);
            Driver.SwitchTo().Window(windowHandle1);

            var threadCache = File.Exists(ThreadCacheFile)
                ? DeserializeObjectFromFile<ThreadCache>(ThreadCacheFile)
                : new ThreadCache();

            var forumUrlsById = Driver
                .GetForumUrls($"https://{domain.Value}", DefaultRefreshBy)
                .Select(url => new KeyValuePair<int, string>(GetIdFromUrl(url), url))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            var deletedForumIds = threadCache.ForumsById.Keys.Except(forumUrlsById.Keys);

            foreach (var forumId in deletedForumIds)
            {
                threadCache.ForumsById.Remove(forumId);
            }

            foreach (var forumKvp in forumUrlsById)
            {
                Driver.GoToUrlWithRetries(
                    $"{forumKvp.Value}&order=reply_count&direction=desc",
                    DefaultRefreshBy);

                Forum forum;
                var forumId = forumKvp.Key;
                var forumName = Driver.FindElement(By.ClassName("p-title-value")).Text;

                if (threadCache.ForumsById.ContainsKey(forumId))
                {
                    forum = threadCache.ForumsById[forumId];
                    forum.Name = forumName;
                }
                else
                {
                    forum = new Forum(forumId, forumName);
                    threadCache.ForumsById[forum.Id] = forum;
                }

                var page = 0;

                while (true)
                {
                    page++;

                    var threadUrlsById = Driver
                        .FindElements(By.XPath("//li[@class='structItem-startDate']/a"))
                        .Select(a =>
                        {
                            var url = a.GetAttribute("href");

                            return new KeyValuePair<int, string>(GetIdFromUrl(url), url);
                        })
                        .Where(kvp => !threadCache.ThreadIds.Contains(kvp.Key))
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                    var stickyThreadCount = Driver
                        .FindElements(By.ClassName("structItem-status--sticky"))
                        .Count;

                    Driver.SwitchTo().Window(windowHandle2);

                    var threadCounter = 0;
                    var zeroReplies = false;

                    if (threadUrlsById.Count == 0)
                    {
                        Console.WriteLine($"{forum.Name} | Page {page}");
                    }

                    foreach (var threadKvp in threadUrlsById)
                    {
                        threadCounter++;

                        var threadId = threadKvp.Key;
                        var threadUrl = threadKvp.Value;

                        Console.WriteLine($"{forum.Name} | Page {page} | {threadUrl.TrimEnd('/')}");

                        Driver.GoToUrlWithRetries(threadUrl, DefaultRefreshBy);

                        var postDateTimes = Driver
                            .FindElements(By.XPath("//ul[contains(@class,'message-attribution-main')]//time[@class='u-dt']"))
                            .Select(dateTime => DateTime.Parse(dateTime.GetAttribute("datetime")))
                            .ToArray();

                        if (postDateTimes.Length > 1)
                        {
                            var threadDate = postDateTimes[0];
                            var firstReplyDate = postDateTimes[1];
                            var dateDiff = firstReplyDate - threadDate;
                            var dateDiffMaxComparison = dateDiff
                                .CompareTo(forum.MaxThreadAndFirstReplyDifference);

                            if (dateDiffMaxComparison > 0)
                            {
                                forum.MaxThreadAndFirstReplyDifference = dateDiff;
                                forum.MostBrutalNoReplyThreadUrls = new List<string>
                                {
                                    threadUrl
                                };
                            }
                            else if (dateDiffMaxComparison == 0)
                            {
                                forum.MostBrutalNoReplyThreadUrls.Add(threadUrl);
                            }

                            threadCache.ThreadIds.Add(threadId);
                        }
                        else if (threadCounter > stickyThreadCount)
                        {
                            zeroReplies = true;
                            break;
                        }
                    }

                    Driver.SwitchTo().Window(windowHandle1);

                    var nextButtons = Driver.FindElements(By.ClassName("pageNav-jump--next"));

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

            SerializeObjectToFile(ThreadCacheFile, threadCache);

            return threadCache;
        }

        static void PrintResults(ThreadCache threadCache)
        {
            var forums = threadCache.ForumsById.Values;

            var maxForumNameLength = forums.Count > 0
                ? forums.Max(f => f.Name.Length)
                : 0;

            if (AllForumsName.Length > maxForumNameLength || ForumHeader.Length > maxForumNameLength)
            {
                maxForumNameLength = AllForumsName.Length > ForumHeader.Length
                    ? AllForumsName.Length
                    : ForumHeader.Length;
            }

            var maxThreadUrlLength = 0;

            if (forums.Count > 0)
            {
                maxThreadUrlLength = forums.Max(f =>
                {
                    if (f.MostBrutalNoReplyThreadUrls.Count > 0)
                    {
                        return f.MostBrutalNoReplyThreadUrls.Max(t => t.Length);
                    }

                    return 0;
                });
            }

            if (MostBrutalNoReplyThreadsHeader.Length > maxThreadUrlLength)
            {
                maxThreadUrlLength = MostBrutalNoReplyThreadsHeader.Length;
            }

            var hyphens = new string('-', maxForumNameLength + maxThreadUrlLength + 3)
                + Environment.NewLine;

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
