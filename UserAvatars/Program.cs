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
        static readonly By ArchiveImageRefreshBy = By.Id("wm-ipp-base");
        static readonly By EzgifRefreshBy = By.ClassName("gifmaker");

        static void Main()
        {
        Start:
            var userId = ReadNumericalInput("Enter user ID:");

            var action = ReadAction(Actions);

            switch (action)
            {
                case ActionGetAvatars:
                    InitChromeDriver(out Driver);
                    ExitSignal.InitSignal(Driver);
                    GetUserAvatars(userId);
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
                Console.Clear();
                goto Start;
            }

            WaitForExit();
        }

        static void GetUserAvatars(int userId)
        {
            Console.WriteLine("Searching for avatars...");

            var fluentWait = Driver.GetFluentWait();

            var avatarDictionary = AvatarHelper.GetDictionary(userId);
            var avatarNewSources = new HashSet<string>();
            var avatarFolder = Math.Floor((double)userId / 1000);

            Driver.ExecuteScript("window.open('');");
            Driver.ExecuteScript("window.open('');");
            var windowHandle1 = Driver.WindowHandles.ElementAt(0);
            var windowHandle2 = Driver.WindowHandles.ElementAt(1);
            var windowHandle3 = Driver.WindowHandles.ElementAt(2);
            Driver.SwitchTo().Window(windowHandle1);

            foreach (var avatarSize in AvatarHelper.Sizes)
            {
                foreach (var domain in Domains)
                {
                    var avatarBaseUrl = $"https://incels.{domain}/data/avatars/{avatarSize}/{avatarFolder}/{userId}.*";

                    Driver.GoToUrlWithRetries($"https://web.archive.org/web/*/{avatarBaseUrl}", ArchiveRefreshBy);

                    fluentWait.Until(driver => driver.FindElements(By.ClassName("fa-spinner")).Count == 0);

                    var urlSorting = Driver
                        .FindElementByXPath("//table[@id='resultsUrl']//th[contains(@class,'url sorting')]");
                    while (!urlSorting.GetAttribute("class").Trim().EndsWith("asc"))
                    {
                        urlSorting.Click();
                    }

                    while (true)
                    {
                        var avatarCalendarUrls = Driver
                            .FindElementsByXPath("//td[contains(@class,'url')]/a")
                            .Select(url => url.GetAttribute("href"))
                            .ToArray();

                        Driver.SwitchTo().Window(windowHandle2);

                        foreach (var avatarCalendarUrl in avatarCalendarUrls)
                        {
                            Driver.GoToUrlWithRetries(avatarCalendarUrl, ArchiveRefreshBy);

                            fluentWait.WaitForElement(By.ClassName("calendar-grid"));

                            var avatarCaptureRangeDates = Driver
                                .FindElementsByXPath("//div[@class='captures-range-info']//a")
                                .Select(date => date.Text)
                                .ToArray();
                            var startYear = DateTime.Parse(avatarCaptureRangeDates.First()).Year;
                            var endYear = DateTime.Parse(avatarCaptureRangeDates.Last()).Year;

                            foreach (var year in Enumerable.Range(startYear, endYear - startYear + 1))
                            {
                                var yearLabel = Driver.FindElementByXPath(
                                    $"//span[contains(@class,'sparkline-year-label') and contains(text(),'{year}')]");
                                yearLabel.Click();

                                fluentWait.WaitForElement(By.ClassName("calendar-grid"));

                                var avatarArchiveUrls = Driver
                                    .FindElementsByXPath("//div[contains(@class,'calendar-day')]/a")
                                    .Select(url => url.GetAttribute("href"))
                                    .ToArray();

                                Driver.SwitchTo().Window(windowHandle3);

                                foreach (var avatarArchiveUrl in avatarArchiveUrls)
                                {
                                    if (!avatarDictionary.ContainsKey(avatarArchiveUrl))
                                    {
                                        Console.WriteLine(avatarArchiveUrl);

                                        Driver.GoToUrlWithRetries(avatarArchiveUrl, ArchiveImageRefreshBy);

                                        var avatarSources = Driver
                                            .FindElementsById("playback")
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
                                }

                                Driver.SwitchTo().Window(windowHandle2);
                            }
                        }

                        Driver.SwitchTo().Window(windowHandle1);

                        var next = Driver.FindElementByClassName("next");

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

            var duplicateDirectoryPath = AvatarHelper.GetDuplicateDirectoryPath(userId);
            Directory.CreateDirectory(duplicateDirectoryPath);

            if (avatarNewSources.Count > 0)
            {
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

                var newAvatars = avatarDictionary.Values.Where(avatar => avatarNewSources.Contains(avatar.Source));

                foreach (var avatar in newAvatars)
                {
                    Console.WriteLine($"Downloading avatar '{avatar.GetName()}'");
                    client.DownloadFileWithRetries(avatar.Source, avatar.GetPath());

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
                            File.Move(avatar.GetPath(), avatar.GetPath(avatar.Directory = duplicateDirectoryPath));
                        }
                    }
                    else
                    {
                        invalidImageFiles.Add(avatar.GetFileName());
                    }
                }

                if (invalidImageFiles.Count > 0)
                {
                    Console.WriteLine("Invalid images:");
                    foreach (var imageFile in invalidImageFiles)
                    {
                        Console.WriteLine(imageFile);
                    }
                }
            }

            AvatarHelper.SaveDictionaryToFile(userId, avatarDictionary);
            AvatarHelper.UpdateUrlFiles(userId);
        }
    }
}
