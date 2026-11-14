using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

using Rock;
using Rock.Utility;
using Rock.Web.Cache;

using com.yesuchum.RockDetective.Data;
using com.yesuchum.RockDetective.Enums;
using com.yesuchum.RockDetective.Method;

namespace com.yesuchum.RockDetective.Mcp
{
    public static class CaseWorkflowService
    {
        private const string CaseBoardBlockGuid = "BE158897-103D-4F32-A2CC-F8A0D8EFA440";

        #region Lookups

        public static object ListCategories( Rock.Model.Person person )
        {
            CaseAccess.RequireInvestigator( person );

            return GetCategories()
                .Select( c => new { c.Name, Guid = c.Guid.ToString(), c.Description, HasOwnTerminology = c.GetAttributeValue( MethodProfile.CategoryAttributeKey ).IsNotNullOrWhiteSpace() } )
                .ToList();
        }

        private static List<CategoryCache> GetCategories()
        {
            var entityTypeId = EntityTypeCache.Get( SystemGuid.EntityType.CASE_FILE.AsGuid() )?.Id;

            if ( !entityTypeId.HasValue )
            {
                return new List<CategoryCache>();
            }

            using ( var rockContext = new Rock.Data.RockContext() )
            {
                var ids = new Rock.Model.CategoryService( rockContext )
                    .Queryable()
                    .Where( c => c.EntityTypeId == entityTypeId.Value )
                    .OrderBy( c => c.Order )
                    .ThenBy( c => c.Name )
                    .Select( c => c.Id )
                    .ToList();

                return ids.Select( id => CategoryCache.Get( id ) ).Where( c => c != null ).ToList();
            }
        }

        private static int ResolveCategoryId( string category )
        {
            if ( category.IsNullOrWhiteSpace() )
            {
                throw new McpToolException( "A category is required. Use the list categories tool to see the choices." );
            }

            var categories = GetCategories();
            var guid = category.AsGuidOrNull();

            var match = guid.HasValue
                ? categories.FirstOrDefault( c => c.Guid == guid.Value )
                : categories.FirstOrDefault( c => c.Name.Equals( category.Trim(), StringComparison.OrdinalIgnoreCase ) );

            if ( match == null )
            {
                throw new McpToolException( $"'{category}' is not a case category. Choices: {categories.Select( c => c.Name ).ToList().AsDelimited( ", " )}." );
            }

            return match.Id;
        }

        private static GateRequirementOptions GetGateOptions()
        {
            var defaults = new GateRequirementOptions();
            var block = BlockCache.Get( CaseBoardBlockGuid.AsGuid() );

            if ( block == null )
            {
                return defaults;
            }

            return new GateRequirementOptions
            {
                AdvancedMinimumFacts = block.GetAttributeValue( "AdvancedMinimumFacts" ).AsIntegerOrNull() ?? defaults.AdvancedMinimumFacts,
                SimpleMinimumFacts = block.GetAttributeValue( "SimpleMinimumFacts" ).AsIntegerOrNull() ?? defaults.SimpleMinimumFacts,
                AdvancedMinimumRootHypotheses = block.GetAttributeValue( "AdvancedMinimumHypotheses" ).AsIntegerOrNull() ?? defaults.AdvancedMinimumRootHypotheses,
                SimpleMinimumRootHypotheses = block.GetAttributeValue( "SimpleMinimumHypotheses" ).AsIntegerOrNull() ?? defaults.SimpleMinimumRootHypotheses,
                MinimumTimelineEvents = block.GetAttributeValue( "MinimumTimelineEvents" ).AsIntegerOrNull() ?? defaults.MinimumTimelineEvents,
                MinimumCompletedExperiments = block.GetAttributeValue( "MinimumExperiments" ).AsIntegerOrNull() ?? defaults.MinimumCompletedExperiments,
                IsAttestationRequired = block.GetAttributeValue( "RequireAttestations" ).AsBooleanOrNull() ?? defaults.IsAttestationRequired
            };
        }

        #endregion

        #region Case

        public static object CreateCase( string title, string problemStatement, string category, string mode, bool isProblemStatementVerified, Rock.Model.Person person )
        {
            RequirePerson( person );
            CaseAccess.RequireInvestigator( person );

            if ( title.IsNullOrWhiteSpace() || problemStatement.IsNullOrWhiteSpace() )
            {
                throw new McpToolException( "A title and a problem statement are required." );
            }

            RequireTitleLength( title );

            var categoryId = ResolveCategoryId( category );
            var caseMode = ParseEnum( mode, CaseMode.Simple, "mode" );

