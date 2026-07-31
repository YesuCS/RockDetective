using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;

using Rock;
using Rock.Rest;
using Rock.Rest.Filters;
using Rock.Web.Cache;

using com.yesuchum.RockDetective.Data;
using com.yesuchum.RockDetective.Enums;
using com.yesuchum.RockDetective.Method;
using com.yesuchum.RockDetective.ViewModels;

namespace com.yesuchum.RockDetective.Rest
{
    [RoutePrefix( "api/com_yesuchum/rockdetective" )]
    [Rock.SystemGuid.RestControllerGuid( "71481A75-AC38-4131-9724-43B874926382" )]
    public class RockDetectiveController : ApiControllerBase
    {
        public class CasePatchBag
        {
            public int? Status { get; set; }

            public int? CurrentPhase { get; set; }

            public string Title { get; set; }

            public string ProblemStatement { get; set; }
        }

        public class ParticipantPostBag
        {
            public Guid? PersonAliasGuid { get; set; }

            public string ExternalName { get; set; }

            public string ExternalContact { get; set; }

            public int Role { get; set; }
        }

        [HttpGet]
        [Authenticate, Secured]
        [Route( "cases" )]
        [Rock.SystemGuid.RestActionGuid( "6D614CCC-4E26-403A-BEDE-542CD2864BAF" )]
        public IHttpActionResult GetCases( int? status = null, string category = null, string owner = null )
        {
            var person = GetPerson();

            if ( person == null )
            {
                return StatusCode( HttpStatusCode.Unauthorized );
            }

            using ( var detectiveContext = new RockDetectiveContext() )
            {
                var query = new CaseFileService( detectiveContext ).Queryable().AsNoTracking();

                if ( status.HasValue )
                {
                    var caseStatus = ( CaseStatus ) status.Value;
                    query = query.Where( c => c.Status == caseStatus );
                }

                var categoryGuid = category.AsGuidOrNull();

                if ( categoryGuid.HasValue )
                {
                    var categoryId = CategoryCache.Get( categoryGuid.Value )?.Id;
                    query = query.Where( c => c.CategoryId == categoryId );
                }

                var cases = query.ToList()
                    .Where( c => c.IsAuthorized( Rock.Security.Authorization.VIEW, person ) )
                    .ToList();

                if ( owner.IsNotNullOrWhiteSpace() )
                {
                    using ( var rockContext = new Rock.Data.RockContext() )
                    {
                        var ownerGuid = owner.AsGuidOrNull();
                        var aliasIds = new Rock.Model.PersonAliasService( rockContext )
                            .Queryable()
                            .Where( pa => pa.Person.Guid == ownerGuid )
                            .Select( pa => pa.Id )
                            .ToList();

                        cases = cases.Where( c => aliasIds.Contains( c.OwnerPersonAliasId ) ).ToList();
                    }
                }

                return Ok( cases.Select( c => BuildFullBag( detectiveContext, c ) ).ToList() );
            }
        }

        [HttpGet]
        [Authenticate, Secured]
        [Route( "cases/{idKey}" )]
        [Rock.SystemGuid.RestActionGuid( "63B7DDC4-AAB3-47F5-9ED2-83EF72F82257" )]
        public IHttpActionResult GetCase( string idKey )
        {
            return WithViewableCase( idKey, ( detectiveContext, caseFile ) => Ok( BuildFullBag( detectiveContext, caseFile ) ) );
        }

        public class CasePostBag
        {
            public string Title { get; set; }

            public string ProblemStatement { get; set; }

            public Guid? CategoryGuid { get; set; }

            public int Mode { get; set; }
        }

