using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Runtime.Serialization;

using Rock.Data;

using com.yesuchum.RockDetective.Enums;

namespace com.yesuchum.RockDetective.Model
{
    [Table( "_com_yesuchum_RockDetective_Experiment" )]
    [DataContract]
    [Rock.SystemGuid.EntityTypeGuid( SystemGuid.EntityType.EXPERIMENT )]
    public class Experiment : Entity<Experiment>, IRockEntity
    {
        [DataMember]
        public int CaseFileId { get; set; }

        [DataMember]
        public int? HypothesisId { get; set; }

        [MaxLength( 300 )]
        [DataMember]
        public string HypothesisText { get; set; }

        [DataMember]
        public string TestPlan { get; set; }

        [DataMember]
        public string ExpectedResult { get; set; }

        [DataMember]
        public string ActualResult { get; set; }

        [DataMember]
        public ExperimentOutcome Outcome { get; set; }

        [DataMember]
        public string ChangesMade { get; set; }

        [DataMember]
        public bool ChangesReverted { get; set; }

        [DataMember]
        public string RevertJustification { get; set; }

        [DataMember]
        public string Learnings { get; set; }

        [DataMember]
        public DateTime? CreatedDateTime { get; set; }

        [DataMember]
        public DateTime? ModifiedDateTime { get; set; }

        [DataMember]
        public int? CreatedByPersonAliasId { get; set; }

        [DataMember]
        public int? ModifiedByPersonAliasId { get; set; }

        [DataMember]
        public virtual CaseFile CaseFile { get; set; }

        [DataMember]
        public virtual Hypothesis Hypothesis { get; set; }
    }

    public partial class ExperimentConfiguration : EntityTypeConfiguration<Experiment>
    {
        public ExperimentConfiguration()
        {
            this.HasRequired( e => e.CaseFile ).WithMany( c => c.Experiments ).HasForeignKey( e => e.CaseFileId ).WillCascadeOnDelete( false );
            this.HasOptional( e => e.Hypothesis ).WithMany().HasForeignKey( e => e.HypothesisId ).WillCascadeOnDelete( false );
            this.HasEntitySetName( "Experiment" );
        }
    }
}