            using ( var detectiveContext = new RockDetectiveContext() )
            {
                var caseFile = new Model.CaseFile
                {
                    Title = title.Trim().Truncate( 200 ),
                    ProblemStatement = problemStatement.Trim(),
                    CategoryId = categoryId,
                    Mode = caseMode,
                    Classification = caseMode == CaseMode.Advanced ? CaseClassification.Difficult : CaseClassification.Easy,
                    Status = CaseStatus.Open,
                    CurrentPhase = caseMode == CaseMode.Advanced ? CasePhase.DefineTeam : CasePhase.FactList,
                    OwnerPersonAliasId = person.PrimaryAliasId.Value,
                    CreatedDateTime = RockDateTime.Now,
                    CreatedByPersonAliasId = person.PrimaryAliasId
                };

                var states = new CasePhaseStates();

                var defineProblemState = states.GetState( CasePhase.DefineProblem );
                defineProblemState.Progress = PhaseProgress.Complete;
                defineProblemState.CompletedDateTime = RockDateTime.Now;
                defineProblemState.Data[PhaseStateKey.AttestNoUnverifiedDetails] = isProblemStatementVerified.ToString();
                defineProblemState.Data[PhaseStateKey.AttestNothingNotGermane] = isProblemStatementVerified.ToString();

                var classifyState = states.GetState( CasePhase.Classify );
                classifyState.Progress = PhaseProgress.Complete;
                classifyState.CompletedDateTime = RockDateTime.Now;

                CaseMapper.SetPhaseStates( caseFile, states );

                new CaseFileService( detectiveContext ).Add( caseFile );
                detectiveContext.SaveChanges();

                return BuildProgress( detectiveContext, caseFile, null, null );
            }
        }

        public static object UpdateCase( string caseIdKey, string title, string problemStatement, string category, bool? isProblemStatementVerified, Rock.Model.Person person )
        {
            return WithEditableCase( caseIdKey, person, ( detectiveContext, caseFile ) =>
            {
                if ( title.IsNotNullOrWhiteSpace() )
                {
                    RequireTitleLength( title );
                    caseFile.Title = title.Trim().Truncate( 200 );
                }

                if ( problemStatement.IsNotNullOrWhiteSpace() )
                {
                    caseFile.ProblemStatement = problemStatement.Trim();
                }

                if ( category.IsNotNullOrWhiteSpace() )
                {
                    caseFile.CategoryId = ResolveCategoryId( category );
                }

                if ( isProblemStatementVerified.HasValue )
                {
                    var states = CaseMapper.GetPhaseStates( caseFile );
                    var state = states.GetState( CasePhase.DefineProblem );
                    state.Data[PhaseStateKey.AttestNoUnverifiedDetails] = isProblemStatementVerified.Value.ToString();
                    state.Data[PhaseStateKey.AttestNothingNotGermane] = isProblemStatementVerified.Value.ToString();
                    CaseMapper.SetPhaseStates( caseFile, states );
                }

                return null;
            } );
        }

        #endregion

        #region Team and statements

        public static object AddParticipant( string caseIdKey, string role, string personAliasGuid, string externalName, string externalContact, Rock.Model.Person person )
        {
            return WithEditableCase( caseIdKey, person, ( detectiveContext, caseFile ) =>
            {
                var participantRole = ParseEnum( role, ParticipantRole.Member, "role" );

                if ( participantRole == ParticipantRole.Owner )
                {
                    throw new McpToolException( "The owner is whoever opened the case and cannot be added as a participant." );
                }

                int? personAliasId = null;
                var aliasGuid = personAliasGuid.AsGuidOrNull();

                if ( aliasGuid.HasValue )
                {
                    using ( var rockContext = new Rock.Data.RockContext() )
                    {
                        personAliasId = new Rock.Model.PersonAliasService( rockContext ).GetId( aliasGuid.Value );
                    }

                    if ( !personAliasId.HasValue )
                    {
                        throw new McpToolException( "No person was found for that person alias guid." );
                    }
                }
                else if ( externalName.IsNullOrWhiteSpace() )
                {
                    throw new McpToolException( "Give either a personAliasGuid for a Rock person or an externalName for someone outside Rock." );
                }

                if ( participantRole == ParticipantRole.Champion && detectiveContext.CaseParticipants.Any( p => p.CaseFileId == caseFile.Id && p.Role == ParticipantRole.Champion ) )
                {
                    throw new McpToolException( "This case already has a Champion." );
                }

                var participant = new Model.CaseParticipant
                {
                    CaseFileId = caseFile.Id,
                    PersonAliasId = personAliasId,
                    ExternalName = personAliasId.HasValue ? null : externalName.Trim().Truncate( 200 ),
                    ExternalContact = personAliasId.HasValue ? null : externalContact.Truncate( 400 ),
                    Role = participantRole,
                    CreatedDateTime = RockDateTime.Now,
                    CreatedByPersonAliasId = person.PrimaryAliasId
                };

                new CaseParticipantService( detectiveContext ).Add( participant );
                detectiveContext.SaveChanges();

                return new CreatedItem( "participantIdKey", participant.IdKey );
            } );
        }

