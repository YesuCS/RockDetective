using Rock.Plugin;

namespace com.yesuchum.RockDetective.Migrations
{
    [MigrationNumber( 4, "19.7" )]
    public class _004_AddTerminologySettingsPage : Migration
    {
        private const string InternalFullWidthLayout = "D65F783D-87A9-4CC9-8110-E83466A0EADB";

        private const string TerminologySettingsBlock = "7D8BE0E7-D53D-419C-866A-B8AE54F4D1C1";

        public override void Up()
        {
            RockMigrationHelper.UpdateEntityType( "com.yesuchum.RockDetective.Blocks.TerminologySettings", "Terminology Settings", "com.yesuchum.RockDetective", false, false, SystemGuid.EntityType.TERMINOLOGY_SETTINGS_BLOCK );

            RockMigrationHelper.AddOrUpdateEntityBlockType(
                "Terminology Settings",
                "Lets an administrator rename Rock Detective phases, terms and gate wording for all cases or for one case category.",
                "com.yesuchum.RockDetective.Blocks.TerminologySettings",
                "Rock Detective",
                SystemGuid.BlockType.TERMINOLOGY_SETTINGS );

            RockMigrationHelper.AddPage( true, SystemGuid.Page.ROCK_DETECTIVE, InternalFullWidthLayout,
                "Terminology Settings",
                "Rename Rock Detective phases, terms and gate wording to fit your ministry.",
                SystemGuid.Page.TERMINOLOGY_SETTINGS, "ti ti-language" );

            RockMigrationHelper.AddBlock( true, SystemGuid.Page.TERMINOLOGY_SETTINGS, null, SystemGuid.BlockType.TERMINOLOGY_SETTINGS,
                "Terminology Settings", "Main", string.Empty, string.Empty, 0, TerminologySettingsBlock );
        }

        public override void Down()
        {
            RockMigrationHelper.DeleteBlock( TerminologySettingsBlock );
            RockMigrationHelper.DeleteBlockType( SystemGuid.BlockType.TERMINOLOGY_SETTINGS );
            RockMigrationHelper.DeletePage( SystemGuid.Page.TERMINOLOGY_SETTINGS );
            RockMigrationHelper.DeleteEntityType( SystemGuid.EntityType.TERMINOLOGY_SETTINGS_BLOCK );
        }
    }
}
