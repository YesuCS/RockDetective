using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Rock;
using Rock.Rest;
using Rock.Rest.Filters;

using com.yesuchum.RockDetective.Mcp;

namespace com.yesuchum.RockDetective.Rest
{
    [RoutePrefix( "api/com_yesuchum/rockdetective/mcp" )]
    [Rock.SystemGuid.RestControllerGuid( "B7E7E6D2-5A1C-4C86-9D3B-16A6E3DD8290" )]
    public class RockDetectiveMcpController : ApiControllerBase
    {
        private const string ProtocolVersion = "2025-03-26";

        private const string ServerVersion = "1.1.0";

        [HttpGet]
        [Route( "" )]
        [Rock.SystemGuid.RestActionGuid( "4F0D2E7A-93C4-4E8B-8F0A-7C5D1B7A2E61" )]
        public IHttpActionResult GetMcp()
        {
            return StatusCode( HttpStatusCode.MethodNotAllowed );
        }

        [HttpPost]
        [Authenticate, Secured]
        [Route( "" )]
        [Rock.SystemGuid.RestActionGuid( "D7A09A3E-06A1-4F26-8E0C-2B4A9C713F58" )]
        public async Task<IHttpActionResult> PostMcp()
        {
            var person = GetPerson();

            if ( person == null )
            {
                return StatusCode( HttpStatusCode.Unauthorized );
            }

            var body = await Request.Content.ReadAsStringAsync();

            JObject request;

            try
            {
                request = JObject.Parse( body );
            }
            catch ( JsonException )
            {
                return JsonRpcResult( ErrorResponse( null, -32700, "The request body could not be parsed as JSON." ) );
            }

            var id = request["id"];
            var method = request.Value<string>( "method" );

            if ( method.IsNullOrWhiteSpace() )
            {
                return JsonRpcResult( ErrorResponse( id, -32600, "The request is not a valid JSON-RPC message." ) );
            }

            if ( id == null || id.Type == JTokenType.Null )
            {
                return StatusCode( HttpStatusCode.Accepted );
            }

            switch ( method )
            {
                case "initialize":
                    return JsonRpcResult( SuccessResponse( id, GetInitializeResult( request ) ) );

                case "ping":
                    return JsonRpcResult( SuccessResponse( id, new JObject() ) );

                case "tools/list":
                    return JsonRpcResult( SuccessResponse( id, GetToolListResult( person ) ) );

                case "tools/call":
                    return JsonRpcResult( GetToolCallResponse( id, request, person ) );

                default:
                    return JsonRpcResult( ErrorResponse( id, -32601, $"The method '{method}' is not supported." ) );
            }
        }

        private static JObject GetInitializeResult( JObject request )
        {
            return new JObject
            {
                ["protocolVersion"] = ProtocolVersion,
                ["capabilities"] = new JObject
                {
                    ["tools"] = new JObject
                    {
                        ["listChanged"] = false
                    }
                },
                ["serverInfo"] = new JObject
                {
                    ["name"] = "rock-detective",
                    ["title"] = "Rock Detective",
                    ["version"] = ServerVersion
                },
                ["instructions"] = Agent.SkillWording.GetDefault( "Skill.Purpose" ) + "\n\n" + Agent.SkillWording.GetDefault( "Skill.Usage" ) + "\n\n" + Agent.SkillWording.GetDefault( "Skill.Guardrails" )
            };
        }

        private static JObject GetToolListResult( Rock.Model.Person person )
        {
            var tools = new JArray();

            foreach ( var tool in McpToolService.GetTools( person ) )
            {
                tools.Add( new JObject
                {
                    ["name"] = tool.Name,
                    ["description"] = McpToolService.GetDescription( tool ),
                    ["inputSchema"] = tool.InputSchema
                } );
            }

            return new JObject
            {
                ["tools"] = tools
            };
        }

        private static JObject GetToolCallResponse( JToken id, JObject request, Rock.Model.Person person )
        {
            var paramsObject = request["params"] as JObject;
            var toolName = paramsObject?.Value<string>( "name" );
            var arguments = paramsObject?["arguments"] as JObject;

            if ( toolName.IsNullOrWhiteSpace() )
            {
                return ErrorResponse( id, -32602, "The tool name is required." );
            }

            try
            {
                var toolResult = McpToolService.Execute( toolName, arguments, person );

                return SuccessResponse( id, ToolContentResult( toolResult.ToString( Formatting.None ), false ) );
            }
            catch ( McpToolException ex )
            {
                return SuccessResponse( id, ToolContentResult( ex.Message, true ) );
            }
            catch ( Exception ex )
            {
                Rock.Model.ExceptionLogService.LogException( ex );

                return SuccessResponse( id, ToolContentResult( "An unexpected error occurred while executing the tool.", true ) );
            }
        }

        private static JObject ToolContentResult( string text, bool isError )
        {
            return new JObject
            {
                ["content"] = new JArray
                {
                    new JObject
                    {
                        ["type"] = "text",
                        ["text"] = text
                    }
                },
                ["isError"] = isError
            };
        }

        private static JObject SuccessResponse( JToken id, JObject result )
        {
            return new JObject
            {
                ["jsonrpc"] = "2.0",
                ["id"] = id,
                ["result"] = result
            };
        }

        private static JObject ErrorResponse( JToken id, int code, string message )
        {
            return new JObject
            {
                ["jsonrpc"] = "2.0",
                ["id"] = id ?? JValue.CreateNull(),
                ["error"] = new JObject
                {
                    ["code"] = code,
                    ["message"] = message
                }
            };
        }

        private IHttpActionResult JsonRpcResult( JObject response )
        {
            var message = new HttpResponseMessage( HttpStatusCode.OK )
            {
                Content = new StringContent( response.ToString( Formatting.None ), Encoding.UTF8 )
            };

            message.Content.Headers.ContentType = new MediaTypeHeaderValue( "application/json" );

            return ResponseMessage( message );
        }
    }
}
