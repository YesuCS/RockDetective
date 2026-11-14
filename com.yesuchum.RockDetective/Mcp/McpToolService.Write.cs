using System;
using System.Collections.Generic;

using Newtonsoft.Json.Linq;

namespace com.yesuchum.RockDetective.Mcp
{
    public static partial class McpToolService
    {
        private const string CaseIdKeyProperty = @"""caseIdKey"": { ""type"": ""string"", ""description"": ""The IdKey of the case file, as returned by list_cases or create_case."" }";

        private const string ProgressNote = " Returns where the case stands: its current phase, that phase's guidance and checklist, and the next step.";

        private static McpToolDefinition Tool( string name, string description, string properties, string required, Func<JObject, Rock.Model.Person, object> handler )
        {
            var schema = new JObject
            {
                ["type"] = "object",
                ["properties"] = JObject.Parse( "{" + properties + "}" )
            };

            if ( required != null )
            {
                schema["required"] = new JArray( required.Split( ',' ) );
            }

            return new McpToolDefinition
            {
                Name = name,
                Description = description,
                InputSchema = schema,
                Handler = ( arguments, person ) => ToJson( handler( arguments, person ) )
            };
        }

        private static IEnumerable<McpToolDefinition> GetWriteTools()
        {
            yield return Tool(
                "list_my_statement_requests",
                "For a witness: lists the statement requests that are waiting on the signed-in person, with the question being asked. It shows nothing else about the case.",
                string.Empty,
                null,
                ( a, p ) => CaseWorkflowService.ListMyStatementRequests( p ) );

            yield return Tool(
                "submit_statement",
                "For a witness: submits the signed-in person's answer to a statement request that was addressed to them. It needs no other access to the case.",
                @"""statementIdKey"": { ""type"": ""string"", ""description"": ""The IdKey of the request, from list_my_statement_requests."" },
                  ""statement"": { ""type"": ""string"", ""description"": ""What the person saw or knows, in their own words."" },
                  ""url"": { ""type"": ""string"", ""description"": ""An optional link that goes with the statement."" }",
                "statementIdKey,statement",
                ( a, p ) => CaseWorkflowService.SubmitStatement( a.Value<string>( "statementIdKey" ), a.Value<string>( "statement" ), a.Value<string>( "url" ), p ) );

            yield return Tool(
                "decline_statement",
                "For a witness: declines a statement request that was addressed to the signed-in person, with an optional reason.",
                @"""statementIdKey"": { ""type"": ""string"", ""description"": ""The IdKey of the request, from list_my_statement_requests."" },
                  ""reason"": { ""type"": ""string"" }",
                "statementIdKey",
                ( a, p ) => CaseWorkflowService.DeclineStatement( a.Value<string>( "statementIdKey" ), a.Value<string>( "reason" ), p ) );

            yield return Tool(
                "list_categories",
                "Lists the case categories a case can be opened under. Each category can have its own names for phases and terms.",
                string.Empty,
                null,
                ( a, p ) => CaseWorkflowService.ListCategories() );

            yield return Tool(
                "create_case",
                "Opens a new case file. The signed-in person becomes the owner. Simple mode starts at the fact list; Advanced mode runs all nine phases." + ProgressNote,
                @"""title"": { ""type"": ""string"", ""description"": ""A short handle for the case."" },
                  ""problemStatement"": { ""type"": ""string"", ""description"": ""One clear, concise statement of the problem with nothing unverified and nothing that is not germane."" },
                  ""category"": { ""type"": ""string"", ""description"": ""The category name or guid, from list_categories."" },
                  ""mode"": { ""type"": ""string"", ""enum"": [ ""Simple"", ""Advanced"" ], ""description"": ""Simple for easy problems, Advanced for difficult ones. Defaults to Simple."" },
                  ""isProblemStatementVerified"": { ""type"": ""boolean"", ""description"": ""True only if every detail in the problem statement has been verified and all of it is germane. Advanced cases need this to complete the problem definition phase."" }",
                "title,problemStatement,category",
                ( a, p ) => CaseWorkflowService.CreateCase( a.Value<string>( "title" ), a.Value<string>( "problemStatement" ), a.Value<string>( "category" ), a.Value<string>( "mode" ), a.Value<bool?>( "isProblemStatementVerified" ) ?? false, p ) );

