using System;

using Rock.Utility;

namespace com.yesuchum.RockDetective.Agent
{
    public class RockDetectiveStartup : IRockStartup
    {
        public int StartupOrder
        {
            get
            {
                return 0;
            }
        }

        public void OnStartup()
        {
            try
            {
                SkillSeeder.EnsureSkill();
            }
            catch ( Exception ex )
            {
                Rock.Model.ExceptionLogService.LogException( new Exception( "Rock Detective could not set up its AI skill. The Skill Settings page can add the standard tools once Rock is running.", ex ) );
            }
        }
    }
}
