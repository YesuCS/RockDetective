using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using Rock;
using Rock.Attribute;
using Rock.Communication;
using Rock.Jobs;
using Rock.Web.Cache;

using com.yesuchum.RockDetective.Data;
using com.yesuchum.RockDetective.Enums;

namespace com.yesuchum.RockDetective.Jobs
{
    [DisplayName( "Rock Detective Witness Reminders" )]
    [Description( "Sends reminder communications for witness statement requests that have gone unanswered longer than the configured number of days." )]

    [IntegerField( "Reminder Days",
        Description = "Number of days a witness statement request may sit unanswered before a reminder is sent.",
        DefaultIntegerValue = 3,
        IsRequired = true,
        Order = 0,
        Key = "ReminderDays" )]

    public class SendWitnessReminders : RockJob
    {
        public override void Execute()
        {
            var reminderDays = GetAttributeValue( "ReminderDays" ).AsIntegerOrNull() ?? 3;
            var cutoff = RockDateTime.Now.AddDays( -reminderDays );
            var sentCount = 0;
            var errors = new List<string>();

            using ( var detectiveContext = new RockDetectiveContext() )
            using ( var rockContext = new Rock.Data.RockContext() )
            {
                var pending = detectiveContext.CaseStatements
                    .Where( s =>
                        s.RequestStatus == StatementRequestStatus.Requested
                        && s.Participant.PersonAliasId.HasValue
                        && s.SentDateTime.HasValue
                        && s.SentDateTime.Value < cutoff )
                    .ToList();

                if ( !pending.Any() )
                {
                    Result = "No witness requests needed reminders.";
                    return;
                }

                var systemCommunication = new Rock.Model.SystemCommunicationService( rockContext )
                    .Get( SystemGuid.SystemCommunication.WITNESS_STATEMENT_REMINDER.AsGuid() );

                if ( systemCommunication == null )
                {
                    Rock.Model.ExceptionLogService.LogException( new System.InvalidOperationException( "Rock Detective witness reminder system communication is missing." ) );
                    Result = "System communication missing; no reminders sent.";
                    return;
                }

                var personAliasService = new Rock.Model.PersonAliasService( rockContext );
                var caseService = new CaseFileService( detectiveContext );
                var root = GlobalAttributesCache.Get().GetValue( "InternalApplicationRoot" )?.TrimEnd( '/' ) ?? string.Empty;
                var witnessPage = PageCache.Get( SystemGuid.Page.WITNESS_STATEMENT.AsGuid() );

                var participantService = new CaseParticipantService( detectiveContext );

                foreach ( var statement in pending )
                {
                    var participant = participantService.Get( statement.ParticipantId );
                    var person = participant?.PersonAliasId != null ? personAliasService.Get( participant.PersonAliasId.Value )?.Person : null;

                    if ( person == null || person.Email.IsNullOrWhiteSpace() )
                    {
                        continue;
                    }

                    var caseFile = caseService.Get( statement.CaseFileId );

                    if ( caseFile == null || caseFile.Status == CaseStatus.Closed || caseFile.Status == CaseStatus.Abandoned )
                    {
                        continue;
                    }

                    var statementLink = witnessPage != null
                        ? $"{root}/page/{witnessPage.Id}?Statement={statement.IdKey}"
                        : root;

                    var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( null, person );
                    mergeFields.Add( "Person", person );
                    mergeFields.Add( "CaseNumber", caseFile.Id.ToString() );
                    mergeFields.Add( "CaseTitle", caseFile.Title );
                    mergeFields.Add( "RequestPrompt", statement.RequestPrompt );
                    mergeFields.Add( "StatementLink", statementLink );

                    var message = new RockEmailMessage( systemCommunication );
                    message.AddRecipient( new RockEmailMessageRecipient( person, mergeFields ) );
                    message.AppRoot = "/";
                    message.CreateCommunicationRecord = true;

                    if ( message.Send( out var sendErrors ) )
                    {
                        statement.RequestStatus = StatementRequestStatus.Reminded;
                        statement.ModifiedDateTime = RockDateTime.Now;
                        sentCount++;
                    }
                    else
                    {
                        errors.AddRange( sendErrors );
                    }
                }

                detectiveContext.SaveChanges();
            }

            Result = $"Sent {sentCount} reminder{( sentCount == 1 ? string.Empty : "s" )}."
                + ( errors.Any() ? $" {errors.Count} failed: {errors.Take( 3 ).ToList().AsDelimited( " " )}" : string.Empty );
        }
    }
}
