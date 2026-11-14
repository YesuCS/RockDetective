using System;
using System.Collections.Generic;
using System.Linq;

using Rock;
using Rock.Web.Cache;

namespace com.yesuchum.RockDetective.Agent
{
    public class SkillWordingItem
    {
        public string Key { get; set; }

        public string Group { get; set; }

        public string Label { get; set; }

        public string Default { get; set; }
    }

    public static class SkillWording
    {
        public const string AttributeKey = "com_yesuchum_RockDetective_SkillWording";

        public const string SkillGroup = "The skill";

        private static readonly List<SkillWordingItem> _items = BuildItems();

        private static readonly Dictionary<string, string> _defaults = _items.ToDictionary( i => i.Key, i => i.Default, StringComparer.OrdinalIgnoreCase );

        public static IReadOnlyList<SkillWordingItem> Items => _items;

        public static string GetDefault( string key )
        {
            return _defaults.TryGetValue( key, out var value ) ? value : string.Empty;
        }

        public static string Get( string key )
        {
            try
            {
                var overrides = GetOverrides();

                if ( overrides.TryGetValue( key, out var custom ) && custom.IsNotNullOrWhiteSpace() )
                {
                    return custom.Trim();
                }
            }
            catch ( Exception ex )
            {
                Rock.Model.ExceptionLogService.LogException( new Exception( "Rock Detective could not read its custom skill wording; the standard wording was used.", ex ) );
            }

            return GetDefault( key );
        }

        public static Dictionary<string, string> GetOverrides()
        {
            var json = GlobalAttributesCache.Get().GetValue( AttributeKey );
            var parsed = json.IsNotNullOrWhiteSpace() ? json.FromJsonOrNull<Dictionary<string, string>>() : null;

            return new Dictionary<string, string>( parsed ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase );
        }

        public static void SaveOverrides( Dictionary<string, string> values )
        {
            var cleaned = ( values ?? new Dictionary<string, string>() )
                .Where( kv => _defaults.ContainsKey( kv.Key ) && kv.Value.IsNotNullOrWhiteSpace() && kv.Value.Trim() != GetDefault( kv.Key ) )
                .ToDictionary( kv => kv.Key, kv => kv.Value.Trim() );

            GlobalAttributesCache.Get().SetValue( AttributeKey, cleaned.Any() ? cleaned.ToJson() : string.Empty, true );
        }