        [HttpPost]
        [Authenticate, Secured]
        [Route( "cases" )]
        [Rock.SystemGuid.RestActionGuid( "621B0E8B-8AAA-4C66-918A-64A391526016" )]
        public IHttpActionResult PostCase( [FromBody] CasePostBag bag )
        {
            var person = GetPerson();

            if ( person == null || !person.PrimaryAliasId.HasValue )
            {
                return StatusCode( HttpStatusCode.Unauthorized );
            }

            if ( bag == null || bag.Title.IsNullOrWhiteSpace() || bag.ProblemStatement.IsNullOrWhiteSpace() )
            {
                return BadRequest( "Title and ProblemStatement are required." );
            }

            int? categoryId = bag.CategoryGuid.HasValue ? CategoryCache.Get( bag.CategoryGuid.Value )?.Id : null;

            if ( !categoryId.HasValue )
            {
                return BadRequest( "CategoryGuid is required and must reference a Case File category." );
            }

            using ( var detectiveContext = new RockDetectiveContext() )
            {
                var caseService = new CaseFileService( detectiveContext );
                var mode = bag.Mode == ( int ) CaseMode.Advanced ? CaseMode.Advanced : CaseMode.Simple;

                var caseFile = new Model.CaseFile
                {
                    Title = bag.Title.Truncate( 200 ),
                    ProblemStatement = bag.ProblemStatement,
                    CategoryId = categoryId,
                    Mode = mode,
                    Classification = mode == CaseMode.Advanced ? CaseClassification.Difficult : CaseClassification.Easy,
                    Status = CaseStatus.Open,
                    CurrentPhase = mode == CaseMode.Advanced ? CasePhase.DefineTeam : CasePhase.FactList,
                    OwnerPersonAliasId = person.PrimaryAliasId.Value,
                    CreatedDateTime = RockDateTime.Now,
                    CreatedByPersonAliasId = person.PrimaryAliasId
                };

                caseService.Add( caseFile );
                detectiveContext.SaveChanges();

                return Ok( BuildFullBag( detectiveContext, caseFile ) );
            }
        }

        [HttpPatch]
        [Authenticate, Secured]
        [Route( "cases/{idKey}" )]
        [Rock.SystemGuid.RestActionGuid( "8A0C9D59-0B1E-4375-B12D-31448E22BC26" )]
        public IHttpActionResult PatchCase( string idKey, [FromBody] CasePatchBag bag )
        {
            return WithEditableCase( idKey, ( detectiveContext, caseFile ) =>
            {
                if ( bag == null )
                {
                    return BadRequest( "A patch body is required." );
                }

                if ( bag.Title.IsNotNullOrWhiteSpace() )
                {
                    caseFile.Title = bag.Title.Truncate( 200 );
                }

                if ( bag.ProblemStatement.IsNotNullOrWhiteSpace() )
                {
                    caseFile.ProblemStatement = bag.ProblemStatement;
                }

                if ( bag.Status.HasValue )
                {
                    caseFile.Status = ( CaseStatus ) bag.Status.Value;

                    if ( caseFile.Status == CaseStatus.Closed )
                    {
                        caseFile.ClosedDateTime = RockDateTime.Now;
                    }
                }

                if ( bag.CurrentPhase.HasValue )
                {
                    var targetPhase = ( CasePhase ) bag.CurrentPhase.Value;
                    var currentPhase = caseFile.CurrentPhase;

                    if ( targetPhase > currentPhase )
                    {
                        var entities = LoadEntities( detectiveContext, caseFile.Id );
                        var aggregate = CaseMapper.BuildAggregate( caseFile, entities.Item1, entities.Item2, entities.Item3, entities.Item4 );

                        for ( var phase = currentPhase; phase < targetPhase; phase++ )
                        {
                            if ( GateEvaluator.IsPhaseAutoSkipped( aggregate, phase ) )
                            {
                                continue;
                            }

                            var gate = GateEvaluator.Evaluate( aggregate, phase );

                            if ( !gate.IsMet )
                            {
                                return BadRequest( $"Cannot advance past phase {( int ) phase}; its gate requirements are not met." );
                            }
                        }
                    }

                    caseFile.CurrentPhase = targetPhase;
                }

                caseFile.ModifiedDateTime = RockDateTime.Now;
                detectiveContext.SaveChanges();

                return Ok( BuildFullBag( detectiveContext, caseFile ) );
            } );
        }

        public class EntryPostBag
        {
            public int EntryType { get; set; }

            public string Text { get; set; }

            public DateTime? EventDateTime { get; set; }

            public int? TimelineEventType { get; set; }

            public string Url { get; set; }

            public bool IsCorroborated { get; set; }
        }

        [HttpGet]
        [Authenticate, Secured]
        [Route( "cases/{idKey}/entries" )]
        [Rock.SystemGuid.RestActionGuid( "EE56BA28-9BEE-4638-A475-718050F6A00D" )]
        public IHttpActionResult GetEntries( string idKey )
        {
            return WithViewableCase( idKey, ( detectiveContext, caseFile ) =>
            {
                var bag = BuildFullBag( detectiveContext, caseFile );
                return Ok( bag.Entries );
            } );
        }

