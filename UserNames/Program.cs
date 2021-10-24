using static Globals.ConsoleMethods;
using static Globals.Constants;
using static Globals.FileMethods;
using static Globals.IEnumerableMethods;
using static Globals.WebMethods;
using Globals;
using OpenQA.Selenium;
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
        const string UserDictionaryFile = "UserDictionary.json";
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
        static readonly SortedDictionary<int, string> UserNameActions = new SortedDictionary<int, string>()
        {
            { ActionSearchUsersByKeywords, "Search users by keywords." },
            { ActionSearchUsersByRegex, "Search users by a regular expression." },
            { ActionSearchDeletedUsers, "Search deleted users." },
            { ActionSearchGuestUsers, "Search guest users." },
            { ActionExit, "Exit." }
        };

        const int ActionCreateNewUserDictionary = 1;
        const int ActionUseExistingUserDictionary = 2;
        static readonly SortedDictionary<int, string> UserDictionaryActions = new SortedDictionary<int, string>()
        {
            { ActionCreateNewUserDictionary, "Create new user dictionary." },
            { ActionUseExistingUserDictionary, "Use existing user dictionary if available." },
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

            var userNameAction = ReadSelection(UserNameActions);

            var caseSensitiveQuestion = "Case sensitive user search?";

            int userDictionaryAction;
            bool caseSensitive;
            string pattern;
            Regex regex;

            switch (userNameAction)
            {
                case ActionSearchUsersByKeywords:
                    userDictionaryAction = ReadSelection(
                        UserDictionaryActions,
                        ActionUseExistingUserDictionary);
                    
                    caseSensitive = ReadConfirmation(caseSensitiveQuestion, FalseAction);
                    
                    Console.WriteLine($"Enter keywords (separated by whitespace):");
                    
                    pattern = Console
                        .ReadLine()
                        .Trim()
                        .Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries)
                        .Select(k => Regex.Escape(k))
                        .ToString("|", "(", ")");
                    
                    regex = caseSensitive
                        ? new Regex(pattern)
                        : new Regex(pattern, RegexOptions.IgnoreCase);
                    
                    break;
                case ActionSearchUsersByRegex:
                    userDictionaryAction = ReadSelection(
                        UserDictionaryActions,
                        ActionUseExistingUserDictionary);

                    caseSensitive = ReadConfirmation(caseSensitiveQuestion, FalseAction);
                    
                    pattern = ReadRegexPatternInput();
                    
                    regex = caseSensitive
                        ? new Regex(pattern)
                        : new Regex(pattern, RegexOptions.IgnoreCase);
                    
                    break;
                case ActionSearchDeletedUsers:
                    userDictionaryAction = ReadSelection(
                        UserDictionaryActions,
                        ActionCreateNewUserDictionary);

                    regex = new Regex("^Deleted member");
                    
                    break;
                case ActionSearchGuestUsers:
                    userDictionaryAction = ReadSelection(
                        UserDictionaryActions,
                        ActionCreateNewUserDictionary);

                    regex = new Regex("^$");
                    
                    break;
                default:
                    Driver.Quit();
                    return;
            }

            var mainDirectory = Directory.GetCurrentDirectory();

            Directory.CreateDirectory(domain.Value);
            Directory.SetCurrentDirectory(domain.Value);

            GetUserNames(domain, userNameAction, userDictionaryAction, regex);

            Driver.Quit();

            Console.WriteLine(SuccessMessage);

            if (ReadConfirmation("Restart?", FalseAction))
            {
                Directory.SetCurrentDirectory(mainDirectory);
                Console.Clear();
                goto Start;
            }

            WaitForExit();
        }

        static string GetUserBbCode(int userId, string userName)
        {
            return string.Format("[{0}={1}]@{2}[/{0}]", "USER", userId, userName);
        }

        static string GetUserNameFromFilter(string filterToggleText)
        {
            var startIndex = filterToggleText.IndexOf(':') + 2;

            return filterToggleText[startIndex..];
        }

        static IDictionary<int, string> AppendUserDictionary(
            IDictionary<int, string> userDictionary,
            KeyValuePair<int, string> domain,
            bool fillUserGaps = false)
        {
            Console.WriteLine("Appending user dictionary...");

            var mainForumFilterUrl = Driver
                .GetForumUrls($"https://{domain.Value}", DefaultRefreshBy, false)
                .First() + "&starter_id=";

            Driver.GoToUrlWithRetries($"https://{domain.Value}", DefaultRefreshBy);

            var newestUserName = Driver
                .FindElement(By.XPath("//div[contains(@data-widget-key,'forum_overview_forum_statistics')]"))
                .FindElement(By.XPath(".//*[contains(@class,'username')]"))
                .Text;

            var newestUserNameReached = !fillUserGaps && userDictionary.Values.Contains(newestUserName);

            var userId = fillUserGaps || userDictionary.Count == 0 ? 1 : userDictionary.Keys.Last();

            while (true)
            {
                if (!userDictionary.ContainsKey(userId))
                {
                    Driver.GoToUrlWithRetries(mainForumFilterUrl + userId, DefaultRefreshBy);

                    var filterToggles = Driver.FindElements(By.ClassName("filterBar-filterToggle"));

                    var isGuestUser = filterToggles.Count == 0;

                    var userName = isGuestUser
                        ? string.Empty
                        : GetUserNameFromFilter(filterToggles.First().Text);

                    if (userName == newestUserName)
                    {
                        newestUserNameReached = true;
                    }

                    if (newestUserNameReached && isGuestUser)
                    {
                        break;
                    }

                    userDictionary.Add(userId, userName);

                    Console.WriteLine(isGuestUser ? $"Guest member {userId}" : userName);
                }
                else if (userDictionary[userId] == newestUserName)
                {
                    newestUserNameReached = true;
                }

                userId++;
            }

            return userDictionary;
        }

        static IDictionary<int, string> CreateUserDictionary(KeyValuePair<int, string> domain)
        {
            Console.WriteLine("Creating user dictionary...");

            var userDictionary = new SortedDictionary<int, string>();

            var baseUrl = Driver.GetBaseForumUrl(domain.Value, DefaultRefreshBy);

            Driver.GoToUrlWithRetries($"{baseUrl}?members/list", DefaultRefreshBy);

            while (true)
            {
                var userIds = Driver
                    .FindElements(By.XPath("//*[contains(@class,'avatar-u')]"))
                    .Select(a => {
                        var avatarClass = a.GetAttribute("class");
                        var startIndex = avatarClass.LastIndexOf('u') + 1;
                        var endIndex = avatarClass.LastIndexOf('-');
                        return int.Parse(avatarClass[startIndex..endIndex]);
                    })
                    .ToArray();

                var userNames = Driver
                    .FindElements(By.ClassName("contentRow-header"))
                    .Select(u => u.Text)
                    .ToArray();

                for (int i = 0; i < userIds.Length; i++)
                {
                    Console.WriteLine(userNames[i]);

                    userDictionary.Add(userIds[i], userNames[i]);
                }

                var nextButtons = Driver.FindElements(By.ClassName("pageNav-jump--next"));

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

            return AppendUserDictionary(userDictionary, domain, true);
        }

        static void GetUserNames(
            KeyValuePair<int, string> domain,
            int userNameAction,
            int userDictionaryAction,
            Regex regex)
        {
            IDictionary<int, string> userDictionary;

            if (userDictionaryAction == ActionUseExistingUserDictionary && File.Exists(UserDictionaryFile))
            {
                userDictionary = AppendUserDictionary(
                    DeserializeObjectFromFile<SortedDictionary<int, string>>(
                        UserDictionaryFile),
                    domain);
            }
            else
            {
                userDictionary = CreateUserDictionary(domain);
            }

            SerializeObjectToFile(UserDictionaryFile, userDictionary);

            Console.WriteLine("Searching for users...");

            var foundUserNames = userDictionary
                .Where(kvp => regex.IsMatch(kvp.Value))
                .Select(kvp => {
                    if (string.IsNullOrEmpty(kvp.Value))
                    {
                        return GetUserBbCode(kvp.Key, $"Guest member {kvp.Key})");
                    }

                    return GetUserBbCode(kvp.Key, kvp.Value);
                });

            var fileContent = string.Join(Environment.NewLine, foundUserNames);

            if (string.IsNullOrEmpty(fileContent))
            {
                fileContent = "No users found.";
            }

            Console.WriteLine(fileContent);

            switch (userNameAction)
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

                    File.WriteAllText(AllDeletedUsersFile, fileContent);

                    fileContent = string.Join(Environment.NewLine, newDeletedUsers);

                    if (string.IsNullOrEmpty(fileContent))
                    {
                        fileContent = "No new deleted users found.";
                    }

                    File.WriteAllText(NewDeletedUsersFile, fileContent);
                    
                    break;
                default:
                    var oldGuestUsers = ReadAllNonEmptyLines(AllGuestUsersFile);
                    var newGuestUsers = foundUserNames.Except(oldGuestUsers);

                    File.WriteAllText(AllGuestUsersFile, fileContent);

                    fileContent = string.Join(Environment.NewLine, newGuestUsers);

                    if (string.IsNullOrEmpty(fileContent))
                    {
                        fileContent = "No new guest users found.";
                    }

                    File.WriteAllText(NewGuestUsersFile, fileContent);
                    
                    break;
            }
        }
    }
}
