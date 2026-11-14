using System.ComponentModel;

using Rock.AI.Agent;
using Rock.AI.Agent.Annotations;
using Rock.SystemGuid;

using com.yesuchum.RockDetective.Logs;
using com.yesuchum.RockDetective.Mcp;

namespace com.yesuchum.RockDetective.Agent
{
    [AgentSkillGuid( "2C5BE126-3BEC-4B15-A2F4-80FD685ABE84" )]
    [EntityTypeGuid( "AB74DD39-8E95-44A5-B057-63039BAE8AB3" )]
    [SkillDescription( "Skill.Description" )]
    [SkillPurpose( "Skill.Purpose" )]
    [SkillUsage( "Skill.Usage" )]
    [SkillGuardrail( "Skill.Guardrails" )]
    public partial class RockDetectiveSkill : AgentSkillComponent
    {
        [SkillDescription( "ListCases.Description" )]
        [SkillPurpose( "ListCases.Purpose" )]
        [SkillUsage( "ListCases.Description" )]
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

        [SkillDescription( "GetCase.Description" )]
        [SkillPurpose( "GetCase.Purpose" )]
        [SkillUsage( "GetCase.Description" )]
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

        [SkillDescription( "SearchIisLogs.Description" )]
        [SkillPurpose( "SearchIisLogs.Purpose" )]
        [SkillUsage( "SearchIisLogs.Description" )]
        [AgentToolName( "SearchIISLogs" )]
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
            return Run( person => McpToolService.SearchIisLogs( new IisLogQueryOptions
            {
                MinutesBack = minutes,
                StatusCode = status,
                PathContains = pathContains,
                MethodEquals = method,
                MinTimeTakenMs = minTimeTakenMs,
                MaxEntries = limit
            }, person ) );
        }
    }
}
