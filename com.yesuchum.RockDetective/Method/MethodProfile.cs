using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using Rock;
using Rock.Web.Cache;

using com.yesuchum.RockDetective.Enums;

namespace com.yesuchum.RockDetective.Method
{
    public class TermDefinition
    {
        public string Singular { get; set; }

        public string Plural { get; set; }
    }

    public class PhaseOverride
    {
        public string Name { get; set; }

        public string Guidance { get; set; }

        public List<string> Prompts { get; set; }
    }

    public class MethodProfileConfiguration
    {
        public Dictionary<string, PhaseOverride> Phases { get; set; } = new Dictionary<string, PhaseOverride>( StringComparer.OrdinalIgnoreCase );

        public Dictionary<string, TermDefinition> Terms { get; set; } = new Dictionary<string, TermDefinition>( StringComparer.OrdinalIgnoreCase );

        public Dictionary<string, string> GateDescriptions { get; set; } = new Dictionary<string, string>( StringComparer.OrdinalIgnoreCase );
    }

    public static class TermKey
    {
        public const string CaseFile = "CaseFile";
        public const string Owner = "Owner";
        public const string Champion = "Champion";
        public const string Recorder = "Recorder";
        public const string Communicator = "Communicator";
        public const string Member = "Member";
        public const string Witness = "Witness";
        public const string Fact = "Fact";
        public const string TimelineEvent = "TimelineEvent";
        public const string Evidence = "Evidence";
        public const string Hypothesis = "Hypothesis";
        public const string Experiment = "Experiment";
        public const string Statement = "Statement";
    }

    public static class GateKey
    {
        public const string TeamOwner = "Team.Owner";
        public const string TeamChampion = "Team.Champion";
        public const string ProblemStatement = "Problem.Statement";
        public const string ProblemCategory = "Problem.Category";
        public const string ProblemAttestUnverified = "Problem.AttestUnverified";
        public const string ProblemAttestGermane = "Problem.AttestGermane";
        public const string Classified = "Classify.Classified";
        public const string TimelineMinimum = "Timeline.Minimum";
        public const string TimelineProblemStart = "Timeline.ProblemStart";
        public const string FactsMinimum = "Facts.Minimum";
        public const string HypothesesMinimum = "Hypotheses.Minimum";
        public const string ExperimentsMinimum = "Experiments.Minimum";
        public const string ReevaluateConfirmed = "Reevaluate.Confirmed";
        public const string ReevaluateChampionSignOff = "Reevaluate.ChampionSignOff";
        public const string PostmortemSummary = "Postmortem.Summary";
        public const string PostmortemImpact = "Postmortem.Impact";
        public const string PostmortemSolution = "Postmortem.Solution";
        public const string PostmortemContributingFactors = "Postmortem.ContributingFactors";
        public const string PostmortemLessonsLearned = "Postmortem.LessonsLearned";
        public const string PostmortemPrevention = "Postmortem.Prevention";
        public const string PostmortemActionItems = "Postmortem.ActionItems";
    }

    public class MethodProfile
    {
        public const string CategoryAttributeKey = "com_yesuchum_RockDetective_Terminology";

        public const string DefaultAttributeKey = "com_yesuchum_RockDetective_DefaultTerminology";

        private static readonly Regex _tokenRegex = new Regex( @"\{([A-Za-z]+)([+#]?)\}", RegexOptions.Compiled );

        private static readonly Regex _articleRegex = new Regex( @"\b(An|an|A|a) (\{[A-Za-z]+[+#]?\})", RegexOptions.Compiled );

        public static readonly IReadOnlyDictionary<string, TermDefinition> DefaultTerms = new Dictionary<string, TermDefinition>( StringComparer.OrdinalIgnoreCase )
        {
            [TermKey.CaseFile] = new TermDefinition { Singular = "Case", Plural = "Cases" },
            [TermKey.Owner] = new TermDefinition { Singular = "Owner", Plural = "Owners" },
            [TermKey.Champion] = new TermDefinition { Singular = "Champion", Plural = "Champions" },
            [TermKey.Recorder] = new TermDefinition { Singular = "Recorder", Plural = "Recorders" },
            [TermKey.Communicator] = new TermDefinition { Singular = "Communicator", Plural = "Communicators" },
            [TermKey.Member] = new TermDefinition { Singular = "Member", Plural = "Members" },
            [TermKey.Witness] = new TermDefinition { Singular = "Witness", Plural = "Witnesses" },
            [TermKey.Fact] = new TermDefinition { Singular = "Fact", Plural = "Facts" },
            [TermKey.TimelineEvent] = new TermDefinition { Singular = "Timeline Event", Plural = "Timeline Events" },
            [TermKey.Evidence] = new TermDefinition { Singular = "Evidence", Plural = "Evidence" },
            [TermKey.Hypothesis] = new TermDefinition { Singular = "Hypothesis", Plural = "Hypotheses" },
            [TermKey.Experiment] = new TermDefinition { Singular = "Experiment", Plural = "Experiments" },
            [TermKey.Statement] = new TermDefinition { Singular = "Statement", Plural = "Statements" }
        };

