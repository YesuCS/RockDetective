using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

using Rock;
using Rock.Utility;

using com.yesuchum.RockDetective.Data;
using com.yesuchum.RockDetective.Enums;
using com.yesuchum.RockDetective.Logs;

namespace com.yesuchum.RockDetective.Mcp
{
    public static class McpToolService
    {
        private static readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore
        };

        public static IReadOnlyList<McpToolDefinition> GetTools()
        {
            return _tools;
        }

        public static JToken Execute( string toolName, JObject arguments, Rock.Model.Person person )
        {
            var tool = _tools.FirstOrDefault( t => t.Name == toolName );

            if ( tool == null )
            {
                throw new McpToolException( $"Unknown tool '{toolName}'." );
            }

            return tool.Handler( arguments ?? new JObject(), person );
        }

        private static readonly List<McpToolDefinition> _tools = new List<McpToolDefinition>
        {
            new McpToolDefinition
            {
                Name = "list_cases",
                Description = "Lists Rock Detective problem-solving case files the current person is authorized to view. Optionally filter by status.",
                InputSchema = JObject.Parse( @"{
                    ""type"": ""object"",
                    ""properties"": {
                        ""status"": { ""type"": ""string"", ""enum"": [ ""Open"", ""Investigating"", ""Solved"", ""Closed"", ""Reopened"", ""Abandoned"" ], ""description"": ""Only return cases with this status."" },
                        ""limit"": { ""type"": ""integer"", ""description"": ""Maximum number of cases to return. Default 25, max 100."" }
                    }
                }" ),
                Handler = ListCases
            },
            new McpToolDefinition
            {
                Name = "get_case",
                Description = "Gets the full detail of a single Rock Detective case file, including entries, hypotheses, experiments, participants and witness statements.",
                InputSchema = JObject.Parse( @"{
                    ""type"": ""object"",
                    ""properties"": {
                        ""idKey"": { ""type"": ""string"", ""description"": ""The IdKey of the case file, as returned by list_cases."" }
                    },
                    ""required"": [ ""idKey"" ]
                }" ),
                Handler = GetCase
            },
            new McpToolDefinition
            {
                Name = "search_iis_logs",
                Description = "Searches the IIS W3C log files on this Rock server for recent requests. Useful as evidence when investigating errors or slow pages. Results are capped and times are returned in the server's local time zone.",
                InputSchema = JObject.Parse( @"{
                    ""type"": ""object"",
                    ""properties"": {
                        ""minutes"": { ""type"": ""integer"", ""description"": ""How many minutes back to search. Default 60, max 10080 (7 days)."" },
                        ""status"": { ""type"": ""integer"", ""description"": ""Only return requests with this HTTP status code (e.g. 500)."" },
                        ""pathContains"": { ""type"": ""string"", ""description"": ""Only return requests whose URL path contains this text (case-insensitive)."" },
                        ""method"": { ""type"": ""string"", ""description"": ""Only return requests with this HTTP method (e.g. POST)."" },
                        ""minTimeTakenMs"": { ""type"": ""integer"", ""description"": ""Only return requests that took at least this many milliseconds."" },
                        ""limit"": { ""type"": ""integer"", ""description"": ""Maximum number of entries to return. Default 100, max 500."" }
                    }
                }" ),
                Handler = SearchIisLogs
            }
        };

        private static JToken ListCases( JObject arguments, Rock.Model.Person person )
        {
            return ToJson( ListCases( arguments.Value<string>( "status" ), arguments.Value<int?>( "limit" ), person ) );
        }

        public static object ListCases( string statusText, int? requestedLimit, Rock.Model.Person person )
        {
            var limit = Math.Min( Math.Max( requestedLimit ?? 25, 1 ), 100 );

