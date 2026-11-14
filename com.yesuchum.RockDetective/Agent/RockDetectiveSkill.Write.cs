using System;
using System.ComponentModel;

using Rock.AI.Agent;
using Rock.AI.Agent.Annotations;
using Rock.SystemGuid;

using com.yesuchum.RockDetective.Mcp;

namespace com.yesuchum.RockDetective.Agent
{
    public partial class RockDetectiveSkill
    {
        private AgentToolResult Run( Func<Rock.Model.Person, object> action )
        {
            try
            {
                return Success( action( AgentRequestContext.CurrentPerson ) );
            }
            catch ( McpToolException ex )
            {
                return Error( ex.Message );
            }
        }

        [SkillDescription( "ListMyStatementRequests.Description" )]
        [SkillPurpose( "ListMyStatementRequests.Purpose" )]
        [SkillUsage( "ListMyStatementRequests.Description" )]
        [AgentToolGuid( "6B40FC5E-8D1D-4E3A-9C47-2F1D3C4B5A61" )]
        public AgentToolResult ListMyStatementRequests()
        {
            return Run( person => CaseWorkflowService.ListMyStatementRequests( person ) );
        }

        [SkillDescription( "SubmitStatement.Description" )]
        [SkillPurpose( "SubmitStatement.Purpose" )]
        [SkillUsage( "SubmitStatement.Description" )]
        [AgentToolGuid( "7C510D6F-9E2E-4F4B-8D58-302E4D5C6B72" )]
        public AgentToolResult SubmitStatement(
            [Description( "The IdKey of the request, from ListMyStatementRequests." )]
            string statementIdKey,

            [Description( "What the person saw or knows, in their own words." )]
            string statement,

            [Description( "An optional link that goes with the statement." )]
            string url = null )
        {
            return Run( person => CaseWorkflowService.SubmitStatement( statementIdKey, statement, url, person ) );
        }

        [SkillDescription( "DeclineStatement.Description" )]
        [SkillPurpose( "DeclineStatement.Purpose" )]
        [SkillUsage( "DeclineStatement.Description" )]
        [AgentToolGuid( "8D621E70-AF3F-405C-9E69-413F5E6D7C83" )]
        public AgentToolResult DeclineStatement(
            [Description( "The IdKey of the request, from ListMyStatementRequests." )]
            string statementIdKey,

            string reason = null )
        {
            return Run( person => CaseWorkflowService.DeclineStatement( statementIdKey, reason, person ) );
        }

        [SkillDescription( "ListCategories.Description" )]
        [SkillPurpose( "ListCategories.Purpose" )]
        [SkillUsage( "ListCategories.Description" )]
        [AgentToolGuid( "6A2F0C5E-8B1D-4E3A-9C47-1F0D2B3A4C51" )]
        public AgentToolResult ListCategories()
        {
            return Run( person => CaseWorkflowService.ListCategories( person ) );
        }

        [SkillDescription( "CreateCase.Description" )]
        [SkillPurpose( "CreateCase.Purpose" )]
        [SkillUsage( "CreateCase.Description" )]
        [AgentToolGuid( "7B301D6F-9C2E-4F4B-8D58-201E3C4B5D62" )]
        public AgentToolResult CreateCase(
            [Description( "A short handle for the case." )]
            string title,

            [Description( "One clear, concise statement of the problem with nothing unverified and nothing that is not germane." )]
            string problemStatement,

            [Description( "The category name or guid, from ListCategories." )]
            string category,

            [Description( "Simple for easy problems or Advanced for difficult ones. Defaults to Simple." )]
            string mode = null,

            [Description( "True only if every detail in the problem statement has been verified and all of it is germane. Advanced cases need this to complete the problem definition phase." )]
            bool isProblemStatementVerified = false )
        {
            return Run( person => CaseWorkflowService.CreateCase( title, problemStatement, category, mode, isProblemStatementVerified, person ) );
        }

        [SkillDescription( "UpdateCase.Description" )]
        [SkillPurpose( "UpdateCase.Purpose" )]
        [SkillUsage( "UpdateCase.Description" )]
        [AgentToolGuid( "8C412E70-AD3F-405C-9E69-312F4D5C6E73" )]
        public AgentToolResult UpdateCase(
            string caseIdKey,
            string title = null,
            string problemStatement = null,

            [Description( "The category name or guid." )]
            string category = null,

            [Description( "True only if every detail in the problem statement has been verified and all of it is germane." )]
            bool? isProblemStatementVerified = null )
        {
            return Run( person => CaseWorkflowService.UpdateCase( caseIdKey, title, problemStatement, category, isProblemStatementVerified, person ) );
        }

