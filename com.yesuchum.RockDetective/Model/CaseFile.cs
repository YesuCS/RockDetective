using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Runtime.Serialization;

using Rock.Data;
using Rock.Security;

using com.yesuchum.RockDetective.Enums;

namespace com.yesuchum.RockDetective.Model
{
    [Table( "_com_yesuchum_RockDetective_CaseFile" )]
    [DataContract]
    [Rock.SystemGuid.EntityTypeGuid( SystemGuid.EntityType.CASE_FILE )]
    public class CaseFile : Entity<CaseFile>, IRockEntity, ISecured, ICategorized, IModel
    {
        [Required]
        [MaxLength( 200 )]
        [DataMember]
        public string Title { get; set; }

        [DataMember]
        public string ProblemStatement { get; set; }

        [DataMember]
        public int? CategoryId { get; set; }

        [DataMember]
        public CaseMode Mode { get; set; }

        [DataMember]
        public CaseClassification Classification { get; set; }

        [DataMember]
        public CaseStatus Status { get; set; }

        [DataMember]
        public CasePhase CurrentPhase { get; set; }

        [DataMember]
        public string PhaseStateJson { get; set; }

        [DataMember]
        public int OwnerPersonAliasId { get; set; }

        [DataMember]
        public string ImpactSummary { get; set; }

        [DataMember]
        public string SolutionSummary { get; set; }

        [DataMember]
        public string ContributingFactors { get; set; }

        [DataMember]
        public string LessonsLearned { get; set; }

        [DataMember]
        public string PreventionMeasures { get; set; }

        [DataMember]
        public string ActionItems { get; set; }

        [DataMember]
        public DateTime? FollowUpDate { get; set; }

        [DataMember]
        public DateTime? FollowUpConfirmedDateTime { get; set; }

        [DataMember]
        public DateTime? ClosedDateTime { get; set; }

        [DataMember]
        public DateTime? CreatedDateTime { get; set; }

        [DataMember]
        public DateTime? ModifiedDateTime { get; set; }

        [DataMember]
        public int? CreatedByPersonAliasId { get; set; }

        [DataMember]
        public int? ModifiedByPersonAliasId { get; set; }

        [DataMember]
        public virtual ICollection<CaseParticipant> Participants { get; set; } = new List<CaseParticipant>();

        [DataMember]
        public virtual ICollection<CaseEntry> Entries { get; set; } = new List<CaseEntry>();

        [DataMember]
        public virtual ICollection<Hypothesis> Hypotheses { get; set; } = new List<Hypothesis>();

        [DataMember]
        public virtual ICollection<Experiment> Experiments { get; set; } = new List<Experiment>();

        [NotMapped]
        public string Name => Title;

        public override string ToString()
        {
            return Title;
        }

        private Rock.Model.Category _category;
        private bool _categoryLoaded;

        [NotMapped]
        public virtual Rock.Model.Category Category
        {
            get
            {
                if ( !_categoryLoaded )
                {
                    _category = LoadCategory( CategoryId );
                    _categoryLoaded = true;
                }
                return _category;
            }
        }

        private static Rock.Model.Category LoadCategory( int? categoryId )
        {
            if ( !categoryId.HasValue )
            {
                return null;
            }

            try
            {
                using ( var rockContext = new RockContext() )
                {
                    return new Rock.Model.CategoryService( rockContext )
                        .Queryable()
                        .AsNoTracking()
                        .FirstOrDefault( c => c.Id == categoryId.Value );
                }
            }
            catch
            {
                return null;
            }
        }

        #region ISecured

        [NotMapped]
        public virtual ISecured ParentAuthority
        {
            get
            {
                if ( Id == 0 )
                {
                    return new GlobalDefault();
                }

                return new CaseFile();
            }
        }

        [NotMapped]
        public virtual ISecured ParentAuthorityPre => null;

        [NotMapped]
        public virtual Dictionary<string, string> SupportedActions => new Dictionary<string, string>
        {
            { Authorization.VIEW, "The roles and/or users that have access to view this case." },
            { Authorization.EDIT, "The roles and/or users that have access to edit this case." },
            { Authorization.ADMINISTRATE, "The roles and/or users that have access to administrate this case." }
        };

        public virtual bool IsAuthorized( string action, Rock.Model.Person person )
        {
            if ( action == Authorization.VIEW && person != null && IsParticipant( person ) )
            {
                return true;
            }

            return Authorization.Authorized( this, action, person );
        }

        public virtual bool IsAllowedByDefault( string action )
        {
            return false;
        }