        public static object WaiveChampion( string caseIdKey, string reason, Rock.Model.Person person )
        {
            return WithEditableCase( caseIdKey, person, ( detectiveContext, caseFile ) =>
            {
                if ( reason.IsNullOrWhiteSpace() )
                {
                    throw new McpToolException( "A reason is required to waive the Champion." );
                }

                if ( detectiveContext.CaseParticipants.Any( p => p.CaseFileId == caseFile.Id && p.Role == ParticipantRole.Champion ) )
                {
                    throw new McpToolException( "This case already has a Champion, so waiving would change nothing. The Champion signs off at the reevaluate phase; when they are outside Rock, the case owner records their sign-off." );
                }

                var states = CaseMapper.GetPhaseStates( caseFile );
                var state = states.GetState( CasePhase.DefineTeam );
                state.Data[PhaseStateKey.ChampionWaived] = true.ToString();
                state.Data[PhaseStateKey.ChampionWaiveReason] = reason.Trim();
                CaseMapper.SetPhaseStates( caseFile, states );

                return null;
            } );
        }

        public static object RecordStatement( string caseIdKey, string participantIdKey, string statement, string url, bool addToFacts, bool isCorroborated, Rock.Model.Person person )
        {
            return WithEditableCase( caseIdKey, person, ( detectiveContext, caseFile ) =>
            {
                if ( statement.IsNullOrWhiteSpace() )
                {
                    throw new McpToolException( "The statement text is required." );
                }

                var participantId = IdHasher.Instance.GetId( participantIdKey ?? string.Empty );
                var participant = participantId.HasValue ? new CaseParticipantService( detectiveContext ).Get( participantId.Value ) : null;

                if ( participant == null || participant.CaseFileId != caseFile.Id )
                {
                    throw new McpToolException( "That participant was not found on this case." );
                }

                RequireSafeUrl( url );

                var caseStatement = new Model.CaseStatement
                {
                    CaseFileId = caseFile.Id,
                    ParticipantId = participant.Id,
                    RequestStatus = StatementRequestStatus.ManuallyRecorded,
                    Statement = statement.Trim(),
                    Url = url,
                    SubmittedDateTime = RockDateTime.Now,
                    CreatedDateTime = RockDateTime.Now,
                    CreatedByPersonAliasId = person.PrimaryAliasId
                };

                new CaseStatementService( detectiveContext ).Add( caseStatement );

                if ( addToFacts )
                {
                    new CaseEntryService( detectiveContext ).Add( new Model.CaseEntry
                    {
                        CaseFileId = caseFile.Id,
                        EntryType = CaseEntryType.Fact,
                        Text = statement.Trim(),
                        Url = url,
                        Source = CaseEntrySource.Witness,
                        SourceParticipantId = participant.Id,
                        IsCorroborated = isCorroborated,
                        Order = detectiveContext.CaseEntries.Count( e => e.CaseFileId == caseFile.Id ),
                        CreatedDateTime = RockDateTime.Now,
                        CreatedByPersonAliasId = person.PrimaryAliasId
                    } );
                }

                detectiveContext.SaveChanges();

                return new CreatedItem( "statementIdKey", caseStatement.IdKey );
            } );
        }

        #endregion

        #region Entries, hypotheses, experiments

        public static object AddEntry( string caseIdKey, string entryType, string text, DateTime? eventDateTime, string timelineEventType, string url, bool isCorroborated, Rock.Model.Person person )
        {
            return WithEditableCase( caseIdKey, person, ( detectiveContext, caseFile ) =>
            {
                if ( text.IsNullOrWhiteSpace() )
                {
                    throw new McpToolException( "The entry text is required." );
                }

                RequireSafeUrl( url );

                var type = ParseEnum( entryType, CaseEntryType.Fact, "entryType" );
                TimelineEventType? eventType = null;

                if ( type == CaseEntryType.TimelineEvent )
                {
                    if ( !eventDateTime.HasValue )
                    {
                        throw new McpToolException( "A timeline event needs an eventDateTime." );
                    }

                    eventType = ParseEnum( timelineEventType, TimelineEventType.Other, "timelineEventType" );
                }

                var entry = new Model.CaseEntry
                {
                    CaseFileId = caseFile.Id,
                    EntryType = type,
                    Text = text.Trim(),
                    EventDateTime = type == CaseEntryType.TimelineEvent ? eventDateTime : null,
                    TimelineEventType = eventType,
                    Url = url,
                    Source = CaseEntrySource.Agent,
                    IsCorroborated = type == CaseEntryType.Fact && isCorroborated,
                    Order = detectiveContext.CaseEntries.Count( e => e.CaseFileId == caseFile.Id ),
                    CreatedDateTime = RockDateTime.Now,
                    CreatedByPersonAliasId = person.PrimaryAliasId,
                    ModifiedDateTime = RockDateTime.Now,
                    ModifiedByPersonAliasId = person.PrimaryAliasId
                };

                new CaseEntryService( detectiveContext ).Add( entry );
                detectiveContext.SaveChanges();

                return new CreatedItem( "entryIdKey", entry.IdKey );
            } );
        }

