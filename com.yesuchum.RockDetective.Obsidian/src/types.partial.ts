export const CaseStatus = {
    Open: 0,
    Investigating: 1,
    Solved: 2,
    Closed: 3,
    Reopened: 4
} as const;

export const CaseMode = {
    Simple: 0,
    Advanced: 1
} as const;

export const CasePhase = {
    DefineTeam: 1,
    DefineProblem: 2,
    Classify: 3,
    Timeline: 4,
    FactList: 5,
    ProbabilityTree: 6,
    Experiments: 7,
    Reevaluate: 8,
    Postmortem: 9
} as const;

export const ParticipantRole = {
    Owner: 0,
    Champion: 1,
    Recorder: 2,
    Communicator: 3,
    Member: 4,
    Witness: 5
} as const;

export const StatementRequestStatus = {
    None: 0,
    Draft: 1,
    Requested: 2,
    Reminded: 3,
    Submitted: 4,
    Declined: 5,
    ManuallyRecorded: 6
} as const;

export const CaseEntryType = {
    Fact: 0,
    TimelineEvent: 1,
    Evidence: 2
} as const;

export const TimelineEventType = {
    ProblemStart: 0,
    LastKnownGood: 1,
    Change: 2,
    Activity: 3,
    Other: 4
} as const;

export const CaseEntrySource = {
    Direct: 0,
    Witness: 1,
    Experiment: 2
} as const;

export const HypothesisStatus = {
    Proposed: 0,
    Testing: 1,
    RuledOut: 2,
    Confirmed: 3
} as const;

export const ExperimentOutcome = {
    Pending: 0,
    Supported: 1,
    RuledOut: 2,
    Inconclusive: 3
} as const;

export const statusNames = ["Open", "Investigating", "Solved", "Closed", "Reopened", "Abandoned"];
export const roleNames = ["Owner", "Champion", "Recorder", "Communicator", "Member", "Witness"];
export const requestStatusNames = ["None", "Draft", "Requested", "Reminded", "Submitted", "Declined", "Recorded Manually"];
export const hypothesisStatusNames = ["Proposed", "Testing", "Ruled Out", "Confirmed"];
export const outcomeNames = ["Pending", "Supported", "Ruled Out", "Inconclusive"];
export const timelineTypeNames = ["Problem Start", "Last Known Good", "Change", "Activity", "Other"];

export type CategoryOptionBag = {
    guid: string;
    name: string;
    iconCssClass?: string | null;
};

export type GateRequirementBag = {
    description: string;
    isMet: boolean;
};

export type GateResultBag = {
    phase: number;
    isMet: boolean;
    requirements: GateRequirementBag[];
};

export type PhaseDefinitionBag = {
    phase: number;
    name: string;
    guidance: string;
    prompts: string[];
    isAutoSkippedInSimpleMode: boolean;
    isLoopPhase: boolean;
};

export type PhaseStateBag = {
    phase: number;
    progress: number;
    completedDateTime?: string | null;
    summary?: string | null;
    data: Record<string, string>;
};

export type CaseFileListItemBag = {
    idKey: string;
    caseNumber: string;
    title: string;
    categoryName?: string | null;
    status: string;
    phase: string;
    mode: string;
    ageDays: number;
    hasOverdueWitness: boolean;
    ownerName: string;
    solutionSummary?: string | null;
    canDelete: boolean;
    isMine: boolean;
    isAssignedToMe: boolean;
    hasMyPendingStatement: boolean;
    createdDateTime?: string | null;
    closedDateTime?: string | null;
};

export type CaseFileListInitBag = {
    cases: CaseFileListItemBag[];
    categories: CategoryOptionBag[];
    defaultView: string;
    canAddCase: boolean;
    detailPageUrl?: string | null;
};

export type NewCaseResultBag = {
    idKey: string;
    caseNumber: string;
    detailUrl: string;
};

export type ArchiveSearchResultBag = {
    cases: CaseFileListItemBag[];
};