        public virtual bool IsPrivate( string action, Rock.Model.Person person )
        {
            return Authorization.IsPrivate( this, action, person );
        }

        public virtual void MakePrivate( string action, Rock.Model.Person person, RockContext rockContext = null )
        {
            Authorization.MakePrivate( this, action, person, rockContext );
        }

        public virtual void MakeUnPrivate( string action, Rock.Model.Person person, RockContext rockContext = null )
        {
            Authorization.MakeUnPrivate( this, action, person, rockContext );
        }

        private List<int> _participantPersonIds;

        private bool IsParticipant( Rock.Model.Person person )
        {
            if ( Id == 0 )
            {
                return false;
            }

            if ( _participantPersonIds == null )
            {
                try
                {
                    using ( var rockContext = new RockContext() )
                    using ( var detectiveContext = new Data.RockDetectiveContext() )
                    {
                        var caseId = Id;
                        var aliasIds = detectiveContext.CaseParticipants
                            .Where( p => p.CaseFileId == caseId && p.PersonAliasId.HasValue )
                            .Select( p => p.PersonAliasId.Value )
                            .ToList();

                        aliasIds.Add( OwnerPersonAliasId );

                        _participantPersonIds = new Rock.Model.PersonAliasService( rockContext )
                            .Queryable()
                            .AsNoTracking()
                            .Where( pa => aliasIds.Contains( pa.Id ) )
                            .Select( pa => pa.PersonId )
                            .Distinct()
                            .ToList();
                    }
                }
                catch
                {
                    _participantPersonIds = new List<int>();
                }
            }

            return _participantPersonIds.Contains( person.Id );
        }

        #endregion

        #region IModel

        private Rock.Model.PersonAlias _createdByPersonAlias;
        private bool _createdByPersonAliasLoaded;
        private Rock.Model.PersonAlias _modifiedByPersonAlias;
        private bool _modifiedByPersonAliasLoaded;

        [NotMapped]
        public virtual Rock.Model.PersonAlias CreatedByPersonAlias
        {
            get
            {
                if ( !_createdByPersonAliasLoaded )
                {
                    _createdByPersonAlias = LoadPersonAlias( CreatedByPersonAliasId );
                    _createdByPersonAliasLoaded = true;
                }
                return _createdByPersonAlias;
            }
            set
            {
                _createdByPersonAlias = value;
                _createdByPersonAliasLoaded = true;
            }
        }

        [NotMapped]
        public virtual Rock.Model.PersonAlias ModifiedByPersonAlias
        {
            get
            {
                if ( !_modifiedByPersonAliasLoaded )
                {
                    _modifiedByPersonAlias = LoadPersonAlias( ModifiedByPersonAliasId );
                    _modifiedByPersonAliasLoaded = true;
                }
                return _modifiedByPersonAlias;
            }
            set
            {
                _modifiedByPersonAlias = value;
                _modifiedByPersonAliasLoaded = true;
            }
        }

        private static Rock.Model.PersonAlias LoadPersonAlias( int? personAliasId )
        {
            if ( !personAliasId.HasValue )
            {
                return null;
            }

            try
            {
                using ( var rockContext = new RockContext() )
                {
                    return new Rock.Model.PersonAliasService( rockContext )
                        .Queryable( "Person" )
                        .AsNoTracking()
                        .FirstOrDefault( pa => pa.Id == personAliasId.Value );
                }
            }
            catch
            {
                return null;
            }
        }

        [NotMapped]
        public virtual object CustomSortValue { get; set; }

        [NotMapped]
        public bool ModifiedAuditValuesAlreadyUpdated { get; set; } = true;

        public virtual void PreSaveChanges( Rock.Data.DbContext dbContext, System.Data.Entity.Infrastructure.DbEntityEntry entry )
        {
        }

        public virtual void PreSaveChanges( Rock.Data.DbContext dbContext, System.Data.Entity.Infrastructure.DbEntityEntry entry, EntityState state )
        {
        }

        public virtual void PostSaveChanges( Rock.Data.DbContext dbContext )
        {
        }

        public virtual string GetCreatedAuditHtml( string rootUrl )
        {
            return string.Empty;
        }

        public virtual string GetModifiedAuditHtml( string rootUrl )
        {
            return string.Empty;
        }

        #endregion
    }

    public partial class CaseFileConfiguration : EntityTypeConfiguration<CaseFile>
    {
        public CaseFileConfiguration()
        {
            this.HasEntitySetName( "CaseFile" );
        }
    }
}
