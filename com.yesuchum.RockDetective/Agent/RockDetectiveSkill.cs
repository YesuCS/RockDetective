using System.ComponentModel;

using Rock.AI.Agent;
using Rock.AI.Agent.Annotations;
using Rock.SystemGuid;

using com.yesuchum.RockDetective.Logs;
using com.yesuchum.RockDetective.Mcp;

namespace com.yesuchum.RockDetective.Agent
{
    [AgentSkillName( "Rock Detective" )]
    [AgentSkillGuid( "2C5BE126-3BEC-4B15-A2F4-80FD685ABE84" )]
    [EntityTypeGuid( "AB74DD39-8E95-44A5-B057-63039BAE8AB3" )]
    [Description( "Reads Rock Detective problem-solving case files and searches the web server's IIS logs for evidence." )]
    [AgentPurpose( "Lets the agent look up Rock Detective cases and gather request-level evidence from the IIS logs." )]
    [AgentUsage( "Use ListCases to find a case and its IdKey, then GetCase for its full detail. Use SearchIISLogs when investigating errors or slow pages." )]
    [AgentGuardrail( "Phase names, terms, and gate wording come back the way the case's category has them set up. Use those words when talking about the case." )]
    public class RockDetectiveSkill : AgentSkillComponent
    {
        [Description( "Lists the Rock Detective case files the current person can view, newest first." )]
        [AgentPurpose( "Finds case files and their IdKeys." )]
        [AgentToolGuid( "3BC9DDB1-7E2B-49DB-AC7B-4BC7FCEB3302" )]
        public AgentToolResult ListCases(
            [Description( "Only return cases with this status: Open, Investigating, Solved, Closed, Reopened, or Abandoned." )]
            string status = null,

            [Description( "The most cases to return. Defaults to 25 and cannot exceed 100." )]
            int limit = 25 )
        {
            var person = AgentRequestContext.CurrentPerson;

            if ( person == null )
            {
                return Error( "You must be signed in to list case files." );
            }

            try
            {
                return Success( McpToolService.ListCases( status, limit, person ) );
            }
            catch ( McpToolException ex )
            {
                return Error( ex.Message );
            }
        }

        [Description( "Gets one Rock Detective case file in full: phases, gate checklists, facts, timeline, evidence, hypotheses, experiments, participants, and statements." )]
        [AgentPurpose( "Retrieves the full detail of a single case file." )]
        [AgentToolGuid( "94C24041-4E24-4863-91A3-A9961101B8E4" )]
        public AgentToolResult GetCase(
            [Description( "The IdKey of the case file, as returned by ListCases." )]
            string caseIdKey )
        {
            var person = AgentRequestContext.CurrentPerson;

            if ( person == null )
            {
                return Error( "You must be signed in to view a case file." );
            }

            try
            {
                return Success( McpToolService.GetCase( caseIdKey, person ) );
            }
            catch ( McpToolException ex )
            {
                return Error( ex.Message );
            }
        }

        [Description( "Searches the web server's IIS log files for recent requests. Times are returned in the server's local time zone and results are capped." )]
        [AgentPurpose( "Gathers request-level evidence such as errors and slow pages." )]
        [AgentGuardrail( "Only Rock administrators can search the logs." )]
        [AgentToolName( "Search IIS Logs" )]
        [AgentToolGuid( "125ED239-935B-4013-84A4-D747F42DED48" )]
        public AgentToolResult SearchIisLogs(
            [Description( "How many minutes back to search. Defaults to 60 and cannot exceed 10080 (7 days)." )]
            int minutes = 60,

            [Description( "Only return requests with this HTTP status code, such as 500." )]
            int? status = null,

            [Description( "Only return requests whose URL path contains this text. Case does not matter." )]
            string pathContains = null,

            [Description( "Only return requests with this HTTP method, such as POST." )]
            string method = null,

            [Description( "Only return requests that took at least this many milliseconds." )]
            int? minTimeTakenMs = null,

            [Description( "The most entries to return. Defaults to 100 and cannot exceed 500." )]
            int limit = 100 )
        {
            var person = AgentRequestContext.CurrentPerson;

            if ( person == null || !new Rock.Security.GlobalDefault().IsAuthorized( Rock.Security.Authorization.ADMINISTRATE, person ) )
            {
                return Error( "Only Rock administrators can search the IIS logs." );
            }

            return Success( McpToolService.SearchIisLogs( new IisLogQueryOptions
            {
                MinutesBack = minutes,
                StatusCode = status,
                PathContains = pathContains,
                MethodEquals = method,
                MinTimeTakenMs = minTimeTakenMs,
                MaxEntries = limit
            } ) );
        }
    }
}
