using Rock.Plugin;

namespace com.yesuchum.RockDetective.Migrations
{
    [MigrationNumber( 6, "19.7" )]
    public class _006_AddRoleAccess : Migration
    {
        private const string CaseFileEntityName = "com.yesuchum.RockDetective.Model.CaseFile";

        private const string AuthPageRoleView = "3E6A1C52-9B47-4D08-A6F3-51C2E8D9B704";

        private const string AuthCaseDenyOthersView = "8F4D2B19-6C3E-4A75-B1D0-72E5A9C3F816";

        private const int AllUsers = 1;

        public override void Up()
        {
            RockMigrationHelper.AddSecurityAuthForPage( SystemGuid.Page.ROCK_DETECTIVE, 0, Rock.Security.Authorization.VIEW, true, SystemGuid.Group.ROCK_DETECTIVE_USERS, 0, AuthPageRoleView );

            RockMigrationHelper.AddSecurityAuthForEntityType( CaseFileEntityName, 5, Rock.Security.Authorization.VIEW, false, null, AllUsers, AuthCaseDenyOthersView );

            Sql( $@"
                UPDATE [a]
                SET [a].[DefaultValue] = 'True'
                FROM [Attribute] AS [a]
                INNER JOIN [BlockType] AS [bt] ON CAST( [bt].[Id] AS NVARCHAR(20) ) = [a].[EntityTypeQualifierValue]
                WHERE [a].[Key] = 'RestrictPhaseJumping'
                    AND [a].[EntityTypeQualifierColumn] = 'BlockTypeId'
                    AND [bt].[Guid] = '{SystemGuid.BlockType.CASE_FILE_DETAIL}'" );
        }

        public override void Down()
        {
            RockMigrationHelper.DeleteSecurityAuth( AuthCaseDenyOthersView );
            RockMigrationHelper.DeleteSecurityAuth( AuthPageRoleView );
        }
    }
}
