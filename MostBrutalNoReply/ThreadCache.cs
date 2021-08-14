using System.Collections.Generic;
using System.Linq;

namespace MostBrutalNoReply
{
    public class ThreadCache
    {
        public HashSet<int> ThreadIds { get; set; }
        public Dictionary<int, Forum> ForumsById { get; set; }
        public List<string> MostBrutalNoReplyThreadUrls { get; set; }

        public ThreadCache()
        {
            ThreadIds = new HashSet<int>();
            MostBrutalNoReplyThreadUrls = new List<string>();
            ResetForumsById();
        }

        public void ResetForumsById()
        {
            ForumsById = new Dictionary<int, Forum>();
        }

        public void UpdatetMostBrutalNoReplyThreadUrls()
        {
            var forums = ForumsById.Values;
            var maxThreadAndFirstReplyDifference = forums.Max(f => f.MaxThreadAndFirstReplyDifference);
            var sameReplyDifferenceForums = forums
                .Where(f => f.MaxThreadAndFirstReplyDifference == maxThreadAndFirstReplyDifference);

            MostBrutalNoReplyThreadUrls = sameReplyDifferenceForums
                .SelectMany(f => f.MostBrutalNoReplyThreadUrls)
                .ToList();
        }
    }
}
