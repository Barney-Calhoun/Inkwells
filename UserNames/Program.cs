using static Globals.ConsoleMethods;
using static Globals.Constants;
using static Globals.FileMethods;
using static Globals.IEnumerableMethods;
using static Globals.WebMethods;
using Globals;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace UserNames
{
    class Program
    {
        const string UsersByKeywordsFile = "UsersByKeywords.txt";
        const string UsersByRegexFile = "UsersByRegularExpression.txt";
        const string AllDeletedUsersFile = "AllDeletedUsers.txt";
        const string NewDeletedUsersFile = "NewDeletedUsers.txt";
        const string AllGuestUsersFile = "AllGuestUsers.txt";
        const string NewGuestUsersFile = "NewGuestUsers.txt";

        const int ActionSearchUsersByKeywords = 1;
        const int ActionSearchUsersByRegex = 2;
        const int ActionSearchDeletedUsers = 3;
        const int ActionSearchGuestUsers = 4;
        const int ActionExit = 5;
        static readonly SortedDictionary<int, string> Actions = new SortedDictionary<int, string>()
        {
            { ActionSearchUsersByKeywords, "Search users by keywords." },
            { ActionSearchUsersByRegex, "Search users by a regular expression." },
            { ActionSearchDeletedUsers, "Search deleted users." },
            { ActionSearchGuestUsers, "Search guest users." },
            { ActionExit, "Exit." }
        };

        static ChromeDriver Driver;

        static void Main()
        {
            var domain = ReadDomain();

            var action = ReadAction(Actions);

            bool caseSensitive;
            string pattern;
            Regex regex;

            switch (action)
            {
                case ActionSearchUsersByKeywords:
                    caseSensitive = ReadConfirmation("Case sensitive user search?", FalseAction);
                    Console.WriteLine($"Enter keywords (separated by whitespace):");
                    pattern = Console
                        .ReadLine()
                        .Trim()
                        .Split(new char[0], StringSplitOptions.RemoveEmptyEntries)
                        .Select(k => Regex.Escape(k))
                        .ToString("|", "(", ")");
                    regex = caseSensitive
                        ? new Regex(pattern)
                        : new Regex(pattern, RegexOptions.IgnoreCase);
                    break;
                case ActionSearchUsersByRegex:
                    caseSensitive = ReadConfirmation("Case sensitive user search?", FalseAction);
                    pattern = ReadRegexPatternInput();
                    regex = caseSensitive
                        ? new Regex(pattern)
                        : new Regex(pattern, RegexOptions.IgnoreCase);
                    break;
                case ActionSearchDeletedUsers:
                    regex = new Regex("^Deleted member");
                    break;
                case ActionSearchGuestUsers:
                    regex = new Regex("^$");
                    break;
                default:
                    return;
            }

            InitChromeDriver(out Driver);

            ExitSignal.InitSignal(Driver);

            GetUserNames(domain, action, regex);

            Driver.Quit();

            Console.WriteLine(SuccessMessage);

            WaitForExit();
        }

        static string GetUserBbCode(int userId, string userName)
        {
            return $"[USER={userId}]@{userName}[/USER]";
        }

        static string GetUnlistedUserName(string filterToggleText)
        {
            var startIndex = filterToggleText.IndexOf(':') + 2;

            return filterToggleText[startIndex..];
        }

        static void GetUserNames(string domain, int action, Regex regex)
        {
            Console.WriteLine("Searching for users...");

            var baseUrl = $"https://incels.{domain}";

            var foundUserNames = new List<string>();

            var registeredUserIds = new List<int>();

            Driver.GoToUrlWithRetries($"{baseUrl}/members/list/", DefaultRefreshBy);

            while (true)
            {
                var userIds = Driver
                    .FindElementsByXPath("//*[contains(@class,'avatar-u')]")
                    .Select(a => {
                        var avatarClass = a.GetAttribute("class");
                        var startIndex = avatarClass.LastIndexOf('u') + 1;
                        var endIndex = avatarClass.LastIndexOf('-');
                        return int.Parse(avatarClass[startIndex..endIndex]);
                    })
                    .ToArray();

                registeredUserIds.AddRange(userIds);

                if (action != ActionSearchDeletedUsers && action != ActionSearchGuestUsers)
                {
                    var userNames = Driver
                        .FindElementsByXPath("//span[contains(@class,'username--style')]")
                        .Select(u => u.Text)
                        .ToArray();

                    for (int i = 0; i < userNames.Length; i++)
                    {
                        if (regex.IsMatch(userNames[i]))
                        {
                            Console.WriteLine(userNames[i]);
                            foundUserNames.Add(GetUserBbCode(userIds[i], userNames[i]));
                        }
                    }
                }

                var nextButtons = Driver.FindElementsByClassName("pageNav-jump--next");

                if (nextButtons.Count > 0)
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

            var unlistedUserIds = Enumerable
                .Range(1, registeredUserIds.Max())
                .Except(registeredUserIds);

            foreach (var userId in unlistedUserIds)
            {
                Driver.GoToUrlWithRetries(
                    $"{baseUrl}/forums/inceldom-discussion.2/?starter_id={userId}",
                    DefaultRefreshBy);

                var filterToggles = Driver.FindElementsByClassName("filterBar-filterToggle");

                var isGuestUser = filterToggles.Count == 0;

                var userName = isGuestUser
                    ? string.Empty
                    : GetUnlistedUserName(filterToggles.First().Text);

                if (regex.IsMatch(userName))
                {
                    if (isGuestUser)
                    {
                        userName = $"Guest {userId}";
                    }

                    Console.WriteLine(userName);

                    foundUserNames.Add(GetUserBbCode(userId, userName));
                }
            }

            var fileContent = string.Join(Environment.NewLine, foundUserNames);

            switch (action)
            {
                case ActionSearchUsersByKeywords:
                    fileContent = $"Keywords: {regex.ToString().Trim('(', ')').Replace('|', ' ')}"
                        + Environment.NewLine
                        + fileContent;
                    File.WriteAllText(UsersByKeywordsFile, fileContent);
                    break;
                case ActionSearchUsersByRegex:
                    fileContent = $"Regular expression: {regex}"
                        + Environment.NewLine
                        + fileContent;
                    File.WriteAllText(UsersByRegexFile, fileContent);
                    break;
                case ActionSearchDeletedUsers:
                    var oldDeletedUsers = ReadAllNonEmptyLines(AllDeletedUsersFile);
                    var newDeletedUsers = foundUserNames.Except(oldDeletedUsers);
                    File.WriteAllText(
                        NewDeletedUsersFile,
                        string.Join(Environment.NewLine, newDeletedUsers));
                    File.WriteAllText(AllDeletedUsersFile, fileContent);
                    break;
                default:
                    var oldGuestUsers = ReadAllNonEmptyLines(AllGuestUsersFile);
                    var newGuestUsers = foundUserNames.Except(oldGuestUsers);
                    File.WriteAllText(
                        NewGuestUsersFile,
                        string.Join(Environment.NewLine, newGuestUsers));
                    File.WriteAllText(AllGuestUsersFile, fileContent);
                    break;
            }
        }
    }
}