export type CaseEntryBag = {
    idKey?: string | null;
    entryType: number;
    text: string;
    eventDateTime?: string | null;
    timelineEventType?: number | null;
    binaryFileGuid?: string | null;
    binaryFileName?: string | null;
    url?: string | null;
    source: number;
    sourceParticipantIdKey?: string | null;
    sourceParticipantName?: string | null;
    sourceExperimentIdKey?: string | null;
    isCorroborated: boolean;
    order: number;
    createdDateTime?: string | null;
};

export type HypothesisBag = {
    idKey?: string | null;
    parentHypothesisIdKey?: string | null;
    name: string;
    description?: string | null;
    probability: number;
    status: number;
    ruledOutReason?: string | null;
};

export type ExperimentBag = {
    idKey?: string | null;
    hypothesisIdKey?: string | null;
    hypothesisText?: string | null;
    testPlan?: string | null;
    expectedResult?: string | null;
    actualResult?: string | null;
    outcome: number;
    changesMade?: string | null;
    changesReverted: boolean;
    revertJustification?: string | null;
    learnings?: string | null;
    createdDateTime?: string | null;
};

export type ParticipantBag = {
    idKey?: string | null;
    personAliasGuid?: string | null;
    displayName?: string | null;
    externalContact?: string | null;
    isExternal: boolean;
    role: number;
    statements: StatementBag[];
};

export type StatementBag = {
    idKey?: string | null;
    participantIdKey?: string | null;
    requestPrompt?: string | null;
    requestStatus: number;
    requestDateTime?: string | null;
    sentDateTime?: string | null;
    submittedDateTime?: string | null;
    statement?: string | null;
    binaryFileGuid?: string | null;
    binaryFileName?: string | null;
    url?: string | null;
    isOverdue: boolean;
};

export type CaseFileBag = {
    idKey: string;
    caseNumber: string;
    title: string;
    problemStatement?: string | null;
    categoryGuid?: string | null;
    categoryName?: string | null;
    mode: number;
    classification: number;
    status: number;
    currentPhase: number;
    ownerName: string;
    ownerPersonAliasGuid?: string | null;
    impactSummary?: string | null;
    solutionSummary?: string | null;
    contributingFactors?: string | null;
    lessonsLearned?: string | null;
    preventionMeasures?: string | null;
    actionItems?: string | null;
    followUpDate?: string | null;
    followUpConfirmedDateTime?: string | null;
    closedDateTime?: string | null;
    createdDateTime?: string | null;
    phaseStates: PhaseStateBag[];
    gateResults: GateResultBag[];
    entries: CaseEntryBag[];
    hypotheses: HypothesisBag[];
    experiments: ExperimentBag[];
    participants: ParticipantBag[];
};

export type CaseBoardInitBag = {
    caseFile?: CaseFileBag | null;
    phaseDefinitions: PhaseDefinitionBag[];
    principles: string[];
    mindsetGuidance: string;
    contextualTips: string[];
    canEdit: boolean;
    canAdministrate: boolean;
    isOwner: boolean;
    isChampion: boolean;
    categories: CategoryOptionBag[];
    errorMessage?: string | null;
    followUpActionMessage?: string | null;
};

export type WitnessStatementInitBag = {
    participantIdKey?: string | null;
    caseNumber?: string | null;
    caseTitle?: string | null;
    requestPrompt?: string | null;
    requestorName?: string | null;
    canSubmit: boolean;
    isAlreadySubmitted: boolean;
    isDeclined: boolean;
    errorMessage?: string | null;
};

export type PdfResultBag = {
    fileName: string;
    base64: string;
};

export function downloadPdf(result: PdfResultBag): void {
    const bytes = atob(result.base64);
    const buffer = new Uint8Array(bytes.length);
    for (let i = 0; i < bytes.length; i++) {
        buffer[i] = bytes.charCodeAt(i);
    }
    const blob = new Blob([buffer], { type: "application/pdf" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = result.fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    setTimeout(() => URL.revokeObjectURL(url), 0);
}
