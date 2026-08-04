using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;

using Rock;
using Rock.Attribute;
using Rock.Blocks;
using Rock.Communication;
using Rock.Web.Cache;

using com.yesuchum.RockDetective.Data;
using com.yesuchum.RockDetective.Enums;
using com.yesuchum.RockDetective.Method;
using com.yesuchum.RockDetective.ViewModels;

namespace com.yesuchum.RockDetective.Blocks
{
    [DisplayName( "Case Board" )]
    [Category( "Rock Detective" )]
    [Description( "The Rock Detective case workspace: phase stepper, phase forms with gate checklists, facts, probability tree, experiments, timeline, evidence, witnesses, and team." )]
    [IconCssClass( "ti ti-clipboard-list" )]
    [SupportedSiteTypes( Rock.Model.SiteType.Web )]

    [LinkedPage( "Witness Statement Page",
        Description = "The page witnesses land on to submit a requested statement.",
        IsRequired = true,
        Order = 0,
        Key = AttributeKey.WitnessStatementPage )]

    [BooleanField( "Send Requests Automatically",
        Description = "When on, creating a witness statement request immediately sends the request communication. When off, requests start as drafts and are sent manually from the case board.",
        DefaultBooleanValue = false,
        Order = 1,
        Key = AttributeKey.SendRequestsAutomatically )]

    [BooleanField( "Restrict Phase Jumping",
        Description = "When on, phases must be entered in order by completing the previous phase; the stepper only allows revisiting phases that have already been started. When off, any phase can be selected at any time.",
        DefaultBooleanValue = false,
        Order = 2,
        Key = AttributeKey.RestrictPhaseJumping )]

    [IntegerField( "Minimum Facts (Advanced)",
        Description = "Facts required before the Fact List phase can complete on a Advanced case. The method's ideal is 3; a real fact list keeps growing well past the gate.",
        DefaultIntegerValue = 3,
        IsRequired = true,
        Category = AttributeCategory.GateRequirements,
        Order = 2,
        Key = AttributeKey.AdvancedMinimumFacts )]

    [IntegerField( "Minimum Facts (Simple)",
        Description = "Facts required before the Fact List phase can complete on a Simple case. The method's ideal is 1.",
        DefaultIntegerValue = 1,
        IsRequired = true,
        Category = AttributeCategory.GateRequirements,
        Order = 3,
        Key = AttributeKey.SimpleMinimumFacts )]

    [IntegerField( "Minimum Root Hypotheses (Advanced)",
        Description = "Root hypotheses with probability estimates required before the Probability Tree phase can complete on a Advanced case. The method's ideal is 2; a single hypothesis usually means brainstorming stopped too early.",
        DefaultIntegerValue = 2,
        IsRequired = true,
        Category = AttributeCategory.GateRequirements,
        Order = 4,
        Key = AttributeKey.AdvancedMinimumHypotheses )]

    [IntegerField( "Minimum Root Hypotheses (Simple)",
        Description = "Root hypotheses with probability estimates required before the Probability Tree phase can complete on a Simple case. The method's ideal is 1.",
        DefaultIntegerValue = 1,
        IsRequired = true,
        Category = AttributeCategory.GateRequirements,
        Order = 5,
        Key = AttributeKey.SimpleMinimumHypotheses )]

    [IntegerField( "Minimum Timeline Events",
        Description = "Timeline events required before the Timeline phase can complete. A Problem Start event is always required in addition to this count. The method's ideal is 2 (problem start plus last known good).",
        DefaultIntegerValue = 2,
        IsRequired = true,
        Category = AttributeCategory.GateRequirements,
        Order = 6,
        Key = AttributeKey.MinimumTimelineEvents )]

    [IntegerField( "Minimum Completed Experiments",
        Description = "Experiments with a recorded outcome (and changes reverted or justified) required before the Experiments phase can complete. The method's ideal is 1 per loop iteration.",
        DefaultIntegerValue = 1,
        IsRequired = true,
        Category = AttributeCategory.GateRequirements,
        Order = 7,
        Key = AttributeKey.MinimumExperiments )]

    [BooleanField( "Require Problem Statement Attestations",
        Description = "When on, Advanced cases must attest that the problem statement contains no unverified details and nothing that is not germane before the Define the Problem phase can complete. The method's ideal is on; the attestations are what force a disciplined statement.",
        DefaultBooleanValue = true,
        Category = AttributeCategory.GateRequirements,
        Order = 8,
        Key = AttributeKey.RequireAttestations )]

    [Rock.SystemGuid.EntityTypeGuid( SystemGuid.EntityType.CASE_BOARD_BLOCK )]
    [Rock.SystemGuid.BlockTypeGuid( SystemGuid.BlockType.CASE_FILE_DETAIL )]
    public class CaseFileDetail : RockBlockType
    {
        private static class AttributeKey
        {
            public const string WitnessStatementPage = "WitnessStatementPage";
            public const string SendRequestsAutomatically = "SendRequestsAutomatically";
            public const string RestrictPhaseJumping = "RestrictPhaseJumping";
            public const string AdvancedMinimumFacts = "AdvancedMinimumFacts";
            public const string SimpleMinimumFacts = "SimpleMinimumFacts";
            public const string AdvancedMinimumHypotheses = "AdvancedMinimumHypotheses";
            public const string SimpleMinimumHypotheses = "SimpleMinimumHypotheses";
            public const string MinimumTimelineEvents = "MinimumTimelineEvents";
            public const string MinimumExperiments = "MinimumExperiments";
            public const string RequireAttestations = "RequireAttestations";
        }

        private static class AttributeCategory
        {
            public const string GateRequirements = "Gate Requirements";
        }

        private GateRequirementOptions GetGateOptions()
        {
            var defaults = new GateRequirementOptions();

            return new GateRequirementOptions
            {
                AdvancedMinimumFacts = GetAttributeValue( AttributeKey.AdvancedMinimumFacts ).AsIntegerOrNull() ?? defaults.AdvancedMinimumFacts,
                SimpleMinimumFacts = GetAttributeValue( AttributeKey.SimpleMinimumFacts ).AsIntegerOrNull() ?? defaults.SimpleMinimumFacts,
                AdvancedMinimumRootHypotheses = GetAttributeValue( AttributeKey.AdvancedMinimumHypotheses ).AsIntegerOrNull() ?? defaults.AdvancedMinimumRootHypotheses,
                SimpleMinimumRootHypotheses = GetAttributeValue( AttributeKey.SimpleMinimumHypotheses ).AsIntegerOrNull() ?? defaults.SimpleMinimumRootHypotheses,
                MinimumTimelineEvents = GetAttributeValue( AttributeKey.MinimumTimelineEvents ).AsIntegerOrNull() ?? defaults.MinimumTimelineEvents,
                MinimumCompletedExperiments = GetAttributeValue( AttributeKey.MinimumExperiments ).AsIntegerOrNull() ?? defaults.MinimumCompletedExperiments,
                IsAttestationRequired = GetAttributeValue( AttributeKey.RequireAttestations ).AsBooleanOrNull() ?? defaults.IsAttestationRequired
            };
        }

        private static class PageParameterKey
        {
            public const string CaseFile = "CaseFile";
            public const string Participant = "Participant";
            public const string Statement = "Statement";
            public const string FollowUp = "FollowUp";
        }

        public override string ObsidianFileUrl => "~/Plugins/com_yesuchum/RockDetective/caseFileDetail.obs";

