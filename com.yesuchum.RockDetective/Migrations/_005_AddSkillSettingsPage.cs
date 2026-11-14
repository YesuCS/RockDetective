using Rock.Plugin;

namespace com.yesuchum.RockDetective.Migrations
{
    [MigrationNumber( 5, "19.7" )]
    public class _005_AddSkillSettingsPage : Migration
    {
        private const string InternalFullWidthLayout = "D65F783D-87A9-4CC9-8110-E83466A0EADB";

        private const string SkillSettingsBlock = "5F3C2A91-7B4D-4E86-A1C2-9D0E6F7B8A13";

        public override void Up()
        {
            RockMigrationHelper.UpdateEntityType( "com.yesuchum.RockDetective.Blocks.SkillSettings", "Skill Settings", "com.yesuchum.RockDetective", false, false, SystemGuid.EntityType.SKILL_SETTINGS_BLOCK );

            RockMigrationHelper.AddOrUpdateEntityBlockType(
                "Skill Settings",
                "Shows the tools in the Rock Detective AI skill, which ones have been edited, and lets an administrator put them back to standard.",
                "com.yesuchum.RockDetective.Blocks.SkillSettings",
                "Rock Detective",
                SystemGuid.BlockType.SKILL_SETTINGS );

            RockMigrationHelper.AddPage( true, SystemGuid.Page.ROCK_DETECTIVE, InternalFullWidthLayout,
                "Skill Settings",
                "See the tools in the Rock Detective AI skill and put them back to standard.",
                SystemGuid.Page.SKILL_SETTINGS, "ti ti-robot" );

            RockMigrationHelper.AddBlock( true, SystemGuid.Page.SKILL_SETTINGS, null, SystemGuid.BlockType.SKILL_SETTINGS,
                "Skill Settings", "Main", string.Empty, string.Empty, 0, SkillSettingsBlock );
        }

        public override void Down()
        {
            RockMigrationHelper.DeleteBlock( SkillSettingsBlock );
            RockMigrationHelper.DeleteBlockType( SystemGuid.BlockType.SKILL_SETTINGS );
            RockMigrationHelper.DeletePage( SystemGuid.Page.SKILL_SETTINGS );
            RockMigrationHelper.DeleteEntityType( SystemGuid.EntityType.SKILL_SETTINGS_BLOCK );
        }
    }
}