        public static object AddHypothesis( string caseIdKey, string name, string description, int probability, string parentHypothesisIdKey, Rock.Model.Person person )
        {
            return WithEditableCase( caseIdKey, person, ( detectiveContext, caseFile ) =>
            {
                if ( name.IsNullOrWhiteSpace() )
                {
                    throw new McpToolException( "A hypothesis name is required." );
                }

                RequireProbability( probability );

                int? parentId = null;

                if ( parentHypothesisIdKey.IsNotNullOrWhiteSpace() )
                {
                    parentId = GetHypothesis( detectiveContext, caseFile, parentHypothesisIdKey ).Id;
                }

                var hypothesis = new Model.Hypothesis
                {
                    CaseFileId = caseFile.Id,
                    Name = name.Trim().Truncate( 300 ),
                    Description = description,
                    Probability = probability,
                    Status = HypothesisStatus.Proposed,
                    ParentHypothesisId = parentId,
                    CreatedDateTime = RockDateTime.Now,
                    CreatedByPersonAliasId = person.PrimaryAliasId,
                    ModifiedDateTime = RockDateTime.Now,
                    ModifiedByPersonAliasId = person.PrimaryAliasId
                };

                new HypothesisService( detectiveContext ).Add( hypothesis );
                detectiveContext.SaveChanges();

                return new CreatedItem( "hypothesisIdKey", hypothesis.IdKey );
            } );
        }

        public static object UpdateHypothesis( string caseIdKey, string hypothesisIdKey, int? probability, string status, string ruledOutReason, Rock.Model.Person person )
        {
            return WithEditableCase( caseIdKey, person, ( detectiveContext, caseFile ) =>
            {
                var hypothesis = GetHypothesis( detectiveContext, caseFile, hypothesisIdKey );

                if ( probability.HasValue )
                {
                    RequireProbability( probability.Value );
                    hypothesis.Probability = probability.Value;
                }

                if ( status.IsNotNullOrWhiteSpace() )
                {
                    hypothesis.Status = ParseEnum( status, hypothesis.Status, "status" );
                }

                if ( hypothesis.Status == HypothesisStatus.RuledOut )
                {
                    if ( ruledOutReason.IsNullOrWhiteSpace() && hypothesis.RuledOutReason.IsNullOrWhiteSpace() )
                    {
                        throw new McpToolException( "Give a ruledOutReason when ruling a hypothesis out." );
                    }

                    hypothesis.RuledOutReason = ruledOutReason.IsNotNullOrWhiteSpace() ? ruledOutReason.Trim() : hypothesis.RuledOutReason;
                }

                hypothesis.ModifiedDateTime = RockDateTime.Now;
                hypothesis.ModifiedByPersonAliasId = person.PrimaryAliasId;

                return new CreatedItem( "hypothesisIdKey", hypothesis.IdKey );
            } );
        }

        public static object AddExperiment( string caseIdKey, string hypothesisIdKey, string hypothesisText, string testPlan, string expectedResult, Rock.Model.Person person )
        {
            return WithEditableCase( caseIdKey, person, ( detectiveContext, caseFile ) =>
            {
                if ( hypothesisIdKey.IsNullOrWhiteSpace() && hypothesisText.IsNullOrWhiteSpace() )
                {
                    throw new McpToolException( "An experiment must reference a hypothesis by hypothesisIdKey or describe one in hypothesisText." );
                }

                int? hypothesisId = null;

                if ( hypothesisIdKey.IsNotNullOrWhiteSpace() )
                {
                    hypothesisId = GetHypothesis( detectiveContext, caseFile, hypothesisIdKey ).Id;
                }

                var experiment = new Model.Experiment
                {
                    CaseFileId = caseFile.Id,
                    HypothesisId = hypothesisId,
                    HypothesisText = hypothesisText.Truncate( 300 ),
                    TestPlan = testPlan,
                    ExpectedResult = expectedResult,
                    Outcome = ExperimentOutcome.Pending,
                    CreatedDateTime = RockDateTime.Now,
                    CreatedByPersonAliasId = person.PrimaryAliasId,
                    ModifiedDateTime = RockDateTime.Now,
                    ModifiedByPersonAliasId = person.PrimaryAliasId
                };

                new ExperimentService( detectiveContext ).Add( experiment );
                detectiveContext.SaveChanges();

                return new CreatedItem( "experimentIdKey", experiment.IdKey );
            } );
        }

