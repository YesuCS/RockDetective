using System.Data.Entity;

using com.yesuchum.RockDetective.Model;

namespace com.yesuchum.RockDetective.Data
{
    public class RockDetectiveContext : Rock.Data.DbContext
    {
        static RockDetectiveContext()
        {
            Database.SetInitializer<RockDetectiveContext>( null );
        }

        public DbSet<CaseFile> CaseFiles { get; set; }

        public DbSet<CaseParticipant> CaseParticipants { get; set; }

        public DbSet<CaseEntry> CaseEntries { get; set; }

        public DbSet<Hypothesis> Hypotheses { get; set; }

        public DbSet<Experiment> Experiments { get; set; }

        public DbSet<CaseStatement> CaseStatements { get; set; }

        public RockDetectiveContext()
            : base( "RockContext" )
        {
        }

        public RockDetectiveContext( string nameOrConnectionString )
            : base( nameOrConnectionString )
        {
        }

        protected override void OnModelCreating( DbModelBuilder modelBuilder )
        {
            base.OnModelCreating( modelBuilder );

            modelBuilder.Configurations.Add( new CaseFileConfiguration() );
            modelBuilder.Configurations.Add( new CaseParticipantConfiguration() );
            modelBuilder.Configurations.Add( new CaseEntryConfiguration() );
            modelBuilder.Configurations.Add( new HypothesisConfiguration() );
            modelBuilder.Configurations.Add( new ExperimentConfiguration() );
            modelBuilder.Configurations.Add( new CaseStatementConfiguration() );
        }
    }
}
