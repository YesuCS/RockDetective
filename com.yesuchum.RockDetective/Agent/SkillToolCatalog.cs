using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Newtonsoft.Json.Linq;

using com.yesuchum.RockDetective.Mcp;

namespace com.yesuchum.RockDetective.Agent
{
    public class SkillParameterDefault
    {
        public string Name { get; set; }

        public int DataType { get; set; }

        public string DefaultValue { get; set; }

        public string Instructions { get; set; }

        public bool IsCollection { get; set; }

        public bool IsRequired { get; set; }

        public List<string> AllowedValues { get; set; }
    }

    public class SkillToolDefault
    {
        public Guid Guid { get; set; }

        public string Key { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Purpose { get; set; }

        public string Lava { get; set; }

        public List<SkillParameterDefault> Parameters { get; set; } = new List<SkillParameterDefault>();
    }

    public static class SkillToolCatalog
    {
        public const string SkillGuid = "2C5BE126-3BEC-4B15-A2F4-80FD685ABE84";

        public const string SkillName = "Rock Detective";

        private const int StringType = 0;
        private const int NumberType = 1;
        private const int BooleanType = 2;

        private static readonly Dictionary<string, string> _toolGuids = new Dictionary<string, string>
        {
            ["list_cases"] = "3BC9DDB1-7E2B-49DB-AC7B-4BC7FCEB3302",
            ["get_case"] = "94C24041-4E24-4863-91A3-A9961101B8E4",
            ["search_iis_logs"] = "125ED239-935B-4013-84A4-D747F42DED48",
            ["list_my_statement_requests"] = "6B40FC5E-8D1D-4E3A-9C47-2F1D3C4B5A61",
            ["submit_statement"] = "7C510D6F-9E2E-4F4B-8D58-302E4D5C6B72",
            ["decline_statement"] = "8D621E70-AF3F-405C-9E69-413F5E6D7C83",
            ["list_categories"] = "6A2F0C5E-8B1D-4E3A-9C47-1F0D2B3A4C51",
            ["create_case"] = "7B301D6F-9C2E-4F4B-8D58-201E3C4B5D62",
            ["update_case"] = "8C412E70-AD3F-405C-9E69-312F4D5C6E73",
            ["add_participant"] = "9D523F81-BE40-416D-AF7A-42305E6D7F84",
            ["waive_champion"] = "AE634092-CF51-427E-B08B-534160708095",
            ["record_statement"] = "BF7451A3-D062-438F-819C-6452718190A6",
            ["add_entry"] = "C08562B4-E173-4490-92AD-75638292A1B7",
            ["add_hypothesis"] = "D19673C5-F284-45A1-A3BE-867493A3B2C8",
            ["update_hypothesis"] = "E2A784D6-0395-46B2-B4CF-9785A4B4C3D9",
            ["add_experiment"] = "F3B895E7-14A6-47C3-85D0-A896B5C5D4EA",
            ["record_experiment_result"] = "04C9A6F8-25B7-48D4-96E1-B9A7C6D6E5FB",
            ["save_phase_notes"] = "15DAB709-36C8-49E5-A7F2-CAB8D7E7F60C",
            ["complete_phase"] = "26EBC81A-47D9-4AF6-B803-DBC9E8F8071D",
            ["loop_back"] = "37FCD92B-58EA-4B07-8914-ECDAF909182E",
            ["champion_sign_off"] = "480DEA3C-69FB-4C18-9A25-FDEB0A1A293F",
            ["close_case"] = "591EFB4D-7A0C-4D29-AB36-0EFC1B2B3A40"
        };

        private static readonly Lazy<List<SkillToolDefault>> _defaults = new Lazy<List<SkillToolDefault>>( Build );

        public static IReadOnlyList<SkillToolDefault> Defaults => _defaults.Value;

        public static string ToWordingKey( string toolKey )
        {
            return string.Concat( toolKey.Split( '_' ).Select( part => char.ToUpperInvariant( part[0] ) + part.Substring( 1 ) ) );
        }

        private static List<SkillToolDefault> Build()
        {
            var defaults = new List<SkillToolDefault>();

            foreach ( var tool in McpToolService.AllTools )
            {
                if ( !_toolGuids.TryGetValue( tool.Name, out var guid ) )
                {
                    continue;
                }

                var wordingKey = ToWordingKey( tool.Name );
                var nameItem = SkillWording.Items.FirstOrDefault( i => i.Key == wordingKey + ".Description" );
                var parameters = BuildParameters( tool.InputSchema );

                defaults.Add( new SkillToolDefault
                {
                    Guid = new Guid( guid ),
                    Key = tool.Name,
                    Name = nameItem?.Group ?? wordingKey,
                    Description = SkillWording.GetDefault( wordingKey + ".Description" ),
                    Purpose = SkillWording.GetDefault( wordingKey + ".Purpose" ),
                    Parameters = parameters,
                    Lava = BuildLava( tool.Name, parameters )
                } );
            }

            return defaults;
        }

        private static List<SkillParameterDefault> BuildParameters( JObject schema )
        {
            var required = ( schema["required"] as JArray )?.Select( r => r.ToString() ).ToList() ?? new List<string>();
            var properties = schema["properties"] as JObject ?? new JObject();
            var parameters = new List<SkillParameterDefault>();

            foreach ( var property in properties.Properties() )
            {
                var definition = property.Value as JObject ?? new JObject();
                var type = definition.Value<string>( "type" );
                var dataType = StringType;

                if ( type == "integer" || type == "number" )
                {
                    dataType = NumberType;
                }
                else if ( type == "boolean" )
                {
                    dataType = BooleanType;
                }

                parameters.Add( new SkillParameterDefault
                {
                    Name = property.Name,
                    DataType = dataType,
                    Instructions = definition.Value<string>( "description" ) ?? string.Empty,
                    IsRequired = required.Contains( property.Name ),
                    AllowedValues = ( definition["enum"] as JArray )?.Select( v => v.ToString() ).ToList()
                } );
            }

            return parameters;
        }

        private static string BuildLava( string toolKey, List<SkillParameterDefault> parameters )
        {
            var lava = new StringBuilder();

            lava.Append( "{% rockdetective tool:'" ).Append( toolKey ).Append( "' %}" );

            if ( parameters.Any() )
            {
                lava.Append( "\n{\n" );
                lava.Append( string.Join( ",\n", parameters.Select( p => "  \"" + p.Name + "\": {{ " + p.Name + " | ToJSON }}" ) ) );
                lava.Append( "\n}\n" );
            }

            lava.Append( "{% endrockdetective %}" );

            return lava.ToString();
        }
    }
}