        public override object GetObsidianBlockInitialization()
        {
            var box = new CaseBoardInitBag
            {
                PhaseDefinitions = CaseMapper.GetPhaseDefinitionBags(),
                Principles = MethodDefinition.Principles,
                MindsetGuidance = MethodDefinition.MindsetGuidance,
                ContextualTips = MethodDefinition.ContextualTips,
                Categories = GetCategoryOptions()
            };

            var currentPerson = RequestContext.CurrentPerson;

            using ( var detectiveContext = new RockDetectiveContext() )
            {
                var caseFile = GetCaseFromPageParameter( detectiveContext );

                if ( caseFile == null )
                {
                    box.ErrorMessage = "Case file not found.";
                    return box;
                }

                if ( !caseFile.IsAuthorized( Rock.Security.Authorization.VIEW, currentPerson ) )
                {
                    box.ErrorMessage = "You are not authorized to view this case.";
                    return box;
                }

                box.CanEdit = caseFile.IsAuthorized( Rock.Security.Authorization.EDIT, currentPerson ) || IsOwner( caseFile, currentPerson );
                box.FollowUpActionMessage = ProcessFollowUpParameter( detectiveContext, caseFile, box.CanEdit );

                var entities = LoadCaseEntities( detectiveContext, caseFile.Id );

                box.CaseFile = CaseMapper.ToCaseFileBag( caseFile, entities.Entries, entities.Hypotheses, entities.Experiments, entities.Participants, GetGateOptions(), entities.Statements );
                box.CanAdministrate = caseFile.IsAuthorized( Rock.Security.Authorization.ADMINISTRATE, currentPerson );
                box.IsOwner = IsOwner( caseFile, currentPerson );
                box.IsChampion = IsInRole( entities.Participants, currentPerson, ParticipantRole.Champion );
                box.IsPhaseJumpingRestricted = GetAttributeValue( AttributeKey.RestrictPhaseJumping ).AsBoolean();
            }

            return box;
        }

        private class CaseEntities
        {
            public List<Model.CaseEntry> Entries { get; set; }

            public List<Model.Hypothesis> Hypotheses { get; set; }

            public List<Model.Experiment> Experiments { get; set; }

            public List<Model.CaseParticipant> Participants { get; set; }

            public List<Model.CaseStatement> Statements { get; set; }
        }

        private static CaseEntities LoadCaseEntities( RockDetectiveContext detectiveContext, int caseFileId )
        {
            return new CaseEntities
            {
                Entries = detectiveContext.CaseEntries.AsNoTracking().Where( e => e.CaseFileId == caseFileId ).ToList(),
                Hypotheses = detectiveContext.Hypotheses.AsNoTracking().Where( h => h.CaseFileId == caseFileId ).ToList(),
                Experiments = detectiveContext.Experiments.AsNoTracking().Where( e => e.CaseFileId == caseFileId ).ToList(),
                Participants = detectiveContext.CaseParticipants.AsNoTracking().Where( p => p.CaseFileId == caseFileId ).ToList(),
                Statements = detectiveContext.CaseStatements.AsNoTracking().Where( s => s.CaseFileId == caseFileId ).ToList()
            };
        }

        private string ProcessFollowUpParameter( RockDetectiveContext detectiveContext, Model.CaseFile caseFile, bool canEdit )
        {
            var followUpAction = PageParameter( PageParameterKey.FollowUp );

            if ( followUpAction.IsNullOrWhiteSpace() || !canEdit )
            {
                return null;
            }

            if ( caseFile.Status != CaseStatus.Closed || !caseFile.FollowUpDate.HasValue || caseFile.FollowUpConfirmedDateTime.HasValue )
            {
                return null;
            }

            if ( followUpAction.Equals( "Confirm", StringComparison.OrdinalIgnoreCase ) )
            {
                caseFile.FollowUpConfirmedDateTime = RockDateTime.Now;
                StampModified( caseFile );
                detectiveContext.SaveChanges();

                return "Marked the fix as held; no further follow-up checks will be sent.";
            }

            if ( followUpAction.Equals( "Reopen", StringComparison.OrdinalIgnoreCase ) )
            {
                caseFile.Status = CaseStatus.Reopened;
                caseFile.ClosedDateTime = null;
                caseFile.FollowUpConfirmedDateTime = null;
                caseFile.CurrentPhase = CasePhase.FactList;
                StampModified( caseFile );
                detectiveContext.SaveChanges();

                return "The case has been reopened; the investigation resumes at the Fact List phase.";
            }

            return null;
        }

        private Model.CaseFile GetCaseFromPageParameter( RockDetectiveContext detectiveContext )
        {
            var key = PageParameter( PageParameterKey.CaseFile );

            if ( key.IsNullOrWhiteSpace() )
            {
                return null;
            }

            return GetCaseByKey( detectiveContext, key );
        }

        private static Model.CaseFile GetCaseByKey( RockDetectiveContext detectiveContext, string idKey )
        {
            var id = Rock.Utility.IdHasher.Instance.GetId( idKey );

            if ( !id.HasValue )
            {
                var guid = idKey.AsGuidOrNull();

                if ( guid.HasValue )
                {
                    return new CaseFileService( detectiveContext ).Queryable().FirstOrDefault( c => c.Guid == guid.Value );
                }

                return null;
            }

            return new CaseFileService( detectiveContext ).Get( id.Value );
        }

        private static bool IsOwner( Model.CaseFile caseFile, Rock.Model.Person person )
        {
            if ( person == null )
            {
                return false;
            }

            return person.Aliases.Any( a => a.Id == caseFile.OwnerPersonAliasId );
        }

        private static bool IsInRole( List<Model.CaseParticipant> participants, Rock.Model.Person person, ParticipantRole role )
        {
            if ( person == null )
            {
                return false;
            }

            var aliasIds = person.Aliases.Select( a => a.Id ).ToList();

            return participants.Any( p => p.Role == role && p.PersonAliasId.HasValue && aliasIds.Contains( p.PersonAliasId.Value ) );
        }

        private List<CategoryOptionBag> GetCategoryOptions()
        {
            var entityTypeId = EntityTypeCache.Get( SystemGuid.EntityType.CASE_FILE.AsGuid() )?.Id;

            if ( !entityTypeId.HasValue )
            {
                return new List<CategoryOptionBag>();
            }

            using ( var rockContext = new Rock.Data.RockContext() )
            {
                return new Rock.Model.CategoryService( rockContext )
                    .Queryable()
                    .Where( c => c.EntityTypeId == entityTypeId.Value )
                    .OrderBy( c => c.Order )
                    .ThenBy( c => c.Name )
                    .ToList()
                    .Select( c => new CategoryOptionBag { Guid = c.Guid.ToString(), Name = c.Name, IconCssClass = c.IconCssClass } )
                    .ToList();
            }
        }

        private BlockActionResult WithEditableCase( string idKey, Func<RockDetectiveContext, Model.CaseFile, BlockActionResult> action, bool allowClosed = false )
        {
            var currentPerson = RequestContext.CurrentPerson;

            using ( var detectiveContext = new RockDetectiveContext() )
            {
                var caseFile = GetCaseByKey( detectiveContext, idKey );

                if ( caseFile == null )
                {
                    return ActionNotFound( "Case file not found." );
                }

                if ( !caseFile.IsAuthorized( Rock.Security.Authorization.EDIT, currentPerson ) && !IsOwner( caseFile, currentPerson ) )
                {
                    return ActionForbidden( "You are not authorized to edit this case." );
                }

                if ( !allowClosed && caseFile.Status == CaseStatus.Closed )
                {
                    return ActionBadRequest( "This case is closed. Reopen it to make changes." );
                }

                if ( !allowClosed && caseFile.Status == CaseStatus.Abandoned )
                {
                    return ActionBadRequest( "This case is abandoned. Resume it to make changes." );
                }

                return action( detectiveContext, caseFile );
            }
        }