        [HttpPost]
        [Authenticate, Secured]
        [Route( "cases/{idKey}/entries" )]
        [Rock.SystemGuid.RestActionGuid( "C2F988C9-FC27-4A78-BAF7-8A6B1D8B2FCC" )]
        public IHttpActionResult PostEntry( string idKey, [FromBody] EntryPostBag bag )
        {
            return WithEditableCase( idKey, ( detectiveContext, caseFile ) =>
            {
                if ( bag == null || bag.Text.IsNullOrWhiteSpace() )
                {
                    return BadRequest( "Text is required." );
                }

                var entry = new Model.CaseEntry
                {
                    CaseFileId = caseFile.Id,
                    EntryType = ( CaseEntryType ) bag.EntryType,
                    Text = bag.Text,
                    EventDateTime = bag.EventDateTime,
                    TimelineEventType = bag.TimelineEventType.HasValue ? ( TimelineEventType? ) bag.TimelineEventType.Value : null,
                    Url = bag.Url,
                    Source = CaseEntrySource.Direct,
                    IsCorroborated = bag.IsCorroborated,
                    Order = detectiveContext.CaseEntries.Count( e => e.CaseFileId == caseFile.Id ),
                    CreatedDateTime = RockDateTime.Now,
                    CreatedByPersonAliasId = GetPerson()?.PrimaryAliasId
                };

                new CaseEntryService( detectiveContext ).Add( entry );
                detectiveContext.SaveChanges();

                return Ok( CaseMapper.ToBag( entry, new Dictionary<int, string>() ) );
            } );
        }

        public class HypothesisPostBag
        {
            public string Name { get; set; }

            public string Description { get; set; }

            public int Probability { get; set; }

            public string ParentHypothesisIdKey { get; set; }
        }

        [HttpPost]
        [Authenticate, Secured]
        [Route( "cases/{idKey}/hypotheses" )]
        [Rock.SystemGuid.RestActionGuid( "406704AD-318C-4F2B-BA2A-95C41CC7D6E0" )]
        public IHttpActionResult PostHypothesis( string idKey, [FromBody] HypothesisPostBag bag )
        {
            return WithEditableCase( idKey, ( detectiveContext, caseFile ) =>
            {
                if ( bag == null || bag.Name.IsNullOrWhiteSpace() )
                {
                    return BadRequest( "Name is required." );
                }

                var hypothesis = new Model.Hypothesis
                {
                    CaseFileId = caseFile.Id,
                    Name = bag.Name.Truncate( 300 ),
                    Description = bag.Description,
                    Probability = Math.Max( 0, Math.Min( 100, bag.Probability ) ),
                    Status = HypothesisStatus.Proposed,
                    ParentHypothesisId = Rock.Utility.IdHasher.Instance.GetId( bag.ParentHypothesisIdKey ?? string.Empty ),
                    CreatedDateTime = RockDateTime.Now,
                    CreatedByPersonAliasId = GetPerson()?.PrimaryAliasId
                };

                new HypothesisService( detectiveContext ).Add( hypothesis );
                detectiveContext.SaveChanges();

                return Ok( CaseMapper.ToBag( hypothesis ) );
            } );
        }

        public class HypothesisPatchBag
        {
            public int? Probability { get; set; }

            public int? Status { get; set; }

            public string RuledOutReason { get; set; }
        }

        [HttpPatch]
        [Authenticate, Secured]
        [Route( "hypotheses/{idKey}" )]
        [Rock.SystemGuid.RestActionGuid( "50EC9633-8009-4A9F-832C-FECBC209EF83" )]
        public IHttpActionResult PatchHypothesis( string idKey, [FromBody] HypothesisPatchBag bag )
        {
            using ( var detectiveContext = new RockDetectiveContext() )
            {
                var id = Rock.Utility.IdHasher.Instance.GetId( idKey ?? string.Empty );
                var hypothesis = id.HasValue ? new HypothesisService( detectiveContext ).Get( id.Value ) : null;

                if ( hypothesis == null )
                {
                    return NotFound();
                }

                var editCheck = CheckEdit( detectiveContext, hypothesis.CaseFileId, out var caseFile );

                if ( editCheck != null )
                {
                    return editCheck;
                }

                if ( bag?.Probability != null )
                {
                    hypothesis.Probability = Math.Max( 0, Math.Min( 100, bag.Probability.Value ) );
                }

                if ( bag?.Status != null )
                {
                    hypothesis.Status = ( HypothesisStatus ) bag.Status.Value;
                }

                if ( bag?.RuledOutReason != null )
                {
                    hypothesis.RuledOutReason = bag.RuledOutReason;
                }

                hypothesis.ModifiedDateTime = RockDateTime.Now;
                detectiveContext.SaveChanges();

                return Ok( CaseMapper.ToBag( hypothesis ) );
            }
        }

