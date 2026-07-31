using System;
using System.Data.Entity;
using System.Linq;

using Rock;
using Rock.Data;

using com.yesuchum.RockDetective.Enums;
using com.yesuchum.RockDetective.Model;

namespace com.yesuchum.RockDetective.Data
{
    public class CaseFileService : Service<CaseFile>
    {
        public CaseFileService( RockDetectiveContext context ) : base( context )
        {
        }

        public CaseFileService( Rock.Data.RockContext context ) : base( context )
        {
        }

        public IQueryable<CaseFile> GetOwnedBy( int personId )
        {
            var detectiveContext = ( RockDetectiveContext ) Context;

            using ( var rockContext = new RockContext() )
            {
                var aliasIds = new Rock.Model.PersonAliasService( rockContext )
                    .Queryable()
                    .AsNoTracking()
                    .Where( pa => pa.PersonId == personId )
                    .Select( pa => pa.Id )
                    .ToList();

                return Queryable().Where( c => aliasIds.Contains( c.OwnerPersonAliasId ) );
            }
        }

        public IQueryable<CaseFile> GetAssignedTo( int personId )
        {
            var detectiveContext = ( RockDetectiveContext ) Context;

            using ( var rockContext = new RockContext() )
            {
                var aliasIds = new Rock.Model.PersonAliasService( rockContext )
                    .Queryable()
                    .AsNoTracking()
                    .Where( pa => pa.PersonId == personId )
                    .Select( pa => pa.Id )
                    .ToList();

                var participantCaseIds = detectiveContext.CaseParticipants
                    .Where( p => p.PersonAliasId.HasValue && aliasIds.Contains( p.PersonAliasId.Value ) )
                    .Select( p => p.CaseFileId );

                return Queryable().Where( c => participantCaseIds.Contains( c.Id ) || aliasIds.Contains( c.OwnerPersonAliasId ) );
            }
        }

        public bool CanDelete( CaseFile caseFile, out string errorMessage )
        {
            errorMessage = string.Empty;

            if ( caseFile == null )
            {
                errorMessage = "Case file not found.";
                return false;
            }

            return true;
        }

        public void DeleteWithChildren( CaseFile caseFile )
        {
            var detectiveContext = ( RockDetectiveContext ) Context;

            var statements = detectiveContext.CaseStatements.Where( s => s.CaseFileId == caseFile.Id );
            detectiveContext.CaseStatements.RemoveRange( statements );

            var experiments = detectiveContext.Experiments.Where( e => e.CaseFileId == caseFile.Id );
            detectiveContext.Experiments.RemoveRange( experiments );

            var hypotheses = detectiveContext.Hypotheses.Where( h => h.CaseFileId == caseFile.Id ).ToList();
            foreach ( var hypothesis in hypotheses.Where( h => h.ParentHypothesisId.HasValue ) )
            {
                detectiveContext.Hypotheses.Remove( hypothesis );
            }
            foreach ( var hypothesis in hypotheses.Where( h => !h.ParentHypothesisId.HasValue ) )
            {
                detectiveContext.Hypotheses.Remove( hypothesis );
            }

            var entries = detectiveContext.CaseEntries.Where( e => e.CaseFileId == caseFile.Id );
            detectiveContext.CaseEntries.RemoveRange( entries );

            var participants = detectiveContext.CaseParticipants.Where( p => p.CaseFileId == caseFile.Id );
            detectiveContext.CaseParticipants.RemoveRange( participants );

            detectiveContext.CaseFiles.Remove( caseFile );
        }
    }

    public class CaseParticipantService : Service<CaseParticipant>
    {
        public CaseParticipantService( RockDetectiveContext context ) : base( context )
        {
        }

        public CaseParticipantService( Rock.Data.RockContext context ) : base( context )
        {
        }

        public IQueryable<CaseParticipant> GetByCase( int caseFileId )
        {
            return Queryable().Where( p => p.CaseFileId == caseFileId );
        }

    }

    public class CaseStatementService : Service<CaseStatement>
    {
        public CaseStatementService( RockDetectiveContext context ) : base( context )
        {
        }

        public CaseStatementService( Rock.Data.RockContext context ) : base( context )
        {
        }

        public IQueryable<CaseStatement> GetByCase( int caseFileId )
        {
            return Queryable().Where( s => s.CaseFileId == caseFileId );
        }

        public IQueryable<CaseStatement> GetOverdueRequests( int reminderDays )
        {
            var cutoff = RockDateTime.Now.AddDays( -reminderDays );

            return Queryable().Where( s =>
                ( s.RequestStatus == StatementRequestStatus.Requested || s.RequestStatus == StatementRequestStatus.Reminded )
                && s.SentDateTime.HasValue
                && s.SentDateTime.Value < cutoff );
        }
    }

    public class CaseEntryService : Service<CaseEntry>
    {
        public CaseEntryService( RockDetectiveContext context ) : base( context )
        {
        }

        public CaseEntryService( Rock.Data.RockContext context ) : base( context )
        {
        }

        public IQueryable<CaseEntry> GetByCase( int caseFileId )
        {
            return Queryable().Where( e => e.CaseFileId == caseFileId );
        }
    }

    public class HypothesisService : Service<Hypothesis>
    {
        public HypothesisService( RockDetectiveContext context ) : base( context )
        {
        }

        public HypothesisService( Rock.Data.RockContext context ) : base( context )
        {
        }

        public IQueryable<Hypothesis> GetByCase( int caseFileId )
        {
            return Queryable().Where( h => h.CaseFileId == caseFileId );
        }
    }

    public class ExperimentService : Service<Experiment>
    {
        public ExperimentService( RockDetectiveContext context ) : base( context )
        {
        }

        public ExperimentService( Rock.Data.RockContext context ) : base( context )
        {
        }

        public IQueryable<Experiment> GetByCase( int caseFileId )
        {
            return Queryable().Where( e => e.CaseFileId == caseFileId );
        }
    }
}
