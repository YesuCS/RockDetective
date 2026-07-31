using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.ComponentModel;
using System.Linq;

using Rock;
using Rock.Attribute;
using Rock.Blocks;
using Rock.Web.Cache;

using com.yesuchum.RockDetective.Data;
using com.yesuchum.RockDetective.Enums;
using com.yesuchum.RockDetective.Method;
using com.yesuchum.RockDetective.ViewModels;

namespace com.yesuchum.RockDetective.Blocks
{
    [DisplayName( "Case File List" )]
    [Category( "Rock Detective" )]
    [Description( "Lists Rock Detective case files with My Requests, My Assignments, and All views, plus the closed-case archive search." )]
    [IconCssClass( "ti ti-zoom-question" )]
    [SupportedSiteTypes( Rock.Model.SiteType.Web )]

    [LinkedPage( "Detail Page",
        Description = "The Case Board page for viewing and working a case.",
        IsRequired = true,
        Order = 0,
        Key = AttributeKey.DetailPage )]

    [CustomDropdownListField( "Default View",
        Description = "The view shown when the block first loads.",
        ListSource = "MyRequests^My Requests,MyAssignments^My Assignments,All^All",
        DefaultValue = "MyRequests",
        IsRequired = true,
        Order = 1,
        Key = AttributeKey.DefaultView )]

    [BooleanField( "Show All View To Everyone",
        Description = "When off, the All view only includes cases the viewer can see through entity security. When on, the All view widens to every case regardless of per-case security. View access on the case itself is still enforced when opening a case.",
        DefaultBooleanValue = false,
        Order = 2,
        Key = AttributeKey.ShowAllViewToEveryone )]

    [Rock.SystemGuid.EntityTypeGuid( SystemGuid.EntityType.CASE_FILE_LIST_BLOCK )]
    [Rock.SystemGuid.BlockTypeGuid( SystemGuid.BlockType.CASE_FILE_LIST )]
    public class CaseFileList : RockBlockType
    {
        private static class AttributeKey
        {
            public const string DetailPage = "DetailPage";
            public const string DefaultView = "DefaultView";
            public const string ShowAllViewToEveryone = "ShowAllViewToEveryone";
        }

        private static class PageParameterKey
        {
            public const string CaseFile = "CaseFile";
        }

        public override string ObsidianFileUrl => "~/Plugins/com_yesuchum/RockDetective/caseFileList.obs";

        public override object GetObsidianBlockInitialization()
        {
            var currentPerson = RequestContext.CurrentPerson;

            var box = new CaseFileListInitBag
            {
                DefaultView = GetAttributeValue( AttributeKey.DefaultView ),
                CanAddCase = currentPerson != null,
                DetailPageUrl = this.GetLinkedPageUrl( AttributeKey.DetailPage, new Dictionary<string, string> { { PageParameterKey.CaseFile, "PLACEHOLDER" } } ),
                Categories = GetCategoryOptions()
            };

            if ( currentPerson != null )
            {
                box.Cases = LoadCases( currentPerson, includeClosed: false );
            }

            return box;
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
                    .Select( c => new CategoryOptionBag
                    {
                        Guid = c.Guid.ToString(),
                        Name = c.Name,
                        IconCssClass = c.IconCssClass
                    } )
                    .ToList();
            }
        }

