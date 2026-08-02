using System.ComponentModel;
using System.Data.Entity;
using System.Linq;

using Rock;
using Rock.Attribute;
using Rock.Blocks;

using com.yesuchum.RockDetective.Data;
using com.yesuchum.RockDetective.Enums;
using com.yesuchum.RockDetective.ViewModels;

namespace com.yesuchum.RockDetective.Blocks
{
    [DisplayName( "Witness Statement Entry" )]
    [Category( "Rock Detective" )]
    [Description( "Landing page for witness statement requests; the requested witness can submit or decline their statement." )]
    [IconCssClass( "ti ti-message-report" )]
    [SupportedSiteTypes( Rock.Model.SiteType.Web )]

    [Rock.SystemGuid.EntityTypeGuid( SystemGuid.EntityType.WITNESS_STATEMENT_ENTRY_BLOCK )]
    [Rock.SystemGuid.BlockTypeGuid( SystemGuid.BlockType.WITNESS_STATEMENT_ENTRY )]
    public class WitnessStatementEntry : RockBlockType
    {
        private static class PageParameterKey
        {
            public const string Statement = "Statement";
        }

        public override string ObsidianFileUrl => "~/Plugins/com_yesuchum/RockDetective/witnessStatementEntry.obs";

        public override object GetObsidianBlockInitialization()
        {
            var box = new WitnessStatementInitBag();
            var currentPerson = RequestContext.CurrentPerson;

            if ( currentPerson == null )
            {
                box.ErrorMessage = "You must be logged in to submit a statement.";
                return box;
            }

            using ( var detectiveContext = new RockDetectiveContext() )
            {
                var statement = GetStatement( detectiveContext, PageParameter( PageParameterKey.Statement ) );

                if ( statement == null )
                {
                    box.ErrorMessage = "Statement request not found.";
                    return box;
                }

                var participant = new CaseParticipantService( detectiveContext ).Get( statement.ParticipantId );
                var caseFile = new CaseFileService( detectiveContext ).Get( statement.CaseFileId );

                if ( participant == null || caseFile == null )
                {
                    box.ErrorMessage = "Case file not found.";
                    return box;
                }

                if ( !CanAccess( participant, caseFile, currentPerson ) )
                {
                    box.ErrorMessage = "This statement request was not addressed to you.";
                    return box;
                }

                box.ParticipantIdKey = statement.IdKey;
                box.CaseNumber = caseFile.Id.ToString();
                box.CaseTitle = caseFile.Title;
                box.RequestPrompt = statement.RequestPrompt;
                box.IsAlreadySubmitted = statement.RequestStatus == StatementRequestStatus.Submitted;
                box.IsDeclined = statement.RequestStatus == StatementRequestStatus.Declined;
                box.CanSubmit = statement.RequestStatus == StatementRequestStatus.Requested
                    || statement.RequestStatus == StatementRequestStatus.Reminded
                    || statement.RequestStatus == StatementRequestStatus.Draft;
            }

            return box;
        }

        private static Model.CaseStatement GetStatement( RockDetectiveContext detectiveContext, string idKey )
        {
            if ( idKey.IsNullOrWhiteSpace() )
            {
                return null;
            }

            var id = Rock.Utility.IdHasher.Instance.GetId( idKey );

            if ( id.HasValue )
            {
                return new CaseStatementService( detectiveContext ).Get( id.Value );
            }

            var guid = idKey.AsGuidOrNull();

            if ( guid.HasValue )
            {
                return detectiveContext.CaseStatements.FirstOrDefault( s => s.Guid == guid.Value );
            }

            return null;
        }

        private static bool CanAccess( Model.CaseParticipant participant, Model.CaseFile caseFile, Rock.Model.Person person )
        {
            if ( participant.PersonAliasId.HasValue && person.Aliases.Any( a => a.Id == participant.PersonAliasId.Value ) )
            {
                return true;
            }

            return caseFile.IsAuthorized( Rock.Security.Authorization.EDIT, person );
        }

        [BlockAction]
        public BlockActionResult SubmitStatement( string participantIdKey, string statement, string binaryFileGuid, string url )
        {
            return Resolve( participantIdKey, ( detectiveContext, statementEntity ) =>
            {
                if ( statement.IsNullOrWhiteSpace() )
                {
                    return ActionBadRequest( "Please write a statement before submitting." );
                }

                statementEntity.Statement = statement;
                statementEntity.Url = url.Truncate( 2000 );
                statementEntity.BinaryFileId = ResolveBinaryFileId( binaryFileGuid );
                statementEntity.RequestStatus = StatementRequestStatus.Submitted;
                statementEntity.SubmittedDateTime = RockDateTime.Now;
                statementEntity.ModifiedDateTime = RockDateTime.Now;
                statementEntity.ModifiedByPersonAliasId = RequestContext.CurrentPerson?.PrimaryAliasId;
                detectiveContext.SaveChanges();

                CompleteRemindersIfSettled( detectiveContext, statementEntity );

                return ActionOk();
            } );
        }

        [BlockAction]
        public BlockActionResult DeclineStatement( string participantIdKey, string reason )
        {
            return Resolve( participantIdKey, ( detectiveContext, statementEntity ) =>
            {
                statementEntity.Statement = reason.IsNotNullOrWhiteSpace() ? $"Declined: {reason}" : null;
                statementEntity.RequestStatus = StatementRequestStatus.Declined;
                statementEntity.SubmittedDateTime = RockDateTime.Now;
                statementEntity.ModifiedDateTime = RockDateTime.Now;
                statementEntity.ModifiedByPersonAliasId = RequestContext.CurrentPerson?.PrimaryAliasId;
                detectiveContext.SaveChanges();

                CompleteRemindersIfSettled( detectiveContext, statementEntity );

                return ActionOk();
            } );
        }

        private static void CompleteRemindersIfSettled( RockDetectiveContext detectiveContext, Model.CaseStatement statementEntity )
        {
            var participant = new CaseParticipantService( detectiveContext ).Get( statementEntity.ParticipantId );

            if ( participant?.PersonAliasId == null )
            {
                return;
            }

            WitnessReminderHelper.CompleteReminders( statementEntity.Id, participant.PersonAliasId.Value );
        }

        private static int? ResolveBinaryFileId( string binaryFileGuid )
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

        private BlockActionResult Resolve( string statementIdKey, System.Func<RockDetectiveContext, Model.CaseStatement, BlockActionResult> action )
        {
            var currentPerson = RequestContext.CurrentPerson;

            if ( currentPerson == null )
            {
                return ActionForbidden( "You must be logged in." );
            }

            using ( var detectiveContext = new RockDetectiveContext() )
            {
                var statement = GetStatement( detectiveContext, statementIdKey );

                if ( statement == null )
                {
                    return ActionNotFound( "Statement request not found." );
                }

                var participant = new CaseParticipantService( detectiveContext ).Get( statement.ParticipantId );
                var caseFile = new CaseFileService( detectiveContext ).Get( statement.CaseFileId );

                if ( participant == null || caseFile == null || !CanAccess( participant, caseFile, currentPerson ) )
                {
                    return ActionForbidden( "This statement request was not addressed to you." );
                }

                return action( detectiveContext, statement );
            }
        }
    }
}
