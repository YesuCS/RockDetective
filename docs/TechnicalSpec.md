# Rock Detective Technical Specification

Rock Detective is a self-contained Rock RMS plugin with no core modifications. It is a C# class library (net472) plus Obsidian frontends (Vue 3 and TypeScript) for the Triumphant Problem Solving method by Jon Edmiston. The plugin migration is numbered for Rock 18.2, which is the minimum version. The current version is 1.0.1.

## Architecture

```
com.yesuchum.RockDetective/           C# class library
  Model/                              Six entities (Entity<T> + IRockEntity)
  Enums/                              All case enums
  Data/                               Plugin DbContext, services, CaseMapper,
                                      WitnessReminderHelper
  Method/                             Method content, gate evaluator, phase state
  Migrations/                         Plugin migration (squashed _001)
  Blocks/                             Three RockBlockType backends
  Jobs/                               Two RockJob classes
  Pdf/                                HTML report builder
  Rest/                               ApiControllerBase REST controller
  SystemGuid/                         Stable GUID constants
  ViewModels/                         Bags for blocks and REST
com.yesuchum.RockDetective.Obsidian/  Frontends, built with obsidian-build-tools
com.yesuchum.RockDetective.Tests/     MSTest unit tests for the gate evaluator
```

The three blocks live under Admin Tools > Installed Plugins > Rock Detective:

| Block | Page | Role |
|---|---|---|
| Case File List | Rock Detective | Tabs for My Requests, My Assignments, All, and Archive; new case modal; delete. |
| Case Board | Case Board | The workspace for one case. Takes a `CaseFile` page parameter (IdKey or Guid). |
| Witness Statement Entry | Witness Statement | Where a witness submits or declines a request. Takes a `Statement` page parameter. |

Case Board and Witness Statement are set to never show in navigation. Block settings are ordinary attributes on the block classes. The Case Board reads its gate settings into `GateRequirementOptions` and falls back to the evaluator defaults when a value is blank. Both linked-page settings (`DetailPage` on the list, `WitnessStatementPage` on the board) are created by the migration itself.

## Data model

Six tables, prefix `_com_yesuchum_RockDetective_`:

- **CaseFile**: title, problem statement, CategoryId (Rock Category tree scoped to this entity type), mode (Simple or Advanced), classification, status (Open, Investigating, Solved, Closed, Reopened, Abandoned), current phase, PhaseStateJson, owner, postmortem fields, follow-up fields. The case number shown everywhere is just the entity Id.
- **CaseParticipant**: person (PersonAliasId) or external (name and contact), role (Owner, Champion, Recorder, Communicator, Member, Witness).
- **CaseStatement**: one row per statement request or recorded statement; prompt, request status (None, Draft, Requested, Reminded, Submitted, Declined, ManuallyRecorded), statement text, optional BinaryFileId and Url. A participant can have any number.
- **CaseEntry**: facts, timeline events, and evidence in one table; type, optional event date and timeline event type, source tracking (Direct, Witness, Experiment), corroboration flag, optional BinaryFileId and Url.
- **Hypothesis**: tree via ParentHypothesisId; probability 0 to 100; status (Proposed, Testing, RuledOut, Confirmed).
- **Experiment**: optional hypothesis link or free-text hypothesis, test plan, expected and actual results, outcome (Pending, Supported, RuledOut, Inconclusive), changes made, changes reverted or a justification, learnings.

Per-phase state (progress, completion dates, notes, attestations, champion waiver and sign-off, loop-back count, classify scores) is JSON on CaseFile.PhaseStateJson, not a table. All tables have foreign keys back to CaseFile, and the case delete removes children in order.

### Entity plumbing that matters

These were all learned the hard way; do not undo them:

- Entities inherit `Rock.Data.Entity<T>` with plain audit columns, not `Model<T>`. Model<T>'s PersonAlias navigation properties drag the full Rock entity graph into the plugin's EF6 context and break its model build.
- Every entity implements `IRockEntity`. RockContext only registers plugin entities that carry that marker (plus `[DataContract]`); without it, anything resolving entities through RockContext (the AuditDetail endpoint, generic `Service<T>` reflection) fails with "not part of the model for the current context".
- Every service has two constructors: the plugin context and `Rock.Data.RockContext`. Rock's `Reflection.GetServiceForEntityType` instantiates plugin services with a RockContext.
- The plugin DbContext passes `base("RockContext")` (not RockApp settings; those are not ready during migration runs) and disables the EF6 initializer in a static constructor (`Database.SetInitializer<RockDetectiveContext>(null)`); without that, any schema change bricks the site with "the model backing the context has changed".
- CaseFile implements `ISecured`, `ICategorized`, and `IModel` (lazy PersonAlias loads via `[NotMapped]` properties so the AuditDetail control shows names).

