using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using com.yesuchum.RockDetective.Enums;
using com.yesuchum.RockDetective.Method;

namespace com.yesuchum.RockDetective.Tests
{
    [TestClass]
    public class GateEvaluatorTests
    {
        private static CaseAggregate AdvancedAggregate()
        {
            return new CaseAggregate
            {
                Mode = CaseMode.Advanced,
                Classification = CaseClassification.Difficult,
                ProblemStatement = "Sunday attendance is heavily skewed to the 11am service.",
                CategoryId = 1,
                HasOwner = true
            };
        }

        [TestMethod]
        public void DefineTeam_FailsWithoutChampionOrWaiver()
        {
            var aggregate = AdvancedAggregate();

            var result = GateEvaluator.Evaluate( aggregate, CasePhase.DefineTeam );

            Assert.IsFalse( result.IsMet );
        }

        [TestMethod]
        public void DefineTeam_PassesWithChampion()
        {
            var aggregate = AdvancedAggregate();
            aggregate.HasChampion = true;

            Assert.IsTrue( GateEvaluator.Evaluate( aggregate, CasePhase.DefineTeam ).IsMet );
        }

        [TestMethod]
        public void DefineTeam_PassesWithWaiverAndReason()
        {
            var aggregate = AdvancedAggregate();
            var state = aggregate.PhaseStates.GetState( CasePhase.DefineTeam );
            state.Data[PhaseStateKey.ChampionWaived] = "True";
            state.Data[PhaseStateKey.ChampionWaiveReason] = "Solo investigation on a low-stakes problem.";

            Assert.IsTrue( GateEvaluator.Evaluate( aggregate, CasePhase.DefineTeam ).IsMet );
        }

        [TestMethod]
        public void DefineTeam_FailsWithWaiverButNoReason()
        {
            var aggregate = AdvancedAggregate();
            aggregate.PhaseStates.GetState( CasePhase.DefineTeam ).Data[PhaseStateKey.ChampionWaived] = "True";

            Assert.IsFalse( GateEvaluator.Evaluate( aggregate, CasePhase.DefineTeam ).IsMet );
        }

        [TestMethod]
        public void DefineProblem_AdvancedRequiresAttestations()
        {
            var aggregate = AdvancedAggregate();

            Assert.IsFalse( GateEvaluator.Evaluate( aggregate, CasePhase.DefineProblem ).IsMet );

            var state = aggregate.PhaseStates.GetState( CasePhase.DefineProblem );
            state.Data[PhaseStateKey.AttestNoUnverifiedDetails] = "True";
            state.Data[PhaseStateKey.AttestNothingNotGermane] = "True";

            Assert.IsTrue( GateEvaluator.Evaluate( aggregate, CasePhase.DefineProblem ).IsMet );
        }

        [TestMethod]
        public void DefineProblem_SimpleSkipsAttestations()
        {
            var aggregate = AdvancedAggregate();
            aggregate.Mode = CaseMode.Simple;

            Assert.IsTrue( GateEvaluator.Evaluate( aggregate, CasePhase.DefineProblem ).IsMet );
        }

        [TestMethod]
        public void DefineProblem_FailsWithoutCategory()
        {
            var aggregate = AdvancedAggregate();
            aggregate.Mode = CaseMode.Simple;
            aggregate.CategoryId = null;

            Assert.IsFalse( GateEvaluator.Evaluate( aggregate, CasePhase.DefineProblem ).IsMet );
        }

        [TestMethod]
        public void Classify_RequiresClassification()
        {
            var aggregate = AdvancedAggregate();
            aggregate.Classification = CaseClassification.Unclassified;

            Assert.IsFalse( GateEvaluator.Evaluate( aggregate, CasePhase.Classify ).IsMet );

            aggregate.Classification = CaseClassification.Easy;

            Assert.IsTrue( GateEvaluator.Evaluate( aggregate, CasePhase.Classify ).IsMet );
        }