        private CaseFileBag BuildBag( RockDetectiveContext detectiveContext, Model.CaseFile caseFile )
        {
            var entities = LoadCaseEntities( detectiveContext, caseFile.Id );

            return CaseMapper.ToCaseFileBag( caseFile, entities.Entries, entities.Hypotheses, entities.Experiments, entities.Participants, GetGateOptions(), entities.Statements );
        }

        private void StampModified( Model.CaseFile caseFile )
        {
            caseFile.ModifiedDateTime = RockDateTime.Now;
            caseFile.ModifiedByPersonAliasId = RequestContext.CurrentPerson?.PrimaryAliasId;
        }

        #region Case level actions

        [BlockAction]
        public BlockActionResult SaveProblemDefinition( string idKey, string title, string problemStatement, string categoryGuid, bool attestNoUnverifiedDetails, bool attestNothingNotGermane )
        {
            return WithEditableCase( idKey, ( detectiveContext, caseFile ) =>
            {
                if ( title.IsNullOrWhiteSpace() || problemStatement.IsNullOrWhiteSpace() )
                {
                    return ActionBadRequest( "A title and problem statement are required." );
                }

                var categoryId = CategoryCache.Get( categoryGuid.AsGuid() )?.Id;

                if ( !categoryId.HasValue )
                {
                    return ActionBadRequest( "A problem category is required." );
                }

                caseFile.Title = title.Truncate( 200 );
                caseFile.ProblemStatement = problemStatement;
                caseFile.CategoryId = categoryId;

                var states = CaseMapper.GetPhaseStates( caseFile );
                var state = states.GetState( CasePhase.DefineProblem );
                state.Data[PhaseStateKey.AttestNoUnverifiedDetails] = attestNoUnverifiedDetails.ToString();
                state.Data[PhaseStateKey.AttestNothingNotGermane] = attestNothingNotGermane.ToString();
                CaseMapper.SetPhaseStates( caseFile, states );

                StampModified( caseFile );
                detectiveContext.SaveChanges();

                return ActionOk( BuildBag( detectiveContext, caseFile ) );
            } );
        }

        [BlockAction]
        public BlockActionResult SavePhaseForm( string idKey, PhaseFormSaveBag bag )
        {
            return WithEditableCase( idKey, ( detectiveContext, caseFile ) =>
            {
                if ( bag == null )
                {
                    return ActionBadRequest();
                }

                var phase = ( CasePhase ) bag.Phase;
                var states = CaseMapper.GetPhaseStates( caseFile );
                var state = states.GetState( phase );

                state.Summary = bag.Summary;

                foreach ( var kv in bag.Data ?? new Dictionary<string, string>() )
                {
                    state.Data[kv.Key] = kv.Value;
                }

                if ( state.Progress == PhaseProgress.NotStarted || state.Progress == PhaseProgress.Skipped )
                {
                    state.Progress = PhaseProgress.InProgress;
                }

                CaseMapper.SetPhaseStates( caseFile, states );
                StampModified( caseFile );
                detectiveContext.SaveChanges();

                return ActionOk( BuildBag( detectiveContext, caseFile ) );
            } );
        }

        [BlockAction]
        public BlockActionResult CompletePhase( string idKey, int phase )
        {
            return WithEditableCase( idKey, ( detectiveContext, caseFile ) =>
            {
                var casePhase = ( CasePhase ) phase;
                var entities = LoadCaseEntities( detectiveContext, caseFile.Id );
                var aggregate = CaseMapper.BuildAggregate( caseFile, entities.Entries, entities.Hypotheses, entities.Experiments, entities.Participants );
                var gate = GateEvaluator.Evaluate( aggregate, casePhase, GetGateOptions() );

                if ( !gate.IsMet )
                {
                    return ActionBadRequest( "This phase's gate requirements are not met: "
                        + gate.Requirements.Where( r => !r.IsMet ).Select( r => r.Description ).ToList().AsDelimited( " " ) );
                }

                var states = CaseMapper.GetPhaseStates( caseFile );
                var state = states.GetState( casePhase );
                state.Progress = PhaseProgress.Complete;
                state.CompletedDateTime = RockDateTime.Now;

                if ( casePhase != CasePhase.Postmortem )
                {
                    var next = ( CasePhase ) ( phase + 1 );

                    while ( GateEvaluator.IsPhaseAutoSkipped( aggregate, next ) && next < CasePhase.Postmortem )
                    {
                        states.GetState( next ).Progress = PhaseProgress.Skipped;
                        next = ( CasePhase ) ( ( int ) next + 1 );
                    }

                    caseFile.CurrentPhase = next;

                    if ( caseFile.Status == CaseStatus.Open && next >= CasePhase.FactList )
                    {
                        caseFile.Status = CaseStatus.Investigating;
                    }
                }

                var confirmedIds = entities.Hypotheses.Where( h => h.Status == HypothesisStatus.Confirmed ).Select( h => h.Id ).ToList();

                if ( casePhase == CasePhase.Reevaluate && confirmedIds.Any() )
                {
                    caseFile.Status = CaseStatus.Solved;
                }

                CaseMapper.SetPhaseStates( caseFile, states );
                StampModified( caseFile );
                detectiveContext.SaveChanges();

                return ActionOk( BuildBag( detectiveContext, caseFile ) );
            } );
        }

        [BlockAction]
        public BlockActionResult SetCurrentPhase( string idKey, int phase )
        {
            return WithEditableCase( idKey, ( detectiveContext, caseFile ) =>
            {
                var isRestricted = GetAttributeValue( AttributeKey.RestrictPhaseJumping ).AsBoolean();
                var targetPhase = ( CasePhase ) phase;

                if ( isRestricted && targetPhase != caseFile.CurrentPhase )
                {
                    var targetState = CaseMapper.GetPhaseStates( caseFile ).GetState( targetPhase );

                    if ( targetState.Progress == PhaseProgress.NotStarted )
                    {
                        return ActionBadRequest( "Phases must be entered in order. Complete the current phase to advance." );
                    }
                }

                caseFile.CurrentPhase = targetPhase;

                var states = CaseMapper.GetPhaseStates( caseFile );
                var state = states.GetState( caseFile.CurrentPhase );

                if ( state.Progress == PhaseProgress.NotStarted || state.Progress == PhaseProgress.Skipped )
                {
                    state.Progress = PhaseProgress.InProgress;
                }

                CaseMapper.SetPhaseStates( caseFile, states );
                StampModified( caseFile );
                detectiveContext.SaveChanges();

                return ActionOk( BuildBag( detectiveContext, caseFile ) );
            } );
        }

        [BlockAction]
        public BlockActionResult LoopBack( string idKey )
        {
            return WithEditableCase( idKey, ( detectiveContext, caseFile ) =>
            {
                var states = CaseMapper.GetPhaseStates( caseFile );
                var reevaluateState = states.GetState( CasePhase.Reevaluate );

                var loopCount = 0;
                if ( reevaluateState.Data.TryGetValue( PhaseStateKey.LoopBackCount, out var existing ) )
                {
                    loopCount = existing.AsInteger();
                }

                reevaluateState.Data[PhaseStateKey.LoopBackCount] = ( loopCount + 1 ).ToString();
                reevaluateState.Data[PhaseStateKey.LastLoopBackDateTime] = RockDateTime.Now.ToString( "s" );

                foreach ( var loopPhase in new[] { CasePhase.FactList, CasePhase.ProbabilityTree, CasePhase.Experiments, CasePhase.Reevaluate } )
                {
                    var state = states.GetState( loopPhase );

                    if ( state.Progress == PhaseProgress.Complete )
                    {
                        state.Progress = PhaseProgress.InProgress;
                    }
                }

                caseFile.CurrentPhase = CasePhase.FactList;
                CaseMapper.SetPhaseStates( caseFile, states );
                StampModified( caseFile );
                detectiveContext.SaveChanges();

                return ActionOk( BuildBag( detectiveContext, caseFile ) );
            } );
        }

