using System;
using System.Collections.Generic;
using System.Linq;

using Rock;

using com.yesuchum.RockDetective.Enums;
using com.yesuchum.RockDetective.Method;

namespace com.yesuchum.RockDetective.Data
{
    public static class CaseRules
    {
        public const string UnsafeUrlMessage = "A link has to be a full web address that starts with http:// or https://.";

        public const string ProbabilityMessage = "A probability has to be a whole number from 0 to 100.";

        public static bool IsSafeUrl( string url )
        {
            if ( url.IsNullOrWhiteSpace() )
            {
                return true;
            }

            return Uri.TryCreate( url.Trim(), UriKind.Absolute, out var uri )
                && ( uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps );
        }

        public static string SafeUrlOrNull( string url )
        {
            return url.IsNotNullOrWhiteSpace() && IsSafeUrl( url ) ? url.Trim() : null;
        }

        public const int MaxTitleLength = 200;

        public static string TitleTooLongMessage => $"A title can be up to {MaxTitleLength} characters. Put the detail in the problem statement.";

        public static DateTime? ToRockLocalTime( Newtonsoft.Json.Linq.JToken token, string name )
        {
            if ( token == null || token.Type == Newtonsoft.Json.Linq.JTokenType.Null )
            {
                return null;
            }

            if ( token.Type == Newtonsoft.Json.Linq.JTokenType.Date )
            {
                var value = token.ToObject<object>();

                if ( value is DateTimeOffset offsetValue )
                {
                    return TimeZoneInfo.ConvertTime( offsetValue, RockDateTime.OrgTimeZoneInfo ).DateTime;
                }

                var dateValue = ( DateTime ) value;

                if ( dateValue.Kind == DateTimeKind.Unspecified )
                {
                    return dateValue;
                }

                return DateTime.SpecifyKind( TimeZoneInfo.ConvertTime( dateValue, RockDateTime.OrgTimeZoneInfo ), DateTimeKind.Unspecified );
            }

            var text = token.ToString().Trim();

            if ( text.Length == 0 )
            {
                return null;
            }

            var hasOffset = System.Text.RegularExpressions.Regex.IsMatch( text, @"(Z|[+-]\d{2}:?\d{2})$", System.Text.RegularExpressions.RegexOptions.IgnoreCase ) && text.Contains( "T" );
            var culture = System.Globalization.CultureInfo.InvariantCulture;

            if ( hasOffset && DateTimeOffset.TryParse( text, culture, System.Globalization.DateTimeStyles.None, out var parsedOffset ) )
            {
                return TimeZoneInfo.ConvertTime( parsedOffset, RockDateTime.OrgTimeZoneInfo ).DateTime;
            }

            if ( DateTime.TryParse( text, culture, System.Globalization.DateTimeStyles.None, out var parsed ) )
            {
                return DateTime.SpecifyKind( parsed, DateTimeKind.Unspecified );
            }

            throw new Mcp.McpToolException( $"'{text}' is not a date the {name} argument understands. Use ISO 8601, like 2026-11-13T09:30:00." );
        }

        public static bool IsProbabilityValid( int probability )
        {
            return probability >= 0 && probability <= 100;
        }

        public static bool HasConfirmedHypothesisWithSupport( RockDetectiveContext detectiveContext, int caseFileId )
        {
            var confirmedIds = detectiveContext.Hypotheses
                .Where( h => h.CaseFileId == caseFileId && h.Status == HypothesisStatus.Confirmed )
                .Select( h => h.Id )
                .ToList();

            if ( !confirmedIds.Any() )
            {
                return false;
            }

            return detectiveContext.Experiments.Any( e =>
                e.CaseFileId == caseFileId
                && e.HypothesisId.HasValue
                && confirmedIds.Contains( e.HypothesisId.Value )
                && e.Outcome == ExperimentOutcome.Supported );
        }

        public static bool CanSignOffAsChampion( List<Model.CaseParticipant> participants, Rock.Model.Person person, bool canEditCase )
        {
            if ( person == null )
            {
                return false;
            }

            var champions = participants.Where( p => p.Role == ParticipantRole.Champion ).ToList();

            if ( !champions.Any() )
            {
                return false;
            }

            var aliasIds = person.Aliases.Select( a => a.Id ).ToList();

            if ( champions.Any( p => p.PersonAliasId.HasValue && aliasIds.Contains( p.PersonAliasId.Value ) ) )
            {
                return true;
            }

            var isEveryChampionOutsideRock = champions.All( p => !p.PersonAliasId.HasValue );

            return isEveryChampionOutsideRock && canEditCase;
        }

        public static string GetChampionSignOffRefusal( RockDetectiveContext detectiveContext, Model.CaseFile caseFile, Rock.Model.Person person, bool canEditCase )
        {
            var participants = detectiveContext.CaseParticipants.Where( p => p.CaseFileId == caseFile.Id ).ToList();

            if ( !participants.Any( p => p.Role == ParticipantRole.Champion ) )
            {
                return "This case has no Champion, so there is no sign-off to record.";
            }

            if ( !CanSignOffAsChampion( participants, person, canEditCase ) )
            {
                return "Only the person assigned as Champion on this case can sign off. When the Champion is someone outside Rock, the case owner records their sign-off.";
            }

            if ( !HasConfirmedHypothesisWithSupport( detectiveContext, caseFile.Id ) )
            {
                return "There is nothing to sign off on yet. Confirm a hypothesis that has a supporting experiment first.";
            }

            return null;
        }

        public static void RecordChampionSignOff( CasePhaseStates states, Rock.Model.Person person )
        {
            var state = states.GetState( CasePhase.Reevaluate );

            state.Data[PhaseStateKey.ChampionSignOffDateTime] = RockDateTime.Now.ToString( "s" );
            state.Data[PhaseStateKey.ChampionSignOffPersonAliasId] = person.PrimaryAliasId.ToString();
        }

        public static void ClearChampionSignOff( CasePhaseStates states )
        {
            var state = states.GetState( CasePhase.Reevaluate );

            state.Data.Remove( PhaseStateKey.ChampionSignOffDateTime );
            state.Data.Remove( PhaseStateKey.ChampionSignOffPersonAliasId );
        }
    }
}