        [SkillDescription( "AddParticipant.Description" )]
        [SkillPurpose( "AddParticipant.Purpose" )]
        [SkillUsage( "AddParticipant.Description" )]
        [AgentToolGuid( "9D523F81-BE40-416D-AF7A-42305E6D7F84" )]
        public AgentToolResult AddParticipant(
            string caseIdKey,

            [Description( "Champion, Recorder, Communicator, Member, or Witness. Defaults to Member." )]
            string role = null,

            [Description( "The person alias guid of a Rock person." )]
            string personAliasGuid = null,

            [Description( "The name of someone who is not in Rock." )]
            string externalName = null,

            [Description( "How to reach an external person." )]
            string externalContact = null )
        {
            return Run( person => CaseWorkflowService.AddParticipant( caseIdKey, role, personAliasGuid, externalName, externalContact, person ) );
        }

        [SkillDescription( "WaiveChampion.Description" )]
        [SkillPurpose( "WaiveChampion.Purpose" )]
        [SkillUsage( "WaiveChampion.Description" )]
        [AgentToolGuid( "AE634092-CF51-427E-B08B-534160708095" )]
        public AgentToolResult WaiveChampion(
            string caseIdKey,

            [Description( "Why a Champion is not needed." )]
            string reason )
        {
            return Run( person => CaseWorkflowService.WaiveChampion( caseIdKey, reason, person ) );
        }

        [SkillDescription( "RecordStatement.Description" )]
        [SkillPurpose( "RecordStatement.Purpose" )]
        [SkillUsage( "RecordStatement.Description" )]
        [AgentToolGuid( "BF7451A3-D062-438F-819C-6452718190A6" )]
        public AgentToolResult RecordStatement(
            string caseIdKey,

            [Description( "The IdKey of the participant, from AddParticipant or GetCase." )]
            string participantIdKey,

            string statement,

            [Description( "An optional link that goes with the statement." )]
            string url = null,

            [Description( "Also add the statement to the fact list." )]
            bool addToFacts = false,

            [Description( "Marks the added fact as corroborated by evidence." )]
            bool isCorroborated = false )
        {
            return Run( person => CaseWorkflowService.RecordStatement( caseIdKey, participantIdKey, statement, url, addToFacts, isCorroborated, person ) );
        }

        [SkillDescription( "AddEntry.Description" )]
        [SkillPurpose( "AddEntry.Purpose" )]
        [SkillUsage( "AddEntry.Description" )]
        [AgentToolGuid( "C08562B4-E173-4490-92AD-75638292A1B7" )]
        public AgentToolResult AddEntry(
            string caseIdKey,
            string text,

            [Description( "Fact, TimelineEvent, or Evidence. Defaults to Fact." )]
            string entryType = null,

            [Description( "When a timeline event happened. Required for timeline events." )]
            DateTime? eventDateTime = null,

            [Description( "For timeline events: ProblemStart, LastKnownGood, Change, Activity, or Other. The timeline needs one ProblemStart." )]
            string timelineEventType = null,

            [Description( "An optional link, mostly for evidence." )]
            string url = null,

            [Description( "For facts: true only when evidence backs the fact." )]
            bool isCorroborated = false )
        {
            return Run( person => CaseWorkflowService.AddEntry( caseIdKey, entryType, text, eventDateTime, timelineEventType, url, isCorroborated, person ) );
        }

        [SkillDescription( "AddHypothesis.Description" )]
        [SkillPurpose( "AddHypothesis.Purpose" )]
        [SkillUsage( "AddHypothesis.Description" )]
        [AgentToolGuid( "D19673C5-F284-45A1-A3BE-867493A3B2C8" )]
        public AgentToolResult AddHypothesis(
            string caseIdKey,

            [Description( "The candidate cause, stated so it can be tested." )]
            string name,

            string description = null,

            [Description( "How likely this cause is, 0 to 100. A root hypothesis only counts toward the gate when this is above 0." )]
            int probability = 0,

            string parentHypothesisIdKey = null )
        {
            return Run( person => CaseWorkflowService.AddHypothesis( caseIdKey, name, description, probability, parentHypothesisIdKey, person ) );
        }

        [SkillDescription( "UpdateHypothesis.Description" )]
        [SkillPurpose( "UpdateHypothesis.Purpose" )]
        [SkillUsage( "UpdateHypothesis.Description" )]
        [AgentToolGuid( "E2A784D6-0395-46B2-B4CF-9785A4B4C3D9" )]
        public AgentToolResult UpdateHypothesis(
            string caseIdKey,
            string hypothesisIdKey,

            [Description( "0 to 100." )]
            int? probability = null,

            [Description( "Proposed, Testing, RuledOut, or Confirmed." )]
            string status = null,

            string ruledOutReason = null )
        {
            return Run( person => CaseWorkflowService.UpdateHypothesis( caseIdKey, hypothesisIdKey, probability, status, ruledOutReason, person ) );
        }