        [BlockAction]
        public BlockActionResult UpgradeMode( string idKey )
        {
            return WithEditableCase( idKey, ( detectiveContext, caseFile ) =>
            {
                if ( caseFile.Mode == CaseMode.Advanced )
                {
                    return ActionBadRequest( "This case is already in Advanced mode." );
                }

                caseFile.Mode = CaseMode.Advanced;
                caseFile.Classification = CaseClassification.Difficult;
                StampModified( caseFile );
                detectiveContext.SaveChanges();

                return ActionOk( BuildBag( detectiveContext, caseFile ) );
            } );
        }

        [BlockAction]
        public BlockActionResult ChampionSignOff( string idKey )
        {
            var currentPerson = RequestContext.CurrentPerson;

            using ( var detectiveContext = new RockDetectiveContext() )
            {
                var caseFile = GetCaseByKey( detectiveContext, idKey );

                if ( caseFile == null )
                {
                    return ActionNotFound( "Case file not found." );
                }

                var participants = detectiveContext.CaseParticipants.AsNoTracking().Where( p => p.CaseFileId == caseFile.Id ).ToList();

                if ( !IsInRole( participants, currentPerson, ParticipantRole.Champion ) )
                {
                    return ActionForbidden( "Only the case Champion can sign off." );
                }

                var states = CaseMapper.GetPhaseStates( caseFile );
                var state = states.GetState( CasePhase.Reevaluate );
                state.Data[PhaseStateKey.ChampionSignOffDateTime] = RockDateTime.Now.ToString( "s" );
                state.Data[PhaseStateKey.ChampionSignOffPersonAliasId] = currentPerson.PrimaryAliasId.ToString();
                CaseMapper.SetPhaseStates( caseFile, states );
                StampModified( caseFile );
                detectiveContext.SaveChanges();

                return ActionOk( BuildBag( detectiveContext, caseFile ) );
            }
        }

        [BlockAction]
        public BlockActionResult CloseCase( string idKey, CloseCaseBag bag )
        {
            return WithEditableCase( idKey, ( detectiveContext, caseFile ) =>
            {
                if ( bag == null )
                {
                    return ActionBadRequest();
                }

                caseFile.ImpactSummary = bag.ImpactSummary;
                caseFile.SolutionSummary = bag.SolutionSummary;
                caseFile.ContributingFactors = bag.ContributingFactors;
                caseFile.LessonsLearned = bag.LessonsLearned;
                caseFile.PreventionMeasures = bag.PreventionMeasures;
                caseFile.ActionItems = bag.ActionItems;

                var entities = LoadCaseEntities( detectiveContext, caseFile.Id );
                var aggregate = CaseMapper.BuildAggregate( caseFile, entities.Entries, entities.Hypotheses, entities.Experiments, entities.Participants );
                var gate = GateEvaluator.Evaluate( aggregate, CasePhase.Postmortem, GetGateOptions() );

                if ( !gate.IsMet )
                {
                    detectiveContext.SaveChanges();
                    return ActionBadRequest( "The postmortem is incomplete: "
                        + gate.Requirements.Where( r => !r.IsMet ).Select( r => r.Description ).ToList().AsDelimited( " " ) );
                }

                var states = CaseMapper.GetPhaseStates( caseFile );
                var state = states.GetState( CasePhase.Postmortem );
                state.Progress = PhaseProgress.Complete;
                state.CompletedDateTime = RockDateTime.Now;
                CaseMapper.SetPhaseStates( caseFile, states );

                caseFile.Status = CaseStatus.Closed;
                caseFile.ClosedDateTime = RockDateTime.Now;
                caseFile.FollowUpDate = bag.FollowUpDate;
                StampModified( caseFile );
                detectiveContext.SaveChanges();

                return ActionOk( BuildBag( detectiveContext, caseFile ) );
            } );
        }

        [BlockAction]
        public BlockActionResult ReopenCase( string idKey )
        {
            return WithEditableCase( idKey, ( detectiveContext, caseFile ) =>
            {
                caseFile.Status = CaseStatus.Reopened;
                caseFile.ClosedDateTime = null;
                caseFile.FollowUpConfirmedDateTime = null;
                caseFile.CurrentPhase = CasePhase.FactList;
                StampModified( caseFile );
                detectiveContext.SaveChanges();

                return ActionOk( BuildBag( detectiveContext, caseFile ) );
            }, allowClosed: true );
        }

        [BlockAction]
        public BlockActionResult AbandonCase( string idKey )
        {
            return WithEditableCase( idKey, ( detectiveContext, caseFile ) =>
            {
                caseFile.Status = CaseStatus.Abandoned;
                StampModified( caseFile );
                detectiveContext.SaveChanges();

                return ActionOk( BuildBag( detectiveContext, caseFile ) );
            } );
        }

        [BlockAction]
        public BlockActionResult ResumeCase( string idKey )
        {
            return WithEditableCase( idKey, ( detectiveContext, caseFile ) =>
            {
                if ( caseFile.Status != CaseStatus.Abandoned )
                {
                    return ActionBadRequest( "Only an abandoned case can be resumed." );
                }

                caseFile.Status = caseFile.CurrentPhase >= CasePhase.FactList ? CaseStatus.Investigating : CaseStatus.Open;
                StampModified( caseFile );
                detectiveContext.SaveChanges();

                return ActionOk( BuildBag( detectiveContext, caseFile ) );
            }, allowClosed: true );
        }

        [BlockAction]
        public BlockActionResult ConfirmFollowUp( string idKey )
        {
            return WithEditableCase( idKey, ( detectiveContext, caseFile ) =>
            {
                caseFile.FollowUpConfirmedDateTime = RockDateTime.Now;
                StampModified( caseFile );
                detectiveContext.SaveChanges();

                return ActionOk( BuildBag( detectiveContext, caseFile ) );
            }, allowClosed: true );
        }

        #endregion

        #region Entries

