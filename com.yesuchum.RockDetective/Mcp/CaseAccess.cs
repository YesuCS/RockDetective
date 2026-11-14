using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

using com.yesuchum.RockDetective.Data;
using com.yesuchum.RockDetective.Enums;

namespace com.yesuchum.RockDetective.Mcp
{
    public enum ToolAudience
    {
        Everyone = 0,
        Investigator = 1,
        Administrator = 2
    }

    public static class CaseAccess
    {
        public const string WitnessOnlyMessage = "This tool is for people who work cases. If you were asked for a statement, use the statement request tools to see what you were asked and to answer it.";

        public static bool IsInvestigator( Rock.Model.Person person )
        {
            if ( person == null )
            {
                return false;
            }

            return Rock.Security.Authorization.Authorized( new Model.CaseFile(), Rock.Security.Authorization.EDIT, person );
        }

        public static bool IsAdministrator( Rock.Model.Person person )
        {
            return person != null && new Rock.Security.GlobalDefault().IsAuthorized( Rock.Security.Authorization.ADMINISTRATE, person );
        }

        public static bool IsInAudience( ToolAudience audience, Rock.Model.Person person )
        {
            switch ( audience )
            {
                case ToolAudience.Administrator:
                    return IsAdministrator( person );
                case ToolAudience.Investigator:
                    return IsInvestigator( person );
                default:
                    return person != null;
            }
        }

        public static void RequireInvestigator( Rock.Model.Person person )
        {
            if ( !IsInvestigator( person ) )
            {
                throw new McpToolException( WitnessOnlyMessage );
            }
        }

        public static Dictionary<int, List<ParticipantRole>> GetRolesByCase( RockDetectiveContext detectiveContext, Rock.Model.Person person )
        {
            var aliasIds = person.Aliases.Select( a => a.Id ).ToList();

            return detectiveContext.CaseParticipants
                .AsNoTracking()
                .Where( p => p.PersonAliasId.HasValue && aliasIds.Contains( p.PersonAliasId.Value ) )
                .Select( p => new { p.CaseFileId, p.Role } )
                .ToList()
                .GroupBy( p => p.CaseFileId )
                .ToDictionary( g => g.Key, g => g.Select( p => p.Role ).ToList() );
        }

        public static bool CanReadCase( Model.CaseFile caseFile, Rock.Model.Person person, Dictionary<int, List<ParticipantRole>> rolesByCase )
        {
            if ( person == null )
            {
                return false;
            }

            if ( person.Aliases.Any( a => a.Id == caseFile.OwnerPersonAliasId ) )
            {
                return true;
            }

            if ( rolesByCase.TryGetValue( caseFile.Id, out var roles ) && roles.Any( r => r != ParticipantRole.Witness ) )
            {
                return true;
            }

            return IsInvestigator( person ) && Rock.Security.Authorization.Authorized( caseFile, Rock.Security.Authorization.VIEW, person );
        }
    }
}
