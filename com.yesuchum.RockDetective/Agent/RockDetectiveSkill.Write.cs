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

        [Description( "Lists the case categories a case can be opened under. Each category can have its own names for phases and terms." )]
        [AgentPurpose( "Finds the category to open a case under." )]
        [AgentToolGuid( "6A2F0C5E-8B1D-4E3A-9C47-1F0D2B3A4C51" )]
        public AgentToolResult ListCategories()
        {
            return Run( person => CaseWorkflowService.ListCategories() );
        }

        [Description( "Opens a new case file. The signed-in person becomes the owner. Simple mode starts at the fact list; Advanced mode runs all nine phases. Returns where the case stands and the next step." )]
        [AgentPurpose( "Opens a case for a problem that needs to be worked methodically." )]
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

        [Description( "Changes a case's title, problem statement, category, or whether the problem statement is verified. Leave out anything that should stay as it is." )]
        [AgentPurpose( "Refines the problem definition as understanding improves." )]
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

        [Description( "Adds a person to the case team or as a witness. Use personAliasGuid for a Rock person or externalName for someone outside Rock. A case has one Champion." )]
        [AgentPurpose( "Builds the team and records who has information about the problem." )]
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

        [Description( "Records that this case will go without a Champion, with the reason. Use it for problems small enough not to need an independent sanity check." )]
        [AgentPurpose( "Satisfies the team gate when no Champion is needed." )]
        [AgentToolGuid( "AE634092-CF51-427E-B08B-534160708095" )]
        public AgentToolResult WaiveChampion(
            string caseIdKey,

            [Description( "Why a Champion is not needed." )]
            string reason )
        {
            return Run( person => CaseWorkflowService.WaiveChampion( caseIdKey, reason, person ) );
        }

        [Description( "Records what a participant or witness said. Optionally adds it to the fact list. Statements are never fully trusted, so only mark a fact corroborated when evidence backs it." )]
        [AgentPurpose( "Documents what people reported about the problem." )]
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

        [Description( "Adds a fact, a timeline event, or a piece of evidence to the case. Facts must be specific, correct, and pertinent. The entry is marked as added by an AI assistant." )]
        [AgentPurpose( "Records what is known, when things happened, and the evidence for it." )]
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

        [Description( "Adds a candidate cause to the probability tree. Brainstorm causes first and assign probabilities second. Give parentHypothesisIdKey to break a broad hypothesis into a child node." )]
        [AgentPurpose( "Builds the list of possible causes." )]
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

        [Description( "Changes a hypothesis's probability or status. Ruling one out needs a reason. Confirming one only satisfies the reevaluate gate when an experiment testing it came out Supported." )]
        [AgentPurpose( "Keeps the probability tree current as evidence comes in." )]
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

        [Description( "Logs a planned experiment that tests one hypothesis, changing one factor at a time. Link it with hypothesisIdKey or describe the hypothesis in hypothesisText." )]
        [AgentPurpose( "Plans a test of a possible cause." )]
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

        [Description( "Records what an experiment showed. Log every change made so it can be reverted; the gate wants changes reverted, justified, or never made. Learnings are added to the fact list once." )]
        [AgentPurpose( "Documents the result of a test and what was learned." )]
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

        [Description( "Saves working notes for a phase: the answers to its prompts, decisions made, and why. Defaults to the current phase and replaces that phase's earlier notes." )]
        [AgentPurpose( "Documents decisions and reasoning as the case moves along." )]
        [AgentToolGuid( "15DAB709-36C8-49E5-A7F2-CAB8D7E7F60C" )]
        public AgentToolResult SavePhaseNotes(
            string caseIdKey,
            string notes,

            [Description( "The phase number, 1 to 9. Defaults to the current phase." )]
            int? phase = null )
        {
            return Run( person => CaseWorkflowService.SavePhaseNotes( caseIdKey, phase, notes, person ) );
        }

        [Description( "Completes the case's current phase and moves to the next one. It only works when every item on the phase's checklist is met; otherwise it says what is still needed." )]
        [AgentPurpose( "Moves the case forward once a phase's requirements are met." )]
        [AgentGuardrail( "Never try to get around a checklist. If something is still needed, do that work first." )]
        [AgentToolGuid( "26EBC81A-47D9-4AF6-B803-DBC9E8F8071D" )]
        public AgentToolResult CompletePhase( string caseIdKey )
        {
            return Run( person => CaseWorkflowService.CompletePhase( caseIdKey, person ) );
        }

        [Description( "Sends the case back to the fact list for another round through facts, the probability tree, experiments, and reevaluation. Use it when no cause is confirmed yet." )]
        [AgentPurpose( "Starts another round of investigation." )]
        [AgentToolGuid( "37FCD92B-58EA-4B07-8914-ECDAF909182E" )]
        public AgentToolResult LoopBack( string caseIdKey )
        {
            return Run( person => CaseWorkflowService.LoopBack( caseIdKey, person ) );
        }

        [Description( "Records the Champion's sign-off on the confirmed explanation. It only works when the signed-in person is the Champion on the case." )]
        [AgentPurpose( "Lets the Champion approve the confirmed explanation." )]
        [AgentToolGuid( "480DEA3C-69FB-4C18-9A25-FDEB0A1A293F" )]
        public AgentToolResult ChampionSignOff( string caseIdKey )
        {
            return Run( person => CaseWorkflowService.ChampionSignOff( caseIdKey, person ) );
        }

        [Description( "Closes the case with its postmortem and sends it to the archive. The case must be on its last phase and all six postmortem fields are required." )]
        [AgentPurpose( "Documents the outcome: what happened, what fixed it, and how to prevent it." )]
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