        [BlockAction]
        public BlockActionResult SaveEntry( string idKey, CaseEntryBag bag )
        {
            return WithEditableCase( idKey, ( detectiveContext, caseFile ) =>
            {
                if ( bag == null || bag.Text.IsNullOrWhiteSpace() )
                {
                    return ActionBadRequest( "Entry text is required." );
                }

                var entryService = new CaseEntryService( detectiveContext );
                Model.CaseEntry entry;

                var entryId = Rock.Utility.IdHasher.Instance.GetId( bag.IdKey ?? string.Empty );

                if ( entryId.HasValue )
                {
                    entry = entryService.Get( entryId.Value );

                    if ( entry == null || entry.CaseFileId != caseFile.Id )
                    {
                        return ActionNotFound( "Entry not found." );
                    }
                }
                else
                {
                    entry = new Model.CaseEntry
                    {
                        CaseFileId = caseFile.Id,
                        CreatedDateTime = RockDateTime.Now,
                        CreatedByPersonAliasId = RequestContext.CurrentPerson?.PrimaryAliasId,
                        Order = detectiveContext.CaseEntries.Count( e => e.CaseFileId == caseFile.Id )
                    };
                    entryService.Add( entry );
                }

                entry.EntryType = ( CaseEntryType ) bag.EntryType;
                entry.Text = bag.Text;
                entry.EventDateTime = bag.EventDateTime;
                entry.TimelineEventType = bag.TimelineEventType.HasValue ? ( TimelineEventType? ) bag.TimelineEventType.Value : null;
                entry.Url = bag.Url;
                entry.Source = ( CaseEntrySource ) bag.Source;
                entry.IsCorroborated = bag.IsCorroborated;
                entry.ModifiedDateTime = RockDateTime.Now;
                entry.ModifiedByPersonAliasId = RequestContext.CurrentPerson?.PrimaryAliasId;

                var binaryFileGuid = bag.BinaryFileGuid.AsGuidOrNull();

                if ( binaryFileGuid.HasValue )
                {
                    using ( var rockContext = new Rock.Data.RockContext() )
                    {
                        var binaryFile = new Rock.Model.BinaryFileService( rockContext ).Get( binaryFileGuid.Value );

                        if ( binaryFile != null )
                        {
                            entry.BinaryFileId = binaryFile.Id;

                            if ( binaryFile.IsTemporary )
                            {
                                binaryFile.IsTemporary = false;
                                rockContext.SaveChanges();
                            }
                        }
                    }
                }
                else
                {
                    entry.BinaryFileId = null;
                }

                var sourceParticipantId = Rock.Utility.IdHasher.Instance.GetId( bag.SourceParticipantIdKey ?? string.Empty );
                entry.SourceParticipantId = sourceParticipantId;

                var sourceExperimentId = Rock.Utility.IdHasher.Instance.GetId( bag.SourceExperimentIdKey ?? string.Empty );
                entry.SourceExperimentId = sourceExperimentId;

                StampModified( caseFile );
                detectiveContext.SaveChanges();

                return ActionOk( BuildBag( detectiveContext, caseFile ) );
            } );
        }

        [BlockAction]
        public BlockActionResult DeleteEntry( string idKey, string entryIdKey )
        {
            return WithEditableCase( idKey, ( detectiveContext, caseFile ) =>
            {
                var entryService = new CaseEntryService( detectiveContext );
                var entryId = Rock.Utility.IdHasher.Instance.GetId( entryIdKey ?? string.Empty );
                var entry = entryId.HasValue ? entryService.Get( entryId.Value ) : null;

                if ( entry == null || entry.CaseFileId != caseFile.Id )
                {
                    return ActionNotFound( "Entry not found." );
                }

                entryService.Delete( entry );
                StampModified( caseFile );
                detectiveContext.SaveChanges();

                return ActionOk( BuildBag( detectiveContext, caseFile ) );
            } );
        }

        #endregion

        #region Hypotheses

        [BlockAction]
        public BlockActionResult SaveHypothesis( string idKey, HypothesisBag bag )
        {
            return WithEditableCase( idKey, ( detectiveContext, caseFile ) =>
            {
                if ( bag == null || bag.Name.IsNullOrWhiteSpace() )
                {
                    return ActionBadRequest( "A hypothesis name is required." );
                }

                var hypothesisService = new HypothesisService( detectiveContext );
                Model.Hypothesis hypothesis;

                var hypothesisId = Rock.Utility.IdHasher.Instance.GetId( bag.IdKey ?? string.Empty );

                if ( hypothesisId.HasValue )
                {
                    hypothesis = hypothesisService.Get( hypothesisId.Value );

                    if ( hypothesis == null || hypothesis.CaseFileId != caseFile.Id )
                    {
                        return ActionNotFound( "Hypothesis not found." );
                    }
                }
                else
                {
                    hypothesis = new Model.Hypothesis
                    {
                        CaseFileId = caseFile.Id,
                        CreatedDateTime = RockDateTime.Now,
                        CreatedByPersonAliasId = RequestContext.CurrentPerson?.PrimaryAliasId
                    };
                    hypothesisService.Add( hypothesis );
                }

                hypothesis.Name = bag.Name.Truncate( 300 );
                hypothesis.Description = bag.Description;
                hypothesis.Probability = Math.Max( 0, Math.Min( 100, bag.Probability ) );
                hypothesis.Status = ( HypothesisStatus ) bag.Status;
                hypothesis.RuledOutReason = bag.RuledOutReason;
                hypothesis.ParentHypothesisId = Rock.Utility.IdHasher.Instance.GetId( bag.ParentHypothesisIdKey ?? string.Empty );
                hypothesis.ModifiedDateTime = RockDateTime.Now;
                hypothesis.ModifiedByPersonAliasId = RequestContext.CurrentPerson?.PrimaryAliasId;

                StampModified( caseFile );
                detectiveContext.SaveChanges();

                return ActionOk( BuildBag( detectiveContext, caseFile ) );
            } );
        }

        [BlockAction]
        public BlockActionResult DeleteHypothesis( string idKey, string hypothesisIdKey )
        {
            return WithEditableCase( idKey, ( detectiveContext, caseFile ) =>
            {
                var hypothesisService = new HypothesisService( detectiveContext );
                var hypothesisId = Rock.Utility.IdHasher.Instance.GetId( hypothesisIdKey ?? string.Empty );
                var hypothesis = hypothesisId.HasValue ? hypothesisService.Get( hypothesisId.Value ) : null;

                if ( hypothesis == null || hypothesis.CaseFileId != caseFile.Id )
                {
                    return ActionNotFound( "Hypothesis not found." );
                }

                if ( detectiveContext.Hypotheses.Any( h => h.ParentHypothesisId == hypothesis.Id ) )
                {
                    return ActionBadRequest( "Delete or reassign this hypothesis's child nodes first." );
                }

                if ( detectiveContext.Experiments.Any( e => e.HypothesisId == hypothesis.Id ) )
                {
                    return ActionBadRequest( "One or more experiments reference this hypothesis. Remove the link on those experiments first." );
                }

                hypothesisService.Delete( hypothesis );
                StampModified( caseFile );
                detectiveContext.SaveChanges();

                return ActionOk( BuildBag( detectiveContext, caseFile ) );
            } );
        }

        #endregion

        #region Experiments

