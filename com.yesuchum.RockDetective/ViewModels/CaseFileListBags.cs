using System;
using System.Collections.Generic;

namespace com.yesuchum.RockDetective.ViewModels
{
    public class CaseFileListInitBag
    {
        public List<CaseFileListItemBag> Cases { get; set; } = new List<CaseFileListItemBag>();

        public List<CategoryOptionBag> Categories { get; set; } = new List<CategoryOptionBag>();

        public string DefaultView { get; set; }

        public bool CanAddCase { get; set; }

        public string DetailPageUrl { get; set; }
    }

    public class CaseFileListItemBag
    {
        public string IdKey { get; set; }

        public string CaseNumber { get; set; }

        public string Title { get; set; }

        public string CategoryName { get; set; }

        public string Status { get; set; }

        public string Phase { get; set; }

        public string Mode { get; set; }

        public int AgeDays { get; set; }

        public bool HasOverdueWitness { get; set; }

        public string OwnerName { get; set; }

        public string SolutionSummary { get; set; }

        public bool CanDelete { get; set; }

        public bool IsMine { get; set; }

        public bool IsAssignedToMe { get; set; }

        public bool HasMyPendingStatement { get; set; }

        public DateTime? CreatedDateTime { get; set; }

        public DateTime? ClosedDateTime { get; set; }
    }

    public class NewCaseRequestBag
    {
        public string Title { get; set; }

        public string ProblemStatement { get; set; }

        public string CategoryGuid { get; set; }

        public bool AttestNoUnverifiedDetails { get; set; }

        public bool AttestNothingNotGermane { get; set; }

        public int ScoreImpact { get; set; }

        public int ScoreUrgency { get; set; }

        public int ScoreRecurrence { get; set; }

        public int ScoreScope { get; set; }

        public int Mode { get; set; }
    }

    public class NewCaseResultBag
    {
        public string IdKey { get; set; }

        public string CaseNumber { get; set; }

        public string DetailUrl { get; set; }
    }

    public class ArchiveSearchResultBag
    {
        public List<CaseFileListItemBag> Cases { get; set; } = new List<CaseFileListItemBag>();
    }
}