        public class ExperimentPostBag
        {
            public string HypothesisIdKey { get; set; }

            public string HypothesisText { get; set; }

            public string TestPlan { get; set; }

            public string ExpectedResult { get; set; }
        }

        [HttpPost]
        [Authenticate, Secured]
        [Route( "cases/{idKey}/experiments" )]
        [Rock.SystemGuid.RestActionGuid( "DD3E78EF-2A45-4ED3-B5E8-A1DAC45DAD2A" )]
        public IHttpActionResult PostExperiment( string idKey, [FromBody] ExperimentPostBag bag )
        {
            return WithEditableCase( idKey, ( detectiveContext, caseFile ) =>
            {
                if ( bag == null || ( bag.HypothesisIdKey.IsNullOrWhiteSpace() && bag.HypothesisText.IsNullOrWhiteSpace() ) )
                {
                    return BadRequest( "HypothesisIdKey or HypothesisText is required." );
                }

                var experiment = new Model.Experiment
                {
                    CaseFileId = caseFile.Id,
                    HypothesisId = Rock.Utility.IdHasher.Instance.GetId( bag.HypothesisIdKey ?? string.Empty ),
                    HypothesisText = bag.HypothesisText.Truncate( 300 ),
                    TestPlan = bag.TestPlan,
                    ExpectedResult = bag.ExpectedResult,
                    Outcome = ExperimentOutcome.Pending,
                    CreatedDateTime = RockDateTime.Now,
                    CreatedByPersonAliasId = GetPerson()?.PrimaryAliasId
                };

                new ExperimentService( detectiveContext ).Add( experiment );
                detectiveContext.SaveChanges();

                return Ok( CaseMapper.ToBag( experiment ) );
            } );
        }

        public class ExperimentPatchBag
        {
            public string ActualResult { get; set; }

            public int? Outcome { get; set; }

            public string ChangesMade { get; set; }

            public bool? ChangesReverted { get; set; }

            public string Learnings { get; set; }
        }

        [HttpPatch]
        [Authenticate, Secured]
        [Route( "experiments/{idKey}" )]
        [Rock.SystemGuid.RestActionGuid( "2D495520-191E-4929-B242-F6EC3A117DD4" )]
        public IHttpActionResult PatchExperiment( string idKey, [FromBody] ExperimentPatchBag bag )
        {
            using ( var detectiveContext = new RockDetectiveContext() )
            {
                var id = Rock.Utility.IdHasher.Instance.GetId( idKey ?? string.Empty );
                var experiment = id.HasValue ? new ExperimentService( detectiveContext ).Get( id.Value ) : null;

                if ( experiment == null )
                {
                    return NotFound();
                }

                var editCheck = CheckEdit( detectiveContext, experiment.CaseFileId, out var caseFile );

                if ( editCheck != null )
                {
                    return editCheck;
                }

                if ( bag?.ActualResult != null )
                {
                    experiment.ActualResult = bag.ActualResult;
                }

                if ( bag?.Outcome != null )
                {
                    experiment.Outcome = ( ExperimentOutcome ) bag.Outcome.Value;
                }

                if ( bag?.ChangesMade != null )
                {
                    experiment.ChangesMade = bag.ChangesMade;
                }

                if ( bag?.ChangesReverted != null )
                {
                    experiment.ChangesReverted = bag.ChangesReverted.Value;
                }

                if ( bag?.Learnings != null )
                {
                    experiment.Learnings = bag.Learnings;
                }

                experiment.ModifiedDateTime = RockDateTime.Now;
                detectiveContext.SaveChanges();

                return Ok( CaseMapper.ToBag( experiment ) );
            }
        }

