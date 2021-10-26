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
            MaxThreadAndFirstReplyDifference = new TimeSpan(0, 0, 0, 0, 0);
            MostBrutalNoReplyThreadUrls = new List<string>();
        }

        public Forum(int id, string name) : this()
        {
            Id = id;
            Name = name;
        }
    }
}
