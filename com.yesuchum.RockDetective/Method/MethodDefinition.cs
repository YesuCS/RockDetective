using System.Collections.Generic;
using System.Linq;

using com.yesuchum.RockDetective.Enums;

namespace com.yesuchum.RockDetective.Method
{
    public class PhaseDefinition
    {
        public CasePhase Phase { get; set; }

        public string Name { get; set; }

        public string Guidance { get; set; }

        public List<string> Prompts { get; set; } = new List<string>();

        public bool IsAutoSkippedInSimpleMode { get; set; }

        public bool IsLoopPhase { get; set; }
    }

    public static class MethodDefinition
    {
        public static readonly List<string> Principles = new List<string>
        {
            "Protect the crime scene. Capture evidence before changing anything: log files, current configurations, timestamps, who last modified what.",
            "Stay focused on the two key questions: What is different? What changed?",
            "Problems have logical causes. Never assume the cause is found until evidence proves it beyond a reasonable doubt; an unsolved problem always comes back.",
            "Sniff test: before acting on any explanation, ask whether it is logical and likely.",
            "Correlation does not equal causation."
        };

        public static readonly string MindsetGuidance =
            "Work like a research scientist, not in reactive berserker mode. Periodically stop digging and reorient; be the meerkat that surveys the landscape, not only the mole that tunnels.";

        public static readonly List<string> ContextualTips = new List<string>
        {
            "Divide and conquer: split the problem space in half and rule out one side at a time.",
            "Check traffic and IIS logs for what actually reached the server.",
            "Check Rock's Exception Log for errors that surfaced without anyone noticing.",
            "Check Rock Logs with an increased verbosity level for the subsystem in question.",
            "Zoom in on metrics; aggregates mask patterns. A machine-beat pattern (perfectly regular spikes) suggests a scheduled job."
        };

        public static readonly List<PhaseDefinition> Phases = new List<PhaseDefinition>
        {
            new PhaseDefinition
            {
                Phase = CasePhase.DefineTeam,
                Name = "Define the Team",
                Guidance = "Assign roles. The owner runs the investigation. A Champion is someone not emotionally tied to the problem who sanity-checks the final explanation. Recorder and Communicator keep notes and stakeholders current. Small problems can skip everything except the owner.",
                Prompts = new List<string>
                {
                    "Who owns this investigation?",
                    "Who will serve as Champion, or why is one not needed?",
                    "Who records findings and who communicates status?"
                },
                IsAutoSkippedInSimpleMode = true
            },
            new PhaseDefinition
            {
                Phase = CasePhase.DefineProblem,
                Name = "Define the Problem",
                Guidance = "Write one clear, concise problem statement. Include nothing unverified and nothing that is not germane. Assign a problem category.",
                Prompts = new List<string>
                {
                    "What exactly is the problem, stated in one or two sentences?",
                    "What category does this problem fall into?"
                }
            },
            new PhaseDefinition
            {
                Phase = CasePhase.Classify,
                Name = "Classify",
                Guidance = "Decide whether this is an easy or difficult problem. Easy problems run in Simple mode with a collapsed flow; difficult problems run in Advanced mode with the full method.",
                Prompts = new List<string>
                {
                    "How large is the impact?",
                    "How urgent is a resolution?",
                    "Has it happened before?",
                    "How broad is the scope?"
                }
            },
            new PhaseDefinition
            {
                Phase = CasePhase.Timeline,
                Name = "Timeline",
                Guidance = "Establish when the problem started, the last known success, the last code, config, or process change, and what people and machines were doing around those times.",
                Prompts = new List<string>
                {
                    "When did the problem start?",
                    "When was the last known good state?",
                    "What changed most recently?",
                    "What human or machine activity coincided?"
                },
                IsAutoSkippedInSimpleMode = true
            },
            new PhaseDefinition
            {
                Phase = CasePhase.FactList,
                Name = "Fact List",
                Guidance = "Write down facts: specific, correct, and pertinent only. Append continuously as the investigation develops. Witness statements feed this list but are never fully trusted; track corroboration.",
                Prompts = new List<string>
                {
                    "What do we know for certain?",
                    "Which statements are corroborated by evidence?"
                },
                IsLoopPhase = true
            },
            new PhaseDefinition
            {
                Phase = CasePhase.ProbabilityTree,
                Name = "Probability Tree",
                Guidance = "Brainstorm candidate causes first; assign probabilities second. Break broad hypotheses into child nodes.",
                Prompts = new List<string>
                {
                    "What could logically cause this?",
                    "How likely is each candidate?"
                },
                IsLoopPhase = true
            },
            new PhaseDefinition
            {
                Phase = CasePhase.Experiments,
                Name = "Experiments",
                Guidance = "Test one hypothesis at a time. Change one factor at a time. Log every change so it can be reverted. Record expected versus actual results and what was learned.",
                Prompts = new List<string>
                {
                    "Which hypothesis does this experiment test?",
                    "What is the test plan and expected result?",
                    "What actually happened, and what did we learn?"
                },
                IsLoopPhase = true
            },
            new PhaseDefinition
            {
                Phase = CasePhase.Reevaluate,
                Name = "Reevaluate",
                Guidance = "Update probabilities, rule hypotheses out, add facts. Loop back through Facts, Probability Tree, and Experiments until a cause is confirmed beyond a reasonable doubt and passes the Champion's sniff test.",
                Prompts = new List<string>
                {
                    "What did the last round of experiments change?",
                    "Is any hypothesis confirmed beyond a reasonable doubt?",
                    "Does the confirmed explanation pass the sniff test?"
                },
                IsLoopPhase = true
            },
            new PhaseDefinition
            {
                Phase = CasePhase.Postmortem,
                Name = "Postmortem",
                Guidance = "Summarize the problem, who was involved, the impact, the solution, contributing factors, lessons learned, prevention measures, and action items.",
                Prompts = new List<string>
                {
                    "What happened and who was involved?",
                    "What was the impact and the solution?",
                    "What contributed, what did we learn, and how do we prevent recurrence?",
                    "What action items remain?"
                }
            }
        };

        public static PhaseDefinition GetPhase( CasePhase phase )
        {
            return Phases.First( p => p.Phase == phase );
        }
    }
}
