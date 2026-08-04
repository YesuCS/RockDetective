# Rock Detective Technical Specification

Self-contained Rock RMS plugin; no core modifications. C# class library plus Obsidian (Vue 3 / TypeScript) frontends. Targets Rock v17+; built and verified against the develop branch.

## Architecture

```
com.yesuchum.RockDetective/           C# class library
  Model/                              Six entities (Entity<T> + IRockEntity)
  Enums/                              All case enums
  Data/                               Plugin DbContext, services, CaseMapper
  Method/                             Method content + gate evaluator
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

## Data model

Six tables, prefix `_com_yesuchum_RockDetective_`:

- **CaseFile**: title, problem statement, CategoryId (Rock Category tree scoped to this entity type), mode (Simple/Advanced), classification, status (Open, Investigating, Solved, Closed, Reopened, Abandoned), current phase, PhaseStateJson, owner, postmortem fields, follow-up fields. The case number shown everywhere is just the entity Id.
- **CaseParticipant**: person (PersonAliasId) or external (name + contact), role (Owner, Champion, Recorder, Communicator, Member, Witness).
- **CaseStatement**: one row per statement request or recorded statement; prompt, request lifecycle (None, Draft, Requested, Reminded, Submitted, Declined, ManuallyRecorded), statement text, optional BinaryFileId and Url. A participant can have any number.
- **CaseEntry**: facts, timeline events, and evidence in one table; source tracking (Direct, Witness, Experiment), corroboration flag, optional BinaryFileId and Url.
- **Hypothesis**: tree via ParentHypothesisId; probability 0 to 100; status (Proposed, Testing, RuledOut, Confirmed).
- **Experiment**: optional hypothesis link, test plan, expected and actual results, outcome, changes made and reverted (or justified), learnings.

Per-phase state (progress, completion dates, notes, attestations, champion waiver and sign-off, loop-back count, classify scores) is JSON on CaseFile.PhaseStateJson, not a table.

### Entity plumbing that matters

These were all learned the hard way; do not undo them:

- Entities inherit `Rock.Data.Entity<T>` with plain audit columns, not `Model<T>`. Model<T>'s PersonAlias navigation properties drag the full Rock entity graph into the plugin's EF6 context and break its model build.
- Every entity implements `IRockEntity`. RockContext only registers plugin entities that carry that marker (plus `[DataContract]`); without it, anything resolving entities through RockContext (the AuditDetail endpoint, generic `Service<T>` reflection) fails with "not part of the model for the current context".
- Every service has two constructors: the plugin context and `Rock.Data.RockContext`. Rock's `Reflection.GetServiceForEntityType` instantiates plugin services with a RockContext.
- The plugin DbContext passes `base("RockContext")` (not RockApp settings; those are not ready during migration runs) and disables the EF6 initializer in a static constructor (`Database.SetInitializer<RockDetectiveContext>(null)`); without that, any schema change bricks the site with "the model backing the context has changed".
- CaseFile implements `ISecured` (delegating to `Rock.Security.Authorization`, with participants granted View in `IsAuthorized`), `ICategorized`, and `IModel` (lazy PersonAlias loads via `[NotMapped]` properties so the AuditDetail control shows names).

## Method content and gates

`Method/MethodDefinition.cs` holds the nine phases, prompts, principles, and contextual tips as code; they are method content, not user data. `Method/GateEvaluator.cs` is a pure function over a `CaseAggregate` snapshot; it takes a `GateRequirementOptions` whose values come from Case Board block settings (defaults match the method). Simple mode auto-skips Define the Team and Timeline and uses the Simple minimums. The evaluator is fully unit-tested in the Tests project; run `dotnet test`.

Phase completion, loop-back (5 through 8), champion sign-off, mode upgrade (Simple to Advanced only), abandon and resume, close and reopen are all block actions on the Case Board that re-check gates server-side. Never trust the client. When the Restrict Phase Jumping block setting is on, SetCurrentPhase also rejects any phase whose state is NotStarted; the stepper disables those buttons but the server check is the real gate. `GetPhase` falls back to the first phase on an unknown value so one corrupt row cannot crash the Case File List.

## Witness flow

CreateStatementRequest makes a CaseStatement in Draft (auto-sends if the block setting is on and the participant is a Rock person). SendStatementRequest emails the request or, if already Requested/Reminded, a reminder; the link targets the Witness Statement page with `?Statement={IdKey}` (a Guid also resolves). The witness page authorizes by matching the logged-in person to the participant's alias (case editors can also open it). External participants are manual-record only. The reminder job re-sends after Reminder Days and flips status to Reminded; overdue is Sent more than 7 days ago and still open.

Every send attempt also creates (or refreshes) a core Rock `Reminder` for the witness via `Data/WitnessReminderHelper.cs`, before the email goes out so a broken mail transport still nudges them in Rock. The helper lazily creates a "Witness Statement Request" `ReminderType` (stable GUID, entity type CaseStatement) and stamps the CaseStatement entity type's `LinkUrlLavaTemplate` with the Witness Statement page URL so the bell-menu card titles itself from `CaseStatement.ToString()` (the prompt) and links straight to the entry form. Reminders complete automatically when the statement is submitted, declined, recorded inline, or deleted, and `RecalculateReminderCount` keeps the bell badge current both ways.

RecordStatementResponse fills a pending request from the board (owner heard the answer in person): statement text, optional attachment and link, status ManuallyRecorded, and the witness's reminder completes. It rejects requests that were already answered.

## Jobs

- `Jobs/SendWitnessReminders`: reminder emails for stale Requested statements. Skips closed and abandoned cases.
- `Jobs/ProcessFollowUpChecks`: on a closed case's follow-up date, emails the owner with `FollowUp=Confirm` and `FollowUp=Reopen` links (handled by the Case Board on page load, idempotently, edit rights required) and pushes the date a week so it re-pings until answered.

Note for dev boxes: jobs only run when `RunJobsInIISContext` is `True` in web.config.

## REST API

`api/com_yesuchum/rockdetective`, ApiControllerBase with `[Authenticate, Secured]` and a `[RestActionGuid]` on every action (without those attributes Rock registers the controller with zero actions). Entity security enforced per call: View for reads, Edit for writes; phase transitions re-run the gate evaluator. Endpoints are documented in the README with examples; the design goal is that a monitoring script can open a case, seed facts, and append experiment results without touching the UI.

## PDF

`Pdf/CaseFileReport.cs` builds plain HTML; `Rock.Pdf.PdfGenerator` (Rock's bundled headless Chromium) renders it. No added dependency. Served from a block action (base64 to the browser) and from `GET /cases/{idKey}/pdf`.

## Migrations

One squashed `_001_CreateSchema` (pre-1.0 development iterations were folded in before release): six tables, entity type registration, category seed, the Rock Detective Users role (guarded insert; `AddSecurityRoleGroup` alone is not idempotent) and entity auth, block types, page tree, three system communications, two jobs. Everything is add-or-update or existence-guarded so a re-run over surviving rows is safe. Plugin migrations only run on a full Rock application restart. Post-1.0 changes go in new higher-numbered migrations; never edit a shipped one.

## Build and deploy

```
dotnet build com.yesuchum.RockDetective                    debug DLL, auto-copies to RockWeb\Bin
dotnet build com.yesuchum.RockDetective -c Release         release DLL for packaging
dotnet test com.yesuchum.RockDetective.Tests               gate evaluator tests

cd com.yesuchum.RockDetective.Obsidian
npm install
npm run build                                              dist\*.obs.js, auto-copies to RockWeb\Plugins\com_yesuchum\RockDetective
```

The `.plugin` package is a zip of `content/` (bin DLL + Plugins output), `install/`, and `uninstall/deletefile.lst`, named `com.yesuchum.RockDetective.{version}.plugin`.
