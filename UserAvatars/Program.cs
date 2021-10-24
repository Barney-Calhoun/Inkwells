using static Globals.ConsoleMethods;
using static Globals.Constants;
using static Globals.FileMethods;
using static Globals.WebMethods;
using Globals;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace UserAvatars
{
    class Program
    {
        const int ActionGetAvatars = 1;
        const int ActionUpdateAvatarUrlFiles = 2;
        const int ActionExit = 3;
        static readonly SortedDictionary<int, string> Actions = new SortedDictionary<int, string>()
        {
            { ActionGetAvatars, "Get avatars." },
            { ActionUpdateAvatarUrlFiles, "Update avatar URL files." },
            { ActionExit, "Exit." }
        };

        static ChromeDriver Driver;

        static readonly By ArchiveRefreshBy = By.ClassName("search-toolbar");
        static readonly By EzgifRefreshBy = By.ClassName("gifmaker");

        static void Main()
        {
        Start:
            var domains = ReadDomains();

            var userId = ReadNumericalInput("Enter user ID:");

            var action = ReadSelection(Actions);

            var mainDirectory = Directory.GetCurrentDirectory();

            Directory.CreateDirectory(Domains[domains.Key]);
            Directory.SetCurrentDirectory(Domains[domains.Key]);

            switch (action)
            {
                case ActionGetAvatars:
                    InitChromeDriver(out Driver, mainDirectory);

                    ExitSignal.InitSignal(Driver);
                    
                    GetUserAvatars(domains, userId);
                    
                    Driver.Quit();
                    
                    Console.WriteLine(SuccessMessage);
                    
                    break;
                case ActionUpdateAvatarUrlFiles:
                    var response = AvatarHelper.UpdateUrlFiles(userId);
                    
                    switch (response)
                    {
                        case AvatarHelper.UserIdDirNotFound:
                            Console.WriteLine(ErrorMessage);
                            
                            Console.WriteLine(string.Format("User ID ({0}) directory doesn't exist.", userId));
                            
                            break;
                        case AvatarHelper.DictionaryFileNotFound:
                            Console.WriteLine(ErrorMessage);
                            
                            Console.WriteLine(
                                string.Format(
                                    "User avatar URLs dictionary file ({0}) doesn't exist.",
                                    AvatarHelper.DictionaryFileName));
                            
                            break;
                        default:
                            Console.WriteLine(SuccessMessage);

                            break;
                    }
                    break;
                default:
                    return;
            }

            if (ReadConfirmation("Restart?", FalseAction))
            {
                Directory.SetCurrentDirectory(mainDirectory);
                Console.Clear();
                goto Start;
            }

            WaitForExit();
        }

        static void GetUserAvatars(KeyValuePair<int, string[]> domains, int userId)
        {
            var fluentWait = Driver.GetFluentWait();

            var avatarDictionary = AvatarHelper.GetDictionary(userId);
            var avatarNewSources = new HashSet<string>();
            var avatarFolder = Math.Floor((double)userId / 1000);

            var maxAvatarArchiveYear = avatarDictionary.Count > 0
                ? avatarDictionary.Values.Max(a => DateTime.Parse(a.GetArchiveDate()).Year)
                : 0;

            Driver.ExecuteScript("window.open('');");
            Driver.ExecuteScript("window.open('');");
            var windowHandle1 = Driver.WindowHandles.ElementAt(0);
            var windowHandle2 = Driver.WindowHandles.ElementAt(1);
            var windowHandle3 = Driver.WindowHandles.ElementAt(2);
            Driver.SwitchTo().Window(windowHandle1);

            Console.WriteLine("Searching for avatars...");

            Directory.CreateDirectory(userId.ToString());

            foreach (var avatarSize in AvatarHelper.Sizes)
            {
                foreach (var domain in domains.Value)
                {
                    var avatarBaseUrl = domains.Key switch
                    {
                        BlackpillDomain => $"https://{domain}/blackpill/data/avatars/{avatarSize}/{avatarFolder}/{userId}.*",
                        LookismDomain => $"https://{LookismAvatarDomain}/data/avatars/{avatarSize}/{avatarFolder}/{userId}.*",
                        _ => $"https://{domain}/data/avatars/{avatarSize}/{avatarFolder}/{userId}.*",
                    };

                    Driver.GoToUrlWithRetries($"{ArchiveBaseUrl}/*/{avatarBaseUrl}", ArchiveRefreshBy);

                    fluentWait.Until(driver => driver.FindElements(By.ClassName("fa-spinner")).Count == 0);

                    var urlSorting = Driver
                        .FindElement(By.XPath("//table[@id='resultsUrl']//th[contains(@class,'url sorting')]"));

                    while (!urlSorting.GetAttribute("class").Trim().EndsWith("asc"))
                    {
                        urlSorting.Click();
                    }

                    while (true)
                    {
                        var avatarCalendarUrls = Driver
                            .FindElements(By.XPath("//td[contains(@class,'url')]/a"))
                            .Select(url => url.GetAttribute("href"))
                            .ToArray();

                        Driver.SwitchTo().Window(windowHandle2);

                        foreach (var avatarCalendarUrl in avatarCalendarUrls)
                        {
                            Driver.GoToUrlWithRetries(avatarCalendarUrl, ArchiveRefreshBy);

                            var avatarCaptureRangeDates = fluentWait
                                .WaitForElements(By.XPath("//div[@class='captures-range-info']//a"))
                                .Select(date => date.Text)
                                .ToArray();

                            var startYear = DateTime.Parse(avatarCaptureRangeDates.First()).Year;
                            var endYear = DateTime.Parse(avatarCaptureRangeDates.Last()).Year;

                            var years = Enumerable
                                .Range(startYear, endYear - startYear + 1)
                                .Where(y => y >= maxAvatarArchiveYear);

                            foreach (var year in years)
                            {
                                var yearLabel = Driver.FindElement(
                                    By.XPath(
                                        $"//span[contains(@class,'sparkline-year-label') and contains(text(),'{year}')]"));

                                yearLabel.Click();

                                fluentWait.WaitForElement(By.ClassName("calendar-grid"));

                                var avatarArchiveUrls = Driver
                                    .FindElements(By.XPath("//div[contains(@class,'calendar-day')]/a"))
                                    .Select(url => url.GetAttribute("href"))
                                    .ToArray();

                                Driver.SwitchTo().Window(windowHandle3);

                                foreach (var avatarArchiveUrl in avatarArchiveUrls)
                                {
                                    if (avatarDictionary.ContainsKey(avatarArchiveUrl))
                                    {
                                        continue;
                                    }

                                    Console.WriteLine(avatarArchiveUrl);

                                    Driver.Navigate().GoToUrl(avatarArchiveUrl);

                                    var avatarSources = Driver
                                        .FindElements(By.Id("playback"))
                                        .Select(s => s.GetAttribute("src"))
                                        .ToArray();

                                    var avatarSource = avatarSources.Length > 0
                                        ? avatarSources.First()
                                        : string.Empty;

                                    if (!string.IsNullOrEmpty(avatarSource))
                                    {
                                        avatarNewSources.Add(avatarSource);
                                    }

                                    var avatarDirectory = !string.IsNullOrEmpty(avatarSource)
                                        ? userId.ToString()
                                        : string.Empty;

                                    var avatarExtension = !string.IsNullOrEmpty(avatarSource)
                                        ? AvatarHelper.GifExtension
                                        : string.Empty;

                                    avatarDictionary[avatarArchiveUrl] = new Avatar(
                                        avatarArchiveUrl,
                                        avatarSource,
                                        avatarDirectory,
                                        avatarExtension);
                                }

                                Driver.SwitchTo().Window(windowHandle2);
                            }
                        }

                        Driver.SwitchTo().Window(windowHandle1);

                        var next = Driver.FindElement(By.ClassName("next"));

                        if (!next.GetAttribute("class").Trim().EndsWith("disabled"))
                        {
                            next.Click();
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            if (avatarNewSources.Count > 0)
            {
                var duplicateDirectoryPath = AvatarHelper.GetDuplicateDirectoryPath(userId);

                var invalidImageDownloads = new List<string>();
                var invalidImageFiles = new List<string>();

                var uniqueImages = avatarDictionary
                    .Values
                    .Where(avatar =>
                    {
                        return !string.IsNullOrEmpty(avatar.Source)
                        && !avatarNewSources.Contains(avatar.Source)
                        && avatar.Directory == userId.ToString()
                        && File.Exists(avatar.GetPath());
                    })
                    .Select(avatar =>
                    {
                        Image<Rgba32> image;

                        try
                        {
                            image = Image.Load<Rgba32>(avatar.GetPath());
                        }
                        catch (Exception)
                        {
                            invalidImageFiles.Add(avatar.GetFileName());

                            return null;
                        }

                        return image;
                    })
                    .Where(image => image != null)
                    .ToList();

                using var client = new WebClient();

                var newAvatars = avatarDictionary
                    .Values
                    .Where(avatar => avatarNewSources.Contains(avatar.Source));

                foreach (var avatar in newAvatars)
                {
                    Console.WriteLine($"Downloading avatar '{avatar.GetName()}'");

                    if (!client.DownloadFileWithRetries(avatar.Source, avatar.GetPath()))
                    {
                        invalidImageDownloads.Add(avatar.Source);
                        continue;
                    }

                    Image<Rgba32> image;

                    try
                    {
                        image = Image.Load<Rgba32>(avatar.GetPath());
                    }
                    catch (Exception)
                    {
                        Driver.GoToUrlWithRetries("https://ezgif.com/effects?url=" + avatar.Source, EzgifRefreshBy);

                        var convertButton = fluentWait.WaitForElement(By.XPath("//a[contains(@class,'-to-')]"));

                        Driver.GoToUrlWithRetries(convertButton.GetAttribute("href"), EzgifRefreshBy);

                        convertButton = fluentWait.WaitForElement(By.XPath("//p[@id='tool-submit-button']/input"));

                        Driver.ExecuteScript("arguments[0].click();", convertButton);
                        
                        var saveButton = fluentWait.WaitForElement(By.XPath("//div[@id='output']//a[@class='save']"));
                        var tempAvatarSource = saveButton.GetAttribute("href");
                        var newAvatarExtension = GetExtensionWithoutPeriod(tempAvatarSource);
                        var newAvatarPath = Path.ChangeExtension(avatar.GetPath(), newAvatarExtension);
                        var downloadSuccess = client.DownloadFileWithRetries(tempAvatarSource, newAvatarPath);
                        
                        if (downloadSuccess && avatar.GetPath() != newAvatarPath)
                        {
                            if (File.Exists(avatar.GetPath()))
                            {
                                File.Delete(avatar.GetPath());
                            }

                            avatar.Extension = newAvatarExtension;
                        }

                        try
                        {
                            image = Image.Load<Rgba32>(avatar.GetPath());
                        }
                        catch (Exception)
                        {
                            image = null;
                        }
                    }

                    if (image != null)
                    {
                        if (!uniqueImages.Exists(uniqueImage => ImageComparer.Compare(uniqueImage, image)))
                        {
                            uniqueImages.Add(image);
                        }
                        else
                        {
                            image.Dispose();

                            if (!Directory.Exists(duplicateDirectoryPath))
                            {
                                Directory.CreateDirectory(duplicateDirectoryPath);
                            }

                            File.Move(
                                avatar.GetPath(),
                                avatar.GetPath(avatar.Directory = duplicateDirectoryPath),
                                true);
                        }
                    }
                    else
                    {
                        invalidImageFiles.Add(avatar.GetFileName());
                    }
                }

                if (invalidImageDownloads.Count > 0)
                {
                    Console.WriteLine("Invalid image downloads:");
                    Console.WriteLine(string.Join(Environment.NewLine, invalidImageDownloads));
                }

                if (invalidImageFiles.Count > 0)
                {
                    Console.WriteLine("Invalid image files:");
                    Console.WriteLine(string.Join(Environment.NewLine, invalidImageFiles));
                }
            }

            SerializeObjectToFile(AvatarHelper.GetDictionaryFilePath(userId), avatarDictionary);

            AvatarHelper.UpdateUrlFiles(userId);
        }
    }
}
