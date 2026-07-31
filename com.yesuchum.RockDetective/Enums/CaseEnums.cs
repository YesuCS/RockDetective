namespace com.yesuchum.RockDetective.Enums
{
    public enum CaseStatus
    {
        Open = 0,
        Investigating = 1,
        Solved = 2,
        Closed = 3,
        Reopened = 4,
        Abandoned = 5
    }

    public enum CaseMode
    {
        Simple = 0,
        Advanced = 1
    }

    public enum CaseClassification
    {
        Unclassified = 0,
        Easy = 1,
        Difficult = 2
    }

    public enum CasePhase
    {
        DefineTeam = 1,
        DefineProblem = 2,
        Classify = 3,
        Timeline = 4,
        FactList = 5,
        ProbabilityTree = 6,
        Experiments = 7,
        Reevaluate = 8,
        Postmortem = 9
    }

    public enum ParticipantRole
    {
        Owner = 0,
        Champion = 1,
        Recorder = 2,
        Communicator = 3,
        Member = 4,
        Witness = 5
    }

    public enum StatementRequestStatus
    {
        None = 0,
        Draft = 1,
        Requested = 2,
        Reminded = 3,
        Submitted = 4,
        Declined = 5,
        ManuallyRecorded = 6
    }

    public enum CaseEntryType
    {
        Fact = 0,
        TimelineEvent = 1,
        Evidence = 2
    }

    public enum TimelineEventType
    {
        ProblemStart = 0,
        LastKnownGood = 1,
        Change = 2,
        Activity = 3,
        Other = 4
    }

    public enum CaseEntrySource
    {
        Direct = 0,
        Witness = 1,
        Experiment = 2
    }

    public enum HypothesisStatus
    {
        Proposed = 0,
        Testing = 1,
        RuledOut = 2,
        Confirmed = 3
    }

    public enum ExperimentOutcome
    {
        Pending = 0,
        Supported = 1,
        RuledOut = 2,
        Inconclusive = 3
    }
}
