using System;

using Newtonsoft.Json.Linq;

namespace com.yesuchum.RockDetective.Mcp
{
    public class McpToolDefinition
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public JObject InputSchema { get; set; }

        public Func<JObject, Rock.Model.Person, JToken> Handler { get; set; }
    }
}
