using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using Rock;
using Rock.Web.Cache;

namespace com.yesuchum.RockDetective.Logs
{
    public static class IisLogReader
    {
        public const string LogPathAttributeKey = "com_yesuchum_RockDetective_IisLogPath";

        private const string DefaultLogPath = @"%SystemDrive%\inetpub\logs\LogFiles";

        private const int MaxEntriesHardCap = 500;

        private const int MaxFilesHardCap = 10;

        private const int MaxMinutesHardCap = 10080;

        private const int MaxDaysBackForRange = 30;

        private const string RedactedValue = "[redacted]";

        private static readonly string[] _sensitiveParameterParts = new[]
        {
            "rckipid", "rckid", "token", "key", "code", "password", "passwd", "pwd", "secret", "signature", "sig", "auth", "session", "ticket", "otp", "credential"
        };

        public static IisLogQueryResultInfo Query( IisLogQueryOptions options )
        {
            options = options ?? new IisLogQueryOptions();

            var result = new IisLogQueryResultInfo();

            var maxEntries = Math.Min( Math.Max( options.MaxEntries, 1 ), MaxEntriesHardCap );
            var maxFiles = Math.Min( Math.Max( options.MaxFiles, 1 ), MaxFilesHardCap );
            var minutesBack = Math.Min( Math.Max( options.MinutesBack, 1 ), MaxMinutesHardCap );
            var nowUtc = DateTime.UtcNow;
            var cutoffUtc = nowUtc.AddMinutes( -minutesBack );
            var endUtc = nowUtc;
            var isRange = options.StartDateTime.HasValue;

            if ( isRange )
            {
                cutoffUtc = ToUtc( options.StartDateTime.Value );

                if ( cutoffUtc < nowUtc.AddDays( -MaxDaysBackForRange ) )
                {
                    cutoffUtc = nowUtc.AddDays( -MaxDaysBackForRange );
                }

                endUtc = options.EndDateTime.HasValue ? ToUtc( options.EndDateTime.Value ) : nowUtc;

                if ( endUtc > cutoffUtc.AddMinutes( MaxMinutesHardCap ) )
                {
                    endUtc = cutoffUtc.AddMinutes( MaxMinutesHardCap );
                }

                if ( endUtc <= cutoffUtc )
                {
                    result.Warning = "The end of the time range has to be after its start.";
                    return result;
                }
            }

            var configuredPath = GlobalAttributesCache.Get().GetValue( LogPathAttributeKey );

            var logPath = Environment.ExpandEnvironmentVariables( configuredPath.IsNotNullOrWhiteSpace()
                ? configuredPath.Trim()
                : DefaultLogPath );

            result.LogPath = logPath;

            if ( !Directory.Exists( logPath ) )
            {
                result.Warning = $"Log directory '{logPath}' does not exist or is not accessible.";
                return result;
            }

            List<FileInfo> files;

            try
            {
                var candidates = Directory.EnumerateFiles( logPath, "*.log", SearchOption.AllDirectories )
                    .Select( f => new FileInfo( f ) )
                    .Where( f => f.LastWriteTimeUtc >= cutoffUtc && ( !isRange || f.CreationTimeUtc <= endUtc ) );

                files = ( isRange ? candidates.OrderBy( f => f.LastWriteTimeUtc ) : candidates.OrderByDescending( f => f.LastWriteTimeUtc ) )
                    .Take( maxFiles )
                    .ToList();
            }
            catch ( UnauthorizedAccessException )
            {
                result.Warning = $"The application does not have read access to '{logPath}'.";
                return result;
            }

            if ( !files.Any() )
            {
                result.Warning = isRange
                    ? $"No log files in '{logPath}' cover that time range."
                    : $"No log files in '{logPath}' were modified within the last {minutesBack} minutes.";
                return result;
            }

            var entries = new List<IisLogEntryInfo>();
            var matchedCount = 0;

            foreach ( var file in files )
            {
                result.FilesScanned++;

                try
                {
                    matchedCount += ParseFile( file.FullName, cutoffUtc, endUtc, options, entries, maxEntries );
                }
                catch ( IOException )
                {
                    continue;
                }
                catch ( UnauthorizedAccessException )
                {
                    continue;
                }
            }

            result.MatchedCount = matchedCount;
            result.Entries = entries.OrderByDescending( e => e.DateTime ).Take( maxEntries ).ToList();
            result.IsTruncated = matchedCount > result.Entries.Count;

            return result;
        }

        private static DateTime ToUtc( DateTime value )
        {
            return value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind( value, DateTimeKind.Local ).ToUniversalTime();
        }

