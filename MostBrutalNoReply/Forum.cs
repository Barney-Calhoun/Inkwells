using System;
using System.Collections.Generic;

namespace MostBrutalNoReply
{
    public class Forum
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public TimeSpan MaxThreadAndFirstReplyDifference { get; set; }
        public List<string> MostBrutalNoReplyThreadUrls { get; set; }

        public Forum()
        {
            ResetNoReplies();
        }

        public Forum(int id, string name)
        {
            Id = id;
            Name = name;
            ResetNoReplies();
        }

        public void ResetNoReplies()
        {
            MaxThreadAndFirstReplyDifference = new TimeSpan(0, 0, 0, 0, 0);
            MostBrutalNoReplyThreadUrls = new List<string>();
        }
    }
}