        [TestMethod]
        public void Timeline_RequiresTwoEventsIncludingProblemStart()
        {
            var aggregate = AdvancedAggregate();
            aggregate.TimelineEventTypes = new List<TimelineEventType?> { TimelineEventType.Change };

            Assert.IsFalse( GateEvaluator.Evaluate( aggregate, CasePhase.Timeline ).IsMet );

            aggregate.TimelineEventTypes.Add( TimelineEventType.LastKnownGood );

            Assert.IsFalse( GateEvaluator.Evaluate( aggregate, CasePhase.Timeline ).IsMet );

            aggregate.TimelineEventTypes.Add( TimelineEventType.ProblemStart );

            Assert.IsTrue( GateEvaluator.Evaluate( aggregate, CasePhase.Timeline ).IsMet );
        }

        [TestMethod]
        public void FactList_AdvancedRequiresThreeFacts()
        {
            var aggregate = AdvancedAggregate();
            aggregate.FactCount = 2;

            Assert.IsFalse( GateEvaluator.Evaluate( aggregate, CasePhase.FactList ).IsMet );

            aggregate.FactCount = 3;

            Assert.IsTrue( GateEvaluator.Evaluate( aggregate, CasePhase.FactList ).IsMet );
        }

        [TestMethod]
        public void FactList_SimpleRequiresOneFact()
        {
            var aggregate = AdvancedAggregate();
            aggregate.Mode = CaseMode.Simple;
            aggregate.FactCount = 1;

            Assert.IsTrue( GateEvaluator.Evaluate( aggregate, CasePhase.FactList ).IsMet );
        }

        [TestMethod]
        public void ProbabilityTree_RequiresRootHypothesesWithProbabilities()
        {
            var aggregate = AdvancedAggregate();
            aggregate.RootHypothesesWithProbabilityCount = 1;

            Assert.IsFalse( GateEvaluator.Evaluate( aggregate, CasePhase.ProbabilityTree ).IsMet );

            aggregate.RootHypothesesWithProbabilityCount = 2;

            Assert.IsTrue( GateEvaluator.Evaluate( aggregate, CasePhase.ProbabilityTree ).IsMet );
        }

        [TestMethod]
        public void Experiments_RequiresOutcomeAndRevertOrJustification()
        {
            var aggregate = AdvancedAggregate();
            aggregate.Experiments.Add( new ExperimentSnapshot
            {
                Id = 1,
                Outcome = ExperimentOutcome.Pending
            } );

            Assert.IsFalse( GateEvaluator.Evaluate( aggregate, CasePhase.Experiments ).IsMet );

            aggregate.Experiments[0].Outcome = ExperimentOutcome.Supported;
            aggregate.Experiments[0].HasChangesMade = true;

            Assert.IsFalse( GateEvaluator.Evaluate( aggregate, CasePhase.Experiments ).IsMet );

            aggregate.Experiments[0].HasRevertJustification = true;

            Assert.IsTrue( GateEvaluator.Evaluate( aggregate, CasePhase.Experiments ).IsMet );

            aggregate.Experiments[0].HasRevertJustification = false;
            aggregate.Experiments[0].ChangesReverted = true;

            Assert.IsTrue( GateEvaluator.Evaluate( aggregate, CasePhase.Experiments ).IsMet );
        }

        [TestMethod]
        public void Reevaluate_PassesWithConfirmedHypothesisAndSupportingExperiment()
        {
            var aggregate = AdvancedAggregate();
            aggregate.Hypotheses.Add( new HypothesisSnapshot { Id = 7, Status = HypothesisStatus.Confirmed } );
            aggregate.Experiments.Add( new ExperimentSnapshot { Id = 1, HypothesisId = 7, Outcome = ExperimentOutcome.Supported } );

            Assert.IsTrue( GateEvaluator.Evaluate( aggregate, CasePhase.Reevaluate ).IsMet );
        }

