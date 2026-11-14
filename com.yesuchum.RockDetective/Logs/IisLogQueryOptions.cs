using System;

namespace com.yesuchum.RockDetective.Logs
{
    public class IisLogQueryOptions
    {
        public int MinutesBack { get; set; } = 60;

        public int? StatusCode { get; set; }

        public string PathContains { get; set; }

        public string MethodEquals { get; set; }

        public int MaxEntries { get; set; } = 100;

        public int MaxFiles { get; set; } = 10;

        public DateTime? StartDateTime { get; set; }

        public DateTime? EndDateTime { get; set; }

        public bool IsUserAgentIncluded { get; set; }

        public int? MinTimeTakenMs { get; set; }
    }
}
