using System.Collections.Generic;
using System.Linq;

using com.yesuchum.RockDetective.Enums;

namespace com.yesuchum.RockDetective.Method
{
    public class CaseAggregate
    {
        public CaseMode Mode { get; set; }

        public CaseClassification Classification { get; set; }

        public string ProblemStatement { get; set; }

        public int? CategoryId { get; set; }

        public bool HasOwner { get; set; }

        public bool HasChampion { get; set; }

        public CasePhaseStates PhaseStates { get; set; } = new CasePhaseStates();

        public List<TimelineEventType?> TimelineEventTypes { get; set; } = new List<TimelineEventType?>();

        public int FactCount { get; set; }

        public int RootHypothesesWithProbabilityCount { get; set; }

        public List<ExperimentSnapshot> Experiments { get; set; } = new List<ExperimentSnapshot>();

        public List<HypothesisSnapshot> Hypotheses { get; set; } = new List<HypothesisSnapshot>();

        public bool HasPostmortemSummary { get; set; }

        public bool HasImpactSummary { get; set; }

        public bool HasSolutionSummary { get; set; }

        public bool HasContributingFactors { get; set; }

        public bool HasLessonsLearned { get; set; }

        public bool HasPreventionMeasures { get; set; }

        public bool HasActionItems { get; set; }
    }

    public class ExperimentSnapshot
    {
        public int Id { get; set; }

        public int? HypothesisId { get; set; }

        public ExperimentOutcome Outcome { get; set; }

        public bool ChangesReverted { get; set; }

        public bool HasChangesMade { get; set; }

        public bool HasRevertJustification { get; set; }
    }

    public class HypothesisSnapshot
    {
        public int Id { get; set; }

        public HypothesisStatus Status { get; set; }
    }

    public class GateRequirement
    {
        public string Description { get; set; }

        public bool IsMet { get; set; }
    }

    public class GateResult
    {
        public CasePhase Phase { get; set; }

        public List<GateRequirement> Requirements { get; set; } = new List<GateRequirement>();

        public bool IsMet => Requirements.All( r => r.IsMet );
    }

    public class GateRequirementOptions
    {
        public int AdvancedMinimumFacts { get; set; } = 3;

        public int SimpleMinimumFacts { get; set; } = 1;

        public int AdvancedMinimumRootHypotheses { get; set; } = 2;

        public int SimpleMinimumRootHypotheses { get; set; } = 1;

        public int MinimumTimelineEvents { get; set; } = 2;

        public int MinimumCompletedExperiments { get; set; } = 1;

        public bool IsAttestationRequired { get; set; } = true;
    }

    public static class GateEvaluator
    {
        public static GateResult Evaluate( CaseAggregate aggregate, CasePhase phase, GateRequirementOptions options = null )
        {
            options = options ?? new GateRequirementOptions();
            var result = new GateResult { Phase = phase };
            var isSimpleMode = aggregate.Mode == CaseMode.Simple;

            switch ( phase )
            {
                case CasePhase.DefineTeam:
                    EvaluateDefineTeam( aggregate, result );
                    break;
                case CasePhase.DefineProblem:
                    EvaluateDefineProblem( aggregate, result, isSimpleMode || !options.IsAttestationRequired );
                    break;
                case CasePhase.Classify:
                    EvaluateClassify( aggregate, result );
                    break;
                case CasePhase.Timeline:
                    EvaluateTimeline( aggregate, result, options.MinimumTimelineEvents );
                    break;
                case CasePhase.FactList:
                    AddCountRequirement( result, aggregate.FactCount, isSimpleMode ? options.SimpleMinimumFacts : options.AdvancedMinimumFacts, "fact" );
                    break;
                case CasePhase.ProbabilityTree:
                    AddCountRequirement( result, aggregate.RootHypothesesWithProbabilityCount, isSimpleMode ? options.SimpleMinimumRootHypotheses : options.AdvancedMinimumRootHypotheses, "root hypothesis with a probability estimate", "root hypotheses with probability estimates" );
                    break;
                case CasePhase.Experiments:
                    EvaluateExperiments( aggregate, result, options.MinimumCompletedExperiments );
                    break;
                case CasePhase.Reevaluate:
                    EvaluateReevaluate( aggregate, result );
                    break;
                case CasePhase.Postmortem:
                    EvaluatePostmortem( aggregate, result );
                    break;
            }

            return result;
        }

        public static bool IsPhaseAutoSkipped( CaseAggregate aggregate, CasePhase phase )
        {
            if ( aggregate.Mode != CaseMode.Simple )
            {
                return false;
            }

            var definition = MethodDefinition.GetPhase( phase );
            var state = aggregate.PhaseStates.GetState( phase );

            return definition.IsAutoSkippedInSimpleMode && state.Progress != PhaseProgress.InProgress && state.Progress != PhaseProgress.Complete;
        }

        private static void EvaluateDefineTeam( CaseAggregate aggregate, GateResult result )
        {
            result.Requirements.Add( new GateRequirement
            {
                Description = "An owner is assigned to the case.",
                IsMet = aggregate.HasOwner
            } );

            var state = aggregate.PhaseStates.GetState( CasePhase.DefineTeam );
            var championWaived = state.Data.TryGetValue( PhaseStateKey.ChampionWaived, out var waived ) && waived == "True";
            var hasWaiveReason = state.Data.TryGetValue( PhaseStateKey.ChampionWaiveReason, out var reason ) && !string.IsNullOrWhiteSpace( reason );

            result.Requirements.Add( new GateRequirement
            {
                Description = "A Champion is assigned, or explicitly waived with a reason.",
                IsMet = aggregate.HasChampion || ( championWaived && hasWaiveReason )
            } );
        }

