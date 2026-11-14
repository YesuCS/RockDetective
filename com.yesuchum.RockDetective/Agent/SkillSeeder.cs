using System;
using System.Collections.Generic;
using System.Linq;

using Rock;
using Rock.Data;
using Rock.Enums.AI.Agent;

namespace com.yesuchum.RockDetective.Agent
{
    public enum SkillToolState
    {
        Standard = 0,
        Edited = 1,
        Missing = 2
    }

    public class SkillToolStatus
    {
        public Guid Guid { get; set; }

        public string Name { get; set; }

        public SkillToolState State { get; set; }
    }

    public static class SkillSeeder
    {
        private class StoredParameter
        {
            public string Name { get; set; }

            public int DataType { get; set; }

            public string DefaultValue { get; set; }

            public string Instructions { get; set; }

            public bool IsCollection { get; set; }

            public bool IsRequired { get; set; }

            public List<string> AllowedValues { get; set; }
        }

        private class StoredPrompt
        {
            public string Prompt { get; set; }

            public List<StoredParameter> PromptParameters { get; set; } = new List<StoredParameter>();
        }

        private class StoredInstructions
        {
            public List<string> Purposes { get; set; } = new List<string>();

            public List<string> Usages { get; set; } = new List<string>();
        }

        private class SeedState
        {
            public List<Guid> SeededTools { get; set; } = new List<Guid>();
        }

        private const string SeedStateKey = "RockDetectiveSeedSettings";

        public static void EnsureSkill()
        {
            EnsureSkill( false );
        }

        private static void EnsureSkill( bool isAddingBackRemovedTools )
        {
            using ( var rockContext = new RockContext() )
            {
                var skillService = new Rock.Model.AISkillService( rockContext );
                var toolService = new Rock.Model.AISkillToolService( rockContext );
                var skill = skillService.Get( SkillToolCatalog.SkillGuid.AsGuid() );
                var isNewOrConverted = false;

                if ( skill == null )
                {
                    skill = new Rock.Model.AISkill { Guid = SkillToolCatalog.SkillGuid.AsGuid() };
                    skillService.Add( skill );
                    isNewOrConverted = true;
                }
                else if ( skill.CodeEntityTypeId.HasValue )
                {
                    skill.CodeEntityTypeId = null;
                    isNewOrConverted = true;
                }

                if ( isNewOrConverted )
                {
                    ApplySkillDefaults( skill );
                    rockContext.SaveChanges();
                }

                var existingTools = toolService.Queryable().Where( t => t.AISkillId == skill.Id ).ToList();
                var seedState = skill.GetAdditionalSettings<SeedState>( SeedStateKey );
                var seededTools = seedState.SeededTools ?? new List<Guid>();
                var isSeedStateChanged = false;

                foreach ( var toolDefault in SkillToolCatalog.Defaults )
                {
                    var tool = existingTools.FirstOrDefault( t => t.Guid == toolDefault.Guid );
                    var wasSeededBefore = seededTools.Contains( toolDefault.Guid );

                    if ( tool == null && ( !wasSeededBefore || isAddingBackRemovedTools ) )
                    {
                        tool = new Rock.Model.AISkillTool { Guid = toolDefault.Guid, AISkillId = skill.Id };
                        toolService.Add( tool );
                        ApplyToolDefaults( tool, toolDefault );
                    }
                    else if ( tool != null && tool.ToolType == ToolType.ExecuteCode )
                    {
                        ApplyToolDefaults( tool, toolDefault );
                    }

                    if ( !wasSeededBefore )
                    {
                        seededTools.Add( toolDefault.Guid );
                        isSeedStateChanged = true;
                    }
                }

                if ( isSeedStateChanged )
                {
                    skill.SetAdditionalSettings( SeedStateKey, new SeedState { SeededTools = seededTools } );
                }

                rockContext.SaveChanges();
            }
        }

        public static List<SkillToolStatus> GetStatus()
        {
            using ( var rockContext = new RockContext() )
            {
                var skill = new Rock.Model.AISkillService( rockContext ).Get( SkillToolCatalog.SkillGuid.AsGuid() );
                var tools = skill == null
                    ? new List<Rock.Model.AISkillTool>()
                    : new Rock.Model.AISkillToolService( rockContext ).Queryable().Where( t => t.AISkillId == skill.Id ).ToList();

                return SkillToolCatalog.Defaults.Select( toolDefault =>
                {
                    var tool = tools.FirstOrDefault( t => t.Guid == toolDefault.Guid );

                    return new SkillToolStatus
                    {
                        Guid = toolDefault.Guid,
                        Name = tool?.Name ?? toolDefault.Name,
                        State = tool == null ? SkillToolState.Missing : ( IsStandard( tool, toolDefault ) ? SkillToolState.Standard : SkillToolState.Edited )
                    };
                } ).ToList();
            }
        }

        public static int CountAddedTools()
        {
            using ( var rockContext = new RockContext() )
            {
                var skill = new Rock.Model.AISkillService( rockContext ).Get( SkillToolCatalog.SkillGuid.AsGuid() );

                if ( skill == null )
                {
                    return 0;
                }

                var standardGuids = SkillToolCatalog.Defaults.Select( d => d.Guid ).ToList();

                return new Rock.Model.AISkillToolService( rockContext ).Queryable().Count( t => t.AISkillId == skill.Id && !standardGuids.Contains( t.Guid ) );
            }
        }