        [BlockAction]
        public BlockActionResult SaveExperiment( string idKey, ExperimentBag bag )
        {
            return WithEditableCase( idKey, ( detectiveContext, caseFile ) =>
            {
                if ( bag == null || ( bag.HypothesisIdKey.IsNullOrWhiteSpace() && bag.HypothesisText.IsNullOrWhiteSpace() ) )
                {
                    return ActionBadRequest( "An experiment must reference a hypothesis or describe one." );
                }

                var experimentService = new ExperimentService( detectiveContext );
                Model.Experiment experiment;

                var experimentId = Rock.Utility.IdHasher.Instance.GetId( bag.IdKey ?? string.Empty );

                if ( experimentId.HasValue )
                {
                    experiment = experimentService.Get( experimentId.Value );

                    if ( experiment == null || experiment.CaseFileId != caseFile.Id )
                    {
                        return ActionNotFound( "Experiment not found." );
                    }
                }
                else
                {
                    experiment = new Model.Experiment
                    {
                        CaseFileId = caseFile.Id,
                        CreatedDateTime = RockDateTime.Now,
                        CreatedByPersonAliasId = RequestContext.CurrentPerson?.PrimaryAliasId
                    };
                    experimentService.Add( experiment );
                }

                experiment.HypothesisId = Rock.Utility.IdHasher.Instance.GetId( bag.HypothesisIdKey ?? string.Empty );
                experiment.HypothesisText = bag.HypothesisText.Truncate( 300 );
                experiment.TestPlan = bag.TestPlan;
                experiment.ExpectedResult = bag.ExpectedResult;
                experiment.ActualResult = bag.ActualResult;
                experiment.Outcome = ( ExperimentOutcome ) bag.Outcome;
                experiment.ChangesMade = bag.ChangesMade;
                experiment.ChangesReverted = bag.ChangesReverted;
                experiment.RevertJustification = bag.RevertJustification;
                experiment.Learnings = bag.Learnings;
                experiment.ModifiedDateTime = RockDateTime.Now;
                experiment.ModifiedByPersonAliasId = RequestContext.CurrentPerson?.PrimaryAliasId;

                detectiveContext.SaveChanges();

                if ( experiment.Learnings.IsNotNullOrWhiteSpace() && experiment.Outcome != ExperimentOutcome.Pending )
                {
                    var hasLearningsFact = detectiveContext.CaseEntries.Any( e =>
                        e.CaseFileId == caseFile.Id
                        && e.SourceExperimentId == experiment.Id
                        && e.EntryType == CaseEntryType.Fact );

                    if ( !hasLearningsFact )
                    {
                        new CaseEntryService( detectiveContext ).Add( new Model.CaseEntry
                        {
                            CaseFileId = caseFile.Id,
                            EntryType = CaseEntryType.Fact,
                            Text = experiment.Learnings,
                            Source = CaseEntrySource.Experiment,
                            SourceExperimentId = experiment.Id,
                            IsCorroborated = true,
                            Order = detectiveContext.CaseEntries.Count( e => e.CaseFileId == caseFile.Id ),
                            CreatedDateTime = RockDateTime.Now,
                            CreatedByPersonAliasId = RequestContext.CurrentPerson?.PrimaryAliasId
                        } );
                    }
                }

                StampModified( caseFile );
                detectiveContext.SaveChanges();

                return ActionOk( BuildBag( detectiveContext, caseFile ) );
            } );
        }

        [BlockAction]
        public BlockActionResult DeleteExperiment( string idKey, string experimentIdKey )
        {
            return WithEditableCase( idKey, ( detectiveContext, caseFile ) =>
            {
                var experimentService = new ExperimentService( detectiveContext );
                var experimentId = Rock.Utility.IdHasher.Instance.GetId( experimentIdKey ?? string.Empty );
                var experiment = experimentId.HasValue ? experimentService.Get( experimentId.Value ) : null;

                if ( experiment == null || experiment.CaseFileId != caseFile.Id )
                {
                    return ActionNotFound( "Experiment not found." );
                }

                var linkedEntries = detectiveContext.CaseEntries.Where( e => e.SourceExperimentId == experiment.Id ).ToList();

                foreach ( var entry in linkedEntries )
                {
                    entry.SourceExperimentId = null;
                }

                experimentService.Delete( experiment );
                StampModified( caseFile );
                detectiveContext.SaveChanges();

                return ActionOk( BuildBag( detectiveContext, caseFile ) );
            } );
        }

        #endregion

        #region Participants and witnesses

        [BlockAction]
        public BlockActionResult SaveParticipant( string idKey, ParticipantBag bag )
        {
            return WithEditableCase( idKey, ( detectiveContext, caseFile ) =>
            {
                if ( bag == null )
                {
                    return ActionBadRequest();
                }

                var personAliasGuid = bag.PersonAliasGuid.AsGuidOrNull();
                int? personAliasId = null;

                if ( personAliasGuid.HasValue )
                {
                    using ( var rockContext = new Rock.Data.RockContext() )
                    {
                        personAliasId = new Rock.Model.PersonAliasService( rockContext ).GetId( personAliasGuid.Value );
                    }
                }

                if ( !personAliasId.HasValue && bag.DisplayName.IsNullOrWhiteSpace() )
                {
                    return ActionBadRequest( "A participant must be a Rock person or have a name." );
                }

                var participantService = new CaseParticipantService( detectiveContext );
                Model.CaseParticipant participant;

                var participantId = Rock.Utility.IdHasher.Instance.GetId( bag.IdKey ?? string.Empty );

                if ( participantId.HasValue )
                {
                    participant = participantService.Get( participantId.Value );

                    if ( participant == null || participant.CaseFileId != caseFile.Id )
                    {
                        return ActionNotFound( "Participant not found." );
                    }
                }
                else
                {
                    participant = new Model.CaseParticipant
                    {
                        CaseFileId = caseFile.Id,
                        CreatedDateTime = RockDateTime.Now,
                        CreatedByPersonAliasId = RequestContext.CurrentPerson?.PrimaryAliasId
                    };
                    participantService.Add( participant );
                }

                var role = ( ParticipantRole ) bag.Role;

                if ( role == ParticipantRole.Champion )
                {
                    var existingChampion = detectiveContext.CaseParticipants.Any( p =>
                        p.CaseFileId == caseFile.Id && p.Role == ParticipantRole.Champion && p.Id != participant.Id );

                    if ( existingChampion )
                    {
                        return ActionBadRequest( "This case already has a Champion." );
                    }
                }

                participant.PersonAliasId = personAliasId;
                participant.ExternalName = personAliasId.HasValue ? null : bag.DisplayName.Truncate( 200 );
                participant.ExternalContact = personAliasId.HasValue ? null : bag.ExternalContact.Truncate( 400 );
                participant.Role = role;
                participant.ModifiedDateTime = RockDateTime.Now;
                participant.ModifiedByPersonAliasId = RequestContext.CurrentPerson?.PrimaryAliasId;

                StampModified( caseFile );
                detectiveContext.SaveChanges();

                return ActionOk( BuildBag( detectiveContext, caseFile ) );
            } );
        }

        [BlockAction]
        public BlockActionResult DeleteParticipant( string idKey, string participantIdKey )
        {
            return WithEditableCase( idKey, ( detectiveContext, caseFile ) =>
            {
                var participantService = new CaseParticipantService( detectiveContext );
                var participantId = Rock.Utility.IdHasher.Instance.GetId( participantIdKey ?? string.Empty );
                var participant = participantId.HasValue ? participantService.Get( participantId.Value ) : null;

                if ( participant == null || participant.CaseFileId != caseFile.Id )
                {
                    return ActionNotFound( "Participant not found." );
                }

                var linkedEntries = detectiveContext.CaseEntries.Where( e => e.SourceParticipantId == participant.Id ).ToList();

                foreach ( var entry in linkedEntries )
                {
                    entry.SourceParticipantId = null;
                }

                var statements = detectiveContext.CaseStatements.Where( s => s.ParticipantId == participant.Id );
                detectiveContext.CaseStatements.RemoveRange( statements );

                participantService.Delete( participant );
                StampModified( caseFile );
                detectiveContext.SaveChanges();

                return ActionOk( BuildBag( detectiveContext, caseFile ) );
            } );
        }

