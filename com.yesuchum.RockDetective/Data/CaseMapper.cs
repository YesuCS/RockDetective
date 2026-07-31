using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

using Rock;
using Rock.Data;

using com.yesuchum.RockDetective.Enums;
using com.yesuchum.RockDetective.Method;
using com.yesuchum.RockDetective.Model;
using com.yesuchum.RockDetective.ViewModels;

namespace com.yesuchum.RockDetective.Data
{
    public static class CaseMapper
    {
        public const int OverdueWitnessDays = 7;

        public static CasePhaseStates GetPhaseStates( CaseFile caseFile )
        {
            return caseFile.PhaseStateJson.FromJsonOrNull<CasePhaseStates>() ?? new CasePhaseStates();
        }

        public static void SetPhaseStates( CaseFile caseFile, CasePhaseStates states )
        {
            caseFile.PhaseStateJson = states.ToJson();
        }

        public static CaseAggregate BuildAggregate( CaseFile caseFile, List<CaseEntry> entries, List<Hypothesis> hypotheses, List<Experiment> experiments, List<CaseParticipant> participants )
        {
            return new CaseAggregate
            {
                Mode = caseFile.Mode,
                Classification = caseFile.Classification,
                ProblemStatement = caseFile.ProblemStatement,
                CategoryId = caseFile.CategoryId,
                HasOwner = caseFile.OwnerPersonAliasId > 0,
                HasChampion = participants.Any( p => p.Role == ParticipantRole.Champion ),
                PhaseStates = GetPhaseStates( caseFile ),
                TimelineEventTypes = entries
                    .Where( e => e.EntryType == CaseEntryType.TimelineEvent )
                    .Select( e => e.TimelineEventType )
                    .ToList(),
                FactCount = entries.Count( e => e.EntryType == CaseEntryType.Fact ),
                RootHypothesesWithProbabilityCount = hypotheses.Count( h => !h.ParentHypothesisId.HasValue && h.Probability > 0 ),
                Experiments = experiments.Select( e => new ExperimentSnapshot
                {
                    Id = e.Id,
                    HypothesisId = e.HypothesisId,
                    Outcome = e.Outcome,
                    ChangesReverted = e.ChangesReverted,
                    HasChangesMade = !string.IsNullOrWhiteSpace( e.ChangesMade ),
                    HasRevertJustification = !string.IsNullOrWhiteSpace( e.RevertJustification )
                } ).ToList(),
                Hypotheses = hypotheses.Select( h => new HypothesisSnapshot
                {
                    Id = h.Id,
                    Status = h.Status
                } ).ToList(),
                HasPostmortemSummary = !string.IsNullOrWhiteSpace( caseFile.SolutionSummary ) || !string.IsNullOrWhiteSpace( caseFile.ProblemStatement ),
                HasImpactSummary = !string.IsNullOrWhiteSpace( caseFile.ImpactSummary ),
                HasSolutionSummary = !string.IsNullOrWhiteSpace( caseFile.SolutionSummary ),
                HasContributingFactors = !string.IsNullOrWhiteSpace( caseFile.ContributingFactors ),
                HasLessonsLearned = !string.IsNullOrWhiteSpace( caseFile.LessonsLearned ),
                HasPreventionMeasures = !string.IsNullOrWhiteSpace( caseFile.PreventionMeasures ),
                HasActionItems = !string.IsNullOrWhiteSpace( caseFile.ActionItems )
            };
        }

        public static List<GateResultBag> EvaluateAllGates( CaseAggregate aggregate, GateRequirementOptions options = null )
        {
            return MethodDefinition.Phases
                .Select( d => ToBag( GateEvaluator.Evaluate( aggregate, d.Phase, options ) ) )
                .ToList();
        }

        public static GateResultBag ToBag( GateResult result )
        {
            return new GateResultBag
            {
                Phase = ( int ) result.Phase,
                IsMet = result.IsMet,
                Requirements = result.Requirements
                    .Select( r => new GateRequirementBag { Description = r.Description, IsMet = r.IsMet } )
                    .ToList()
            };
        }

        public static List<PhaseDefinitionBag> GetPhaseDefinitionBags()
        {
            return MethodDefinition.Phases.Select( d => new PhaseDefinitionBag
            {
                Phase = ( int ) d.Phase,
                Name = d.Name,
                Guidance = d.Guidance,
                Prompts = d.Prompts,
                IsAutoSkippedInSimpleMode = d.IsAutoSkippedInSimpleMode,
                IsLoopPhase = d.IsLoopPhase
            } ).ToList();
        }

        public static List<PhaseStateBag> GetPhaseStateBags( CaseFile caseFile )
        {
            var states = GetPhaseStates( caseFile );

            return states.Select( kv => new PhaseStateBag
            {
                Phase = ( int ) kv.Key,
                Progress = ( int ) kv.Value.Progress,
                CompletedDateTime = kv.Value.CompletedDateTime,
                Summary = kv.Value.Summary,
                Data = kv.Value.Data
            } ).ToList();
        }

