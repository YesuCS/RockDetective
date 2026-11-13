using Rock.Plugin;

namespace com.yesuchum.RockDetective.Migrations
{
    [MigrationNumber( 2, "18.2" )]
    public class _002_AddIisLogPathSetting : Migration
    {
        private const string TextFieldType = "9C204CD0-1233-41C5-818A-C5DA439445AA";

        private const string IisLogPathAttribute = "E2B5B0F4-6F0C-4E0B-9C57-3D2F4A1B8C76";

        public override void Up()
        {
            RockMigrationHelper.AddGlobalAttribute(
                TextFieldType,
                string.Empty,
                string.Empty,
                "Rock Detective IIS Log Folder",
                "The folder on the web server that Rock Detective reads IIS log files from, including its subfolders. Leave blank to use the standard location (%SystemDrive%\\inetpub\\logs\\LogFiles). The application pool identity needs read access to this folder.",
                0,
                string.Empty,
                IisLogPathAttribute,
                "com_yesuchum_RockDetective_IisLogPath",
                false );
        }

        public override void Down()
        {
            RockMigrationHelper.DeleteAttribute( IisLogPathAttribute );
        }
    }
}