        public static readonly IReadOnlyDictionary<string, string> DefaultGateDescriptions = new Dictionary<string, string>( StringComparer.OrdinalIgnoreCase )
        {
            [GateKey.TeamOwner] = "An {owner} is assigned to the {caseFile}.",
            [GateKey.TeamChampion] = "A {Champion} is assigned, or explicitly waived with a reason.",
            [GateKey.ProblemStatement] = "A problem statement is written.",
            [GateKey.ProblemCategory] = "A problem category is assigned.",
            [GateKey.ProblemAttestUnverified] = "Attested: the statement contains no unverified details.",
            [GateKey.ProblemAttestGermane] = "Attested: the statement contains nothing that is not germane.",
            [GateKey.Classified] = "The problem is classified as easy or difficult.",
            [GateKey.TimelineMinimum] = "At least {count} {timelineEvent#} recorded.",
            [GateKey.TimelineProblemStart] = "The timeline includes when the problem started.",
            [GateKey.FactsMinimum] = "At least {count} {fact#} recorded.",
            [GateKey.HypothesesMinimum] = "At least {count} root {hypothesis#} with a probability estimate recorded.",
            [GateKey.ExperimentsMinimum] = "At least {count} {experiment#} with a recorded outcome and changes reverted or justified.",
            [GateKey.ReevaluateConfirmed] = "A {hypothesis} is confirmed with a supporting {experiment}, or the {caseFile} has explicitly looped back for another round.",
            [GateKey.ReevaluateChampionSignOff] = "The {Champion} has signed off on the confirmed explanation.",
            [GateKey.PostmortemSummary] = "Problem summary is written.",
            [GateKey.PostmortemImpact] = "Impact is described.",
            [GateKey.PostmortemSolution] = "Solution summary is written.",
            [GateKey.PostmortemContributingFactors] = "Contributing factors are listed.",
            [GateKey.PostmortemLessonsLearned] = "Lessons learned are recorded.",
            [GateKey.PostmortemPrevention] = "Prevention measures are recorded.",
            [GateKey.PostmortemActionItems] = "Action items are recorded."
        };

        public Dictionary<string, TermDefinition> Terms { get; private set; }

        public Dictionary<string, string> GateDescriptions { get; private set; }

        public List<PhaseDefinition> Phases { get; private set; }

        public bool IsCustomized { get; private set; }

        private MethodProfile()
        {
        }

        public static MethodProfile ForCategory( int? categoryId )
        {
            var configurations = new List<MethodProfileConfiguration>();

            var defaultJson = GlobalAttributesCache.Get().GetValue( DefaultAttributeKey );
            var defaultConfiguration = Parse( defaultJson );

            if ( defaultConfiguration != null )
            {
                configurations.Add( defaultConfiguration );
            }

            if ( categoryId.HasValue )
            {
                var categoryConfiguration = Parse( CategoryCache.Get( categoryId.Value )?.GetAttributeValue( CategoryAttributeKey ) );

                if ( categoryConfiguration != null )
                {
                    configurations.Add( categoryConfiguration );
                }
            }

            return Build( configurations );
        }

