using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Runtime.Serialization;

using Rock.Data;

using com.yesuchum.RockDetective.Enums;

namespace com.yesuchum.RockDetective.Model
{
    [Table( "_com_yesuchum_RockDetective_CaseParticipant" )]
    [DataContract]
    [Rock.SystemGuid.EntityTypeGuid( SystemGuid.EntityType.CASE_PARTICIPANT )]
    public class CaseParticipant : Entity<CaseParticipant>, IRockEntity
    {
        [DataMember]
        public int CaseFileId { get; set; }

        [DataMember]
        public int? PersonAliasId { get; set; }

        [MaxLength( 200 )]
        [DataMember]
        public string ExternalName { get; set; }

        [MaxLength( 400 )]
        [DataMember]
        public string ExternalContact { get; set; }

        [DataMember]
        public ParticipantRole Role { get; set; }

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
        public virtual System.Collections.Generic.ICollection<CaseStatement> Statements { get; set; } = new System.Collections.Generic.List<CaseStatement>();
    }

    public partial class CaseParticipantConfiguration : EntityTypeConfiguration<CaseParticipant>
    {
        public CaseParticipantConfiguration()
        {
            this.HasRequired( p => p.CaseFile ).WithMany( c => c.Participants ).HasForeignKey( p => p.CaseFileId ).WillCascadeOnDelete( false );
            this.HasEntitySetName( "CaseParticipant" );
        }
    }
}
