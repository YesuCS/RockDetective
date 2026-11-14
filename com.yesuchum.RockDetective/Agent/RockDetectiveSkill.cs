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
    [Description( "Works Rock Detective problem-solving case files from start to finish: opens a case, gathers facts and evidence, tests possible causes, and closes with a postmortem. Also searches the web server's IIS logs for evidence." )]
    [AgentPurpose( "Lets the agent diagnose a problem methodically and document the decisions, findings, and fix in a case file." )]
    [AgentUsage( "Use ListCases to find a case and its IdKey, then GetCase for its full detail. Use SearchIISLogs when investigating errors or slow pages." )]
    [AgentUsage( "To work a problem, open a case with CreateCase and follow the phase it reports. Every tool that changes a case returns the current phase, its guidance, its checklist, and the next step. Do the work the checklist asks for, save your reasoning with SavePhaseNotes, then use CompletePhase. Finish with CloseCase." )]
    [AgentGuardrail( "Record only what is verified as a corroborated fact. Mark anything a person reported, or anything inferred, as not corroborated until evidence backs it." )]
    [AgentGuardrail( "Change one thing at a time in an experiment, log every change, and revert it or say why it stays." )]
    [AgentGuardrail( "Do not confirm a hypothesis without an experiment that supports it." )]
    [AgentGuardrail( "Phase names, terms, and gate wording come back the way the case's category has them set up. Use those words when talking about the case." )]
    public partial class RockDetectiveSkill : AgentSkillComponent
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