        [TestMethod]
        public void Reevaluate_FailsWhenConfirmedHypothesisLacksSupportingExperiment()
        {
            var aggregate = AdvancedAggregate();
            aggregate.Hypotheses.Add( new HypothesisSnapshot { Id = 7, Status = HypothesisStatus.Confirmed } );
            aggregate.Experiments.Add( new ExperimentSnapshot { Id = 1, HypothesisId = 8, Outcome = ExperimentOutcome.Supported } );

            Assert.IsFalse( GateEvaluator.Evaluate( aggregate, CasePhase.Reevaluate ).IsMet );
        }

        [TestMethod]
        public void Reevaluate_PassesWithExplicitLoopBack()
        {
            var aggregate = AdvancedAggregate();
            aggregate.PhaseStates.GetState( CasePhase.Reevaluate ).Data[PhaseStateKey.LoopBackCount] = "1";

            Assert.IsTrue( GateEvaluator.Evaluate( aggregate, CasePhase.Reevaluate ).IsMet );
        }

        [TestMethod]
        public void Reevaluate_ChampionSignOffRequiredWhenChampionAssignedAndHypothesisConfirmed()
        {
            var aggregate = AdvancedAggregate();
            aggregate.HasChampion = true;
            aggregate.Hypotheses.Add( new HypothesisSnapshot { Id = 7, Status = HypothesisStatus.Confirmed } );
            aggregate.Experiments.Add( new ExperimentSnapshot { Id = 1, HypothesisId = 7, Outcome = ExperimentOutcome.Supported } );

            Assert.IsFalse( GateEvaluator.Evaluate( aggregate, CasePhase.Reevaluate ).IsMet );

            aggregate.PhaseStates.GetState( CasePhase.Reevaluate ).Data[PhaseStateKey.ChampionSignOffDateTime] = "2026-07-31T09:00:00";

            Assert.IsTrue( GateEvaluator.Evaluate( aggregate, CasePhase.Reevaluate ).IsMet );
        }

        [TestMethod]
        public void Postmortem_RequiresAllFields()
        {
            var aggregate = AdvancedAggregate();

            Assert.IsFalse( GateEvaluator.Evaluate( aggregate, CasePhase.Postmortem ).IsMet );

            aggregate.HasPostmortemSummary = true;
            aggregate.HasImpactSummary = true;
            aggregate.HasSolutionSummary = true;
            aggregate.HasContributingFactors = true;
            aggregate.HasLessonsLearned = true;
            aggregate.HasPreventionMeasures = true;
            aggregate.HasActionItems = true;

            Assert.IsTrue( GateEvaluator.Evaluate( aggregate, CasePhase.Postmortem ).IsMet );
        }

        [TestMethod]
        public void SimpleMode_AutoSkipsTeamAndTimeline()
        {
            var aggregate = AdvancedAggregate();
            aggregate.Mode = CaseMode.Simple;

            Assert.IsTrue( GateEvaluator.IsPhaseAutoSkipped( aggregate, CasePhase.DefineTeam ) );
            Assert.IsTrue( GateEvaluator.IsPhaseAutoSkipped( aggregate, CasePhase.Timeline ) );
            Assert.IsFalse( GateEvaluator.IsPhaseAutoSkipped( aggregate, CasePhase.FactList ) );
        }

        [TestMethod]
        public void AdvancedMode_NeverAutoSkips()
        {
            var aggregate = AdvancedAggregate();

            Assert.IsFalse( MethodDefinition.Phases.Any( p => GateEvaluator.IsPhaseAutoSkipped( aggregate, p.Phase ) ) );
        }

        [TestMethod]
        public void SimpleMode_DoesNotSkipPhaseAlreadyInProgress()
        {
            var aggregate = AdvancedAggregate();
            aggregate.Mode = CaseMode.Simple;
            aggregate.PhaseStates.GetState( CasePhase.Timeline ).Progress = PhaseProgress.InProgress;

            Assert.IsFalse( GateEvaluator.IsPhaseAutoSkipped( aggregate, CasePhase.Timeline ) );
        }
    }
}