        [BlockAction]
        public BlockActionResult CreateStatementRequest( string idKey, string participantIdKey, string requestPrompt )
        {
            return WithEditableCase( idKey, ( detectiveContext, caseFile ) =>
            {
                var participantService = new CaseParticipantService( detectiveContext );
                var participantId = Rock.Utility.IdHasher.Instance.GetId( participantIdKey ?? string.Empty );
                var participant = participantId.HasValue ? participantService.Get( participantId.Value ) : null;

                if ( participant == null || participant.CaseFileId != caseFile.Id )
                {
                    return ActionNotFound( "Participant not found." );
                }

                if ( requestPrompt.IsNullOrWhiteSpace() )
                {
                    return ActionBadRequest( "A request prompt is required." );
                }

                participant.Role = ParticipantRole.Witness;

                var statement = new Model.CaseStatement
                {
                    CaseFileId = caseFile.Id,
                    ParticipantId = participant.Id,
                    RequestPrompt = requestPrompt,
                    RequestDateTime = RockDateTime.Now,
                    RequestStatus = StatementRequestStatus.Draft,
                    CreatedDateTime = RockDateTime.Now,
                    CreatedByPersonAliasId = RequestContext.CurrentPerson?.PrimaryAliasId
                };

                new CaseStatementService( detectiveContext ).Add( statement );
                detectiveContext.SaveChanges();

                var isRockPerson = participant.PersonAliasId.HasValue;
                var sendAutomatically = GetAttributeValue( AttributeKey.SendRequestsAutomatically ).AsBoolean();

                if ( isRockPerson && sendAutomatically )
                {
                    WitnessReminderHelper.AddOrRefreshReminder( statement.Id, participant.PersonAliasId.Value, statement.RequestPrompt );

                    var sendError = SendStatementRequestCommunication( caseFile, participant, statement, SystemGuid.SystemCommunication.WITNESS_STATEMENT_REQUEST );

                    if ( sendError == null )
                    {
                        statement.RequestStatus = StatementRequestStatus.Requested;
                        statement.SentDateTime = RockDateTime.Now;
                        detectiveContext.SaveChanges();
                    }
                }

                StampModified( caseFile );
                detectiveContext.SaveChanges();

                return ActionOk( BuildBag( detectiveContext, caseFile ) );
            } );
        }

        [BlockAction]
        public BlockActionResult SendStatementRequest( string idKey, string statementIdKey )
        {
            return WithEditableCase( idKey, ( detectiveContext, caseFile ) =>
            {
                var statementService = new CaseStatementService( detectiveContext );
                var statementId = Rock.Utility.IdHasher.Instance.GetId( statementIdKey ?? string.Empty );
                var statement = statementId.HasValue ? statementService.Get( statementId.Value ) : null;

                if ( statement == null || statement.CaseFileId != caseFile.Id )
                {
                    return ActionNotFound( "Statement request not found." );
                }

                var participant = new CaseParticipantService( detectiveContext ).Get( statement.ParticipantId );

                if ( participant == null || !participant.PersonAliasId.HasValue )
                {
                    return ActionBadRequest( "External witnesses cannot receive communications. Record their statement manually." );
                }

                if ( statement.RequestPrompt.IsNullOrWhiteSpace() )
                {
                    return ActionBadRequest( "Create the statement request first." );
                }

                var isReminder = statement.RequestStatus == StatementRequestStatus.Requested || statement.RequestStatus == StatementRequestStatus.Reminded;
                var communicationGuid = isReminder
                    ? SystemGuid.SystemCommunication.WITNESS_STATEMENT_REMINDER
                    : SystemGuid.SystemCommunication.WITNESS_STATEMENT_REQUEST;

                WitnessReminderHelper.AddOrRefreshReminder( statement.Id, participant.PersonAliasId.Value, statement.RequestPrompt );

                var sendError = SendStatementRequestCommunication( caseFile, participant, statement, communicationGuid );

                if ( sendError != null )
                {
                    return ActionBadRequest( sendError + " The witness was still given an in-Rock reminder." );
                }

                statement.RequestStatus = isReminder ? StatementRequestStatus.Reminded : StatementRequestStatus.Requested;
                statement.SentDateTime = RockDateTime.Now;
                statement.ModifiedDateTime = RockDateTime.Now;
                StampModified( caseFile );
                detectiveContext.SaveChanges();

                return ActionOk( BuildBag( detectiveContext, caseFile ) );
            } );
        }

        private string SendStatementRequestCommunication( Model.CaseFile caseFile, Model.CaseParticipant participant, Model.CaseStatement statement, string systemCommunicationGuid )
        {
            using ( var rockContext = new Rock.Data.RockContext() )
            {
                var personAlias = new Rock.Model.PersonAliasService( rockContext ).Get( participant.PersonAliasId.Value );
                var person = personAlias?.Person;

                if ( person == null )
                {
                    return "Witness person record not found.";
                }

                if ( person.Email.IsNullOrWhiteSpace() )
                {
                    return "The witness has no email address on record.";
                }

                var systemCommunication = new Rock.Model.SystemCommunicationService( rockContext ).Get( systemCommunicationGuid.AsGuid() );

                if ( systemCommunication == null )
                {
                    return "The witness request system communication is missing.";
                }

                var statementUrl = GetWitnessStatementUrl( statement );

                var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( null, person );
                mergeFields.Add( "Person", person );
                mergeFields.Add( "CaseNumber", caseFile.Id.ToString() );
                mergeFields.Add( "CaseTitle", caseFile.Title );
                mergeFields.Add( "RequestPrompt", statement.RequestPrompt );
                mergeFields.Add( "RequestorName", RequestContext.CurrentPerson?.FullName ?? "A case owner" );
                mergeFields.Add( "StatementLink", statementUrl );

                var message = new RockEmailMessage( systemCommunication );
                message.AddRecipient( new RockEmailMessageRecipient( person, mergeFields ) );
                message.AppRoot = "/";
                message.CreateCommunicationRecord = true;

                if ( !message.Send( out var errors ) && errors.Any() )
                {
                    return errors.ToList().AsDelimited( " " );
                }

                return null;
            }
        }

        private string GetWitnessStatementUrl( Model.CaseStatement statement )
        {
            var relativeUrl = this.GetLinkedPageUrl( AttributeKey.WitnessStatementPage, new Dictionary<string, string>
            {
                { PageParameterKey.Statement, statement.IdKey }
            } );

            var root = GlobalAttributesCache.Get().GetValue( "InternalApplicationRoot" )?.TrimEnd( '/' ) ?? string.Empty;

            return root + relativeUrl;
        }

        [BlockAction]
        public BlockActionResult RecordStatement( string idKey, string participantIdKey, string statement, string binaryFileGuid, string url, bool addToFacts, bool isCorroborated )
        {
            return WithEditableCase( idKey, ( detectiveContext, caseFile ) =>
            {
                var participantService = new CaseParticipantService( detectiveContext );
                var participantId = Rock.Utility.IdHasher.Instance.GetId( participantIdKey ?? string.Empty );
                var participant = participantId.HasValue ? participantService.Get( participantId.Value ) : null;

                if ( participant == null || participant.CaseFileId != caseFile.Id )
                {
                    return ActionNotFound( "Participant not found." );
                }

                if ( statement.IsNullOrWhiteSpace() )
                {
                    return ActionBadRequest( "A statement is required." );
                }

                participant.Role = ParticipantRole.Witness;

                var statementEntity = new Model.CaseStatement
                {
                    CaseFileId = caseFile.Id,
                    ParticipantId = participant.Id,
                    Statement = statement,
                    Url = url.Truncate( 2000 ),
                    RequestStatus = StatementRequestStatus.ManuallyRecorded,
                    SubmittedDateTime = RockDateTime.Now,
                    CreatedDateTime = RockDateTime.Now,
                    CreatedByPersonAliasId = RequestContext.CurrentPerson?.PrimaryAliasId
                };

                statementEntity.BinaryFileId = ResolveBinaryFileId( binaryFileGuid );

                new CaseStatementService( detectiveContext ).Add( statementEntity );
                detectiveContext.SaveChanges();

                if ( addToFacts )
                {
                    AddStatementFact( detectiveContext, caseFile, statementEntity, statement, isCorroborated );
                }

                StampModified( caseFile );
                detectiveContext.SaveChanges();

                return ActionOk( BuildBag( detectiveContext, caseFile ) );
            } );
        }

