using Rock.Plugin;

namespace com.yesuchum.RockDetective.Migrations
{
    [MigrationNumber( 3, "19.7" )]
    public class _003_AddTerminologySettings : Migration
    {
        private const string CodeEditorFieldType = "1D0D3794-C210-48A8-8C68-3FBEC08A6BA5";

        private const string CategoryTerminologyAttribute = "5C0E0B5B-7A7E-4E0C-93B5-0B8B1E7D2A41";

        private const string DefaultTerminologyAttribute = "A93F6E1D-2B64-4F3A-8E55-6C1D9F0B7E32";

        private const string TerminologyDescription = "Optional JSON that renames Rock Detective phases, terms and gate wording. Shape: { \"Phases\": { \"FactList\": { \"Name\": \"...\", \"Guidance\": \"...\", \"Prompts\": [ \"...\" ] } }, \"Terms\": { \"Champion\": { \"Singular\": \"...\", \"Plural\": \"...\" } }, \"GateDescriptions\": { \"Facts.Minimum\": \"At least {count} {fact#} recorded.\" } }. Anything left out keeps its standard wording.";

        public override void Up()
        {
            var caseFileEntityTypeId = SqlScalar( $"SELECT [Id] FROM [EntityType] WHERE [Guid] = '{SystemGuid.EntityType.CASE_FILE}'" ).ToString();

            RockMigrationHelper.AddOrUpdateEntityAttribute(
                "Rock.Model.Category",
                CodeEditorFieldType,
                "EntityTypeId",
                caseFileEntityTypeId,
                "Rock Detective Terminology",
                string.Empty,
                "Applies to cases in this category. " + TerminologyDescription,
                0,
                string.Empty,
                CategoryTerminologyAttribute,
                "com_yesuchum_RockDetective_Terminology" );

            RockMigrationHelper.AddGlobalAttribute(
                CodeEditorFieldType,
                string.Empty,
                string.Empty,
                "Rock Detective Default Terminology",
                "Applies to every case unless its category has its own terminology. " + TerminologyDescription,
                0,
                string.Empty,
                DefaultTerminologyAttribute,
                "com_yesuchum_RockDetective_DefaultTerminology",
                false );
        }

        public override void Down()
        {
            RockMigrationHelper.DeleteAttribute( DefaultTerminologyAttribute );
            RockMigrationHelper.DeleteAttribute( CategoryTerminologyAttribute );
        }
    }
}