        public static object RecordExperimentResult( string caseIdKey, string experimentIdKey, string actualResult, string outcome, string changesMade, bool changesReverted, string revertJustification, string learnings, Rock.Model.Person person )
        {
            return WithEditableCase( caseIdKey, person, ( detectiveContext, caseFile ) =>
            {
                var experimentId = IdHasher.Instance.GetId( experimentIdKey ?? string.Empty );
                var experiment = experimentId.HasValue ? new ExperimentService( detectiveContext ).Get( experimentId.Value ) : null;

                if ( experiment == null || experiment.CaseFileId != caseFile.Id )
                {
                    throw new McpToolException( "That experiment was not found on this case." );
                }

                var experimentOutcome = ParseEnum( outcome, ExperimentOutcome.Pending, "outcome" );

                if ( experimentOutcome == ExperimentOutcome.Pending )
                {
                    throw new McpToolException( "Give an outcome of Supported, RuledOut, or Inconclusive." );
                }

                experiment.ActualResult = actualResult;
                experiment.Outcome = experimentOutcome;
                experiment.ChangesMade = changesMade;
                experiment.ChangesReverted = changesReverted;
                experiment.RevertJustification = revertJustification;
                experiment.Learnings = learnings;
                experiment.ModifiedDateTime = RockDateTime.Now;
                experiment.ModifiedByPersonAliasId = person.PrimaryAliasId;

                detectiveContext.SaveChanges();

                if ( experiment.Learnings.IsNotNullOrWhiteSpace() )
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
                            CreatedByPersonAliasId = person.PrimaryAliasId
                        } );
                    }
                }

