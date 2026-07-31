using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Runtime.Serialization;

using Rock.Data;

using com.yesuchum.RockDetective.Enums;

namespace com.yesuchum.RockDetective.Model
{
    [Table( "_com_yesuchum_RockDetective_Hypothesis" )]
    [DataContract]
    [Rock.SystemGuid.EntityTypeGuid( SystemGuid.EntityType.HYPOTHESIS )]
    public class Hypothesis : Entity<Hypothesis>, IRockEntity
    {
        [DataMember]
        public int CaseFileId { get; set; }

        [DataMember]
        public int? ParentHypothesisId { get; set; }

        [Required]
        [MaxLength( 300 )]
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public int Probability { get; set; }

        [DataMember]
        public HypothesisStatus Status { get; set; }

        [DataMember]
        public string RuledOutReason { get; set; }

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
        public virtual Hypothesis ParentHypothesis { get; set; }

        [DataMember]
        public virtual ICollection<Hypothesis> ChildHypotheses { get; set; } = new List<Hypothesis>();
    }

    public partial class HypothesisConfiguration : EntityTypeConfiguration<Hypothesis>
    {
        public HypothesisConfiguration()
        {
            this.HasRequired( h => h.CaseFile ).WithMany( c => c.Hypotheses ).HasForeignKey( h => h.CaseFileId ).WillCascadeOnDelete( false );
            this.HasOptional( h => h.ParentHypothesis ).WithMany( h => h.ChildHypotheses ).HasForeignKey( h => h.ParentHypothesisId ).WillCascadeOnDelete( false );
            this.HasEntitySetName( "Hypothesis" );
        }
    }
}