### Security

`CaseFile.ParentAuthority` is the entity-type default, which the migration seeds for the Rock Detective Users role (View, Edit) and Rock Administration (View, Edit, Administrate). `IsAuthorized` adds one rule: the owner and every Rock-person participant is granted View. Edit is never granted that way.

The Case Board treats the owner as an editor too (`CanEdit` is Edit security or ownership), and every block action re-checks that server-side. The REST controller checks Edit security only, so an owner without Edit security can work a case on the board but not through the API. The witness page authorizes by matching the logged-in person to the participant's alias; anyone with Edit on the case can also open it.

## Method content and gates

`Method/MethodDefinition.cs` holds the nine phases, prompts, principles, and contextual tips as code; they are method content, not user data. `Method/GateEvaluator.cs` is a pure function over a `CaseAggregate` snapshot. It takes a `GateRequirementOptions` whose defaults match the method (3 facts Advanced, 1 Simple; 2 root hypotheses Advanced, 1 Simple; 2 timeline events; 1 experiment; attestations on). The evaluator is fully unit-tested in the Tests project (21 tests); run `dotnet test`.

Gate rules worth knowing:

- Simple mode auto-skips Define the Team and Timeline, and skips the attestations.
- Define the Team requires an owner and a Champion, or a recorded waiver (`ChampionWaived` plus a reason in phase state). The board has no control for setting the waiver yet.
- The timeline gate counts all events toward the minimum and separately requires one Problem Start.
- A root hypothesis counts only when its probability is above 0.
- An experiment counts when its outcome is not Pending and its changes are reverted, justified, or were never made.
- Reevaluate passes with a confirmed hypothesis that has a Supported experiment, or once the case has looped back at least once. With a Champion and a confirmed hypothesis, it also needs the Champion's sign-off.
- Postmortem requires impact, solution, contributing factors, lessons learned, prevention measures, and action items. The problem summary line is met by the problem statement.

Hypothesis status changes are not gated; confirming one without a supporting experiment just leaves the Reevaluate gate unmet.

### Case lifecycle

All of these are block actions on the Case Board that re-check gates and permissions server-side. Never trust the client.

| Action | Effect |
|---|---|
| Create case | Status Open. Define the Problem and Classify are marked complete. Simple starts at Fact List, Advanced at Define the Team. |
| Complete Phase | Re-runs the gate, then moves to the next phase that is not auto-skipped. Open becomes Investigating at Fact List or later. Completing Reevaluate with a confirmed hypothesis sets Solved. |
| Set current phase | Moves the case. With Restrict Phase Jumping on, rejects any phase still NotStarted. The stepper disables those buttons, but the server check is the real gate. |
| Loop back | Increments the loop count, sets completed loop phases (5 to 8) back to in progress, and moves to Fact List. |
| Upgrade mode | Simple to Advanced only. |
| Close | Runs the Postmortem gate, saves the postmortem, sets Closed and an optional follow-up date. |
| Reopen | Sets Reopened, clears the closed date and follow-up confirmation, and moves to Fact List. |
| Abandon and resume | Abandon sets Abandoned. Resume sets Investigating at Fact List or later, otherwise Open. |

Closed and Abandoned cases reject edits except Reopen, Resume, and Confirm Follow-Up. `GetPhase` falls back to the first phase on an unknown value so one corrupt row cannot crash the Case File List.

## Witness flow

`CreateStatementRequest` makes a CaseStatement in Draft and sets the participant's role to Witness. It sends immediately when the Send Requests Automatically setting is on and the participant is a Rock person. `SendStatementRequest` emails the request or, if the status is already Requested or Reminded, the reminder template. External participants are manual-record only.

The email link targets the Witness Statement page with `?Statement={IdKey}`; a Guid also resolves. The page can be submitted while the request is Draft, Requested, or Reminded.

Every send attempt also creates (or refreshes) a core Rock `Reminder` for the witness via `Data/WitnessReminderHelper.cs`, before the email goes out, so a broken mail transport still nudges them in Rock. The helper lazily creates a "Witness Statement Request" `ReminderType` (stable GUID, entity type CaseStatement) and stamps the CaseStatement entity type's `LinkUrlLavaTemplate` with the Witness Statement page URL, so the reminder card titles itself from `CaseStatement.ToString()` (the prompt) and links straight to the entry form. Reminders complete when the statement is submitted, declined, recorded inline, or deleted, and `RecalculateReminderCount` keeps the count current in both directions.

`RecordStatementResponse` fills a pending request from the board (the owner heard the answer in person): statement text, optional attachment and link, status ManuallyRecorded, and the witness's reminder completes. It rejects requests that were already answered. `RecordStatement` creates a separate manual statement and is the path for external witnesses. Either can add the statement to the fact list, and `StatementToFact` does it later.

