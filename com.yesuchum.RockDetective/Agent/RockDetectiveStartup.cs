using System;
using System.Linq;

using Rock.Data;
using Rock.Utility;
using Rock.Web.Cache;

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
                RestoreSkillAssemblyName();
            }
            catch ( Exception ex )
            {
                Rock.Model.ExceptionLogService.LogException( new Exception( "Rock Detective could not verify its agent skill registration.", ex ) );
            }
        }

        private static void RestoreSkillAssemblyName()
        {
            var skillType = typeof( RockDetectiveSkill );
            int? entityTypeId = null;

            using ( var rockContext = new RockContext() )
            {
                var entityType = new Rock.Model.EntityTypeService( rockContext )
                    .Queryable()
                    .FirstOrDefault( e => e.Name == skillType.FullName );

                if ( entityType == null || entityType.AssemblyName == skillType.AssemblyQualifiedName )
                {
                    return;
                }

                entityType.AssemblyName = skillType.AssemblyQualifiedName;
                rockContext.SaveChanges();

                entityTypeId = entityType.Id;
            }

            EntityTypeCache.Remove( entityTypeId.Value );
        }
    }
}