        public static MethodProfile Build( IEnumerable<MethodProfileConfiguration> configurations )
        {
            var layers = ( configurations ?? Enumerable.Empty<MethodProfileConfiguration>() ).Where( c => c != null ).ToList();

            var profile = new MethodProfile
            {
                IsCustomized = layers.Any(),
                Terms = DefaultTerms.ToDictionary( kv => kv.Key, kv => new TermDefinition { Singular = kv.Value.Singular, Plural = kv.Value.Plural }, StringComparer.OrdinalIgnoreCase ),
                GateDescriptions = DefaultGateDescriptions.ToDictionary( kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase )
            };

            foreach ( var layer in layers )
            {
                foreach ( var term in layer.Terms ?? new Dictionary<string, TermDefinition>() )
                {
                    if ( term.Value == null || !profile.Terms.ContainsKey( term.Key ) )
                    {
                        continue;
                    }

                    if ( term.Value.Singular.IsNotNullOrWhiteSpace() )
                    {
                        profile.Terms[term.Key].Singular = term.Value.Singular.Trim();
                        profile.Terms[term.Key].Plural = term.Value.Plural.IsNotNullOrWhiteSpace() ? term.Value.Plural.Trim() : term.Value.Singular.Trim().Pluralize();
                    }
                    else if ( term.Value.Plural.IsNotNullOrWhiteSpace() )
                    {
                        profile.Terms[term.Key].Plural = term.Value.Plural.Trim();
                    }
                }

                foreach ( var gate in layer.GateDescriptions ?? new Dictionary<string, string>() )
                {
                    if ( gate.Value.IsNotNullOrWhiteSpace() && profile.GateDescriptions.ContainsKey( gate.Key ) )
                    {
                        profile.GateDescriptions[gate.Key] = gate.Value.Trim();
                    }
                }
            }

            profile.Phases = MethodDefinition.Phases.Select( d =>
            {
                var name = d.Name;
                var guidance = d.Guidance;
                var prompts = d.Prompts;

                foreach ( var layer in layers )
                {
                    PhaseOverride phaseOverride = null;

                    if ( layer.Phases != null && !layer.Phases.TryGetValue( d.Phase.ToString(), out phaseOverride ) )
                    {
                        layer.Phases.TryGetValue( ( ( int ) d.Phase ).ToString(), out phaseOverride );
                    }

                    if ( phaseOverride == null )
                    {
                        continue;
                    }

                    if ( phaseOverride.Name.IsNotNullOrWhiteSpace() )
                    {
                        name = phaseOverride.Name.Trim();
                    }

                    if ( phaseOverride.Guidance.IsNotNullOrWhiteSpace() )
                    {
                        guidance = phaseOverride.Guidance.Trim();
                    }

                    if ( phaseOverride.Prompts != null && phaseOverride.Prompts.Any( p => p.IsNotNullOrWhiteSpace() ) )
                    {
                        prompts = phaseOverride.Prompts.Where( p => p.IsNotNullOrWhiteSpace() ).ToList();
                    }
                }

                return new PhaseDefinition
                {
                    Phase = d.Phase,
                    Name = name,
                    Guidance = profile.Format( guidance ),
                    Prompts = prompts.Select( p => profile.Format( p ) ).ToList(),
                    IsAutoSkippedInSimpleMode = d.IsAutoSkippedInSimpleMode,
                    IsLoopPhase = d.IsLoopPhase
                };
            } ).ToList();

            return profile;
        }

        public PhaseDefinition GetPhase( CasePhase phase )
        {
            return Phases.FirstOrDefault( p => p.Phase == phase ) ?? Phases[0];
        }

        public string Term( string key )
        {
            return Terms.TryGetValue( key, out var term ) ? term.Singular : key;
        }

        public string TermPlural( string key )
        {
            return Terms.TryGetValue( key, out var term ) ? term.Plural : key;
        }

        public string DescribeGate( GateRequirement requirement )
        {
            if ( requirement.Key.IsNullOrWhiteSpace() || !GateDescriptions.TryGetValue( requirement.Key, out var template ) )
            {
                return requirement.Description;
            }

            return Format( template, requirement.Count );
        }

        public string Format( string template, int? count = null )
        {
            if ( template.IsNullOrWhiteSpace() )
            {
                return template;
            }

            if ( _articleRegex.IsMatch( template ) )
            {
                template = FixArticles( template, count );
            }

            var resolved = _tokenRegex.Replace( template, match =>
            {
                var name = match.Groups[1].Value;
                var modifier = match.Groups[2].Value;

                if ( name.Equals( "count", StringComparison.OrdinalIgnoreCase ) )
                {
                    return count.HasValue ? count.Value.ToString() : match.Value;
                }

                if ( !Terms.TryGetValue( name, out var term ) )
                {
                    return match.Value;
                }

                var isPlural = modifier == "+" || ( modifier == "#" && count.HasValue && count.Value != 1 );
                var text = isPlural ? term.Plural : term.Singular;

                return char.IsLower( name[0] ) ? text.ToLower() : text;
            } );

            return resolved;
        }

        private string FixArticles( string template, int? count )
        {
            return _articleRegex.Replace( template, match =>
            {
                var article = match.Groups[1].Value;
                var token = match.Groups[2].Value;
                var text = Format( token, count );

                if ( text.Length == 0 || text == token )
                {
                    return match.Value;
                }

                var startsWithVowel = "aeiou".IndexOf( char.ToLower( text[0] ) ) >= 0;
                var corrected = startsWithVowel ? "an" : "a";

                if ( char.IsUpper( article[0] ) )
                {
                    corrected = char.ToUpper( corrected[0] ) + corrected.Substring( 1 );
                }

                return corrected + " " + token;
            } );
        }

        private static MethodProfileConfiguration Parse( string json )
        {
            if ( json.IsNullOrWhiteSpace() )
            {
                return null;
            }

            return json.FromJsonOrNull<MethodProfileConfiguration>();
        }
    }
}