            yield return Tool(
                "update_case",
                "Changes a case's title, problem statement, category, or whether the problem statement is verified. Leave out anything that should stay as it is." + ProgressNote,
                CaseIdKeyProperty + @",
                  ""title"": { ""type"": ""string"" },
                  ""problemStatement"": { ""type"": ""string"" },
                  ""category"": { ""type"": ""string"", ""description"": ""The category name or guid."" },
                  ""isProblemStatementVerified"": { ""type"": ""boolean"", ""description"": ""True only if every detail in the problem statement has been verified and all of it is germane."" }",
                "caseIdKey",
                ( a, p ) => CaseWorkflowService.UpdateCase( a.Value<string>( "caseIdKey" ), a.Value<string>( "title" ), a.Value<string>( "problemStatement" ), a.Value<string>( "category" ), a.Value<bool?>( "isProblemStatementVerified" ), p ) );

            yield return Tool(
                "add_participant",
                "Adds a person to the case team or as a witness. Use personAliasGuid for a Rock person or externalName for someone outside Rock. A case has one Champion." + ProgressNote,
                CaseIdKeyProperty + @",
                  ""role"": { ""type"": ""string"", ""enum"": [ ""Champion"", ""Recorder"", ""Communicator"", ""Member"", ""Witness"" ], ""description"": ""Defaults to Member."" },
                  ""personAliasGuid"": { ""type"": ""string"", ""description"": ""The person alias guid of a Rock person."" },
                  ""externalName"": { ""type"": ""string"", ""description"": ""The name of someone who is not in Rock."" },
                  ""externalContact"": { ""type"": ""string"", ""description"": ""How to reach an external person."" }",
                "caseIdKey",
                ( a, p ) => CaseWorkflowService.AddParticipant( a.Value<string>( "caseIdKey" ), a.Value<string>( "role" ), a.Value<string>( "personAliasGuid" ), a.Value<string>( "externalName" ), a.Value<string>( "externalContact" ), p ) );

            yield return Tool(
                "waive_champion",
                "Records that this case will go without a Champion, with the reason. Use it for problems small enough not to need an independent sanity check." + ProgressNote,
                CaseIdKeyProperty + @",
                  ""reason"": { ""type"": ""string"", ""description"": ""Why a Champion is not needed."" }",
                "caseIdKey,reason",
                ( a, p ) => CaseWorkflowService.WaiveChampion( a.Value<string>( "caseIdKey" ), a.Value<string>( "reason" ), p ) );

            yield return Tool(
                "record_statement",
                "Records what a participant or witness said. Optionally adds it to the fact list; statements are never fully trusted, so only mark a fact corroborated when evidence backs it." + ProgressNote,
                CaseIdKeyProperty + @",
                  ""participantIdKey"": { ""type"": ""string"", ""description"": ""The IdKey of the participant, from add_participant or get_case."" },
                  ""statement"": { ""type"": ""string"" },
                  ""url"": { ""type"": ""string"", ""description"": ""An optional link that goes with the statement."" },
                  ""addToFacts"": { ""type"": ""boolean"", ""description"": ""Also add the statement to the fact list. Defaults to false."" },
                  ""isCorroborated"": { ""type"": ""boolean"", ""description"": ""Marks the added fact as corroborated by evidence. Defaults to false."" }",
                "caseIdKey,participantIdKey,statement",
                ( a, p ) => CaseWorkflowService.RecordStatement( a.Value<string>( "caseIdKey" ), a.Value<string>( "participantIdKey" ), a.Value<string>( "statement" ), a.Value<string>( "url" ), a.Value<bool?>( "addToFacts" ) ?? false, a.Value<bool?>( "isCorroborated" ) ?? false, p ) );