Overdue means a Requested or Reminded statement sent more than 7 days ago (`CaseMapper.OverdueWitnessDays`). That is independent of the job's Reminder Days.

## Jobs

| Job | Class | Schedule | Behavior |
|---|---|---|---|
| Rock Detective Witness Reminders | `Jobs/SendWitnessReminders` | Daily, 7:00 AM | Emails a reminder for Requested statements sent longer ago than Reminder Days (default 3), sets them to Reminded, and skips closed and abandoned cases. Reminded requests are not picked up again; further reminders are manual. |
| Rock Detective Follow-Up Checks | `Jobs/ProcessFollowUpChecks` | Daily, 7:15 AM | For closed, unconfirmed cases whose follow-up date has arrived, emails the owner with `FollowUp=Confirm` and `FollowUp=Reopen` links, then pushes the date a week so it re-pings until answered. |

The links are handled by the Case Board on page load, idempotently, and need edit rights. Note for dev boxes: jobs only run when `RunJobsInIISContext` is `True` in web.config.

The three system communications are seeded under the Plugins category. Requests and reminders get `Person`, `CaseNumber`, `CaseTitle`, `RequestPrompt`, and `StatementLink` (requests also get `RequestorName`). The follow-up check gets `Person`, `CaseNumber`, `CaseTitle`, `FixHeldLink`, and `ReopenLink`.

## REST API

`api/com_yesuchum/rockdetective`, ApiControllerBase with `[Authenticate, Secured]` and a `[RestActionGuid]` on every action (without those attributes Rock registers the controller with zero actions). The endpoints are documented in the README. The design goal is that a monitoring script can open a case, seed facts, and append experiment results without touching the UI.

Behavior to know about:

- Reads need View on the case and writes need Edit; a failed check returns 401.
- `PATCH /cases/{idKey}` re-runs the gate evaluator for each phase it advances past, using the default requirements rather than the Case Board's block settings.
- The same PATCH sets `status` as given, including Closed, without running the postmortem gate.
- A case created through the API has no phase state, so it never gets the attestations or classify scores the list modal records.

## PDF

`Pdf/CaseFileReport.cs` builds plain HTML; `Rock.Pdf.PdfGenerator` (Rock's bundled headless Chromium) renders it. No added dependency. It is served from a block action (base64 to the browser) and from `GET /cases/{idKey}/pdf`. Both need View on the case.

## Migrations

One squashed `_001_CreateSchema` (pre-1.0 development iterations were folded in before release). It creates:

- the six tables and their indexes and foreign keys
- entity type registrations for the six entities and three blocks
- the four Case File categories
- the Rock Detective Users role (guarded insert; `AddSecurityRoleGroup` alone is not idempotent) and the entity auth rows
- the three block types, the page tree, the blocks, and the linked-page attributes
- the three system communications
- the two jobs and the Reminder Days job attribute

Everything is add-or-update or existence-guarded, so a re-run over surviving rows is safe. Plugin migrations only run on a full Rock application restart. `Down()` reverses all of it. Post-1.0 changes go in new higher-numbered migrations; never edit a shipped one.

## Frontend

The Obsidian project has one `.obs` file per block, with partials for the Case Board panels (`phaseStepper`, `entriesPanel`, `hypothesesPanel`, `experimentsPanel`, `participantsPanel`) and the list's grid settings. Grid filters on the list are saved as person preferences.

The Case Board renders the phase stepper and a phase dropdown together and switches between them with CSS. Below 768 pixels the stepper is hidden and the dropdown shows, with a check mark on completed phases and disabled options for phases that Restrict Phase Jumping holds back. Timeline entries are sorted by event date in the entries panel; the other entry types keep their stored order.

## Build and deploy

The project is built inside a Rock source checkout. The csproj references `..\Rock`, `..\Rock.Common`, `..\Rock.Enums`, and `..\Rock.Rest`, and the Obsidian build uses `..\Rock.JavaScript.Obsidian` and copies into `..\RockWeb`.

```
dotnet build com.yesuchum.RockDetective                    debug DLL, auto-copies to RockWeb\Bin
dotnet build com.yesuchum.RockDetective -c Release         release DLL for packaging
dotnet test com.yesuchum.RockDetective.Tests               gate evaluator tests

cd com.yesuchum.RockDetective.Obsidian
npm install
npm run build                                              dist\*.obs.js, auto-copies to RockWeb\Plugins\com_yesuchum\RockDetective
```

The `.plugin` package is a zip of `content/` (bin DLL plus Plugins output), `install/`, and `uninstall/deletefile.lst`, named `com.yesuchum.RockDetective.{version}.plugin`. The version comes from the csproj.