        private static void EvaluateDefineProblem( CaseAggregate aggregate, GateResult result, bool skipAttestations )
        {
            result.Requirements.Add( new GateRequirement
            {
                Description = "A problem statement is written.",
                IsMet = !string.IsNullOrWhiteSpace( aggregate.ProblemStatement )
            } );

            result.Requirements.Add( new GateRequirement
            {
                Description = "A problem category is assigned.",
                IsMet = aggregate.CategoryId.HasValue
            } );

            if ( !skipAttestations )
            {
                var state = aggregate.PhaseStates.GetState( CasePhase.DefineProblem );
                var attestedUnverified = state.Data.TryGetValue( PhaseStateKey.AttestNoUnverifiedDetails, out var a1 ) && a1 == "True";
                var attestedGermane = state.Data.TryGetValue( PhaseStateKey.AttestNothingNotGermane, out var a2 ) && a2 == "True";

                result.Requirements.Add( new GateRequirement
                {
                    Description = "Attested: the statement contains no unverified details.",
                    IsMet = attestedUnverified
                } );

                result.Requirements.Add( new GateRequirement
                {
                    Description = "Attested: the statement contains nothing that is not germane.",
                    IsMet = attestedGermane
                } );
            }
        }

        private static void EvaluateClassify( CaseAggregate aggregate, GateResult result )
        {
            result.Requirements.Add( new GateRequirement
            {
                Description = "The problem is classified as easy or difficult.",
                IsMet = aggregate.Classification != CaseClassification.Unclassified
            } );
        }

        private static void EvaluateTimeline( CaseAggregate aggregate, GateResult result, int minimumEvents )
        {
            result.Requirements.Add( new GateRequirement
            {
                Description = $"At least {minimumEvents} timeline event{( minimumEvents == 1 ? " is" : "s are" )} recorded.",
                IsMet = aggregate.TimelineEventTypes.Count >= minimumEvents
            } );

            result.Requirements.Add( new GateRequirement
            {
                Description = "The timeline includes when the problem started.",
                IsMet = aggregate.TimelineEventTypes.Any( t => t == TimelineEventType.ProblemStart )
            } );
        }

        private static void EvaluateExperiments( CaseAggregate aggregate, GateResult result, int minimum )
        {
            var completed = aggregate.Experiments.Count( e =>
                e.Outcome != ExperimentOutcome.Pending
                && ( e.ChangesReverted || !e.HasChangesMade || e.HasRevertJustification ) );

            result.Requirements.Add( new GateRequirement
            {
                Description = $"At least {minimum} experiment{( minimum == 1 ? " has" : "s have" )} a recorded outcome with changes reverted or justified.",
                IsMet = completed >= minimum
            } );
        }

        private static void EvaluateReevaluate( CaseAggregate aggregate, GateResult result )
        {
            var confirmedIds = aggregate.Hypotheses
                .Where( h => h.Status == HypothesisStatus.Confirmed )
                .Select( h => h.Id )
                .ToList();

            var hasConfirmedWithSupport = aggregate.Experiments.Any( e =>
                e.HypothesisId.HasValue
                && confirmedIds.Contains( e.HypothesisId.Value )
                && e.Outcome == ExperimentOutcome.Supported );

            var state = aggregate.PhaseStates.GetState( CasePhase.Reevaluate );
            var hasLoopedBack = state.Data.TryGetValue( PhaseStateKey.LoopBackCount, out var loops ) && loops != "0" && !string.IsNullOrWhiteSpace( loops );

            result.Requirements.Add( new GateRequirement
            {
                Description = "A hypothesis is confirmed with a supporting experiment, or the case has explicitly looped back for another round.",
                IsMet = hasConfirmedWithSupport || hasLoopedBack
            } );

            if ( aggregate.HasChampion && confirmedIds.Any() )
            {
                var hasSignOff = state.Data.TryGetValue( PhaseStateKey.ChampionSignOffDateTime, out var signOff ) && !string.IsNullOrWhiteSpace( signOff );

                result.Requirements.Add( new GateRequirement
                {
                    Description = "The Champion has signed off on the confirmed explanation.",
                    IsMet = hasSignOff
                } );
            }
        }

        private static void EvaluatePostmortem( CaseAggregate aggregate, GateResult result )
        {
            result.Requirements.Add( new GateRequirement { Description = "Problem summary is written.", IsMet = aggregate.HasPostmortemSummary } );
            result.Requirements.Add( new GateRequirement { Description = "Impact is described.", IsMet = aggregate.HasImpactSummary } );
            result.Requirements.Add( new GateRequirement { Description = "Solution summary is written.", IsMet = aggregate.HasSolutionSummary } );
            result.Requirements.Add( new GateRequirement { Description = "Contributing factors are listed.", IsMet = aggregate.HasContributingFactors } );
            result.Requirements.Add( new GateRequirement { Description = "Lessons learned are recorded.", IsMet = aggregate.HasLessonsLearned } );
            result.Requirements.Add( new GateRequirement { Description = "Prevention measures are recorded.", IsMet = aggregate.HasPreventionMeasures } );
            result.Requirements.Add( new GateRequirement { Description = "Action items are recorded.", IsMet = aggregate.HasActionItems } );
        }

        private static void AddCountRequirement( GateResult result, int actual, int minimum, string singular, string plural = null )
        {
            var noun = minimum == 1 ? singular : ( plural ?? singular + "s" );

            result.Requirements.Add( new GateRequirement
            {
                Description = $"At least {minimum} {noun} recorded.",
                IsMet = actual >= minimum
            } );
        }
    }
}
