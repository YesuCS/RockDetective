using System;
using System.ComponentModel;
using System.Linq;

using Rock;
using Rock.Attribute;
using Rock.Blocks;
using Rock.Data;
using Rock.Web.Cache;

using com.yesuchum.RockDetective.Agent;
using com.yesuchum.RockDetective.ViewModels;

namespace com.yesuchum.RockDetective.Blocks
{
    [DisplayName( "Skill Settings" )]
    [Category( "Rock Detective" )]
    [Description( "Shows the tools in the Rock Detective AI skill, which ones have been edited, and lets an administrator put them back to standard. Only people with Administrate on the block can see or change it." )]
    [IconCssClass( "ti ti-robot" )]
    [SupportedSiteTypes( Rock.Model.SiteType.Web )]

    [Rock.SystemGuid.EntityTypeGuid( SystemGuid.EntityType.SKILL_SETTINGS_BLOCK )]
    [Rock.SystemGuid.BlockTypeGuid( SystemGuid.BlockType.SKILL_SETTINGS )]
    public class SkillSettings : RockBlockType
    {
        private const string AiSkillDetailBlockType = "B8B8CEE9-C058-45D3-A1C7-647CAD96FD1E";

        public override string ObsidianFileUrl => "~/Plugins/com_yesuchum/RockDetective/skillSettings.obs";

        private bool IsAdministrator()
        {
            return BlockCache.IsAuthorized( Rock.Security.Authorization.ADMINISTRATE, RequestContext.CurrentPerson );
        }

        public override object GetObsidianBlockInitialization()
        {
            if ( !IsAdministrator() )
            {
                return new SkillSettingsInitBag();
            }

            return new SkillSettingsInitBag
            {
                CanEdit = true,
                SkillUrl = GetSkillUrl(),
                Status = GetStatus()
            };
        }

        [BlockAction]
        public BlockActionResult RestoreTool( string toolGuid )
        {
            if ( !IsAdministrator() )
            {
                return ActionForbidden( "You are not authorized to change the skill." );
            }

            if ( !SkillSeeder.RestoreTool( toolGuid.AsGuid() ) )
            {
                return ActionBadRequest( "That is not one of the standard Rock Detective tools." );
            }

            return ActionOk( GetStatus() );
        }

        [BlockAction]
        public BlockActionResult RestoreAll()
        {
            if ( !IsAdministrator() )
            {
                return ActionForbidden( "You are not authorized to change the skill." );
            }

            SkillSeeder.RestoreAll();

            return ActionOk( GetStatus() );
        }

        private static SkillStatusBag GetStatus()
        {
            var statuses = SkillSeeder.GetStatus();

            return new SkillStatusBag
            {
                AddedToolCount = SkillSeeder.CountAddedTools(),
                Tools = SkillToolCatalog.Defaults.Select( toolDefault =>
                {
                    var status = statuses.First( s => s.Guid == toolDefault.Guid );

                    return new SkillToolStatusBag
                    {
                        Guid = toolDefault.Guid.ToString(),
                        Name = status.Name,
                        StandardName = toolDefault.Name,
                        Description = toolDefault.Description,
                        State = status.State.ToString()
                    };
                } ).ToList()
            };
        }

        private string GetSkillUrl()
        {
            try
            {
                using ( var rockContext = new RockContext() )
                {
                    var skill = new Rock.Model.AISkillService( rockContext ).Get( SkillToolCatalog.SkillGuid.AsGuid() );
                    var blockTypeId = BlockTypeCache.Get( AiSkillDetailBlockType.AsGuid() )?.Id;

                    if ( skill == null || !blockTypeId.HasValue )
                    {
                        return null;
                    }

                    var pageId = new Rock.Model.BlockService( rockContext ).Queryable()
                        .Where( b => b.BlockTypeId == blockTypeId.Value && b.PageId.HasValue )
                        .Select( b => b.PageId )
                        .FirstOrDefault();

                    return pageId.HasValue ? RequestContext.ResolveRockUrl( $"~/page/{pageId.Value}?AISkillId={skill.IdKey}" ) : null;
                }
            }
            catch ( Exception ex )
            {
                Rock.Model.ExceptionLogService.LogException( ex );

                return null;
            }
        }
    }
}
