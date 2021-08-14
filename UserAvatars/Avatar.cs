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

        public string GetArchiveDate()
        {
            var slashIndexes = ArchiveUrl.FindIndexes(c => c == '/').ToArray();
            var startIndex = slashIndexes[^9] + 1;
            var endIndex = slashIndexes[^8];

            return ArchiveUrl[startIndex..endIndex];
        }

        public string GetDate()
        {
            var startIndex = ArchiveUrl.LastIndexOf('?');

            return startIndex < 0 ? string.Empty : ArchiveUrl[(startIndex + 1)..];
        }

        public int GetUserId()
        {
            var startIndex = ArchiveUrl.LastIndexOf('/') + 1;
            var endIndex = ArchiveUrl.LastIndexOf('.');

            return int.Parse(ArchiveUrl[startIndex..endIndex]);
        }

        public string GetName()
        {
            var separator = AvatarHelper.NameSeparator;
            var size = GetSize();
            var archiveDate = GetArchiveDate();
            var date = GetDate();
            var dateOrUserId = string.IsNullOrEmpty(date) ? GetUserId().ToString() : date;

            return $"{size}{separator}{archiveDate}{separator}{dateOrUserId}";
        }

        public string GetFileName()
        {
            return $"{GetName()}.{Extension}";
        }

        public string GetPath(string avatarDirectory = null)
        {
            var directory = string.IsNullOrEmpty(avatarDirectory) ? Directory : avatarDirectory;

            return $"{directory}{Path.DirectorySeparatorChar}{GetFileName()}";
        }
    }
}
