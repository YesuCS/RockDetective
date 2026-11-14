using Rock.Plugin;

namespace com.yesuchum.RockDetective.Migrations
{
    [MigrationNumber( 5, "19.7" )]
    public class _005_AddSkillSettingsPage : Migration
    {
        private const string InternalFullWidthLayout = "D65F783D-87A9-4CC9-8110-E83466A0EADB";

        private const string CodeEditorFieldType = "1D0D3794-C210-48A8-8C68-3FBEC08A6BA5";

        private const string SkillSettingsBlock = "5F3C2A91-7B4D-4E86-A1C2-9D0E6F7B8A13";

        private const string SkillWordingAttribute = "0B7D4E62-3C9A-4F15-B8E7-2A6C1D9F4E05";

        public override void Up()
        {
            RockMigrationHelper.AddGlobalAttribute(
                CodeEditorFieldType,
                string.Empty,
                string.Empty,
                "Rock Detective Skill Wording",
                "Where the custom wording for the Rock Detective AI skill is stored. Change it on the Rock Detective Skill Settings page, not here.",
                0,
                string.Empty,
                SkillWordingAttribute,
                "com_yesuchum_RockDetective_SkillWording",
                false );

            RockMigrationHelper.UpdateEntityType( "com.yesuchum.RockDetective.Blocks.SkillSettings", "Skill Settings", "com.yesuchum.RockDetective", false, false, SystemGuid.EntityType.SKILL_SETTINGS_BLOCK );

            RockMigrationHelper.AddOrUpdateEntityBlockType(
                "Skill Settings",
                "Lets an administrator edit the wording an AI assistant reads for the Rock Detective skill and each of its tools.",
                "com.yesuchum.RockDetective.Blocks.SkillSettings",
                "Rock Detective",
                SystemGuid.BlockType.SKILL_SETTINGS );

            RockMigrationHelper.AddPage( true, SystemGuid.Page.ROCK_DETECTIVE, InternalFullWidthLayout,
                "Skill Settings",
                "Edit the wording an AI assistant reads for the Rock Detective skill and its tools.",
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
            RockMigrationHelper.DeleteAttribute( SkillWordingAttribute );
        }
    }
}
