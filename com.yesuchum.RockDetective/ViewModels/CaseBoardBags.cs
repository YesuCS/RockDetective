using System;
using System.Collections.Generic;

namespace com.yesuchum.RockDetective.ViewModels
{
    public class CaseBoardInitBag
    {
        public CaseFileBag CaseFile { get; set; }

        public List<PhaseDefinitionBag> PhaseDefinitions { get; set; } = new List<PhaseDefinitionBag>();

        public List<string> Principles { get; set; } = new List<string>();

        public string MindsetGuidance { get; set; }

        public List<string> ContextualTips { get; set; } = new List<string>();

        public bool CanEdit { get; set; }

        public bool CanAdministrate { get; set; }

        public bool IsOwner { get; set; }

        public bool IsChampion { get; set; }

        public List<CategoryOptionBag> Categories { get; set; } = new List<CategoryOptionBag>();

        public string ErrorMessage { get; set; }

        public string FollowUpActionMessage { get; set; }
    }

    public class CaseFileBag
    {
        public string IdKey { get; set; }

        public string CaseNumber { get; set; }

        public string Title { get; set; }

        public string ProblemStatement { get; set; }

        public string CategoryGuid { get; set; }

        public string CategoryName { get; set; }

        public int Mode { get; set; }

        public int Classification { get; set; }

        public int Status { get; set; }

        public int CurrentPhase { get; set; }

        public string OwnerName { get; set; }

        public string OwnerPersonAliasGuid { get; set; }

        public string ImpactSummary { get; set; }

        public string SolutionSummary { get; set; }

        public string ContributingFactors { get; set; }

        public string LessonsLearned { get; set; }

        public string PreventionMeasures { get; set; }

        public string ActionItems { get; set; }

        public DateTime? FollowUpDate { get; set; }

        public DateTime? FollowUpConfirmedDateTime { get; set; }

        public DateTime? ClosedDateTime { get; set; }

        public DateTime? CreatedDateTime { get; set; }

        public List<PhaseStateBag> PhaseStates { get; set; } = new List<PhaseStateBag>();

        public List<GateResultBag> GateResults { get; set; } = new List<GateResultBag>();

        public List<CaseEntryBag> Entries { get; set; } = new List<CaseEntryBag>();

        public List<HypothesisBag> Hypotheses { get; set; } = new List<HypothesisBag>();

        public List<ExperimentBag> Experiments { get; set; } = new List<ExperimentBag>();

        public List<ParticipantBag> Participants { get; set; } = new List<ParticipantBag>();
    }

    public class CaseEntryBag
    {
        public string IdKey { get; set; }

        public int EntryType { get; set; }

        public string Text { get; set; }

        public DateTime? EventDateTime { get; set; }

        public int? TimelineEventType { get; set; }

        public string BinaryFileGuid { get; set; }

        public string BinaryFileName { get; set; }

        public string Url { get; set; }

        public int Source { get; set; }

        public string SourceParticipantIdKey { get; set; }

        public string SourceParticipantName { get; set; }

        public string SourceExperimentIdKey { get; set; }

        public bool IsCorroborated { get; set; }

        public int Order { get; set; }

        public DateTime? CreatedDateTime { get; set; }
    }

    public class HypothesisBag
    {
        public string IdKey { get; set; }

        public string ParentHypothesisIdKey { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public int Probability { get; set; }

        public int Status { get; set; }

        public string RuledOutReason { get; set; }
    }

    public class ExperimentBag
    {
        public string IdKey { get; set; }

        public string HypothesisIdKey { get; set; }

        public string HypothesisText { get; set; }

        public string TestPlan { get; set; }

        public string ExpectedResult { get; set; }

        public string ActualResult { get; set; }

        public int Outcome { get; set; }

        public string ChangesMade { get; set; }

        public bool ChangesReverted { get; set; }

        public string RevertJustification { get; set; }

        public string Learnings { get; set; }

        public DateTime? CreatedDateTime { get; set; }
    }

    public class ParticipantBag
    {
        public string IdKey { get; set; }

        public string PersonAliasGuid { get; set; }

        public string DisplayName { get; set; }

        public string ExternalContact { get; set; }

        public bool IsExternal { get; set; }

        public int Role { get; set; }

        public List<StatementBag> Statements { get; set; } = new List<StatementBag>();
    }

    public class StatementBag
    {
        public string IdKey { get; set; }

        public string ParticipantIdKey { get; set; }

        public string RequestPrompt { get; set; }

        public int RequestStatus { get; set; }

        public DateTime? RequestDateTime { get; set; }

        public DateTime? SentDateTime { get; set; }

        public DateTime? SubmittedDateTime { get; set; }

        public string Statement { get; set; }

        public string BinaryFileGuid { get; set; }

        public string BinaryFileName { get; set; }

        public string Url { get; set; }

        public bool IsOverdue { get; set; }
    }

    public class PhaseFormSaveBag
    {
        public int Phase { get; set; }

        public string Summary { get; set; }

        public Dictionary<string, string> Data { get; set; } = new Dictionary<string, string>();
    }

    public class CloseCaseBag
    {
        public string ImpactSummary { get; set; }

        public string SolutionSummary { get; set; }

        public string ContributingFactors { get; set; }

        public string LessonsLearned { get; set; }

        public string PreventionMeasures { get; set; }

        public string ActionItems { get; set; }

        public DateTime? FollowUpDate { get; set; }
    }
}