        public static CaseEntryBag ToBag( CaseEntry entry, Dictionary<int, string> participantNames )
        {
            string binaryFileGuid = null;
            string binaryFileName = null;

            if ( entry.BinaryFileId.HasValue )
            {
                using ( var rockContext = new RockContext() )
                {
                    var file = new Rock.Model.BinaryFileService( rockContext ).Get( entry.BinaryFileId.Value );
                    binaryFileGuid = file?.Guid.ToString();
                    binaryFileName = file?.FileName;
                }
            }

            return new CaseEntryBag
            {
                IdKey = entry.IdKey,
                EntryType = ( int ) entry.EntryType,
                Text = entry.Text,
                EventDateTime = entry.EventDateTime,
                TimelineEventType = entry.TimelineEventType.HasValue ? ( int? ) entry.TimelineEventType.Value : null,
                BinaryFileGuid = binaryFileGuid,
                BinaryFileName = binaryFileName,
                Url = entry.Url,
                Source = ( int ) entry.Source,
                SourceParticipantIdKey = entry.SourceParticipantId.HasValue ? Rock.Utility.IdHasher.Instance.GetHash( entry.SourceParticipantId.Value ) : null,
                SourceParticipantName = entry.SourceParticipantId.HasValue && participantNames.ContainsKey( entry.SourceParticipantId.Value )
                    ? participantNames[entry.SourceParticipantId.Value]
                    : null,
                SourceExperimentIdKey = entry.SourceExperimentId.HasValue ? Rock.Utility.IdHasher.Instance.GetHash( entry.SourceExperimentId.Value ) : null,
                IsCorroborated = entry.IsCorroborated,
                Order = entry.Order,
                CreatedDateTime = entry.CreatedDateTime
            };
        }

        public static HypothesisBag ToBag( Hypothesis hypothesis )
        {
            return new HypothesisBag
            {
                IdKey = hypothesis.IdKey,
                ParentHypothesisIdKey = hypothesis.ParentHypothesisId.HasValue ? Rock.Utility.IdHasher.Instance.GetHash( hypothesis.ParentHypothesisId.Value ) : null,
                Name = hypothesis.Name,
                Description = hypothesis.Description,
                Probability = hypothesis.Probability,
                Status = ( int ) hypothesis.Status,
                RuledOutReason = hypothesis.RuledOutReason
            };
        }

        public static ExperimentBag ToBag( Experiment experiment )
        {
            return new ExperimentBag
            {
                IdKey = experiment.IdKey,
                HypothesisIdKey = experiment.HypothesisId.HasValue ? Rock.Utility.IdHasher.Instance.GetHash( experiment.HypothesisId.Value ) : null,
                HypothesisText = experiment.HypothesisText,
                TestPlan = experiment.TestPlan,
                ExpectedResult = experiment.ExpectedResult,
                ActualResult = experiment.ActualResult,
                Outcome = ( int ) experiment.Outcome,
                ChangesMade = experiment.ChangesMade,
                ChangesReverted = experiment.ChangesReverted,
                RevertJustification = experiment.RevertJustification,
                Learnings = experiment.Learnings,
                CreatedDateTime = experiment.CreatedDateTime
            };
        }

        public static ParticipantBag ToBag( CaseParticipant participant, Dictionary<int, PersonAliasInfo> aliasInfo, List<CaseStatement> statements = null )
        {
            var isExternal = !participant.PersonAliasId.HasValue;
            var displayName = isExternal
                ? participant.ExternalName
                : ( aliasInfo.ContainsKey( participant.PersonAliasId.Value ) ? aliasInfo[participant.PersonAliasId.Value].FullName : "Unknown" );

            return new ParticipantBag
            {
                IdKey = participant.IdKey,
                PersonAliasGuid = participant.PersonAliasId.HasValue && aliasInfo.ContainsKey( participant.PersonAliasId.Value )
                    ? aliasInfo[participant.PersonAliasId.Value].Guid.ToString()
                    : null,
                DisplayName = displayName,
                ExternalContact = participant.ExternalContact,
                IsExternal = isExternal,
                Role = ( int ) participant.Role,
                Statements = ( statements ?? new List<CaseStatement>() )
                    .Where( s => s.ParticipantId == participant.Id )
                    .OrderBy( s => s.Id )
                    .Select( ToBag )
                    .ToList()
            };
        }

