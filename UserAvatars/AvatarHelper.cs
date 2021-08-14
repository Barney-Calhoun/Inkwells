using static Globals.FileMethods;
using static Globals.IEnumerableMethods;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace UserAvatars
{
    public static class AvatarHelper
    {
        public const char NameSeparator = '_';
        public const string GifExtension = "gif";

        public const int Success = 0;
        public const int UserIdDirNotFound = 1;
        public const int DictionaryFileNotFound = 2;

        public static readonly string DuplicatesDirectoryName = $"0A{NameSeparator}Duplicates";
        public static readonly string UrlsFileName = $"0B{NameSeparator}URLs.txt";
        public static readonly string DictionaryFileName = $"0C{NameSeparator}Dictionary.json";

        public static readonly string[] Sizes = new string[] { "o", "h", "l", "m", "s" };
        public static readonly Dictionary<string, int> SizeValues = Sizes
            .Select((size, index) => new KeyValuePair<string, int>(size, index + 1))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        public static readonly string[] ImageFormats = Configuration
            .Default
            .ImageFormats
            .SelectMany(f => f.FileExtensions)
            .ToArray();

        public static string GetSize(string avatarFile)
        {
            var avatarName = Path.GetFileNameWithoutExtension(avatarFile);
            var endIndex = avatarName.IndexOf(NameSeparator);

            return avatarName[..endIndex];
        }

        public static string GetDateOrUserId(string avatarFile)
        {
            var avatarName = Path.GetFileNameWithoutExtension(avatarFile);
            var startIndex = avatarName.LastIndexOf(NameSeparator) + 1;

            return avatarName[startIndex..];
        }

        public static bool FileIsValid(string avatarFile)
        {
            var sizes = Sizes.ToString("|", "(", ")");
            var imageFormats = ImageFormats.ToString("|", "(", ")");
            var pattern = $@"{sizes}{NameSeparator}\d+{NameSeparator}\d+\.{imageFormats}$";

            return Regex.IsMatch(avatarFile, pattern);
        }

        public static string GetDuplicateDirectoryPath(int userId)
        {
            return $"{userId}{Path.DirectorySeparatorChar}{DuplicatesDirectoryName}";
        }

        public static string GetUrlsFilePath(string avatarDirectory)
        {
            return $"{avatarDirectory}{Path.DirectorySeparatorChar}{UrlsFileName}";
        }

        public static string GetDictionaryFilePath(int userId)
        {
            return $"{userId}{Path.DirectorySeparatorChar}{DictionaryFileName}";
        }

        public static Dictionary<string, Avatar> GetDictionary(int userId)
        {
            var avatarDictionaryPath = GetDictionaryFilePath(userId);

            return File.Exists(avatarDictionaryPath)
                ? JsonConvert.DeserializeObject<Dictionary<string, Avatar>>(
                    File.ReadAllText(
                        avatarDictionaryPath))
                : new Dictionary<string, Avatar>();
        }

        public static void SaveDictionaryToFile(int userId, Dictionary<string, Avatar> dictionary)
        {
            File.WriteAllText(
                GetDictionaryFilePath(userId),
                JsonConvert.SerializeObject(
                    dictionary,
                    Formatting.Indented));
        }

        public static int UpdateUrlFiles(int userId)
        {
            if (!Directory.Exists(userId.ToString()))
            {
                return UserIdDirNotFound;
            }

            if (!File.Exists(GetDictionaryFilePath(userId)))
            {
                return DictionaryFileNotFound;
            }

            Console.WriteLine("Updating avatar URL files...");

            var avatarDirectories = new List<string>();
            var duplicateDirectory = GetDuplicateDirectoryPath(userId);
            avatarDirectories.Add(userId.ToString());
            if (Directory.Exists(duplicateDirectory))
            {
                avatarDirectories.Add(duplicateDirectory);
            }

            var avatarDictionary = GetDictionary(userId);
            var avatarDictionaryKeysByName = avatarDictionary
                .Values
                .Select(a => new KeyValuePair<string, string>(a.GetName(), a.ArchiveUrl))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            foreach (var avatarDirectory in avatarDirectories)
            {
                var avatarFiles = Directory
                        .GetFiles(avatarDirectory)
                        .Where(f => FileIsValid(f))
                        .OrderBy(f => SizeValues[GetSize(f)])
                        .ThenBy(f => GetDateOrUserId(f))
                        .ToArray();

                if (avatarFiles.Length > 0)
                {
                    var avatarUrlsFilePath = GetUrlsFilePath(avatarDirectory);
                    File.WriteAllText(avatarUrlsFilePath, string.Empty);

                    using var avatarUrlsFile = new StreamWriter(avatarUrlsFilePath, true);

                    foreach (var avatarFile in avatarFiles)
                    {
                        var avatarName = Path.GetFileNameWithoutExtension(avatarFile);

                        if (avatarDictionaryKeysByName.ContainsKey(avatarName))
                        {
                            var avatarDictionaryKey = avatarDictionaryKeysByName[avatarName];
                            var avatar = avatarDictionary[avatarDictionaryKey];
                            avatar.Directory = avatarDirectory;
                            avatar.Extension = GetExtensionWithoutPeriod(avatarFile);
                            avatarUrlsFile.WriteLine(avatar.GetSourceWithImgTag());
                        }
                    }
                }
            }

            SaveDictionaryToFile(userId, avatarDictionary);

            return Success;
        }
    }
}