            yield return Tool(
                "add_entry",
                "Adds a fact, a timeline event, or a piece of evidence to the case. Facts must be specific, correct, and pertinent. The entry is marked as added by an AI assistant." + ProgressNote,
                CaseIdKeyProperty + @",
                  ""entryType"": { ""type"": ""string"", ""enum"": [ ""Fact"", ""TimelineEvent"", ""Evidence"" ], ""description"": ""Defaults to Fact."" },
                  ""text"": { ""type"": ""string"" },
                  ""eventDateTime"": { ""type"": ""string"", ""description"": ""When a timeline event happened, as an ISO 8601 date and time. Required for timeline events."" },
                  ""timelineEventType"": { ""type"": ""string"", ""enum"": [ ""ProblemStart"", ""LastKnownGood"", ""Change"", ""Activity"", ""Other"" ], ""description"": ""For timeline events. The timeline needs one ProblemStart."" },
                  ""url"": { ""type"": ""string"", ""description"": ""An optional link, mostly for evidence."" },
                  ""isCorroborated"": { ""type"": ""boolean"", ""description"": ""For facts: true only when evidence backs the fact. Defaults to false."" }",
                "caseIdKey,text",
                ( a, p ) => CaseWorkflowService.AddEntry( a.Value<string>( "caseIdKey" ), a.Value<string>( "entryType" ), a.Value<string>( "text" ), a.Value<DateTime?>( "eventDateTime" ), a.Value<string>( "timelineEventType" ), a.Value<string>( "url" ), a.Value<bool?>( "isCorroborated" ) ?? false, p ) );

            yield return Tool(
                "add_hypothesis",
                "Adds a candidate cause to the probability tree. Brainstorm causes first and assign probabilities second. Give parentHypothesisIdKey to break a broad hypothesis into a child node." + ProgressNote,
                CaseIdKeyProperty + @",
                  ""name"": { ""type"": ""string"", ""description"": ""The candidate cause, stated so it can be tested."" },
                  ""description"": { ""type"": ""string"" },
                  ""probability"": { ""type"": ""integer"", ""description"": ""How likely this cause is, 0 to 100. A root hypothesis only counts toward the gate when this is above 0."" },
                  ""parentHypothesisIdKey"": { ""type"": ""string"" }",
                "caseIdKey,name",
                ( a, p ) => CaseWorkflowService.AddHypothesis( a.Value<string>( "caseIdKey" ), a.Value<string>( "name" ), a.Value<string>( "description" ), a.Value<int?>( "probability" ) ?? 0, a.Value<string>( "parentHypothesisIdKey" ), p ) );

            yield return Tool(
                "update_hypothesis",
                "Changes a hypothesis's probability or status. Ruling one out needs a reason. Confirming one only satisfies the reevaluate gate when an experiment testing it came out Supported." + ProgressNote,
                CaseIdKeyProperty + @",
                  ""hypothesisIdKey"": { ""type"": ""string"" },
                  ""probability"": { ""type"": ""integer"", ""description"": ""0 to 100."" },
                  ""status"": { ""type"": ""string"", ""enum"": [ ""Proposed"", ""Testing"", ""RuledOut"", ""Confirmed"" ] },
                  ""ruledOutReason"": { ""type"": ""string"" }",
                "caseIdKey,hypothesisIdKey",
                ( a, p ) => CaseWorkflowService.UpdateHypothesis( a.Value<string>( "caseIdKey" ), a.Value<string>( "hypothesisIdKey" ), a.Value<int?>( "probability" ), a.Value<string>( "status" ), a.Value<string>( "ruledOutReason" ), p ) );

            yield return Tool(
                "add_experiment",
                "Logs a planned experiment that tests one hypothesis, changing one factor at a time. Link it with hypothesisIdKey or describe the hypothesis in hypothesisText." + ProgressNote,
                CaseIdKeyProperty + @",
                  ""hypothesisIdKey"": { ""type"": ""string"" },
                  ""hypothesisText"": { ""type"": ""string"", ""description"": ""Use when the hypothesis is not in the probability tree."" },
                  ""testPlan"": { ""type"": ""string"" },
                  ""expectedResult"": { ""type"": ""string"" }",
                "caseIdKey",
                ( a, p ) => CaseWorkflowService.AddExperiment( a.Value<string>( "caseIdKey" ), a.Value<string>( "hypothesisIdKey" ), a.Value<string>( "hypothesisText" ), a.Value<string>( "testPlan" ), a.Value<string>( "expectedResult" ), p ) );

