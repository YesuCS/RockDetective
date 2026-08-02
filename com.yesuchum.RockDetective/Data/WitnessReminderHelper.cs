using System.Linq;

using Rock;
using Rock.Data;

namespace com.yesuchum.RockDetective.Data
{
    public static class WitnessReminderHelper
    {
        public static void AddOrRefreshReminder( int caseStatementId, int personAliasId, string note )
        {
            using ( var rockContext = new RockContext() )
            {
                var reminderTypeId = GetReminderTypeId( rockContext );

                if ( !reminderTypeId.HasValue )
                {
                    return;
                }

                var reminderService = new Rock.Model.ReminderService( rockContext );
                var existing = reminderService.Queryable()
                    .Where( r => r.ReminderTypeId == reminderTypeId.Value
                        && r.EntityId == caseStatementId
                        && r.PersonAliasId == personAliasId
                        && !r.IsComplete )
                    .FirstOrDefault();

                if ( existing != null )
                {
                    existing.ReminderDate = RockDateTime.Now;
                    existing.Note = note;
                }
                else
                {
                    reminderService.Add( new Rock.Model.Reminder
                    {
                        ReminderTypeId = reminderTypeId.Value,
                        PersonAliasId = personAliasId,
                        EntityId = caseStatementId,
                        ReminderDate = RockDateTime.Now,
                        Note = note,
                        IsComplete = false
                    } );
                }

                rockContext.SaveChanges();
                RecalculateCount( rockContext, personAliasId );
            }
        }

        public static void CompleteReminders( int caseStatementId, int personAliasId )
        {
            using ( var rockContext = new RockContext() )
            {
                var reminderTypeId = GetReminderTypeId( rockContext );

                if ( !reminderTypeId.HasValue )
                {
                    return;
                }

                var reminderService = new Rock.Model.ReminderService( rockContext );
                var reminders = reminderService.Queryable()
                    .Where( r => r.ReminderTypeId == reminderTypeId.Value
                        && r.EntityId == caseStatementId
                        && r.PersonAliasId == personAliasId
                        && !r.IsComplete )
                    .ToList();

                if ( !reminders.Any() )
                {
                    return;
                }

                foreach ( var reminder in reminders )
                {
                    reminder.IsComplete = true;
                }

                rockContext.SaveChanges();
                RecalculateCount( rockContext, personAliasId );
            }
        }

        private static int? GetReminderTypeId( RockContext rockContext )
        {
            var statementEntityType = Rock.Web.Cache.EntityTypeCache.Get( SystemGuid.EntityType.CASE_STATEMENT.AsGuid() );

            if ( statementEntityType == null )
            {
                return null;
            }

            EnsureEntityLinkTemplate( rockContext, statementEntityType.Id );

            var reminderTypeGuid = SystemGuid.ReminderType.WITNESS_STATEMENT.AsGuid();
            var reminderTypeService = new Rock.Model.ReminderTypeService( rockContext );
            var reminderType = reminderTypeService.Queryable().FirstOrDefault( t => t.Guid == reminderTypeGuid );

            if ( reminderType == null )
            {
                reminderType = new Rock.Model.ReminderType
                {
                    Name = "Witness Statement Request",
                    Description = "A Rock Detective case owner has asked you for a witness statement.",
                    EntityTypeId = statementEntityType.Id,
                    IsActive = true,
                    ShouldShowNote = true,
                    ShouldAutoCompleteWhenNotified = false,
                    HighlightColor = "#d97706",
                    Guid = reminderTypeGuid
                };

                reminderTypeService.Add( reminderType );
                rockContext.SaveChanges();
            }
            else if ( reminderType.EntityTypeId != statementEntityType.Id )
            {
                reminderType.EntityTypeId = statementEntityType.Id;
                rockContext.SaveChanges();
            }

            return reminderType.Id;
        }

        private static void EnsureEntityLinkTemplate( RockContext rockContext, int statementEntityTypeId )
        {
            var witnessPage = Rock.Web.Cache.PageCache.Get( SystemGuid.Page.WITNESS_STATEMENT.AsGuid() );

            if ( witnessPage == null )
            {
                return;
            }

            var template = "/page/" + witnessPage.Id + "?Statement={{ Entity.Guid }}";
            var entityType = new Rock.Model.EntityTypeService( rockContext ).Get( statementEntityTypeId );

            if ( entityType != null && entityType.LinkUrlLavaTemplate != template )
            {
                entityType.LinkUrlLavaTemplate = template;
                rockContext.SaveChanges();
                Rock.Web.Cache.EntityTypeCache.Remove( statementEntityTypeId );
            }
        }

        private static void RecalculateCount( RockContext rockContext, int personAliasId )
        {
            var personId = new Rock.Model.PersonAliasService( rockContext ).Get( personAliasId )?.PersonId;

            if ( personId.HasValue )
            {
                new Rock.Model.ReminderService( rockContext ).RecalculateReminderCount( personId.Value );
            }
        }
    }
}
