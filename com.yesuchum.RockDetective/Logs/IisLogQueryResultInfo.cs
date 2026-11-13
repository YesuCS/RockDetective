using System.Collections.Generic;

using Rock.Lava;

namespace com.yesuchum.RockDetective.Logs
{
    public class IisLogQueryResultInfo : LavaDataObject
    {
        public List<IisLogEntryInfo> Entries { get; set; } = new List<IisLogEntryInfo>();

        public int MatchedCount { get; set; }

        public int FilesScanned { get; set; }

        public bool IsTruncated { get; set; }

        public string LogPath { get; set; }

        public string Warning { get; set; }
    }
}