        private List<CaseFileListItemBag> LoadCases( Rock.Model.Person currentPerson, bool includeClosed, string searchTerm = null )
        {
            using ( var detectiveContext = new RockDetectiveContext() )
            {
                var caseService = new CaseFileService( detectiveContext );
                var showAllToEveryone = GetAttributeValue( AttributeKey.ShowAllViewToEveryone ).AsBoolean();

                var query = caseService.Queryable().AsNoTracking();

                if ( includeClosed )
                {
                    query = query.Where( c => c.Status == CaseStatus.Closed );

                    if ( searchTerm.IsNotNullOrWhiteSpace() )
                    {
                        query = query.Where( c =>
                            c.ProblemStatement.Contains( searchTerm )
                            || c.Title.Contains( searchTerm )
                            || c.SolutionSummary.Contains( searchTerm )
                            || c.LessonsLearned.Contains( searchTerm ) );
                    }
                }
                else
                {
                    query = query.Where( c => c.Status != CaseStatus.Closed );
                }

                var cases = query.ToList();

                var myAliasIds = currentPerson.Aliases.Select( a => a.Id ).ToList();

                var caseIds = cases.Select( c => c.Id ).ToList();
                var participants = detectiveContext.CaseParticipants
                    .AsNoTracking()
                    .Where( p => caseIds.Contains( p.CaseFileId ) )
                    .ToList();
                var statements = detectiveContext.CaseStatements
                    .AsNoTracking()
                    .Where( s => caseIds.Contains( s.CaseFileId ) )
                    .ToList();

                var overdueCutoff = RockDateTime.Now.AddDays( -CaseMapper.OverdueWitnessDays );

                var ownerAliasIds = cases.Select( c => c.OwnerPersonAliasId ).ToList();
                var aliasInfo = CaseMapper.GetAliasInfo( ownerAliasIds );

                var items = new List<CaseFileListItemBag>();

                foreach ( var caseFile in cases )
                {
                    var isMine = myAliasIds.Contains( caseFile.OwnerPersonAliasId );
                    var caseParticipants = participants.Where( p => p.CaseFileId == caseFile.Id ).ToList();
                    var myParticipation = caseParticipants.Where( p => p.PersonAliasId.HasValue && myAliasIds.Contains( p.PersonAliasId.Value ) ).ToList();
                    var isAssignedToMe = isMine || myParticipation.Any();

                    if ( !isMine && !isAssignedToMe && !showAllToEveryone && !caseFile.IsAuthorized( Rock.Security.Authorization.VIEW, currentPerson ) )
                    {
                        continue;
                    }

                    items.Add( new CaseFileListItemBag
                    {
                        IdKey = caseFile.IdKey,
                        CaseNumber = caseFile.Id.ToString(),
                        Title = caseFile.Title,
                        CategoryName = caseFile.Category?.Name,
                        Status = caseFile.Status.ToString().SplitCase(),
                        Phase = MethodDefinition.GetPhase( caseFile.CurrentPhase ).Name,
                        Mode = caseFile.Mode == CaseMode.Advanced ? "Advanced" : "Simple",
                        AgeDays = caseFile.CreatedDateTime.HasValue
                            ? ( int ) ( RockDateTime.Now - caseFile.CreatedDateTime.Value ).TotalDays
                            : 0,
                        HasOverdueWitness = statements.Any( s =>
                            s.CaseFileId == caseFile.Id
                            && ( s.RequestStatus == StatementRequestStatus.Requested || s.RequestStatus == StatementRequestStatus.Reminded )
                            && s.SentDateTime.HasValue
                            && s.SentDateTime.Value < overdueCutoff ),
                        OwnerName = aliasInfo.ContainsKey( caseFile.OwnerPersonAliasId ) ? aliasInfo[caseFile.OwnerPersonAliasId].FullName : "Unknown",
                        SolutionSummary = includeClosed ? caseFile.SolutionSummary.Truncate( 200 ) : null,
                        CanDelete = caseFile.IsAuthorized( Rock.Security.Authorization.EDIT, currentPerson ),
                        IsMine = isMine,
                        IsAssignedToMe = isAssignedToMe,
                        HasMyPendingStatement = statements.Any( s =>
                            s.CaseFileId == caseFile.Id
                            && myParticipation.Any( p => p.Id == s.ParticipantId )
                            && ( s.RequestStatus == StatementRequestStatus.Requested || s.RequestStatus == StatementRequestStatus.Reminded ) ),
                        CreatedDateTime = caseFile.CreatedDateTime,
                        ClosedDateTime = caseFile.ClosedDateTime
                    } );
                }

                return items.OrderByDescending( i => i.CreatedDateTime ).ToList();
            }
        }

        [BlockAction]
        public BlockActionResult SearchArchive( string searchTerm )
        {
            var currentPerson = RequestContext.CurrentPerson;

            if ( currentPerson == null )
            {
                return ActionForbidden( "You must be logged in." );
            }

            return ActionOk( new ArchiveSearchResultBag
            {
                Cases = LoadCases( currentPerson, includeClosed: true, searchTerm: searchTerm )
            } );
        }

