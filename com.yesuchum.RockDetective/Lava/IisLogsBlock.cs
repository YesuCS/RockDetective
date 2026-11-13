using System;
using System.Collections.Generic;
using System.IO;

using Rock.Lava;
using Rock.Model;

using com.yesuchum.RockDetective.Logs;

namespace com.yesuchum.RockDetective.Lava
{
    public class IisLogsBlock : LavaBlockBase, ILavaSecured
    {
        private string _attributesMarkup;

        public override void OnInitialize( string tagName, string markup, List<string> tokens )
        {
            _attributesMarkup = markup;

            base.OnInitialize( tagName, markup, tokens );
        }

        public override void OnRender( ILavaRenderContext context, TextWriter result )
        {
            if ( !this.IsAuthorized( context ) )
            {
                result.Write( string.Format( LavaBlockBase.NotAuthorizedMessage, this.SourceElementName ) );
                return;
            }

            try
            {
                var settings = LavaElementAttributes.NewFromMarkup( _attributesMarkup, context );

                var options = new IisLogQueryOptions
                {
                    MinutesBack = settings.GetInteger( "minutes", 60 ),
                    StatusCode = settings.GetIntegerOrNull( "status" ),
                    PathContains = settings.GetStringOrNull( "path" ),
                    MethodEquals = settings.GetStringOrNull( "method" ),
                    MinTimeTakenMs = settings.GetIntegerOrNull( "mintimetaken" ),
                    MaxEntries = settings.GetInteger( "limit", 100 )
                };

                var queryResult = IisLogReader.Query( options );

                context["logEntries"] = queryResult.Entries;
                context["logResult"] = queryResult;

                base.OnRender( context, result );
            }
            catch ( Exception ex )
            {
                ExceptionLogService.LogException( ex );
                result.Write( "IIS log entries are not available. " + ex.Message );
            }
        }

        #region ILavaSecured

        public string RequiredPermissionKey
        {
            get
            {
                return "iislogs";
            }
        }

        #endregion
    }
}