        public static bool RestoreTool( Guid toolGuid )
        {
            var toolDefault = SkillToolCatalog.Defaults.FirstOrDefault( d => d.Guid == toolGuid );

            if ( toolDefault == null )
            {
                return false;
            }

            EnsureSkill( false );

            using ( var rockContext = new RockContext() )
            {
                var skill = new Rock.Model.AISkillService( rockContext ).Get( SkillToolCatalog.SkillGuid.AsGuid() );

                if ( skill == null )
                {
                    return false;
                }

                var toolService = new Rock.Model.AISkillToolService( rockContext );
                var tool = toolService.Get( toolGuid );

                if ( tool == null )
                {
                    tool = new Rock.Model.AISkillTool { Guid = toolGuid, AISkillId = skill.Id };
                    toolService.Add( tool );
                }
                else if ( tool.AISkillId != skill.Id )
                {
                    return false;
                }

                ApplyToolDefaults( tool, toolDefault );
                rockContext.SaveChanges();
            }

            return true;
        }

        public static void RestoreAll()
        {
            EnsureSkill( true );

            using ( var rockContext = new RockContext() )
            {
                var skill = new Rock.Model.AISkillService( rockContext ).Get( SkillToolCatalog.SkillGuid.AsGuid() );

                if ( skill == null )
                {
                    return;
                }

                ApplySkillDefaults( skill );

                var tools = new Rock.Model.AISkillToolService( rockContext ).Queryable().Where( t => t.AISkillId == skill.Id ).ToList();

                foreach ( var toolDefault in SkillToolCatalog.Defaults )
                {
                    var tool = tools.FirstOrDefault( t => t.Guid == toolDefault.Guid );

                    if ( tool != null )
                    {
                        ApplyToolDefaults( tool, toolDefault );
                    }
                }

                rockContext.SaveChanges();
            }
        }

        private static void ApplySkillDefaults( Rock.Model.AISkill skill )
        {
            skill.Name = SkillToolCatalog.SkillName;
            skill.Description = SkillWording.GetDefault( "Skill.Description" );
            skill.SetAdditionalSettings( "SkillInstructionSettings", new
            {
                Purposes = new List<string> { SkillWording.GetDefault( "Skill.Purpose" ) },
                Usages = SkillWording.GetDefault( "Skill.Usage" ).Split( '\n' ).ToList(),
                Guardrails = SkillWording.GetDefault( "Skill.Guardrails" ).Split( '\n' ).ToList()
            } );
        }

        private static void ApplyToolDefaults( Rock.Model.AISkillTool tool, SkillToolDefault toolDefault )
        {
            tool.Name = toolDefault.Name;
            tool.Description = toolDefault.Description;
            tool.ToolType = ToolType.ExecuteLava;

            tool.SetAdditionalSettings( "ToolAdditionalSettings", new { Preamble = string.Empty } );

            tool.SetAdditionalSettings( "ToolInstructionSettings", new
            {
                Purposes = new List<string> { toolDefault.Purpose },
                ReturnDescription = "A JSON object. When the tool could not do what was asked it returns refused true and a reason that says what is needed.",
                Usages = new List<string> { toolDefault.Description },
                Guardrails = new List<string>(),
                Prerequisites = new List<string>(),
                Examples = new List<string>()
            } );

            tool.SetAdditionalSettings( "PromptInformationSettings", new
            {
                PreRenderLava = false,
                Temperature = 0.7M,
                MaxTokens = ( int? ) null,
                Prompt = toolDefault.Lava,
                PromptParameters = toolDefault.Parameters
            } );
        }

        private static bool IsStandard( Rock.Model.AISkillTool tool, SkillToolDefault toolDefault )
        {
            if ( tool.ToolType != ToolType.ExecuteLava || tool.Name != toolDefault.Name || Normalize( tool.Description ) != Normalize( toolDefault.Description ) )
            {
                return false;
            }

            var prompt = tool.GetAdditionalSettings<StoredPrompt>( "PromptInformationSettings" );
            var instructions = tool.GetAdditionalSettings<StoredInstructions>( "ToolInstructionSettings" );

            if ( Normalize( prompt.Prompt ) != Normalize( toolDefault.Lava ) )
            {
                return false;
            }

            if ( Normalize( string.Join( "|", ( instructions.Purposes ?? new List<string>() ).Where( p => p.IsNotNullOrWhiteSpace() ) ) ) != Normalize( toolDefault.Purpose )
                || Normalize( string.Join( "|", instructions.Usages ?? new List<string>() ) ) != Normalize( toolDefault.Description ) )
            {
                return false;
            }

            var stored = prompt.PromptParameters ?? new List<StoredParameter>();

            if ( stored.Count != toolDefault.Parameters.Count )
            {
                return false;
            }

            for ( var i = 0; i < stored.Count; i++ )
            {
                var a = stored[i];
                var b = toolDefault.Parameters[i];

                if ( a.Name != b.Name
                    || a.DataType != b.DataType
                    || a.IsRequired != b.IsRequired
                    || a.IsCollection != b.IsCollection
                    || Normalize( a.DefaultValue ) != Normalize( b.DefaultValue )
                    || Normalize( a.Instructions ) != Normalize( b.Instructions )
                    || Normalize( string.Join( "|", a.AllowedValues ?? new List<string>() ) ) != Normalize( string.Join( "|", b.AllowedValues ?? new List<string>() ) ) )
                {
                    return false;
                }
            }

            return true;
        }

        private static string Normalize( string value )
        {
            return ( value ?? string.Empty ).Replace( "\r\n", "\n" ).Trim();
        }
    }
}
