using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.IO;
using System.Net;

namespace Globals
{
    public static class WebMethods
    {
        private static readonly TimeSpan TimeOut = TimeSpan.FromMinutes(30);

        public static void InitChromeDriver(out ChromeDriver driver)
        {
            var service = ChromeDriverService.CreateDefaultService(Directory.GetCurrentDirectory());
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

            driver = new ChromeDriver(service, options);
            driver.Manage().Timeouts().PageLoad = TimeOut;
        }

        public static void GoToUrlWithRetries(
            this IWebDriver driver,
            string url,
            By refreshBy,
            int maxRetries = 30)
        {
            var fluentWait = driver.GetFluentWait(TimeSpan.FromMinutes(1));
            for (int i = 0; i < maxRetries; i++)
            {
                driver.Navigate().GoToUrl(url);
                if (fluentWait.Until(d => d.FindElements(refreshBy).Count > 0))
                {
                    return;
                }
                else
                {
                    Console.WriteLine($"URL retry: {url}");
                }
            }
            Console.WriteLine($"URL fail: {url}");
        }

        public static void LoginWithRetries(
            this IWebDriver driver,
            string url,
            string name,
            string password,
            By nameField,
            By passwordField,
            By submit,
            By refreshBy,
            int maxRetries = 30)
        {
            driver.GoToUrlWithRetries(url, refreshBy, maxRetries);
            driver.FindElement(nameField).SendKeys(name);
            driver.FindElement(passwordField).SendKeys(password);

            var javaScriptExecutor = (IJavaScriptExecutor)driver;
            javaScriptExecutor.ExecuteScript("arguments[0].click();", driver.FindElement(submit));
        }

        public static DefaultWait<IWebDriver> GetFluentWait(this IWebDriver driver, TimeSpan? timeOut = null)
        {
            var fluentWait = new DefaultWait<IWebDriver>(driver)
            {
                Timeout = timeOut ?? TimeOut,
                PollingInterval = TimeSpan.FromMilliseconds(250)
            };
            fluentWait.IgnoreExceptionTypes(typeof(NoSuchElementException));
            fluentWait.Message = "Element to be searched not found.";

            return fluentWait;
        }

        public static IWebElement WaitForElement(this DefaultWait<IWebDriver> wait, By by)
        {
            return wait.Until(driver =>
            {
                IWebElement element;
                try
                {
                    element = driver.FindElement(by);
                }
                catch (NoSuchElementException)
                {
                    return null;
                }
                return element;
            });
        }

        public static bool DownloadFileWithRetries(
            this WebClient client,
            string address,
            string fileName,
            int maxRetries = 30)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    client.DownloadFile(address, fileName);
                }
                catch (Exception)
                {
                    Console.WriteLine($"Download retry: {address}");
                    continue;
                }
                return true;
            }
            Console.WriteLine($"Download fail: {address}");
            return false;
        }
    }
}