                return new CreatedItem( "experimentIdKey", experiment.IdKey );
            } );
        }

        private static Model.Hypothesis GetHypothesis( RockDetectiveContext detectiveContext, Model.CaseFile caseFile, string hypothesisIdKey )
        {
            var hypothesisId = IdHasher.Instance.GetId( hypothesisIdKey ?? string.Empty );
            var hypothesis = hypothesisId.HasValue ? new HypothesisService( detectiveContext ).Get( hypothesisId.Value ) : null;

            if ( hypothesis == null || hypothesis.CaseFileId != caseFile.Id )
            {
                throw new McpToolException( "That hypothesis was not found on this case." );
            }

            return hypothesis;
        }

        #endregion

        #region Witness

        public static object ListMyStatementRequests( Rock.Model.Person person )
        {
            RequirePerson( person );

            var aliasIds = person.Aliases.Select( a => a.Id ).ToList();

            using ( var detectiveContext = new RockDetectiveContext() )
            {
                var requests = detectiveContext.CaseStatements
                    .AsNoTracking()
                    .Where( s => ( s.RequestStatus == StatementRequestStatus.Requested || s.RequestStatus == StatementRequestStatus.Reminded )
                        && s.Participant.PersonAliasId.HasValue
                        && aliasIds.Contains( s.Participant.PersonAliasId.Value ) )
                    .OrderBy( s => s.SentDateTime )
                    .ToList();

                var caseIds = requests.Select( s => s.CaseFileId ).Distinct().ToList();
                var cases = detectiveContext.CaseFiles.AsNoTracking().Where( c => caseIds.Contains( c.Id ) ).ToList();
                var owners = CaseMapper.GetAliasInfo( cases.Select( c => c.OwnerPersonAliasId ) );

                return requests.Select( s =>
                {
                    var caseFile = cases.First( c => c.Id == s.CaseFileId );

                    return new
                    {
                        StatementIdKey = s.IdKey,
                        Case = caseFile.IdKey,
                        CaseTitle = caseFile.Title,
                        Question = s.RequestPrompt,
                        AskedBy = owners.ContainsKey( caseFile.OwnerPersonAliasId ) ? owners[caseFile.OwnerPersonAliasId].FullName : null,
                        AskedOn = s.SentDateTime?.ToString( "s" )
                    };
                } ).ToList();
            }
        }

        public static object SubmitStatement( string statementIdKey, string statement, string url, Rock.Model.Person person )
        {
            if ( statement.IsNullOrWhiteSpace() )
            {
                throw new McpToolException( "Please give a statement to submit." );
            }

            RequireSafeUrl( url );

            return WithMyStatementRequest( statementIdKey, person, ( detectiveContext, request ) =>
            {
                request.Statement = statement.Trim();
                request.Url = url.Truncate( 2000 );
                request.RequestStatus = StatementRequestStatus.Submitted;

                return "Your statement was submitted. Thank you.";
            } );
        }

        public static object DeclineStatement( string statementIdKey, string reason, Rock.Model.Person person )
        {
            return WithMyStatementRequest( statementIdKey, person, ( detectiveContext, request ) =>
            {
                request.Statement = reason.IsNotNullOrWhiteSpace() ? $"Declined: {reason.Trim()}" : null;
                request.RequestStatus = StatementRequestStatus.Declined;

                return "The request was declined.";
            } );
        }

        private static object WithMyStatementRequest( string statementIdKey, Rock.Model.Person person, Func<RockDetectiveContext, Model.CaseStatement, string> action )
        {
            RequirePerson( person );

            using ( var detectiveContext = new RockDetectiveContext() )
            {
                var statementId = IdHasher.Instance.GetId( statementIdKey ?? string.Empty );
                var request = statementId.HasValue ? new CaseStatementService( detectiveContext ).Get( statementId.Value ) : null;
                var participant = request != null ? new CaseParticipantService( detectiveContext ).Get( request.ParticipantId ) : null;
                var caseFile = request != null ? new CaseFileService( detectiveContext ).Get( request.CaseFileId ) : null;

                var isAddressedToPerson = participant?.PersonAliasId != null && person.Aliases.Any( a => a.Id == participant.PersonAliasId.Value );

                if ( request == null || participant == null || caseFile == null
                    || ( !isAddressedToPerson && !caseFile.IsAuthorized( Rock.Security.Authorization.EDIT, person ) ) )
                {
                    throw new McpToolException( "That statement request was not found, or it was not addressed to you." );
                }

                if ( request.RequestStatus == StatementRequestStatus.Submitted )
                {
                    throw new McpToolException( "A statement was already submitted for this request." );
                }

                if ( request.RequestStatus == StatementRequestStatus.Declined )
                {
                    throw new McpToolException( "This request was already declined." );
                }

                if ( request.RequestStatus != StatementRequestStatus.Requested
                    && request.RequestStatus != StatementRequestStatus.Reminded
                    && request.RequestStatus != StatementRequestStatus.Draft )
                {
                    throw new McpToolException( "This request is not open for a response." );
                }

                var message = action( detectiveContext, request );

                request.SubmittedDateTime = RockDateTime.Now;
                request.ModifiedDateTime = RockDateTime.Now;
                request.ModifiedByPersonAliasId = person.PrimaryAliasId;
                detectiveContext.SaveChanges();

                if ( participant.PersonAliasId.HasValue )
                {
                    WitnessReminderHelper.CompleteReminders( request.Id, participant.PersonAliasId.Value );
                }

                return new { StatementIdKey = request.IdKey, Status = request.RequestStatus.ToString(), Message = message };
            }
        }

        #endregion

        #region Phases

        public static object SavePhaseNotes( string caseIdKey, int? phase, string notes, Rock.Model.Person person )
        {
            return WithEditableCase( caseIdKey, person, ( detectiveContext, caseFile ) =>
            {
                var casePhase = phase.HasValue ? ( CasePhase ) phase.Value : caseFile.CurrentPhase;

                if ( !Enum.IsDefined( typeof( CasePhase ), casePhase ) )
                {
                    throw new McpToolException( "The phase must be a number from 1 to 9." );
                }

                var states = CaseMapper.GetPhaseStates( caseFile );
                var state = states.GetState( casePhase );
                state.Summary = notes;

                if ( state.Progress == PhaseProgress.NotStarted || state.Progress == PhaseProgress.Skipped )
                {
                    state.Progress = PhaseProgress.InProgress;
                }

                CaseMapper.SetPhaseStates( caseFile, states );

                return null;
            } );
        }

        public static object CompletePhase( string caseIdKey, Rock.Model.Person person )
        {
            return WithEditableCase( caseIdKey, person, ( detectiveContext, caseFile ) =>
            {
                var casePhase = caseFile.CurrentPhase;

                if ( casePhase == CasePhase.Postmortem )
                {
                    throw new McpToolException( "The last phase is completed by closing the case with its postmortem. Use the close case tool." );
                }

                var aggregate = BuildAggregate( detectiveContext, caseFile, out var hypotheses );
                var gate = GateEvaluator.Evaluate( aggregate, casePhase, GetGateOptions() );

                if ( !gate.IsMet )
                {
                    var profile = MethodProfile.ForCategory( caseFile.CategoryId );

                    throw new McpToolException( "This phase cannot be completed yet. Still needed: "
                        + gate.Requirements.Where( r => !r.IsMet ).Select( r => profile.DescribeGate( r ) ).ToList().AsDelimited( " " ) );
                }

                var states = CaseMapper.GetPhaseStates( caseFile );
                var state = states.GetState( casePhase );
                state.Progress = PhaseProgress.Complete;
                state.CompletedDateTime = RockDateTime.Now;

                var next = ( CasePhase ) ( ( int ) casePhase + 1 );

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

                if ( casePhase == CasePhase.Reevaluate && hypotheses.Any( h => h.Status == HypothesisStatus.Confirmed ) )
                {
                    caseFile.Status = CaseStatus.Solved;
                }

                CaseMapper.SetPhaseStates( caseFile, states );

                return null;
            } );
        }

        public static object LoopBack( string caseIdKey, Rock.Model.Person person )
        {
            return WithEditableCase( caseIdKey, person, ( detectiveContext, caseFile ) =>
            {
                if ( caseFile.CurrentPhase != CasePhase.Reevaluate )
                {
                    throw new McpToolException( "A case loops back from the reevaluate phase, after its experiments leave no confirmed cause. Keep working the current phase." );
                }

                var states = CaseMapper.GetPhaseStates( caseFile );
                var reevaluateState = states.GetState( CasePhase.Reevaluate );

                var loopCount = 0;

                if ( reevaluateState.Data.TryGetValue( PhaseStateKey.LoopBackCount, out var existing ) )
                {
                    loopCount = existing.AsInteger();
                }

                reevaluateState.Data[PhaseStateKey.LoopBackCount] = ( loopCount + 1 ).ToString();
                reevaluateState.Data[PhaseStateKey.LastLoopBackDateTime] = RockDateTime.Now.ToString( "s" );
                CaseRules.ClearChampionSignOff( states );

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

                return null;
            } );
        }

        public static object ChampionSignOff( string caseIdKey, Rock.Model.Person person )
        {
            RequirePerson( person );

            using ( var detectiveContext = new RockDetectiveContext() )
            {
                var caseFile = GetCase( detectiveContext, caseIdKey );

                if ( caseFile.Status == CaseStatus.Closed || caseFile.Status == CaseStatus.Abandoned )
                {
                    throw new McpToolException( "This case is closed or abandoned, so it cannot be signed off." );
                }

                var canEditCase = person.Aliases.Any( a => a.Id == caseFile.OwnerPersonAliasId ) || caseFile.IsAuthorized( Rock.Security.Authorization.EDIT, person );
                var refusal = CaseRules.GetChampionSignOffRefusal( detectiveContext, caseFile, person, canEditCase );

                if ( refusal != null )
                {
                    throw new McpToolException( refusal );
                }

                var states = CaseMapper.GetPhaseStates( caseFile );
                CaseRules.RecordChampionSignOff( states, person );
                CaseMapper.SetPhaseStates( caseFile, states );

                Stamp( caseFile, person );
                detectiveContext.SaveChanges();

                return BuildProgress( detectiveContext, caseFile, null, null );
            }
        }

        public static object CloseCase( string caseIdKey, string impactSummary, string solutionSummary, string contributingFactors, string lessonsLearned, string preventionMeasures, string actionItems, DateTime? followUpDate, Rock.Model.Person person )
        {
            return WithEditableCase( caseIdKey, person, ( detectiveContext, caseFile ) =>
            {
                if ( caseFile.CurrentPhase != CasePhase.Postmortem )
                {
                    throw new McpToolException( "A case can only be closed from the last phase. Complete the earlier phases first." );
                }

                caseFile.ImpactSummary = impactSummary;
                caseFile.SolutionSummary = solutionSummary;
                caseFile.ContributingFactors = contributingFactors;
                caseFile.LessonsLearned = lessonsLearned;
                caseFile.PreventionMeasures = preventionMeasures;
                caseFile.ActionItems = actionItems;

                var aggregate = BuildAggregate( detectiveContext, caseFile, out _ );
                var gate = GateEvaluator.Evaluate( aggregate, CasePhase.Postmortem, GetGateOptions() );

                if ( !gate.IsMet )
                {
                    var profile = MethodProfile.ForCategory( caseFile.CategoryId );

                    throw new McpToolException( "The postmortem is incomplete. Still needed: "
                        + gate.Requirements.Where( r => !r.IsMet ).Select( r => profile.DescribeGate( r ) ).ToList().AsDelimited( " " ) );
                }

                var states = CaseMapper.GetPhaseStates( caseFile );
                var state = states.GetState( CasePhase.Postmortem );
                state.Progress = PhaseProgress.Complete;
                state.CompletedDateTime = RockDateTime.Now;
                CaseMapper.SetPhaseStates( caseFile, states );

                caseFile.Status = CaseStatus.Closed;
                caseFile.ClosedDateTime = RockDateTime.Now;
                caseFile.FollowUpDate = followUpDate;

                return null;
            } );
        }

        #endregion

        #region Shared helpers

        private class CreatedItem
        {
            public CreatedItem( string name, string idKey )
            {
                Name = name;
                IdKey = idKey;
            }

            public string Name { get; }

            public string IdKey { get; }
        }

        private static void RequirePerson( Rock.Model.Person person )
        {
            if ( person == null || !person.PrimaryAliasId.HasValue )
            {
                throw new McpToolException( "You must be signed in to change a case file." );
            }
        }

        private static Model.CaseFile GetCase( RockDetectiveContext detectiveContext, string caseIdKey )
        {
            var id = IdHasher.Instance.GetId( caseIdKey ?? string.Empty );
            var caseFile = id.HasValue ? new CaseFileService( detectiveContext ).Get( id.Value ) : null;

            if ( caseFile == null )
            {
                throw new McpToolException( $"No case file was found with the IdKey '{caseIdKey}'." );
            }

            return caseFile;
        }

        private static object WithEditableCase( string caseIdKey, Rock.Model.Person person, Func<RockDetectiveContext, Model.CaseFile, CreatedItem> action )
        {
            RequirePerson( person );

            using ( var detectiveContext = new RockDetectiveContext() )
            {
                var caseFile = GetCase( detectiveContext, caseIdKey );
                var isOwner = person.Aliases.Any( a => a.Id == caseFile.OwnerPersonAliasId );

                if ( !isOwner && !caseFile.IsAuthorized( Rock.Security.Authorization.EDIT, person ) )
                {
                    throw new McpToolException( "You are not authorized to change this case file." );
                }

                if ( caseFile.Status == CaseStatus.Closed )
                {
                    throw new McpToolException( "This case is closed. It has to be reopened on the Case Board before it can change." );
                }

                if ( caseFile.Status == CaseStatus.Abandoned )
                {
                    throw new McpToolException( "This case is abandoned. It has to be resumed on the Case Board before it can change." );
                }

                var created = action( detectiveContext, caseFile );

                Stamp( caseFile, person );
                detectiveContext.SaveChanges();

                return BuildProgress( detectiveContext, caseFile, created?.Name, created?.IdKey );
            }
        }

        private static void RequireSafeUrl( string url )
        {
            if ( !CaseRules.IsSafeUrl( url ) )
            {
                throw new McpToolException( CaseRules.UnsafeUrlMessage );
            }
        }

        private static void RequireTitleLength( string title )
        {
            if ( title.Trim().Length > CaseRules.MaxTitleLength )
            {
                throw new McpToolException( CaseRules.TitleTooLongMessage );
            }
        }

        private static void RequireProbability( int probability )
        {
            if ( !CaseRules.IsProbabilityValid( probability ) )
            {
                throw new McpToolException( CaseRules.ProbabilityMessage );
            }
        }

        private static void Stamp( Model.CaseFile caseFile, Rock.Model.Person person )
        {
            caseFile.ModifiedDateTime = RockDateTime.Now;
            caseFile.ModifiedByPersonAliasId = person.PrimaryAliasId;
        }

        private static CaseAggregate BuildAggregate( RockDetectiveContext detectiveContext, Model.CaseFile caseFile, out List<Model.Hypothesis> hypotheses )
        {
            var entries = detectiveContext.CaseEntries.AsNoTracking().Where( e => e.CaseFileId == caseFile.Id ).ToList();
            hypotheses = detectiveContext.Hypotheses.AsNoTracking().Where( h => h.CaseFileId == caseFile.Id ).ToList();
            var experiments = detectiveContext.Experiments.AsNoTracking().Where( e => e.CaseFileId == caseFile.Id ).ToList();
            var participants = detectiveContext.CaseParticipants.AsNoTracking().Where( p => p.CaseFileId == caseFile.Id ).ToList();

            return CaseMapper.BuildAggregate( caseFile, entries, hypotheses, experiments, participants );
        }

        private static object BuildProgress( RockDetectiveContext detectiveContext, Model.CaseFile caseFile, string createdName, string createdIdKey )
        {
            var profile = MethodProfile.ForCategory( caseFile.CategoryId );
            var aggregate = BuildAggregate( detectiveContext, caseFile, out _ );
            var phase = profile.GetPhase( caseFile.CurrentPhase );
            var gate = GateEvaluator.Evaluate( aggregate, caseFile.CurrentPhase, GetGateOptions() );
            var isFinished = caseFile.Status == CaseStatus.Closed;

            return new
            {
                CaseIdKey = caseFile.IdKey,
                CaseNumber = caseFile.IdKey,
                caseFile.Title,
                Status = caseFile.Status.ToString(),
                Mode = caseFile.Mode.ToString(),
                CreatedItemType = createdName,
                CreatedItemIdKey = createdIdKey,
                CurrentPhase = ( int ) caseFile.CurrentPhase,
                CurrentPhaseName = phase.Name,
                CurrentPhaseGuidance = isFinished ? null : phase.Guidance,
                CurrentPhasePrompts = isFinished ? new List<string>() : phase.Prompts,
                IsCurrentPhaseReadyToComplete = !isFinished && gate.IsMet,
                CurrentPhaseChecklist = isFinished
                    ? new List<GateChecklistItem>()
                    : gate.Requirements.Select( r => new GateChecklistItem { Requirement = profile.DescribeGate( r ), IsMet = r.IsMet } ).ToList(),
                NextStep = GetNextStep( caseFile, gate.IsMet, isFinished )
            };
        }

        public class GateChecklistItem
        {
            public string Requirement { get; set; }

            public bool IsMet { get; set; }
        }

        private static string GetNextStep( Model.CaseFile caseFile, bool isGateMet, bool isFinished )
        {
            if ( isFinished )
            {
                return "The case is closed and archived.";
            }

            if ( caseFile.CurrentPhase == CasePhase.Postmortem )
            {
                return "Close the case with its postmortem: impact, solution, contributing factors, lessons learned, prevention measures, and action items.";
            }

            return isGateMet
                ? "The checklist for this phase is met. Complete the phase to move on."
                : "Work this phase until every checklist item is met, then complete the phase.";
        }

        private static TEnum ParseEnum<TEnum>( string value, TEnum fallback, string parameterName ) where TEnum : struct
        {
            if ( value.IsNullOrWhiteSpace() )
            {
                return fallback;
            }

            var cleaned = value.Replace( " ", string.Empty );

            if ( Enum.TryParse<TEnum>( cleaned, true, out var parsed ) && Enum.IsDefined( typeof( TEnum ), parsed ) )
            {
                return parsed;
            }

            throw new McpToolException( $"'{value}' is not a valid {parameterName}. Choices: {string.Join( ", ", Enum.GetNames( typeof( TEnum ) ) )}." );
        }

        #endregion
    }
}
