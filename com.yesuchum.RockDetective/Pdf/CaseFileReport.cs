using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

using com.yesuchum.RockDetective.ViewModels;

namespace com.yesuchum.RockDetective.Pdf
{
    public static class CaseFileReport
    {
        private static readonly string[] StatusNames = { "Open", "Investigating", "Solved", "Closed", "Reopened", "Abandoned" };
        private static readonly string[] RoleNames = { "Owner", "Champion", "Recorder", "Communicator", "Member", "Witness" };
        private static readonly string[] HypothesisStatusNames = { "Proposed", "Testing", "Ruled Out", "Confirmed" };
        private static readonly string[] OutcomeNames = { "Pending", "Supported", "Ruled Out", "Inconclusive" };
        private static readonly string[] TimelineTypeNames = { "Problem Start", "Last Known Good", "Change", "Activity", "Other" };
        private static readonly string[] EntrySourceNames = { "Direct", "Witness", "Experiment" };

        public static string BuildHtml( CaseFileBag bag )
        {
            var sb = new StringBuilder();

            sb.Append( @"<!DOCTYPE html><html><head><meta charset=""utf-8""><style>
                body { font-family: 'Segoe UI', Arial, sans-serif; color: #1a1a2e; font-size: 11pt; margin: 40px; }
                h1 { font-size: 20pt; margin-bottom: 0; }
                h2 { font-size: 13pt; border-bottom: 2px solid #1a1a2e; padding-bottom: 4px; margin-top: 28px; }
                h3 { font-size: 11pt; margin-bottom: 4px; }
                .muted { color: #666; }
                .cover { margin-bottom: 32px; }
                .cover .case-number { font-size: 12pt; color: #666; letter-spacing: 1px; }
                table { width: 100%; border-collapse: collapse; margin-top: 8px; }
                th { text-align: left; font-size: 9pt; text-transform: uppercase; letter-spacing: 0.5px; color: #666; border-bottom: 1px solid #ccc; padding: 4px 8px 4px 0; }
                td { padding: 6px 8px 6px 0; border-bottom: 1px solid #eee; vertical-align: top; }
                .badge { display: inline-block; padding: 1px 8px; border: 1px solid #999; border-radius: 10px; font-size: 8pt; }
                .tree-child { padding-left: 24px; }
                .section-block { margin-bottom: 12px; }
                blockquote { border-left: 3px solid #ccc; margin: 6px 0; padding: 2px 12px; color: #333; }
                </style></head><body>" );

            AppendCover( sb, bag );
            AppendSection( sb, "Problem Statement", bag.ProblemStatement );
            AppendTimeline( sb, bag );
            AppendFacts( sb, bag );
            AppendHypotheses( sb, bag );
            AppendExperiments( sb, bag );
            AppendWitnessStatements( sb, bag );
            AppendPostmortem( sb, bag );

            sb.Append( "</body></html>" );

            return sb.ToString();
        }

        private static string H( string value )
        {
            return HttpUtility.HtmlEncode( value ?? string.Empty );
        }

        private static string Name( string[] names, int index )
        {
            return index >= 0 && index < names.Length ? names[index] : "Unknown";
        }

        private static void AppendCover( StringBuilder sb, CaseFileBag bag )
        {
            sb.Append( "<div class=\"cover\">" );
            sb.Append( $"<div class=\"case-number\">Case #{H( bag.CaseNumber )}</div>" );
            sb.Append( $"<h1>{H( bag.Title )}</h1>" );
            sb.Append( $"<p class=\"muted\">Category: {H( bag.CategoryName ?? "None" )} &middot; Status: {Name( StatusNames, bag.Status )} &middot; Mode: {( bag.Mode == 1 ? "Advanced" : "Simple" )}</p>" );
            sb.Append( $"<p class=\"muted\">Opened: {bag.CreatedDateTime:MMMM d, yyyy}" );

            if ( bag.ClosedDateTime.HasValue )
            {
                sb.Append( $" &middot; Closed: {bag.ClosedDateTime:MMMM d, yyyy}" );
            }

            sb.Append( "</p>" );

            var team = bag.Participants.Where( p => p.Role != 5 ).ToList();

            sb.Append( $"<p><strong>Owner:</strong> {H( bag.OwnerName )}" );

            foreach ( var member in team )
            {
                sb.Append( $" &middot; <strong>{Name( RoleNames, member.Role )}:</strong> {H( member.DisplayName )}" );
            }

            sb.Append( "</p></div>" );
        }

        private static void AppendSection( StringBuilder sb, string title, string content )
        {
            if ( string.IsNullOrWhiteSpace( content ) )
            {
                return;
            }

            sb.Append( $"<h2>{H( title )}</h2><p>{H( content ).Replace( "\n", "<br>" )}</p>" );
        }

        private static void AppendTimeline( StringBuilder sb, CaseFileBag bag )
        {
            var events = bag.Entries.Where( e => e.EntryType == 1 ).OrderBy( e => e.EventDateTime ).ToList();

            if ( !events.Any() )
            {
                return;
            }

            sb.Append( "<h2>Timeline</h2><table><tr><th>When</th><th>Type</th><th>Event</th></tr>" );

            foreach ( var entry in events )
            {
                var typeName = entry.TimelineEventType.HasValue ? Name( TimelineTypeNames, entry.TimelineEventType.Value ) : string.Empty;
                sb.Append( $"<tr><td>{entry.EventDateTime:g}</td><td>{typeName}</td><td>{H( entry.Text )}</td></tr>" );
            }

            sb.Append( "</table>" );
        }

        private static void AppendFacts( StringBuilder sb, CaseFileBag bag )
        {
            var facts = bag.Entries.Where( e => e.EntryType == 0 ).ToList();
            var evidence = bag.Entries.Where( e => e.EntryType == 2 ).ToList();

            if ( facts.Any() )
            {
                sb.Append( "<h2>Fact List</h2><table><tr><th>Fact</th><th>Source</th><th>Corroborated</th></tr>" );

                foreach ( var fact in facts )
                {
                    var source = Name( EntrySourceNames, fact.Source );

                    if ( fact.SourceParticipantName != null )
                    {
                        source += $": {fact.SourceParticipantName}";
                    }

                    sb.Append( $"<tr><td>{H( fact.Text )}</td><td>{H( source )}</td><td>{( fact.IsCorroborated ? "Yes" : "No" )}</td></tr>" );
                }

                sb.Append( "</table>" );
            }

            if ( evidence.Any() )
            {
                sb.Append( "<h2>Evidence</h2><table><tr><th>Description</th><th>Attachment</th><th>Link</th></tr>" );

                foreach ( var item in evidence )
                {
                    sb.Append( $"<tr><td>{H( item.Text )}</td><td>{H( item.BinaryFileName ?? string.Empty )}</td><td>{H( item.Url ?? string.Empty )}</td></tr>" );
                }

                sb.Append( "</table>" );
            }
        }

        private static void AppendHypotheses( StringBuilder sb, CaseFileBag bag )
        {
            if ( !bag.Hypotheses.Any() )
            {
                return;
            }

            sb.Append( "<h2>Probability Tree</h2>" );

            var roots = bag.Hypotheses.Where( h => h.ParentHypothesisIdKey == null ).ToList();

            foreach ( var root in roots )
            {
                AppendHypothesisNode( sb, bag.Hypotheses, root, 0 );
            }
        }

        private static void AppendHypothesisNode( StringBuilder sb, List<HypothesisBag> all, HypothesisBag node, int depth )
        {
            var indentStyle = depth > 0 ? $" style=\"margin-left: {depth * 24}px\"" : string.Empty;

            sb.Append( $"<div class=\"section-block\"{indentStyle}>" );
            sb.Append( $"<h3>{H( node.Name )} <span class=\"badge\">{node.Probability}%</span> <span class=\"badge\">{Name( HypothesisStatusNames, node.Status )}</span></h3>" );

            if ( !string.IsNullOrWhiteSpace( node.Description ) )
            {
                sb.Append( $"<p class=\"muted\">{H( node.Description )}</p>" );
            }

            if ( !string.IsNullOrWhiteSpace( node.RuledOutReason ) )
            {
                sb.Append( $"<p class=\"muted\">Ruled out: {H( node.RuledOutReason )}</p>" );
            }

            sb.Append( "</div>" );

            foreach ( var child in all.Where( h => h.ParentHypothesisIdKey == node.IdKey ) )
            {
                AppendHypothesisNode( sb, all, child, depth + 1 );
            }
        }

        private static void AppendExperiments( StringBuilder sb, CaseFileBag bag )
        {
            if ( !bag.Experiments.Any() )
            {
                return;
            }

            sb.Append( "<h2>Experiment Log</h2>" );

            foreach ( var experiment in bag.Experiments )
            {
                var hypothesisName = experiment.HypothesisIdKey != null
                    ? bag.Hypotheses.FirstOrDefault( h => h.IdKey == experiment.HypothesisIdKey )?.Name
                    : experiment.HypothesisText;

                sb.Append( "<div class=\"section-block\">" );
                sb.Append( $"<h3>{H( hypothesisName ?? "Unlinked experiment" )} <span class=\"badge\">{Name( OutcomeNames, experiment.Outcome )}</span></h3>" );
                sb.Append( "<table>" );
                AppendRow( sb, "Test Plan", experiment.TestPlan );
                AppendRow( sb, "Expected Result", experiment.ExpectedResult );
                AppendRow( sb, "Actual Result", experiment.ActualResult );
                AppendRow( sb, "Changes Made", experiment.ChangesMade );
                AppendRow( sb, "Changes Reverted", experiment.ChangesReverted ? "Yes" : ( string.IsNullOrWhiteSpace( experiment.RevertJustification ) ? "No" : $"No; {experiment.RevertJustification}" ) );
                AppendRow( sb, "Learnings", experiment.Learnings );
                sb.Append( "</table></div>" );
            }
        }

        private static void AppendRow( StringBuilder sb, string label, string value )
        {
            if ( string.IsNullOrWhiteSpace( value ) )
            {
                return;
            }

            sb.Append( $"<tr><td style=\"width: 140px\" class=\"muted\">{H( label )}</td><td>{H( value ).Replace( "\n", "<br>" )}</td></tr>" );
        }

        private static void AppendWitnessStatements( StringBuilder sb, CaseFileBag bag )
        {
            var witnesses = bag.Participants
                .Where( p => p.Statements.Any( s => !string.IsNullOrWhiteSpace( s.Statement ) ) )
                .ToList();

            if ( !witnesses.Any() )
            {
                return;
            }

            sb.Append( "<h2>Witness Statements</h2>" );

            foreach ( var witness in witnesses )
            {
                sb.Append( $"<div class=\"section-block\"><h3>{H( witness.DisplayName )}{( witness.IsExternal ? " (external)" : string.Empty )}</h3>" );

                foreach ( var statement in witness.Statements.Where( s => !string.IsNullOrWhiteSpace( s.Statement ) ) )
                {
                    if ( !string.IsNullOrWhiteSpace( statement.RequestPrompt ) )
                    {
                        sb.Append( $"<p class=\"muted\">Asked: {H( statement.RequestPrompt )}</p>" );
                    }

                    sb.Append( $"<blockquote>{H( statement.Statement ).Replace( "\n", "<br>" )}</blockquote>" );

                    if ( !string.IsNullOrWhiteSpace( statement.BinaryFileName ) || !string.IsNullOrWhiteSpace( statement.Url ) )
                    {
                        sb.Append( "<p class=\"muted\">" );

                        if ( !string.IsNullOrWhiteSpace( statement.BinaryFileName ) )
                        {
                            sb.Append( $"Attachment: {H( statement.BinaryFileName )} " );
                        }

                        if ( !string.IsNullOrWhiteSpace( statement.Url ) )
                        {
                            sb.Append( $"Link: {H( statement.Url )}" );
                        }

                        sb.Append( "</p>" );
                    }
                }

                sb.Append( "</div>" );
            }
        }

        private static void AppendPostmortem( StringBuilder sb, CaseFileBag bag )
        {
            var hasContent = !string.IsNullOrWhiteSpace( bag.ImpactSummary )
                || !string.IsNullOrWhiteSpace( bag.SolutionSummary )
                || !string.IsNullOrWhiteSpace( bag.LessonsLearned );

            if ( !hasContent )
            {
                return;
            }

            sb.Append( "<h2>Postmortem</h2><table>" );
            AppendRow( sb, "Impact", bag.ImpactSummary );
            AppendRow( sb, "Solution", bag.SolutionSummary );
            AppendRow( sb, "Contributing Factors", bag.ContributingFactors );
            AppendRow( sb, "Lessons Learned", bag.LessonsLearned );
            AppendRow( sb, "Prevention Measures", bag.PreventionMeasures );
            AppendRow( sb, "Action Items", bag.ActionItems );
            sb.Append( "</table>" );
        }
    }
}
