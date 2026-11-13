using System;

using Rock.Lava;

namespace com.yesuchum.RockDetective.Logs
{
    public class IisLogEntryInfo : LavaDataObject
    {
        public DateTime DateTime { get; set; }

        public string Method { get; set; }

        public string UriStem { get; set; }

        public string UriQuery { get; set; }

        public int Status { get; set; }

        public int SubStatus { get; set; }

        public int TimeTakenMs { get; set; }

        public string ClientIp { get; set; }

        public string UserName { get; set; }

        public string UserAgent { get; set; }
    }
}
