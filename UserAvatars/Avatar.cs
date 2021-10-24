using static Globals.Constants;
using static Globals.IEnumerableMethods;
using System.IO;
using System.Linq;

namespace UserAvatars
{
    public class Avatar
    {
        public string ArchiveUrl { get; set; }
        public string Source { get; set; }
        public string Directory { get; set; }
        public string Extension { get; set; }

        public Avatar() { }

        public Avatar(string archiveUrl, string source, string directory, string extension)
        {
            ArchiveUrl = archiveUrl;
            Source = source;
            Directory = directory;
            Extension = extension;
        }

        public string GetSourceWithImgTag()
        {
            return string.Format("[{0}]{1}[/{0}]", "IMG", Source);
        }

        public string GetSize()
        {
            var slashIndexes = ArchiveUrl.FindIndexes(c => c == '/').ToArray();
            var startIndex = slashIndexes[^3] + 1;
            var endIndex = slashIndexes[^2];

            return ArchiveUrl[startIndex..endIndex].ToLower();
        }

        public string GetDomain()
        {
            var startIndex = ArchiveUrl
                .FindIndexes(c => c == '/')
                .Where(i => i > ArchiveBaseUrl.Length)
                .First() + 1;

            var endIndex = ArchiveUrl.LastIndexOf("/data/avatars/");

            var domainUrl = ArchiveUrl[startIndex..endIndex];

            startIndex = domainUrl.IndexOf(':') + 1;

            return domainUrl[startIndex..].Trim('/').ToLower();
        }

        public string GetDate()
        {
            var startIndex = ArchiveUrl.LastIndexOf('?');

            if (startIndex < 0)
            {
                return string.Empty;
            }

            startIndex++;

            return ArchiveUrl[startIndex..];
        }

        public int GetUserId()
        {
            var startIndex = ArchiveUrl.LastIndexOf('/') + 1;
            var endIndex = ArchiveUrl.LastIndexOf('.');

            return int.Parse(ArchiveUrl[startIndex..endIndex]);
        }

        public string GetDateOrUserId()
        {
            var date = GetDate();

            return string.IsNullOrEmpty(date) ? GetUserId().ToString() : date;
        }

        public string GetArchiveDate()
        {
            var startIndex = ArchiveBaseUrl.Length + 1;
            var endIndex = ArchiveUrl
                .FindIndexes(c => c == '/')
                .Where(i => i > ArchiveBaseUrl.Length)
                .First();

            return ArchiveUrl[startIndex..endIndex];
        }

        public string GetName()
        {
            var separator = AvatarHelper.NameSeparator;
            var size = GetSize();
            var domain = GetDomain();
            var dateOrUserId = GetDateOrUserId();
            var archiveDate = GetArchiveDate();

            return $"{size}{separator}{domain}{separator}{dateOrUserId}{separator}{archiveDate}";
        }

        public string GetFileName()
        {
            return $"{GetName()}.{Extension}";
        }

        public string GetPath(string avatarDirectory = null)
        {
            var directory = string.IsNullOrEmpty(avatarDirectory) ? Directory : avatarDirectory;

            return Path.Combine(directory, GetFileName());
        }
    }
}
