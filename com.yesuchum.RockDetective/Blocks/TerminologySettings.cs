using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using Rock;
using Rock.Attribute;
using Rock.Blocks;
using Rock.Web.Cache;

using com.yesuchum.RockDetective.Enums;
using com.yesuchum.RockDetective.Method;
using com.yesuchum.RockDetective.ViewModels;

namespace com.yesuchum.RockDetective.Blocks
{
    [DisplayName( "Terminology Settings" )]
    [Category( "Rock Detective" )]
    [Description( "Lets an administrator rename Rock Detective phases, terms and gate wording for all cases or for one case category. Only people with Administrate on the block can see or change it." )]
    [IconCssClass( "ti ti-language" )]
    [SupportedSiteTypes( Rock.Model.SiteType.Web )]

    [Rock.SystemGuid.EntityTypeGuid( SystemGuid.EntityType.TERMINOLOGY_SETTINGS_BLOCK )]
    [Rock.SystemGuid.BlockTypeGuid( SystemGuid.BlockType.TERMINOLOGY_SETTINGS )]
    public class TerminologySettings : RockBlockType
    {
        private static readonly Dictionary<string, string> _termLabels = new Dictionary<string, string>
        {
            [TermKey.CaseFile] = "Case",
            [TermKey.Owner] = "Owner (role)",
            [TermKey.Champion] = "Champion (role)",
            [TermKey.Recorder] = "Recorder (role)",
            [TermKey.Communicator] = "Communicator (role)",
            [TermKey.Member] = "Member (role)",
            [TermKey.Witness] = "Witness (role)",
            [TermKey.Fact] = "Fact",
            [TermKey.TimelineEvent] = "Timeline Event",
            [TermKey.Evidence] = "Evidence",
            [TermKey.Hypothesis] = "Hypothesis",
            [TermKey.Experiment] = "Experiment",
            [TermKey.Statement] = "Statement"
        };

        private static readonly Dictionary<string, CasePhase> _gatePhases = new Dictionary<string, CasePhase>
        {
            ["Team"] = CasePhase.DefineTeam,
            ["Problem"] = CasePhase.DefineProblem,
            ["Classify"] = CasePhase.Classify,
            ["Timeline"] = CasePhase.Timeline,
            ["Facts"] = CasePhase.FactList,
            ["Hypotheses"] = CasePhase.ProbabilityTree,
            ["Experiments"] = CasePhase.Experiments,
            ["Reevaluate"] = CasePhase.Reevaluate,
            ["Postmortem"] = CasePhase.Postmortem
        };

        public override string ObsidianFileUrl => "~/Plugins/com_yesuchum/RockDetective/terminologySettings.obs";

        private bool IsAdministrator()
        {
            return BlockCache.IsAuthorized( Rock.Security.Authorization.ADMINISTRATE, RequestContext.CurrentPerson );
        }

        public override object GetObsidianBlockInitialization()
        {
            if ( !IsAdministrator() )
            {
                return new TerminologySettingsInitBag();
            }

            return new TerminologySettingsInitBag
            {
                CanEdit = true,
                Categories = GetCategoryOptions(),
                Terms = MethodProfile.DefaultTerms.Select( kv => new TerminologyTermBag
                {
                    Key = kv.Key,
                    Label = _termLabels.ContainsKey( kv.Key ) ? _termLabels[kv.Key] : kv.Key,
                    DefaultSingular = kv.Value.Singular
                } ).ToList(),
                Phases = MethodDefinition.Phases.Select( d => new TerminologyPhaseBag
                {
                    Key = d.Phase.ToString(),
                    Phase = ( int ) d.Phase,
                    DefaultName = d.Name,
                    DefaultGuidance = d.Guidance,
                    DefaultPrompts = d.Prompts
                } ).ToList(),
                Gates = MethodProfile.DefaultGateDescriptions.Select( kv => new TerminologyGateBag
                {
                    Key = kv.Key,
                    PhaseName = MethodDefinition.GetPhase( _gatePhases[kv.Key.Split( '.' )[0]] ).Name,
                    DefaultDescription = kv.Value
                } ).ToList()
            };
        }

        [BlockAction]
        public BlockActionResult GetTerminology( string categoryGuid )
        {
            if ( !IsAdministrator() )
            {
                return ActionForbidden( "You are not authorized to view terminology settings." );
            }

            var json = ReadJson( categoryGuid.AsGuidOrNull(), out var errorMessage );

            if ( errorMessage != null )
            {
                return ActionNotFound( errorMessage );
            }

            var configuration = json.FromJsonOrNull<MethodProfileConfiguration>() ?? new MethodProfileConfiguration();

            return ActionOk( new TerminologyValuesBag
            {
                Terms = ( configuration.Terms ?? new Dictionary<string, TermDefinition>() )
                    .Where( kv => kv.Value != null && kv.Value.Singular.IsNotNullOrWhiteSpace() )
                    .ToDictionary( kv => kv.Key, kv => kv.Value.Singular ),
                Phases = ( configuration.Phases ?? new Dictionary<string, PhaseOverride>() )
                    .Where( kv => kv.Value != null )
                    .ToDictionary( kv => kv.Key, kv => new TerminologyPhaseValueBag { Name = kv.Value.Name, Guidance = kv.Value.Guidance, Prompts = kv.Value.Prompts ?? new List<string>() } ),
                GateDescriptions = configuration.GateDescriptions ?? new Dictionary<string, string>()
            } );
        }

