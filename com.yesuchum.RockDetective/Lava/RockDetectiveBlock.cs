using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Rock;
using Rock.Lava;

using com.yesuchum.RockDetective.Mcp;

namespace com.yesuchum.RockDetective.Lava
{
    public class RockDetectiveBlock : LavaBlockBase
    {
        private const string AgentContextKey = "AgentContext";

        private const string OutsideSkillMessage = "The rockdetective command only works inside a Rock Detective AI skill tool.";

        private string _attributesMarkup;

        public override void OnInitialize( string tagName, string markup, List<string> tokens )
        {
            _attributesMarkup = markup;

            base.OnInitialize( tagName, markup, tokens );
        }

        public override void OnRender( ILavaRenderContext context, TextWriter result )
        {
            if ( !( context.GetMergeField( AgentContextKey ) is Rock.AI.Agent.AgentRequestContext ) )
            {
                result.Write( Refusal( OutsideSkillMessage ) );
                return;
            }

            var settings = LavaElementAttributes.NewFromMarkup( _attributesMarkup, context );
            var toolName = settings.GetString( "tool" );

            if ( toolName.IsNullOrWhiteSpace() )
            {
                result.Write( Refusal( "The rockdetective command needs a tool, for example tool:'list_cases'." ) );
                return;
            }

            string body;

            using ( var bodyWriter = new StringWriter() )
            {
                base.OnRender( context, bodyWriter );
                body = bodyWriter.ToString().Trim();
            }

            JObject arguments;

            try
            {
                arguments = body.IsNullOrWhiteSpace()
                    ? new JObject()
                    : JsonConvert.DeserializeObject<JObject>( body, new JsonSerializerSettings { DateParseHandling = DateParseHandling.None } ) ?? new JObject();

                foreach ( var property in arguments.Properties().Where( p => p.Value.Type == JTokenType.Null ).ToList() )
                {
                    property.Remove();
                }
            }
            catch ( JsonException ex )
            {
                result.Write( Refusal( "The Lava inside the rockdetective command has to produce a JSON object of arguments. " + ex.Message ) );
                return;
            }

            try
            {
                var person = LavaHelper.GetCurrentPerson( context );
                var toolResult = McpToolService.Execute( toolName.Trim(), arguments, person );

                result.Write( toolResult.ToString( Formatting.None ) );
            }
            catch ( McpToolException ex )
            {
                result.Write( Refusal( ex.Message ) );
            }
            catch ( Exception ex )
            {
                Rock.Model.ExceptionLogService.LogException( ex );
                result.Write( Refusal( "An unexpected error occurred while running the tool. It was written to Rock's exception log." ) );
            }
        }

        private static string Refusal( string reason )
        {
            return new JObject
            {
                ["refused"] = true,
                ["reason"] = reason
            }.ToString( Formatting.None );
        }
    }
}
