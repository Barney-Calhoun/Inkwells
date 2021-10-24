using Newtonsoft.Json;
using System.IO;
using System.Linq;

namespace Globals
{
    public static class FileMethods
    {
        public static string GetExtensionWithoutPeriod(string filePath)
        {
            return Path.GetExtension(filePath)[1..];
        }

        public static string[] ReadAllNonEmptyLines(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return new string[0];
            }

            return File
                .ReadAllLines(filePath)
                .Select(l => l.Trim())
                .Where(l => l.Length > 0)
                .ToArray();
        }

        public static T DeserializeObjectFromFile<T>(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return default;
            }

            return JsonConvert.DeserializeObject<T>(
                File.ReadAllText(
                    filePath));
        }

        public static void SerializeObjectToFile(string filePath, object value)
        {
            File.WriteAllText(
                filePath,
                JsonConvert.SerializeObject(
                    value,
                    Formatting.Indented));
        }
    }
}
