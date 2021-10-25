using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Runtime.InteropServices;

namespace Globals
{
    public static class ChromeDriverInstaller
    {
        public static void Install(string directory = null, string chromeVersion = null, bool forceDownload = false)
        {
            using var client = new WebClient();

            client.BaseAddress = "https://chromedriver.storage.googleapis.com/";

            // Instructions from "https://chromedriver.chromium.org/downloads/version-selection".
            // First, find out which version of Chrome you are using. Let's say you have Chrome 72.0.3626.81.
            if (string.IsNullOrEmpty(chromeVersion))
            {
                chromeVersion = GetChromeVersion();
            }

            // Take the Chrome version number, remove the last part.
            chromeVersion = chromeVersion[0..chromeVersion.LastIndexOf('.')];

            // Append the result to URL "https://chromedriver.storage.googleapis.com/LATEST_RELEASE".
            // For example, with Chrome version 72.0.3626.81, you'd get a URL "https://chromedriver.storage.googleapis.com/LATEST_RELEASE_72.0.3626".
            var chromeDriverVersion = client.DownloadString($"LATEST_RELEASE_{chromeVersion}");

            string zipName;
            string driverName;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                zipName = "chromedriver_win32.zip";
                driverName = "chromedriver.exe";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                zipName = "chromedriver_linux64.zip";
                driverName = "chromedriver";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                zipName = "chromedriver_mac64.zip";
                driverName = "chromedriver";
            }
            else
            {
                throw new PlatformNotSupportedException("Your operating system is not supported.");
            }

            var driverDirectory = string.IsNullOrEmpty(directory)
                ? Directory.GetCurrentDirectory()
                : directory;

            var driverPath = Path.Combine(driverDirectory, driverName);

            if (!forceDownload && File.Exists(driverPath))
            {
                using var process = Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = driverPath,
                        ArgumentList = { "--version" },
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                    }
                );

                var existingChromeDriverVersion = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                
                process.WaitForExit();
                process.Kill(true);

                // Expected output is something like "ChromeDriver 88.0.4324.96 (68dba2d8a0b149a1d3afac56fa74648032bcf46b-refs/branch-heads/4324@{#1784})".
                // The following line will extract the version number and leave the rest.
                existingChromeDriverVersion = existingChromeDriverVersion
                    .Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries)[1];
                
                if (chromeDriverVersion == existingChromeDriverVersion)
                {
                    return;
                }

                if (!string.IsNullOrEmpty(error))
                {
                    throw new Exception($"Failed to execute '{driverName} --version'.");
                }
            }

            Console.WriteLine("Downloading chromedriver...");

            // Use the URL created in the last step to retrieve a small file containing the version of ChromeDriver to use. For example, the above URL will get your a file containing "72.0.3626.69". (The actual number may change in the future, of course.)
            // Use the version number retrieved from the previous step to construct the URL to download ChromeDriver. With version 72.0.3626.69, the URL would be "https://chromedriver.storage.googleapis.com/index.html?path=72.0.3626.69/".
            if (!client.DownloadFileWithRetries($"{chromeDriverVersion}/{zipName}", zipName))
            {
                throw new Exception("Failed to download chromedriver.");
            }

            // This reads the zipfile as a stream, opens the archive and extracts the chromedriver executable to the driverPath without saving any intermediate files to disk.
            using (var zipFileStream = File.Open(zipName, FileMode.Open))
            using (var zipArchive = new ZipArchive(zipFileStream, ZipArchiveMode.Read))
            using (var chromeDriverWriter = new FileStream(driverPath, FileMode.Create))
            {

                var entry = zipArchive.GetEntry(driverName);

                using Stream chromeDriverStream = entry.Open();

                chromeDriverStream.CopyTo(chromeDriverWriter);
            }

            // On Linux/macOS, you need to add the executable permission (+x) to allow the execution of the chromedriver.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                using var process = Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = "chmod",
                        ArgumentList = { "+x", driverPath },
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                    }
                );

                var error = process.StandardError.ReadToEnd();
                
                process.WaitForExit();
                process.Kill(true);

                if (!string.IsNullOrEmpty(error))
                {
                    throw new Exception("Failed to make chromedriver executable.");
                }
            }

            if (File.Exists(zipName))
            {
                File.Delete(zipName);
            }
        }

        public static string GetChromeVersion()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var chromePath = (string)Registry.GetValue(
                    "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Paths\\chrome.exe",
                    null,
                    null);
                
                if (chromePath == null)
                {
                    throw new Exception("Google Chrome not found in registry.");
                }

                var fileVersionInfo = FileVersionInfo.GetVersionInfo(chromePath);

                return fileVersionInfo.FileVersion;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                try
                {
                    using var process = Process.Start(
                        new ProcessStartInfo
                        {
                            FileName = "google-chrome",
                            ArgumentList = { "--product-version" },
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                        }
                    );

                    var output = process.StandardOutput.ReadToEnd();
                    var error = process.StandardError.ReadToEnd();
                    
                    process.WaitForExit();
                    process.Kill(true);

                    if (!string.IsNullOrEmpty(error))
                    {
                        throw new Exception(error);
                    }

                    return output;
                }
                catch (Exception ex)
                {
                    throw new Exception(
                        "An error occurred trying to execute 'google-chrome --product-version'.",
                        ex);
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                try
                {
                    using var process = Process.Start(
                        new ProcessStartInfo
                        {
                            FileName = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome",
                            ArgumentList = { "--version" },
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                        }
                    );

                    var output = process.StandardOutput.ReadToEnd();
                    var error = process.StandardError.ReadToEnd();
                    
                    process.WaitForExit();
                    process.Kill(true);

                    if (!string.IsNullOrEmpty(error))
                    {
                        throw new Exception(error);
                    }

                    return output.Replace("Google Chrome ", "");
                }
                catch (Exception ex)
                {
                    throw new Exception(
                        $"An error occurred trying to execute '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome --version'.",
                        ex);
                }
            }
            else
            {
                throw new PlatformNotSupportedException("Your operating system is not supported.");
            }
        }
    }
}
