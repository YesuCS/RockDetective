using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using Rock;
using Rock.Communication;
using Rock.Jobs;
using Rock.Web.Cache;

using com.yesuchum.RockDetective.Data;
using com.yesuchum.RockDetective.Enums;

namespace com.yesuchum.RockDetective.Jobs
{
    [DisplayName( "Rock Detective Follow-Up Checks" )]
    [Description( "Notifies case owners when a closed case reaches its follow-up check date so they can confirm the fix held or reopen the case." )]

    public class ProcessFollowUpChecks : RockJob
    {
        public override void Execute()
        {
            var today = RockDateTime.Today;
            var sentCount = 0;
            var errors = new List<string>();

            using ( var detectiveContext = new RockDetectiveContext() )
            using ( var rockContext = new Rock.Data.RockContext() )
            {
                var dueCases = new CaseFileService( detectiveContext )
                    .Queryable()
                    .Where( c =>
                        c.Status == CaseStatus.Closed
                        && c.FollowUpDate.HasValue
                        && c.FollowUpDate.Value <= today
                        && !c.FollowUpConfirmedDateTime.HasValue )
                    .ToList();

                if ( !dueCases.Any() )
                {
                    Result = "No follow-up checks due.";
                    return;
                }

                var systemCommunication = new Rock.Model.SystemCommunicationService( rockContext )
                    .Get( SystemGuid.SystemCommunication.FOLLOW_UP_CHECK.AsGuid() );

                if ( systemCommunication == null )
                {
                    Result = "System communication missing; no notifications sent.";
                    return;
                }

                var personAliasService = new Rock.Model.PersonAliasService( rockContext );
                var root = GlobalAttributesCache.Get().GetValue( "InternalApplicationRoot" )?.TrimEnd( '/' ) ?? string.Empty;
                var boardPage = PageCache.Get( SystemGuid.Page.CASE_BOARD.AsGuid() );

                foreach ( var caseFile in dueCases )
                {
                    var person = personAliasService.Get( caseFile.OwnerPersonAliasId )?.Person;

                    if ( person == null || person.Email.IsNullOrWhiteSpace() )
                    {
                        continue;
                    }

                    var boardUrl = boardPage != null
                        ? $"{root}/page/{boardPage.Id}?CaseFile={caseFile.IdKey}"
                        : root;

                    var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( null, person );
                    mergeFields.Add( "Person", person );
                    mergeFields.Add( "CaseNumber", caseFile.Id.ToString() );
                    mergeFields.Add( "CaseTitle", caseFile.Title );
                    mergeFields.Add( "FixHeldLink", $"{boardUrl}&FollowUp=Confirm" );
                    mergeFields.Add( "ReopenLink", $"{boardUrl}&FollowUp=Reopen" );

                    var message = new RockEmailMessage( systemCommunication );
                    message.AddRecipient( new RockEmailMessageRecipient( person, mergeFields ) );
                    message.AppRoot = "/";
                    message.CreateCommunicationRecord = true;

                    if ( message.Send( out var sendErrors ) )
                    {
                        caseFile.FollowUpDate = today.AddDays( 7 );
                        caseFile.ModifiedDateTime = RockDateTime.Now;
                        sentCount++;
                    }
                    else
                    {
                        errors.AddRange( sendErrors );
                    }
                }

                detectiveContext.SaveChanges();
            }

            Result = $"Sent {sentCount} follow-up notification{( sentCount == 1 ? string.Empty : "s" )}."
                + ( errors.Any() ? $" {errors.Count} failed." : string.Empty );
        }
    }
}