        [BlockAction]
        public BlockActionResult CreateCase( NewCaseRequestBag bag )
        {
            var currentPerson = RequestContext.CurrentPerson;

            if ( currentPerson == null || !currentPerson.PrimaryAliasId.HasValue )
            {
                return ActionForbidden( "You must be logged in to open a case." );
            }

            if ( bag == null || bag.Title.IsNullOrWhiteSpace() || bag.ProblemStatement.IsNullOrWhiteSpace() )
            {
                return ActionBadRequest( "A title and problem statement are required." );
            }

            int? categoryId = null;
            var categoryGuid = bag.CategoryGuid.AsGuidOrNull();

            if ( categoryGuid.HasValue )
            {
                categoryId = CategoryCache.Get( categoryGuid.Value )?.Id;
            }

            if ( !categoryId.HasValue )
            {
                return ActionBadRequest( "A problem category is required." );
            }

            var mode = bag.Mode == ( int ) CaseMode.Advanced ? CaseMode.Advanced : CaseMode.Simple;
            var classification = mode == CaseMode.Advanced ? CaseClassification.Difficult : CaseClassification.Easy;

            using ( var detectiveContext = new RockDetectiveContext() )
            {
                var caseService = new CaseFileService( detectiveContext );

                var caseFile = new Model.CaseFile
                {
                    Title = bag.Title.Truncate( 200 ),
                    ProblemStatement = bag.ProblemStatement,
                    CategoryId = categoryId,
                    Mode = mode,
                    Classification = classification,
                    Status = CaseStatus.Open,
                    CurrentPhase = mode == CaseMode.Advanced ? CasePhase.DefineTeam : CasePhase.FactList,
                    OwnerPersonAliasId = currentPerson.PrimaryAliasId.Value,
                    CreatedDateTime = RockDateTime.Now,
                    CreatedByPersonAliasId = currentPerson.PrimaryAliasId
                };

                var states = new CasePhaseStates();

                var defineProblemState = states.GetState( CasePhase.DefineProblem );
                defineProblemState.Progress = PhaseProgress.Complete;
                defineProblemState.CompletedDateTime = RockDateTime.Now;
                defineProblemState.Data[PhaseStateKey.AttestNoUnverifiedDetails] = bag.AttestNoUnverifiedDetails.ToString();
                defineProblemState.Data[PhaseStateKey.AttestNothingNotGermane] = bag.AttestNothingNotGermane.ToString();

                var classifyState = states.GetState( CasePhase.Classify );
                classifyState.Progress = PhaseProgress.Complete;
                classifyState.CompletedDateTime = RockDateTime.Now;
                classifyState.Data[PhaseStateKey.ClassifyScoreImpact] = bag.ScoreImpact.ToString();
                classifyState.Data[PhaseStateKey.ClassifyScoreUrgency] = bag.ScoreUrgency.ToString();
                classifyState.Data[PhaseStateKey.ClassifyScoreRecurrence] = bag.ScoreRecurrence.ToString();
                classifyState.Data[PhaseStateKey.ClassifyScoreScope] = bag.ScoreScope.ToString();

                CaseMapper.SetPhaseStates( caseFile, states );

                caseService.Add( caseFile );
                detectiveContext.SaveChanges();

                var detailUrl = this.GetLinkedPageUrl( AttributeKey.DetailPage, new Dictionary<string, string>
                {
                    { PageParameterKey.CaseFile, caseFile.IdKey }
                } );

                return ActionOk( new NewCaseResultBag
                {
                    IdKey = caseFile.IdKey,
                    CaseNumber = caseFile.Id.ToString(),
                    DetailUrl = detailUrl
                } );
            }
        }

        [BlockAction]
        public BlockActionResult Delete( string key )
        {
            var currentPerson = RequestContext.CurrentPerson;

            using ( var detectiveContext = new RockDetectiveContext() )
            {
                var caseService = new CaseFileService( detectiveContext );
                var id = Rock.Utility.IdHasher.Instance.GetId( key ?? string.Empty );
                var caseFile = id.HasValue ? caseService.Get( id.Value ) : null;

                if ( caseFile == null )
                {
                    return ActionNotFound( "Case file not found." );
                }

                if ( !caseFile.IsAuthorized( Rock.Security.Authorization.EDIT, currentPerson ) )
                {
                    return ActionForbidden( "You are not authorized to delete this case." );
                }

                if ( !caseService.CanDelete( caseFile, out var errorMessage ) )
                {
                    return ActionBadRequest( errorMessage );
                }

                caseService.DeleteWithChildren( caseFile );
                detectiveContext.SaveChanges();

                return ActionOk();
            }
        }

        [BlockAction]
        public BlockActionResult RefreshCases()
        {
            var currentPerson = RequestContext.CurrentPerson;

            if ( currentPerson == null )
            {
                return ActionForbidden( "You must be logged in." );
            }

            return ActionOk( LoadCases( currentPerson, includeClosed: false ) );
        }
    }
}
