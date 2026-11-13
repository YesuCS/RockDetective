namespace com.yesuchum.RockDetective.Logs
{
    public class IisLogQueryOptions
    {
        public int MinutesBack { get; set; } = 60;

        public int? StatusCode { get; set; }

        public string PathContains { get; set; }

        public string MethodEquals { get; set; }

        public int MaxEntries { get; set; } = 100;

        public int MaxFiles { get; set; } = 5;

        public int? MinTimeTakenMs { get; set; }
    }
}