        [HttpPost]
        [Authenticate, Secured]
        [Route( "cases/{idKey}/participants" )]
        [Rock.SystemGuid.RestActionGuid( "A8B04BC3-BD60-48AB-BCD2-2AD89E1A21B0" )]
        public IHttpActionResult PostParticipant( string idKey, [FromBody] ParticipantPostBag bag )
        {
            return WithEditableCase( idKey, ( detectiveContext, caseFile ) =>
            {
                if ( bag == null )
                {
                    return BadRequest( "A body is required." );
                }

                int? personAliasId = null;

                if ( bag.PersonAliasGuid.HasValue )
                {
                    using ( var rockContext = new Rock.Data.RockContext() )
                    {
                        personAliasId = new Rock.Model.PersonAliasService( rockContext ).GetId( bag.PersonAliasGuid.Value );
                    }
                }

                if ( !personAliasId.HasValue && bag.ExternalName.IsNullOrWhiteSpace() )
                {
                    return BadRequest( "PersonAliasGuid or ExternalName is required." );
                }

                var participant = new Model.CaseParticipant
                {
                    CaseFileId = caseFile.Id,
                    PersonAliasId = personAliasId,
                    ExternalName = personAliasId.HasValue ? null : bag.ExternalName.Truncate( 200 ),
                    ExternalContact = personAliasId.HasValue ? null : bag.ExternalContact.Truncate( 400 ),
                    Role = ( ParticipantRole ) bag.Role,
                    CreatedDateTime = RockDateTime.Now,
                    CreatedByPersonAliasId = GetPerson()?.PrimaryAliasId
                };

                new CaseParticipantService( detectiveContext ).Add( participant );
                detectiveContext.SaveChanges();

                var aliasInfo = participant.PersonAliasId.HasValue
                    ? CaseMapper.GetAliasInfo( new[] { participant.PersonAliasId.Value } )
                    : new Dictionary<int, CaseMapper.PersonAliasInfo>();

                return Ok( CaseMapper.ToBag( participant, aliasInfo ) );
            } );
        }

        [HttpPost]
        [Authenticate, Secured]
        [Route( "participants/{idKey}/request-statement" )]
        [Rock.SystemGuid.RestActionGuid( "32AD1B79-6507-4F2F-80D6-A80571011A90" )]
        public IHttpActionResult RequestStatement( string idKey, [FromBody] Dictionary<string, string> body )
        {
            using ( var detectiveContext = new RockDetectiveContext() )
            {
                var id = Rock.Utility.IdHasher.Instance.GetId( idKey ?? string.Empty );
                var participant = id.HasValue ? new CaseParticipantService( detectiveContext ).Get( id.Value ) : null;

                if ( participant == null )
                {
                    return NotFound();
                }

                var editCheck = CheckEdit( detectiveContext, participant.CaseFileId, out var caseFile );

                if ( editCheck != null )
                {
                    return editCheck;
                }

                var prompt = body != null && body.ContainsKey( "requestPrompt" ) ? body["requestPrompt"] : null;

                if ( prompt.IsNullOrWhiteSpace() )
                {
                    return BadRequest( "requestPrompt is required." );
                }

                participant.Role = ParticipantRole.Witness;

                var statement = new Model.CaseStatement
                {
                    CaseFileId = participant.CaseFileId,
                    ParticipantId = participant.Id,
                    RequestPrompt = prompt,
                    RequestDateTime = RockDateTime.Now,
                    RequestStatus = StatementRequestStatus.Draft,
                    CreatedDateTime = RockDateTime.Now,
                    CreatedByPersonAliasId = GetPerson()?.PrimaryAliasId
                };

                new CaseStatementService( detectiveContext ).Add( statement );
                detectiveContext.SaveChanges();

                return Ok( CaseMapper.ToBag( statement ) );
            }
        }

