using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

using Rock;
using Rock.Attribute;
using Rock.Blocks;
using Rock.Data;

using com.yesuchum.RockDetective.Agent;
using com.yesuchum.RockDetective.ViewModels;

namespace com.yesuchum.RockDetective.Blocks
{
    [DisplayName( "Skill Settings" )]
    [Category( "Rock Detective" )]
    [Description( "Lets an administrator edit the wording an AI assistant reads for the Rock Detective skill and each of its tools. Only people with Administrate on the block can see or change it." )]
    [IconCssClass( "ti ti-robot" )]
    [SupportedSiteTypes( Rock.Model.SiteType.Web )]

    [Rock.SystemGuid.EntityTypeGuid( SystemGuid.EntityType.SKILL_SETTINGS_BLOCK )]
    [Rock.SystemGuid.BlockTypeGuid( SystemGuid.BlockType.SKILL_SETTINGS )]
    public class SkillSettings : RockBlockType
    {
        private const string SkillGuid = "2C5BE126-3BEC-4B15-A2F4-80FD685ABE84";

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
                Items = SkillWording.Items.Select( i => new SkillWordingItemBag { Key = i.Key, Group = i.Group, Label = i.Label, Default = i.Default } ).ToList(),
                Values = SkillWording.GetOverrides()
            };
        }

        [BlockAction]
        public BlockActionResult SaveWording( Dictionary<string, string> values )
        {
            if ( !IsAdministrator() )
            {
                return ActionForbidden( "You are not authorized to change the skill wording." );
            }

            SkillWording.SaveOverrides( values );

            var isApplied = ApplyToRockAgents();

            return ActionOk( new SkillSettingsSaveResultBag
            {
                Values = SkillWording.GetOverrides(),
                IsAppliedToRockAgents = isApplied
            } );
        }

        private static bool ApplyToRockAgents()
        {
            try
            {
                using ( var rockContext = new RockContext() )
                {
                    var skill = new Rock.Model.AISkillService( rockContext ).Get( SkillGuid.AsGuid() );

                    if ( skill == null )
                    {
                        return false;
                    }

                    skill.Description = SkillWording.Get( "Skill.Description" );
                    skill.SetAdditionalSettings( "SkillInstructionSettings", new
                    {
                        Purposes = new List<string> { SkillWording.Get( "Skill.Purpose" ) },
                        Usages = new List<string> { SkillWording.Get( "Skill.Usage" ) },
                        Guardrails = new List<string> { SkillWording.Get( "Skill.Guardrails" ) }
                    } );

                    var methodsByToolGuid = typeof( RockDetectiveSkill )
                        .GetMethods( BindingFlags.Public | BindingFlags.Instance )
                        .Select( m => new { Method = m, Guid = m.GetCustomAttribute<Rock.SystemGuid.AgentToolGuidAttribute>()?.Guid } )
                        .Where( m => m.Guid.HasValue )
                        .ToDictionary( m => m.Guid.Value, m => m.Method.Name );

                    var tools = new Rock.Model.AISkillToolService( rockContext ).Queryable().Where( t => t.AISkillId == skill.Id ).ToList();

                    foreach ( var tool in tools )
                    {
                        if ( !methodsByToolGuid.TryGetValue( tool.Guid, out var methodName ) )
                        {
                            continue;
                        }

                        tool.Description = SkillWording.Get( methodName + ".Description" );
                        tool.SetAdditionalSettings( "ToolInstructionSettings", new
                        {
                            Purposes = new List<string> { SkillWording.Get( methodName + ".Purpose" ) },
                            ReturnDescription = ( string ) null,
                            Usages = new List<string> { SkillWording.Get( methodName + ".Description" ) },
                            Guardrails = new List<string>(),
                            Prerequisites = new List<string>(),
                            Examples = new List<string>()
                        } );
                    }

                    rockContext.SaveChanges();
                }

                return true;
            }
            catch ( Exception ex )
            {
                Rock.Model.ExceptionLogService.LogException( new Exception( "Rock Detective saved its skill wording but could not apply it to Rock's AI agents right away. It will apply at the next Rock restart.", ex ) );

                return false;
            }
        }
    }
}