        [BlockAction]
        public BlockActionResult RecordStatementResponse( string idKey, string statementIdKey, string statement, string binaryFileGuid, string url, bool addToFacts, bool isCorroborated )
        {
            return WithEditableCase( idKey, ( detectiveContext, caseFile ) =>
            {
                var statementService = new CaseStatementService( detectiveContext );
                var statementId = Rock.Utility.IdHasher.Instance.GetId( statementIdKey ?? string.Empty );
                var statementEntity = statementId.HasValue ? statementService.Get( statementId.Value ) : null;

                if ( statementEntity == null || statementEntity.CaseFileId != caseFile.Id )
                {
                    return ActionNotFound( "Statement request not found." );
                }

                var isPending = statementEntity.RequestStatus == StatementRequestStatus.Draft
                    || statementEntity.RequestStatus == StatementRequestStatus.Requested
                    || statementEntity.RequestStatus == StatementRequestStatus.Reminded;

                if ( !isPending )
                {
                    return ActionBadRequest( "This statement request has already been answered." );
                }

                if ( statement.IsNullOrWhiteSpace() )
                {
                    return ActionBadRequest( "A statement is required." );
                }

                statementEntity.Statement = statement;
                statementEntity.Url = url.Truncate( 2000 );
                statementEntity.BinaryFileId = ResolveBinaryFileId( binaryFileGuid );
                statementEntity.RequestStatus = StatementRequestStatus.ManuallyRecorded;
                statementEntity.SubmittedDateTime = RockDateTime.Now;
                statementEntity.ModifiedDateTime = RockDateTime.Now;
                statementEntity.ModifiedByPersonAliasId = RequestContext.CurrentPerson?.PrimaryAliasId;
                detectiveContext.SaveChanges();

                if ( addToFacts )
                {
                    AddStatementFact( detectiveContext, caseFile, statementEntity, statement, isCorroborated );
                }

                StampModified( caseFile );
                detectiveContext.SaveChanges();

                var participant = new CaseParticipantService( detectiveContext ).Get( statementEntity.ParticipantId );

                if ( participant?.PersonAliasId != null )
                {
                    WitnessReminderHelper.CompleteReminders( statementEntity.Id, participant.PersonAliasId.Value );
                }

                return ActionOk( BuildBag( detectiveContext, caseFile ) );
            } );
        }

        [BlockAction]
        public BlockActionResult StatementToFact( string idKey, string statementIdKey, string factText, bool isCorroborated )
        {
            return WithEditableCase( idKey, ( detectiveContext, caseFile ) =>
            {
                var statementService = new CaseStatementService( detectiveContext );
                var statementId = Rock.Utility.IdHasher.Instance.GetId( statementIdKey ?? string.Empty );
                var statement = statementId.HasValue ? statementService.Get( statementId.Value ) : null;

                if ( statement == null || statement.CaseFileId != caseFile.Id )
                {
                    return ActionNotFound( "Statement not found." );
                }

                if ( factText.IsNullOrWhiteSpace() )
                {
                    return ActionBadRequest( "Fact text is required." );
                }

                AddStatementFact( detectiveContext, caseFile, statement, factText, isCorroborated );

                StampModified( caseFile );
                detectiveContext.SaveChanges();

                return ActionOk( BuildBag( detectiveContext, caseFile ) );
            } );
        }

        [BlockAction]
        public BlockActionResult DeleteStatement( string idKey, string statementIdKey )
        {
            return WithEditableCase( idKey, ( detectiveContext, caseFile ) =>
            {
                var statementService = new CaseStatementService( detectiveContext );
                var statementId = Rock.Utility.IdHasher.Instance.GetId( statementIdKey ?? string.Empty );
                var statement = statementId.HasValue ? statementService.Get( statementId.Value ) : null;

                if ( statement == null || statement.CaseFileId != caseFile.Id )
                {
                    return ActionNotFound( "Statement not found." );
                }

                var deletedStatementId = statement.Id;
                var participant = new CaseParticipantService( detectiveContext ).Get( statement.ParticipantId );

                statementService.Delete( statement );
                StampModified( caseFile );
                detectiveContext.SaveChanges();

                if ( participant?.PersonAliasId != null )
                {
                    WitnessReminderHelper.CompleteReminders( deletedStatementId, participant.PersonAliasId.Value );
                }

                return ActionOk( BuildBag( detectiveContext, caseFile ) );
            } );
        }

        private int? ResolveBinaryFileId( string binaryFileGuid )
        {
            var fileGuid = binaryFileGuid.AsGuidOrNull();

            if ( !fileGuid.HasValue )
            {
                return null;
            }

            using ( var rockContext = new Rock.Data.RockContext() )
            {
                var binaryFile = new Rock.Model.BinaryFileService( rockContext ).Get( fileGuid.Value );

                if ( binaryFile == null )
                {
                    return null;
                }

                if ( binaryFile.IsTemporary )
                {
                    binaryFile.IsTemporary = false;
                    rockContext.SaveChanges();
                }

                return binaryFile.Id;
            }
        }

        private void AddStatementFact( RockDetectiveContext detectiveContext, Model.CaseFile caseFile, Model.CaseStatement statement, string factText, bool isCorroborated )
        {
            new CaseEntryService( detectiveContext ).Add( new Model.CaseEntry
            {
                CaseFileId = caseFile.Id,
                EntryType = CaseEntryType.Fact,
                Text = factText,
                Source = CaseEntrySource.Witness,
                SourceParticipantId = statement.ParticipantId,
                IsCorroborated = isCorroborated,
                Order = detectiveContext.CaseEntries.Count( e => e.CaseFileId == caseFile.Id ),
                CreatedDateTime = RockDateTime.Now,
                CreatedByPersonAliasId = RequestContext.CurrentPerson?.PrimaryAliasId
            } );
        }

        #endregion

        #region PDF

        [BlockAction]
        public BlockActionResult ExportPdf( string idKey )
        {
            var currentPerson = RequestContext.CurrentPerson;

            using ( var detectiveContext = new RockDetectiveContext() )
            {
                var caseFile = GetCaseByKey( detectiveContext, idKey );

                if ( caseFile == null )
                {
                    return ActionNotFound( "Case file not found." );
                }

                if ( !caseFile.IsAuthorized( Rock.Security.Authorization.VIEW, currentPerson ) )
                {
                    return ActionForbidden( "You are not authorized to view this case." );
                }

                var entities = LoadCaseEntities( detectiveContext, caseFile.Id );
                var bag = CaseMapper.ToCaseFileBag( caseFile, entities.Entries, entities.Hypotheses, entities.Experiments, entities.Participants );
                var html = Pdf.CaseFileReport.BuildHtml( bag );

                using ( var pdfGenerator = new Rock.Pdf.PdfGenerator() )
                using ( var pdfStream = pdfGenerator.GetPDFDocumentFromHtml( html ) )
                using ( var memoryStream = new System.IO.MemoryStream() )
                {
                    pdfStream.CopyTo( memoryStream );

                    return ActionOk( new PdfResultBag
                    {
                        FileName = $"Case-{caseFile.Id}.pdf",
                        Base64 = Convert.ToBase64String( memoryStream.ToArray() )
                    } );
                }
            }
        }

        #endregion
    }
}