        [BlockAction]
        public BlockActionResult SaveTerminology( string categoryGuid, TerminologyValuesBag bag )
        {
            if ( !IsAdministrator() )
            {
                return ActionForbidden( "You are not authorized to change terminology." );
            }

            bag = bag ?? new TerminologyValuesBag();

            var configuration = new MethodProfileConfiguration();

            foreach ( var term in bag.Terms ?? new Dictionary<string, string>() )
            {
                if ( term.Value.IsNullOrWhiteSpace() || !MethodProfile.DefaultTerms.ContainsKey( term.Key ) )
                {
                    continue;
                }

                configuration.Terms[term.Key] = new TermDefinition
                {
                    Singular = term.Value.Trim().Truncate( 60 )
                };
            }

            foreach ( var phase in bag.Phases ?? new Dictionary<string, TerminologyPhaseValueBag>() )
            {
                if ( phase.Value == null || !Enum.TryParse<CasePhase>( phase.Key, true, out var casePhase ) )
                {
                    continue;
                }

                var prompts = ( phase.Value.Prompts ?? new List<string>() ).Where( p => p.IsNotNullOrWhiteSpace() ).Select( p => p.Trim() ).ToList();

                if ( phase.Value.Name.IsNullOrWhiteSpace() && phase.Value.Guidance.IsNullOrWhiteSpace() && !prompts.Any() )
                {
                    continue;
                }

                configuration.Phases[casePhase.ToString()] = new PhaseOverride
                {
                    Name = phase.Value.Name?.Trim().Truncate( 60 ),
                    Guidance = phase.Value.Guidance?.Trim(),
                    Prompts = prompts.Any() ? prompts : null
                };
            }

            foreach ( var gate in bag.GateDescriptions ?? new Dictionary<string, string>() )
            {
                if ( gate.Value.IsNotNullOrWhiteSpace() && MethodProfile.DefaultGateDescriptions.ContainsKey( gate.Key ) )
                {
                    configuration.GateDescriptions[gate.Key] = gate.Value.Trim();
                }
            }

            var isEmpty = !configuration.Terms.Any() && !configuration.Phases.Any() && !configuration.GateDescriptions.Any();
            var json = isEmpty ? string.Empty : configuration.ToJson();

            var errorMessage = WriteJson( categoryGuid.AsGuidOrNull(), json );

            if ( errorMessage != null )
            {
                return ActionNotFound( errorMessage );
            }

            return ActionOk();
        }

        private List<CategoryOptionBag> GetCategoryOptions()
        {
            var entityTypeId = EntityTypeCache.Get( SystemGuid.EntityType.CASE_FILE.AsGuid() )?.Id;

            if ( !entityTypeId.HasValue )
            {
                return new List<CategoryOptionBag>();
            }

            using ( var rockContext = new Rock.Data.RockContext() )
            {
                return new Rock.Model.CategoryService( rockContext )
                    .Queryable()
                    .Where( c => c.EntityTypeId == entityTypeId.Value )
                    .OrderBy( c => c.Order )
                    .ThenBy( c => c.Name )
                    .ToList()
                    .Select( c => new CategoryOptionBag { Guid = c.Guid.ToString(), Name = c.Name, IconCssClass = c.IconCssClass } )
                    .ToList();
            }
        }

        private string ReadJson( Guid? categoryGuid, out string errorMessage )
        {
            errorMessage = null;

            if ( !categoryGuid.HasValue )
            {
                return GlobalAttributesCache.Get().GetValue( MethodProfile.DefaultAttributeKey );
            }

            using ( var rockContext = new Rock.Data.RockContext() )
            {
                var category = GetCaseCategory( rockContext, categoryGuid.Value );

                if ( category == null )
                {
                    errorMessage = "Category not found.";
                    return null;
                }

                category.LoadAttributes( rockContext );

                return category.GetAttributeValue( MethodProfile.CategoryAttributeKey );
            }
        }

        private string WriteJson( Guid? categoryGuid, string json )
        {
            if ( !categoryGuid.HasValue )
            {
                GlobalAttributesCache.Get().SetValue( MethodProfile.DefaultAttributeKey, json, true );
                return null;
            }

            using ( var rockContext = new Rock.Data.RockContext() )
            {
                var category = GetCaseCategory( rockContext, categoryGuid.Value );

                if ( category == null )
                {
                    return "Category not found.";
                }

                category.LoadAttributes( rockContext );
                category.SetAttributeValue( MethodProfile.CategoryAttributeKey, json );
                category.SaveAttributeValues( rockContext );

                CategoryCache.Remove( category.Id );
            }

            return null;
        }

        private static Rock.Model.Category GetCaseCategory( Rock.Data.RockContext rockContext, Guid categoryGuid )
        {
            var entityTypeId = EntityTypeCache.Get( SystemGuid.EntityType.CASE_FILE.AsGuid() )?.Id;
            var category = new Rock.Model.CategoryService( rockContext ).Get( categoryGuid );

            return category != null && category.EntityTypeId == entityTypeId ? category : null;
        }
    }
}