        public static string RedactQuery( string query )
        {
            if ( query.IsNullOrWhiteSpace() )
            {
                return query;
            }

            var pairs = query.Split( '&' );

            for ( var i = 0; i < pairs.Length; i++ )
            {
                var separator = pairs[i].IndexOf( '=' );

                if ( separator <= 0 )
                {
                    continue;
                }

                var name = pairs[i].Substring( 0, separator );

                if ( _sensitiveParameterParts.Any( part => name.IndexOf( part, StringComparison.OrdinalIgnoreCase ) >= 0 ) )
                {
                    pairs[i] = name + "=" + RedactedValue;
                }
            }

            return string.Join( "&", pairs );
        }

        private static int ParseFile( string path, DateTime cutoffUtc, DateTime endUtc, IisLogQueryOptions options, List<IisLogEntryInfo> entries, int maxEntries )
        {
            Dictionary<string, int> fieldMap = null;
            var matchedCount = 0;

            using ( var stream = new FileStream( path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite ) )
            using ( var reader = new StreamReader( stream ) )
            {
                string line;

                while ( ( line = reader.ReadLine() ) != null )
                {
                    if ( line.StartsWith( "#Fields:" ) )
                    {
                        fieldMap = line.Substring( 8 )
                            .Split( new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries )
                            .Select( ( name, index ) => new { name, index } )
                            .ToDictionary( x => x.name, x => x.index );
                        continue;
                    }

                    if ( line.StartsWith( "#" ) || fieldMap == null )
                    {
                        continue;
                    }

                    var entry = ParseLine( line, fieldMap );

                    if ( entry == null )
                    {
                        continue;
                    }

                    var entryUtc = DateTime.SpecifyKind( entry.DateTime, DateTimeKind.Utc );

                    if ( entryUtc < cutoffUtc || entryUtc > endUtc )
                    {
                        continue;
                    }

                    entry.DateTime = entryUtc.ToLocalTime();

                    if ( options.StatusCode.HasValue && entry.Status != options.StatusCode.Value )
                    {
                        continue;
                    }

                    if ( options.PathContains.IsNotNullOrWhiteSpace()
                        && ( entry.UriStem == null || entry.UriStem.IndexOf( options.PathContains, StringComparison.OrdinalIgnoreCase ) < 0 ) )
                    {
                        continue;
                    }

                    if ( options.MethodEquals.IsNotNullOrWhiteSpace()
                        && !string.Equals( entry.Method, options.MethodEquals, StringComparison.OrdinalIgnoreCase ) )
                    {
                        continue;
                    }

                    if ( options.MinTimeTakenMs.HasValue && entry.TimeTakenMs < options.MinTimeTakenMs.Value )
                    {
                        continue;
                    }

                    matchedCount++;
                    entries.Add( entry );

                    if ( entries.Count > maxEntries * 2 )
                    {
                        var newest = entries.OrderByDescending( e => e.DateTime ).Take( maxEntries ).ToList();
                        entries.Clear();
                        entries.AddRange( newest );
                    }
                }
            }

            return matchedCount;
        }

        private static IisLogEntryInfo ParseLine( string line, Dictionary<string, int> fieldMap )
        {
            var parts = line.Split( ' ' );

            var date = GetField( parts, fieldMap, "date" );
            var time = GetField( parts, fieldMap, "time" );

            if ( date == null || time == null )
            {
                return null;
            }

            if ( !DateTime.TryParseExact( $"{date} {time}", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime ) )
            {
                return null;
            }

            return new IisLogEntryInfo
            {
                DateTime = dateTime,
                Method = GetField( parts, fieldMap, "cs-method" ),
                UriStem = GetField( parts, fieldMap, "cs-uri-stem" ),
                UriQuery = RedactQuery( NormalizeEmpty( GetField( parts, fieldMap, "cs-uri-query" ) ) ),
                Status = GetField( parts, fieldMap, "sc-status" ).AsInteger(),
                SubStatus = GetField( parts, fieldMap, "sc-substatus" ).AsInteger(),
                TimeTakenMs = GetField( parts, fieldMap, "time-taken" ).AsInteger(),
                ClientIp = GetField( parts, fieldMap, "c-ip" ),
                UserName = NormalizeEmpty( GetField( parts, fieldMap, "cs-username" ) ),
                UserAgent = NormalizeEmpty( GetField( parts, fieldMap, "cs(User-Agent)" ) )
            };
        }

        private static string GetField( string[] parts, Dictionary<string, int> fieldMap, string name )
        {
            return fieldMap.TryGetValue( name, out var index ) && index < parts.Length ? parts[index] : null;
        }

        private static string NormalizeEmpty( string value )
        {
            return value == "-" ? null : value;
        }
    }
}
