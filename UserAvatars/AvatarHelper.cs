using static Globals.FileMethods;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

        public static readonly Dictionary<string, int> SizeSortValues = Sizes
            .Select((size, index) => new KeyValuePair<string, int>(size, index + 1))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        public static readonly HashSet<string> ImageFormats = Configuration
            .Default
            .ImageFormats
            .SelectMany(f => f.FileExtensions)
            .ToHashSet();

        public static string GetDuplicateDirectoryPath(int userId)
        {
            return Path.Combine(userId.ToString(), DuplicatesDirectoryName);
        }

        public static string GetDictionaryFilePath(int userId)
        {
            return Path.Combine(userId.ToString(), DictionaryFileName);
        }

        public static string GetUrlsFilePath(string avatarDirectory)
        {
            return Path.Combine(avatarDirectory, UrlsFileName);
        }

        public static IDictionary<string, Avatar> GetDictionary(int userId)
        {
            var avatarDictionaryPath = GetDictionaryFilePath(userId);

            return File.Exists(avatarDictionaryPath)
                ? DeserializeObjectFromFile<IDictionary<string, Avatar>>(avatarDictionaryPath)
                : new Dictionary<string, Avatar>();
        }

        public static int UpdateUrlFiles(int userId)
        {
            if (!Directory.Exists(userId.ToString()))
            {
                return UserIdDirNotFound;
            }

            var avatarDictionaryPath = GetDictionaryFilePath(userId);

            if (!File.Exists(avatarDictionaryPath))
            {
                return DictionaryFileNotFound;
            }

            Console.WriteLine("Updating avatar URL files...");

            var avatarDirectories = new List<string>
            {
                userId.ToString()
            };

            var duplicateDirectory = GetDuplicateDirectoryPath(userId);

            if (Directory.Exists(duplicateDirectory))
            {
                avatarDirectories.Add(duplicateDirectory);
            }

            var avatarDictionary = GetDictionary(userId);

            foreach (var avatarDirectory in avatarDirectories)
            {
                var avatarNamesAndExtensions = Directory
                    .GetFiles(avatarDirectory)
                    .Where(f => ImageFormats.Contains(GetExtensionWithoutPeriod(f)))
                    .ToDictionary(f => Path.GetFileNameWithoutExtension(f), f => GetExtensionWithoutPeriod(f));

                var avatars = avatarDictionary
                    .Values
                    .Where(a => avatarNamesAndExtensions.ContainsKey(a.GetName()))
                    .OrderBy(a => SizeSortValues[a.GetSize()])
                    .ThenBy(a => a.GetDateOrUserId());

                foreach (var avatar in avatars)
                {
                    avatar.Directory = avatarDirectory;
                    avatar.Extension = avatarNamesAndExtensions[avatar.GetName()];
                }

                File.WriteAllText(
                    GetUrlsFilePath(avatarDirectory),
                    string.Join(
                        Environment.NewLine,
                        avatars.Select(a => a.GetSourceWithImgTag())));
            }

            SerializeObjectToFile(avatarDictionaryPath, avatarDictionary);

            return Success;
        }
    }
}
