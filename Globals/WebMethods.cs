using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;

namespace Globals
{
    public static class WebMethods
    {
        private const int DefaultRetryCount = 30;

        private static readonly TimeSpan DefaultTimeOut = TimeSpan.FromMinutes(30);
        private static readonly TimeSpan DefaultCommandTimeout = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan DefaultPollingInterval = TimeSpan.FromMilliseconds(250);

        public static void InitChromeDriver(out ChromeDriver driver, string directory = null)
        {
            ChromeDriverInstaller.Install(directory);

            var driverPath = string.IsNullOrEmpty(directory)
                ? Directory.GetCurrentDirectory()
                : directory;

            var service = ChromeDriverService.CreateDefaultService(driverPath);

            service.EnableVerboseLogging = false;
            service.SuppressInitialDiagnosticInformation = true;
            service.HideCommandPromptWindow = true;

            var options = new ChromeOptions
            {
                PageLoadStrategy = PageLoadStrategy.Eager
            };

            options.AddArgument("--incognito");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--headless");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--disable-crash-reporter");
            options.AddArgument("--disable-extensions");
            options.AddArgument("--disable-in-process-stack-traces");
            options.AddArgument("--disable-logging");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--log-level=3");
            options.AddArgument("--output=/dev/null");

            driver = new ChromeDriver(service, options, DefaultCommandTimeout);
            driver.Manage().Timeouts().PageLoad = DefaultTimeOut;
        }

        public static DefaultWait<IWebDriver> GetFluentWait(
            this IWebDriver driver,
            TimeSpan? timeOut = null,
            TimeSpan? pollingInterval = null)
        {
            var fluentWait = new DefaultWait<IWebDriver>(driver)
            {
                Timeout = timeOut ?? DefaultTimeOut,
                PollingInterval = pollingInterval ?? DefaultPollingInterval
            };

            fluentWait.IgnoreExceptionTypes(typeof(NoSuchElementException));
            fluentWait.Message = "Element to be searched not found.";

            return fluentWait;
        }

        public static IWebElement WaitForElement(this DefaultWait<IWebDriver> wait, By by)
        {
            try
            {
                return wait.Until(driver =>
                {
                    try
                    {
                        return driver.FindElement(by);
                    }
                    catch (NoSuchElementException)
                    {
                        return null;
                    }
                });
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static ReadOnlyCollection<IWebElement> WaitForElements(this DefaultWait<IWebDriver> wait, By by)
        {
            var elements = new ReadOnlyCollection<IWebElement>(new List<IWebElement>());

            try
            {
                wait.Until(driver => (elements = driver.FindElements(by)).Count > 0);
            }
            catch (Exception) { }

            return elements;
        }

        public static bool DownloadFileWithRetries(
            this WebClient client,
            string address,
            string fileName,
            int maxRetries = DefaultRetryCount)
        {
            for (int i = 0; i < maxRetries + 1; i++)
            {
                try
                {
                    client.DownloadFile(address, fileName);
                }
                catch (Exception)
                {
                    if (i + 1 <= maxRetries)
                    {
                        Console.WriteLine($"Download retry: {address}");
                    }

                    continue;
                }

                return true;
            }

            Console.WriteLine($"Download fail: {address}");

            return false;
        }

        public static bool GoToUrlWithRetries(
            this IWebDriver driver,
            string url,
            By refreshBy,
            int maxRetries = DefaultRetryCount)
        {
            var fluentWait = driver.GetFluentWait(TimeSpan.FromMinutes(1));

            for (int i = 0; i < maxRetries + 1; i++)
            {
                driver.Navigate().GoToUrl(url);

                if (fluentWait.WaitForElement(refreshBy) != null)
                {
                    return true;
                }
                else if (i + 1 <= maxRetries)
                {
                    Console.WriteLine($"URL retry: {url}");
                }
            }

            Console.WriteLine($"URL fail: {url}");

            return false;
        }

        public static bool Login(
            this IWebDriver driver,
            string url,
            string name,
            string password,
            By nameField,
            By passwordField,
            By submit,
            By refreshBy,
            int maxRetries = DefaultRetryCount)
        {
            if (!driver.GoToUrlWithRetries(url, refreshBy, maxRetries))
            {
                return false;
            }

            try
            {
                driver.FindElement(nameField).SendKeys(name);
                driver.FindElement(passwordField).SendKeys(password);

                var javaScriptExecutor = (IJavaScriptExecutor)driver;

                javaScriptExecutor.ExecuteScript("arguments[0].click();", driver.FindElement(submit));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }

            return true;
        }

        public static bool CaptchaDetected(this IWebDriver driver, string domain)
        {
            driver.Navigate().GoToUrl($"https://{domain}");

            if (driver.FindElements(By.Id("captcha-bypass")).Count > 0)
            {
                Console.WriteLine("CAPTCHA detected! :reeeeee:");

                return true;
            }

            return false;
        }

        public static string GetBaseForumUrl(
            this IWebDriver driver,
            string domain,
            By refreshBy,
            int maxRetries = DefaultRetryCount)
        {
            driver.GoToUrlWithRetries($"https://{domain}", refreshBy, maxRetries);

            var baseUrl = driver.Url.TrimEnd('/');

            if (!baseUrl.EndsWith("/index.php"))
            {
                baseUrl += "/index.php";
            }

            return baseUrl;
        }

        public static List<string> GetForumUrls(
            this IWebDriver driver,
            string parentForumUrl,
            By refreshBy,
            bool includeSubforums = true,
            int maxRetries = DefaultRetryCount,
            List<string> forumUrls = null)
        {
            forumUrls ??= new List<string>();

            driver.GoToUrlWithRetries(parentForumUrl, refreshBy, maxRetries);

            var forumLinks = driver
                .FindElements(By.XPath("//h3[@class='node-title']/a"))
                .Select(link => link.GetAttribute("href").Replace("/forums", "/index.php?forums"))
                .Where(link => !link.Contains("link-forums"))
                .ToArray();

            if (includeSubforums)
            {
                foreach (var forumLink in forumLinks)
                {
                    forumUrls.Add(forumLink);
                    forumUrls = driver.GetForumUrls(
                        forumLink,
                        refreshBy,
                        includeSubforums,
                        maxRetries,
                        forumUrls);
                }
            }
            else
            {
                forumUrls.AddRange(forumLinks);
            }

            return forumUrls;
        }
    }
}