        private static List<SkillWordingItem> BuildItems()
        {
            var items = new List<SkillWordingItem>();

            void Skill( string key, string label, string text )
            {
                items.Add( new SkillWordingItem { Key = "Skill." + key, Group = SkillGroup, Label = label, Default = text } );
            }

            void Tool( string method, string name, string description, string purpose )
            {
                items.Add( new SkillWordingItem { Key = method + ".Description", Group = name, Label = "Description", Default = description } );
                items.Add( new SkillWordingItem { Key = method + ".Purpose", Group = name, Label = "Purpose", Default = purpose } );
            }

            Skill( "Description", "Description",
                "Works Rock Detective problem-solving case files from start to finish: opens a case, gathers facts and evidence, tests possible causes, and closes with a postmortem. Also lets a witness answer a statement request, and searches the web server's IIS logs for evidence." );

            Skill( "Purpose", "Purpose",
                "Lets the agent diagnose a problem methodically and document the decisions, findings, and fix in a case file." );

            Skill( "Usage", "How to use it",
                "Find a case with the list cases tool and read it with the get case tool.\n"
                + "To work a problem, open a case and follow the phase it reports. Every tool that changes a case returns the current phase, its guidance, its checklist, and the next step. Do the work the checklist asks for, save your reasoning as phase notes, then complete the phase. Finish by closing the case with its postmortem.\n"
                + "Search the IIS logs when investigating errors or slow pages." );

            Skill( "Guardrails", "Guardrails",
                "Phase names, terms, and gate wording come back the way the case's category has them set up. Use those words when talking about the case.\n"
                + "Record only what is verified as a corroborated fact. Mark anything a person reported, or anything inferred, as not corroborated until evidence backs it.\n"
                + "Change one thing at a time in an experiment, log every change, and revert it or say why it stays.\n"
                + "Do not confirm a hypothesis without an experiment that supports it.\n"
                + "Never try to get around a checklist. If something is still needed, do that work first.\n"
                + "What a person may do depends on who they are. Someone who was only asked for a statement can list, submit, and decline their own statement requests, and nothing else. If a tool says it is for people who work cases, do not look for another way to do it." );

            Tool( "ListCases", "List Cases",
                "Lists the Rock Detective case files the current person can read, newest first.",
                "Finds case files and their keys." );

            Tool( "GetCase", "Get Case",
                "Gets one Rock Detective case file in full: phases, gate checklists, facts, timeline, evidence, hypotheses, experiments, participants, and statements.",
                "Retrieves the full detail of a single case file." );

            Tool( "SearchIisLogs", "Search IIS Logs",
                "Searches the web server's IIS log files for recent requests. Times are returned in the server's local time zone and results are capped. Only Rock administrators can use it.",
                "Gathers request-level evidence such as errors and slow pages." );

            Tool( "ListMyStatementRequests", "List My Statement Requests",
                "For a witness: lists the statement requests that are waiting on the signed-in person, with the question being asked. It shows nothing else about the case.",
                "Shows a witness what they have been asked." );

            Tool( "SubmitStatement", "Submit Statement",
                "For a witness: submits the signed-in person's answer to a statement request that was addressed to them. It needs no other access to the case. Submit the person's own words without adding to or rewording them.",
                "Lets a witness answer a statement request." );

            Tool( "DeclineStatement", "Decline Statement",
                "For a witness: declines a statement request that was addressed to the signed-in person, with an optional reason.",
                "Lets a witness decline a statement request." );

            Tool( "ListCategories", "List Categories",
                "Lists the case categories a case can be opened under. Each category can have its own names for phases and terms.",
                "Finds the category to open a case under." );

            Tool( "CreateCase", "Create Case",
                "Opens a new case file. The signed-in person becomes the owner. Simple mode starts at the fact list; Advanced mode runs all nine phases. Returns where the case stands and the next step.",
                "Opens a case for a problem that needs to be worked methodically." );

            Tool( "UpdateCase", "Update Case",
                "Changes a case's title, problem statement, category, or whether the problem statement is verified. Leave out anything that should stay as it is.",
                "Refines the problem definition as understanding improves." );

            Tool( "AddParticipant", "Add Participant",
                "Adds a person to the case team or as a witness, either a Rock person or someone outside Rock. A case has one Champion.",
                "Builds the team and records who has information about the problem." );

            Tool( "WaiveChampion", "Waive Champion",
                "Records that this case will go without a Champion, with the reason. Use it for problems small enough not to need an independent sanity check.",
                "Satisfies the team gate when no Champion is needed." );

            Tool( "RecordStatement", "Record Statement",
                "Records what a participant or witness said. Optionally adds it to the fact list. Statements are never fully trusted, so only mark a fact corroborated when evidence backs it.",
                "Documents what people reported about the problem." );

            Tool( "AddEntry", "Add Entry",
                "Adds a fact, a timeline event, or a piece of evidence to the case. Facts must be specific, correct, and pertinent. The entry is marked as added by an AI assistant.",
                "Records what is known, when things happened, and the evidence for it." );

            Tool( "AddHypothesis", "Add Hypothesis",
                "Adds a candidate cause to the probability tree. Brainstorm causes first and assign probabilities second. A parent hypothesis key breaks a broad hypothesis into a child node.",
                "Builds the list of possible causes." );

            Tool( "UpdateHypothesis", "Update Hypothesis",
                "Changes a hypothesis's probability or status. Ruling one out needs a reason. Confirming one only satisfies the reevaluate gate when an experiment testing it came out Supported.",
                "Keeps the probability tree current as evidence comes in." );

            Tool( "AddExperiment", "Add Experiment",
                "Logs a planned experiment that tests one hypothesis, changing one factor at a time. Link it to a hypothesis in the probability tree or describe the hypothesis in text.",
                "Plans a test of a possible cause." );

            Tool( "RecordExperimentResult", "Record Experiment Result",
                "Records what an experiment showed. Log every change made so it can be reverted; the gate wants changes reverted, justified, or never made. Learnings are added to the fact list once.",
                "Documents the result of a test and what was learned." );

            Tool( "SavePhaseNotes", "Save Phase Notes",
                "Saves working notes for a phase: the answers to its prompts, decisions made, and why. Defaults to the current phase and replaces that phase's earlier notes.",
                "Documents decisions and reasoning as the case moves along." );

            Tool( "CompletePhase", "Complete Phase",
                "Completes the case's current phase and moves to the next one. It only works when every item on the phase's checklist is met; otherwise it says what is still needed.",
                "Moves the case forward once a phase's requirements are met." );

            Tool( "LoopBack", "Loop Back",
                "Sends the case back to the fact list for another round through facts, the probability tree, experiments, and reevaluation. Use it when no cause is confirmed yet.",
                "Starts another round of investigation." );

            Tool( "ChampionSignOff", "Champion Sign-Off",
                "Records the Champion's sign-off on the confirmed explanation. It only works when the signed-in person is the Champion on the case.",
                "Lets the Champion approve the confirmed explanation." );

            Tool( "CloseCase", "Close Case",
                "Closes the case with its postmortem and sends it to the archive. The case must be on its last phase and all six postmortem fields are required.",
                "Documents the outcome: what happened, what fixed it, and how to prevent it." );

            return items;
        }
    }

    public sealed class SkillDescriptionAttribute : System.ComponentModel.DescriptionAttribute
    {
        public SkillDescriptionAttribute( string key )
            : base( SkillWording.Get( key ) )
        {
        }
    }

    public sealed class SkillPurposeAttribute : Rock.AI.Agent.Annotations.AgentPurposeAttribute
    {
        public SkillPurposeAttribute( string key )
            : base( SkillWording.Get( key ) )
        {
        }
    }

    public sealed class SkillUsageAttribute : Rock.AI.Agent.Annotations.AgentUsageAttribute
    {
        public SkillUsageAttribute( string key )
            : base( SkillWording.Get( key ) )
        {
        }
    }

    public sealed class SkillGuardrailAttribute : Rock.AI.Agent.Annotations.AgentGuardrailAttribute
    {
        public SkillGuardrailAttribute( string key )
            : base( SkillWording.Get( key ) )
        {
        }
    }
}