            using ( var detectiveContext = new RockDetectiveContext() )
            {
                var query = new CaseFileService( detectiveContext ).Queryable().AsNoTracking();

                if ( statusText.IsNotNullOrWhiteSpace() )
                {
                    if ( !Enum.TryParse<CaseStatus>( statusText, true, out var status ) )
                    {
                        throw new McpToolException( $"'{statusText}' is not a valid case status." );
                    }

                    query = query.Where( c => c.Status == status );
                }

                var cases = query
                    .OrderByDescending( c => c.CreatedDateTime )
                    .ToList()
                    .Where( c => c.IsAuthorized( Rock.Security.Authorization.VIEW, person ) )
                    .Take( limit )
                    .Select( c => new
                    {
                        IdKey = IdHasher.Instance.GetHash( c.Id ),
                        c.Title,
                        CategoryName = c.Category?.Name,
                        Status = c.Status.ToString(),
                        Phase = Method.MethodProfile.ForCategory( c.CategoryId ).GetPhase( c.CurrentPhase ).Name,
                        Mode = c.Mode.ToString(),
                        ProblemStatement = c.ProblemStatement.Truncate( 300 ),
                        CreatedDateTime = c.CreatedDateTime?.ToString( "s" )
                    } )
                    .ToList();

                return cases;
            }
        }

        private static JToken GetCase( JObject arguments, Rock.Model.Person person )
        {
            return ToJson( GetCase( arguments.Value<string>( "idKey" ), person ) );
        }

        public static ViewModels.CaseFileBag GetCase( string idKey, Rock.Model.Person person )
        {
            if ( idKey.IsNullOrWhiteSpace() )
            {
                throw new McpToolException( "The idKey argument is required." );
            }

            using ( var detectiveContext = new RockDetectiveContext() )
            {
                var id = IdHasher.Instance.GetId( idKey );
                var caseFile = id.HasValue ? new CaseFileService( detectiveContext ).Get( id.Value ) : null;

                if ( caseFile == null )
                {
                    throw new McpToolException( $"No case file was found with the IdKey '{idKey}'." );
                }

                if ( !caseFile.IsAuthorized( Rock.Security.Authorization.VIEW, person ) )
                {
                    throw new McpToolException( "You are not authorized to view this case file." );
                }

                var entries = detectiveContext.CaseEntries.Where( e => e.CaseFileId == caseFile.Id ).ToList();
                var hypotheses = detectiveContext.Hypotheses.Where( h => h.CaseFileId == caseFile.Id ).ToList();
                var experiments = detectiveContext.Experiments.Where( e => e.CaseFileId == caseFile.Id ).ToList();
                var participants = detectiveContext.CaseParticipants.Where( p => p.CaseFileId == caseFile.Id ).ToList();
                var statements = detectiveContext.CaseStatements.Where( s => s.CaseFileId == caseFile.Id ).ToList();

                return CaseMapper.ToCaseFileBag( caseFile, entries, hypotheses, experiments, participants, null, statements );
            }
        }

        private static JToken SearchIisLogs( JObject arguments, Rock.Model.Person person )
        {
            var options = new IisLogQueryOptions
            {
                MinutesBack = arguments.Value<int?>( "minutes" ) ?? 60,
                StatusCode = arguments.Value<int?>( "status" ),
                PathContains = arguments.Value<string>( "pathContains" ),
                MethodEquals = arguments.Value<string>( "method" ),
                MinTimeTakenMs = arguments.Value<int?>( "minTimeTakenMs" ),
                MaxEntries = arguments.Value<int?>( "limit" ) ?? 100
            };

            return ToJson( SearchIisLogs( options ) );
        }

        public static object SearchIisLogs( IisLogQueryOptions options )
        {
            var result = IisLogReader.Query( options );

            return new
            {
                result.MatchedCount,
                result.FilesScanned,
                result.IsTruncated,
                result.LogPath,
                result.Warning,
                Entries = result.Entries.Select( e => new
                {
                    DateTime = e.DateTime.ToString( "s" ),
                    e.Method,
                    e.UriStem,
                    e.UriQuery,
                    e.Status,
                    e.SubStatus,
                    e.TimeTakenMs,
                    e.ClientIp,
                    e.UserName,
                    e.UserAgent
                } ).ToList()
            };
        }

        private static JToken ToJson( object value )
        {
            return JToken.FromObject( value, JsonSerializer.Create( _serializerSettings ) );
        }
    }
}