        [HttpGet]
        [Authenticate, Secured]
        [Route( "cases/{idKey}/pdf" )]
        [Rock.SystemGuid.RestActionGuid( "ACA7B191-E79D-4BDF-ABAF-DEDD759159EA" )]
        public HttpResponseMessage GetCasePdf( string idKey )
        {
            var person = GetPerson();

            using ( var detectiveContext = new RockDetectiveContext() )
            {
                var caseFile = GetCaseByKey( detectiveContext, idKey );

                if ( caseFile == null )
                {
                    return new HttpResponseMessage( HttpStatusCode.NotFound );
                }

                if ( person == null || !caseFile.IsAuthorized( Rock.Security.Authorization.VIEW, person ) )
                {
                    return new HttpResponseMessage( HttpStatusCode.Unauthorized );
                }

                var bag = BuildFullBag( detectiveContext, caseFile );
                var html = Pdf.CaseFileReport.BuildHtml( bag );

                using ( var pdfGenerator = new Rock.Pdf.PdfGenerator() )
                using ( var pdfStream = pdfGenerator.GetPDFDocumentFromHtml( html ) )
                using ( var memoryStream = new System.IO.MemoryStream() )
                {
                    pdfStream.CopyTo( memoryStream );

                    var response = new HttpResponseMessage( HttpStatusCode.OK )
                    {
                        Content = new ByteArrayContent( memoryStream.ToArray() )
                    };
                    response.Content.Headers.ContentType = new MediaTypeHeaderValue( "application/pdf" );
                    response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue( "attachment" )
                    {
                        FileName = $"Case-{caseFile.Id}.pdf"
                    };

                    return response;
                }
            }
        }

        private static Model.CaseFile GetCaseByKey( RockDetectiveContext detectiveContext, string idKey )
        {
            var id = Rock.Utility.IdHasher.Instance.GetId( idKey ?? string.Empty );

            return id.HasValue ? new CaseFileService( detectiveContext ).Get( id.Value ) : null;
        }

        private static Tuple<List<Model.CaseEntry>, List<Model.Hypothesis>, List<Model.Experiment>, List<Model.CaseParticipant>> LoadEntities( RockDetectiveContext detectiveContext, int caseFileId )
        {
            return Tuple.Create(
                detectiveContext.CaseEntries.Where( e => e.CaseFileId == caseFileId ).ToList(),
                detectiveContext.Hypotheses.Where( h => h.CaseFileId == caseFileId ).ToList(),
                detectiveContext.Experiments.Where( e => e.CaseFileId == caseFileId ).ToList(),
                detectiveContext.CaseParticipants.Where( p => p.CaseFileId == caseFileId ).ToList() );
        }

        private static CaseFileBag BuildFullBag( RockDetectiveContext detectiveContext, Model.CaseFile caseFile )
        {
            var entities = LoadEntities( detectiveContext, caseFile.Id );
            var statements = detectiveContext.CaseStatements.Where( s => s.CaseFileId == caseFile.Id ).ToList();

            return CaseMapper.ToCaseFileBag( caseFile, entities.Item1, entities.Item2, entities.Item3, entities.Item4, null, statements );
        }

        private IHttpActionResult WithViewableCase( string idKey, Func<RockDetectiveContext, Model.CaseFile, IHttpActionResult> action )
        {
            var person = GetPerson();

            using ( var detectiveContext = new RockDetectiveContext() )
            {
                var caseFile = GetCaseByKey( detectiveContext, idKey );

                if ( caseFile == null )
                {
                    return NotFound();
                }

                if ( person == null || !caseFile.IsAuthorized( Rock.Security.Authorization.VIEW, person ) )
                {
                    return StatusCode( HttpStatusCode.Unauthorized );
                }

                return action( detectiveContext, caseFile );
            }
        }

        private IHttpActionResult WithEditableCase( string idKey, Func<RockDetectiveContext, Model.CaseFile, IHttpActionResult> action )
        {
            var person = GetPerson();

            using ( var detectiveContext = new RockDetectiveContext() )
            {
                var caseFile = GetCaseByKey( detectiveContext, idKey );

                if ( caseFile == null )
                {
                    return NotFound();
                }

                if ( person == null || !caseFile.IsAuthorized( Rock.Security.Authorization.EDIT, person ) )
                {
                    return StatusCode( HttpStatusCode.Unauthorized );
                }

                return action( detectiveContext, caseFile );
            }
        }

        private IHttpActionResult CheckEdit( RockDetectiveContext detectiveContext, int caseFileId, out Model.CaseFile caseFile )
        {
            caseFile = new CaseFileService( detectiveContext ).Get( caseFileId );

            if ( caseFile == null )
            {
                return NotFound();
            }

            var person = GetPerson();

            if ( person == null || !caseFile.IsAuthorized( Rock.Security.Authorization.EDIT, person ) )
            {
                return StatusCode( HttpStatusCode.Unauthorized );
            }

            return null;
        }
    }
}