            yield return Tool(
                "record_experiment_result",
                "Records what an experiment showed. Log every change made so it can be reverted; the gate wants changes reverted, justified, or never made. Learnings are added to the fact list once." + ProgressNote,
                CaseIdKeyProperty + @",
                  ""experimentIdKey"": { ""type"": ""string"" },
                  ""actualResult"": { ""type"": ""string"" },
                  ""outcome"": { ""type"": ""string"", ""enum"": [ ""Supported"", ""RuledOut"", ""Inconclusive"" ] },
                  ""changesMade"": { ""type"": ""string"", ""description"": ""Every change made during the experiment. Leave out if nothing was changed."" },
                  ""changesReverted"": { ""type"": ""boolean"", ""description"": ""True when all changes have been reverted. Defaults to false."" },
                  ""revertJustification"": { ""type"": ""string"", ""description"": ""Why changes remain in place, when they were not reverted."" },
                  ""learnings"": { ""type"": ""string"", ""description"": ""What was learned. This becomes a corroborated fact."" }",
                "caseIdKey,experimentIdKey,outcome",
                ( a, p ) => CaseWorkflowService.RecordExperimentResult( a.Value<string>( "caseIdKey" ), a.Value<string>( "experimentIdKey" ), a.Value<string>( "actualResult" ), a.Value<string>( "outcome" ), a.Value<string>( "changesMade" ), a.Value<bool?>( "changesReverted" ) ?? false, a.Value<string>( "revertJustification" ), a.Value<string>( "learnings" ), p ) );

            yield return Tool(
                "save_phase_notes",
                "Saves working notes for a phase: the answers to its prompts, decisions made, and why. Defaults to the current phase and replaces that phase's earlier notes." + ProgressNote,
                CaseIdKeyProperty + @",
                  ""phase"": { ""type"": ""integer"", ""description"": ""The phase number, 1 to 9. Defaults to the current phase."" },
                  ""notes"": { ""type"": ""string"" }",
                "caseIdKey,notes",
                ( a, p ) => CaseWorkflowService.SavePhaseNotes( a.Value<string>( "caseIdKey" ), a.Value<int?>( "phase" ), a.Value<string>( "notes" ), p ) );

            yield return Tool(
                "complete_phase",
                "Completes the case's current phase and moves to the next one. It only works when every item on the phase's checklist is met; otherwise it says what is still needed." + ProgressNote,
                CaseIdKeyProperty,
                "caseIdKey",
                ( a, p ) => CaseWorkflowService.CompletePhase( a.Value<string>( "caseIdKey" ), p ) );

            yield return Tool(
                "loop_back",
                "Sends the case back to the fact list for another round through facts, the probability tree, experiments, and reevaluation. Use it when no cause is confirmed yet." + ProgressNote,
                CaseIdKeyProperty,
                "caseIdKey",
                ( a, p ) => CaseWorkflowService.LoopBack( a.Value<string>( "caseIdKey" ), p ) );

            yield return Tool(
                "champion_sign_off",
                "Records the Champion's sign-off on the confirmed explanation. It only works when the signed-in person is the Champion on the case." + ProgressNote,
                CaseIdKeyProperty,
                "caseIdKey",
                ( a, p ) => CaseWorkflowService.ChampionSignOff( a.Value<string>( "caseIdKey" ), p ) );

            yield return Tool(
                "close_case",
                "Closes the case with its postmortem and sends it to the archive. The case must be on its last phase and all six postmortem fields are required." + ProgressNote,
                CaseIdKeyProperty + @",
                  ""impactSummary"": { ""type"": ""string"", ""description"": ""What the problem cost or affected."" },
                  ""solutionSummary"": { ""type"": ""string"", ""description"": ""What fixed it."" },
                  ""contributingFactors"": { ""type"": ""string"" },
                  ""lessonsLearned"": { ""type"": ""string"" },
                  ""preventionMeasures"": { ""type"": ""string"", ""description"": ""What keeps it from happening again."" },
                  ""actionItems"": { ""type"": ""string"", ""description"": ""Follow-up work that remains."" },
                  ""followUpDate"": { ""type"": ""string"", ""description"": ""Optional ISO 8601 date on which the owner is asked whether the fix held."" }",
                "caseIdKey,impactSummary,solutionSummary,contributingFactors,lessonsLearned,preventionMeasures,actionItems",
                ( a, p ) => CaseWorkflowService.CloseCase( a.Value<string>( "caseIdKey" ), a.Value<string>( "impactSummary" ), a.Value<string>( "solutionSummary" ), a.Value<string>( "contributingFactors" ), a.Value<string>( "lessonsLearned" ), a.Value<string>( "preventionMeasures" ), a.Value<string>( "actionItems" ), a.Value<DateTime?>( "followUpDate" ), p ) );
        }
    }
}