        public static StatementBag ToBag( CaseStatement statement )
        {
            string binaryFileGuid = null;
            string binaryFileName = null;

            if ( statement.BinaryFileId.HasValue )
            {
                using ( var rockContext = new RockContext() )
                {
                    var file = new Rock.Model.BinaryFileService( rockContext ).Get( statement.BinaryFileId.Value );
                    binaryFileGuid = file?.Guid.ToString();
                    binaryFileName = file?.FileName;
                }
            }

            var isOverdue = ( statement.RequestStatus == StatementRequestStatus.Requested || statement.RequestStatus == StatementRequestStatus.Reminded )
                && statement.SentDateTime.HasValue
                && statement.SentDateTime.Value < RockDateTime.Now.AddDays( -OverdueWitnessDays );

            return new StatementBag
            {
                IdKey = statement.IdKey,
                ParticipantIdKey = Rock.Utility.IdHasher.Instance.GetHash( statement.ParticipantId ),
                RequestPrompt = statement.RequestPrompt,
                RequestStatus = ( int ) statement.RequestStatus,
                RequestDateTime = statement.RequestDateTime,
                SentDateTime = statement.SentDateTime,
                SubmittedDateTime = statement.SubmittedDateTime,
                Statement = statement.Statement,
                BinaryFileGuid = binaryFileGuid,
                BinaryFileName = binaryFileName,
                Url = statement.Url,
                IsOverdue = isOverdue
            };
        }

        public class PersonAliasInfo
        {
            public Guid Guid { get; set; }

            public int PersonId { get; set; }

            public string FullName { get; set; }
        }

        public static Dictionary<int, PersonAliasInfo> GetAliasInfo( IEnumerable<int> personAliasIds )
        {
            var ids = personAliasIds.Distinct().ToList();

            if ( !ids.Any() )
            {
                return new Dictionary<int, PersonAliasInfo>();
            }

            using ( var rockContext = new RockContext() )
            {
                return new Rock.Model.PersonAliasService( rockContext )
                    .Queryable()
                    .AsNoTracking()
                    .Where( pa => ids.Contains( pa.Id ) )
                    .Select( pa => new { pa.Id, pa.Guid, pa.PersonId, pa.Person.NickName, pa.Person.LastName } )
                    .ToList()
                    .ToDictionary( x => x.Id, x => new PersonAliasInfo
                    {
                        Guid = x.Guid,
                        PersonId = x.PersonId,
                        FullName = $"{x.NickName} {x.LastName}"
                    } );
            }
        }

        public static CaseFileBag ToCaseFileBag( CaseFile caseFile, List<CaseEntry> entries, List<Hypothesis> hypotheses, List<Experiment> experiments, List<CaseParticipant> participants, GateRequirementOptions gateOptions = null, List<CaseStatement> statements = null )
        {
            var aliasIds = participants.Where( p => p.PersonAliasId.HasValue ).Select( p => p.PersonAliasId.Value ).ToList();
            aliasIds.Add( caseFile.OwnerPersonAliasId );
            var aliasInfo = GetAliasInfo( aliasIds );

            var participantNames = participants.ToDictionary(
                p => p.Id,
                p => p.PersonAliasId.HasValue && aliasInfo.ContainsKey( p.PersonAliasId.Value )
                    ? aliasInfo[p.PersonAliasId.Value].FullName
                    : p.ExternalName );

            var aggregate = BuildAggregate( caseFile, entries, hypotheses, experiments, participants );

            return new CaseFileBag
            {
                IdKey = caseFile.IdKey,
                CaseNumber = caseFile.Id.ToString(),
                Title = caseFile.Title,
                ProblemStatement = caseFile.ProblemStatement,
                CategoryGuid = caseFile.Category?.Guid.ToString(),
                CategoryName = caseFile.Category?.Name,
                Mode = ( int ) caseFile.Mode,
                Classification = ( int ) caseFile.Classification,
                Status = ( int ) caseFile.Status,
                CurrentPhase = ( int ) caseFile.CurrentPhase,
                OwnerName = aliasInfo.ContainsKey( caseFile.OwnerPersonAliasId ) ? aliasInfo[caseFile.OwnerPersonAliasId].FullName : "Unknown",
                OwnerPersonAliasGuid = aliasInfo.ContainsKey( caseFile.OwnerPersonAliasId ) ? aliasInfo[caseFile.OwnerPersonAliasId].Guid.ToString() : null,
                ImpactSummary = caseFile.ImpactSummary,
                SolutionSummary = caseFile.SolutionSummary,
                ContributingFactors = caseFile.ContributingFactors,
                LessonsLearned = caseFile.LessonsLearned,
                PreventionMeasures = caseFile.PreventionMeasures,
                ActionItems = caseFile.ActionItems,
                FollowUpDate = caseFile.FollowUpDate,
                FollowUpConfirmedDateTime = caseFile.FollowUpConfirmedDateTime,
                ClosedDateTime = caseFile.ClosedDateTime,
                CreatedDateTime = caseFile.CreatedDateTime,
                PhaseStates = GetPhaseStateBags( caseFile ),
                GateResults = EvaluateAllGates( aggregate, gateOptions ),
                Entries = entries.OrderBy( e => e.Order ).ThenBy( e => e.Id ).Select( e => ToBag( e, participantNames ) ).ToList(),
                Hypotheses = hypotheses.OrderBy( h => h.Id ).Select( ToBag ).ToList(),
                Experiments = experiments.OrderBy( e => e.Id ).Select( ToBag ).ToList(),
                Participants = participants.OrderBy( p => p.Role ).ThenBy( p => p.Id ).Select( p => ToBag( p, aliasInfo, statements ) ).ToList()
            };
        }
    }
}
