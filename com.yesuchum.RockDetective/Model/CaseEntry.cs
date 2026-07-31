using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Runtime.Serialization;

using Rock.Data;

using com.yesuchum.RockDetective.Enums;

namespace com.yesuchum.RockDetective.Model
{
    [Table( "_com_yesuchum_RockDetective_CaseEntry" )]
    [DataContract]
    [Rock.SystemGuid.EntityTypeGuid( SystemGuid.EntityType.CASE_ENTRY )]
    public class CaseEntry : Entity<CaseEntry>, IRockEntity
    {
        [DataMember]
        public int CaseFileId { get; set; }

        [DataMember]
        public CaseEntryType EntryType { get; set; }

        [Required]
        [DataMember]
        public string Text { get; set; }

        [DataMember]
        public DateTime? EventDateTime { get; set; }

        [DataMember]
        public TimelineEventType? TimelineEventType { get; set; }

        [DataMember]
        public int? BinaryFileId { get; set; }

        [MaxLength( 2000 )]
        [DataMember]
        public string Url { get; set; }

        [DataMember]
        public CaseEntrySource Source { get; set; }

        [DataMember]
        public int? SourceParticipantId { get; set; }

        [DataMember]
        public int? SourceExperimentId { get; set; }

        [DataMember]
        public bool IsCorroborated { get; set; }

        [DataMember]
        public int Order { get; set; }

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
    }

    public partial class CaseEntryConfiguration : EntityTypeConfiguration<CaseEntry>
    {
        public CaseEntryConfiguration()
        {
            this.HasRequired( e => e.CaseFile ).WithMany( c => c.Entries ).HasForeignKey( e => e.CaseFileId ).WillCascadeOnDelete( false );
            this.HasEntitySetName( "CaseEntry" );
        }
    }
}