        [SkillDescription( "AddExperiment.Description" )]
        [SkillPurpose( "AddExperiment.Purpose" )]
        [SkillUsage( "AddExperiment.Description" )]
        [AgentToolGuid( "F3B895E7-14A6-47C3-85D0-A896B5C5D4EA" )]
        public AgentToolResult AddExperiment(
            string caseIdKey,
            string hypothesisIdKey = null,

            [Description( "Use when the hypothesis is not in the probability tree." )]
            string hypothesisText = null,

            string testPlan = null,
            string expectedResult = null )
        {
            return Run( person => CaseWorkflowService.AddExperiment( caseIdKey, hypothesisIdKey, hypothesisText, testPlan, expectedResult, person ) );
        }

        [SkillDescription( "RecordExperimentResult.Description" )]
        [SkillPurpose( "RecordExperimentResult.Purpose" )]
        [SkillUsage( "RecordExperimentResult.Description" )]
        [AgentToolGuid( "04C9A6F8-25B7-48D4-96E1-B9A7C6D6E5FB" )]
        public AgentToolResult RecordExperimentResult(
            string caseIdKey,
            string experimentIdKey,

            [Description( "Supported, RuledOut, or Inconclusive." )]
            string outcome,

            string actualResult = null,

            [Description( "Every change made during the experiment. Leave out if nothing was changed." )]
            string changesMade = null,

            [Description( "True when all changes have been reverted." )]
            bool changesReverted = false,

            [Description( "Why changes remain in place, when they were not reverted." )]
            string revertJustification = null,

            [Description( "What was learned. This becomes a corroborated fact." )]
            string learnings = null )
        {
            return Run( person => CaseWorkflowService.RecordExperimentResult( caseIdKey, experimentIdKey, actualResult, outcome, changesMade, changesReverted, revertJustification, learnings, person ) );
        }

        [SkillDescription( "SavePhaseNotes.Description" )]
        [SkillPurpose( "SavePhaseNotes.Purpose" )]
        [SkillUsage( "SavePhaseNotes.Description" )]
        [AgentToolGuid( "15DAB709-36C8-49E5-A7F2-CAB8D7E7F60C" )]
        public AgentToolResult SavePhaseNotes(
            string caseIdKey,
            string notes,

            [Description( "The phase number, 1 to 9. Defaults to the current phase." )]
            int? phase = null )
        {
            return Run( person => CaseWorkflowService.SavePhaseNotes( caseIdKey, phase, notes, person ) );
        }

        [SkillDescription( "CompletePhase.Description" )]
        [SkillPurpose( "CompletePhase.Purpose" )]
        [SkillUsage( "CompletePhase.Description" )]
        [AgentToolGuid( "26EBC81A-47D9-4AF6-B803-DBC9E8F8071D" )]
        public AgentToolResult CompletePhase( string caseIdKey )
        {
            return Run( person => CaseWorkflowService.CompletePhase( caseIdKey, person ) );
        }

        [SkillDescription( "LoopBack.Description" )]
        [SkillPurpose( "LoopBack.Purpose" )]
        [SkillUsage( "LoopBack.Description" )]
        [AgentToolGuid( "37FCD92B-58EA-4B07-8914-ECDAF909182E" )]
        public AgentToolResult LoopBack( string caseIdKey )
        {
            return Run( person => CaseWorkflowService.LoopBack( caseIdKey, person ) );
        }

        [SkillDescription( "ChampionSignOff.Description" )]
        [SkillPurpose( "ChampionSignOff.Purpose" )]
        [SkillUsage( "ChampionSignOff.Description" )]
        [AgentToolGuid( "480DEA3C-69FB-4C18-9A25-FDEB0A1A293F" )]
        public AgentToolResult ChampionSignOff( string caseIdKey )
        {
            return Run( person => CaseWorkflowService.ChampionSignOff( caseIdKey, person ) );
        }

        [SkillDescription( "CloseCase.Description" )]
        [SkillPurpose( "CloseCase.Purpose" )]
        [SkillUsage( "CloseCase.Description" )]
        [AgentToolGuid( "591EFB4D-7A0C-4D29-AB36-0EFC1B2B3A40" )]
        public AgentToolResult CloseCase(
            string caseIdKey,

            [Description( "What the problem cost or affected." )]
            string impactSummary,

            [Description( "What fixed it." )]
            string solutionSummary,

            string contributingFactors,
            string lessonsLearned,

            [Description( "What keeps it from happening again." )]
            string preventionMeasures,

            [Description( "Follow-up work that remains." )]
            string actionItems,

            [Description( "Optional date on which the owner is asked whether the fix held." )]
            DateTime? followUpDate = null )
        {
            return Run( person => CaseWorkflowService.CloseCase( caseIdKey, impactSummary, solutionSummary, contributingFactors, lessonsLearned, preventionMeasures, actionItems, followUpDate, person ) );
        }
    }
}
