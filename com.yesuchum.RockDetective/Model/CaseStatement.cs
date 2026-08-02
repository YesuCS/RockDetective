using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Runtime.Serialization;

using Rock;
using Rock.Data;

using com.yesuchum.RockDetective.Enums;

namespace com.yesuchum.RockDetective.Model
{
    [Table( "_com_yesuchum_RockDetective_CaseStatement" )]
    [DataContract]
    [Rock.SystemGuid.EntityTypeGuid( SystemGuid.EntityType.CASE_STATEMENT )]
    public class CaseStatement : Entity<CaseStatement>, IRockEntity
    {
        [DataMember]
        public int CaseFileId { get; set; }

        [DataMember]
        public int ParticipantId { get; set; }

        [DataMember]
        public string RequestPrompt { get; set; }

        [DataMember]
        public StatementRequestStatus RequestStatus { get; set; }

        [DataMember]
        public DateTime? RequestDateTime { get; set; }

        [DataMember]
        public DateTime? SentDateTime { get; set; }

        [DataMember]
        public DateTime? SubmittedDateTime { get; set; }

        [DataMember]
        public string Statement { get; set; }

        [DataMember]
        public int? BinaryFileId { get; set; }

        [MaxLength( 2000 )]
        [DataMember]
        public string Url { get; set; }

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
        public virtual CaseParticipant Participant { get; set; }

        public override string ToString()
        {
            return RequestPrompt.IsNotNullOrWhiteSpace() ? RequestPrompt : "Witness statement request";
        }
    }

    public partial class CaseStatementConfiguration : EntityTypeConfiguration<CaseStatement>
    {
        public CaseStatementConfiguration()
        {
            this.HasRequired( s => s.CaseFile ).WithMany().HasForeignKey( s => s.CaseFileId ).WillCascadeOnDelete( false );
            this.HasRequired( s => s.Participant ).WithMany( p => p.Statements ).HasForeignKey( s => s.ParticipantId ).WillCascadeOnDelete( false );
            this.HasEntitySetName( "CaseStatement" );
        }
    }
}
