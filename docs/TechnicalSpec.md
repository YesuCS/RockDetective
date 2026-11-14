# Rock Detective Technical Specification

Rock Detective is a self-contained Rock RMS plugin with no core modifications. It is a C# class library (net472) plus Obsidian frontends (Vue 3 and TypeScript) for the Triumphant Problem Solving method by Jon Edmiston. The minimum Rock version is 19.7, which is where the 1.1.0 migrations are numbered and the first version the agent skill is built against. The current version is 1.1.0.

## Architecture

```
com.yesuchum.RockDetective/           C# class library
  Model/                              Six entities (Entity<T> + IRockEntity)
  Enums/                              All case enums
  Data/                               Plugin DbContext, services, CaseMapper,
                                      WitnessReminderHelper
  Method/                             Method content, gate evaluator, phase state,
                                      terminology profile
  Migrations/                         Plugin migrations (_001 through _005)
  Blocks/                             Five RockBlockType backends
  Jobs/                               Two RockJob classes
  Pdf/                                HTML report builder
  Rest/                               ApiControllerBase REST and MCP controllers
  Agent/                              Seeds the Rock Detective skill for Rock's AI agents
  Mcp/                                MCP tool definitions and handlers
  Logs/                               IIS W3C log reader
  Lava/                               The iislogs Lava command
  SystemGuid/                         Stable GUID constants
  ViewModels/                         Bags for blocks and REST
com.yesuchum.RockDetective.Obsidian/  Frontends, built with obsidian-build-tools
com.yesuchum.RockDetective.Tests/     MSTest unit tests for the gate evaluator
```

The five blocks live under Admin Tools > Installed Plugins > Rock Detective:

| Block | Page | Role |
|---|---|---|
| Case File List | Rock Detective | Case Filters with Show (All, Active, Archived), My Involvement (My Requests, My Assignments), category, status, and mode; new case modal; delete. |
| Case Board | Case Board | The workspace for one case. Takes a `CaseFile` page parameter (IdKey or Guid). |
| Witness Statement Entry | Witness Statement | Where a witness submits or declines a request. Takes a `Statement` page parameter. |
| Terminology Settings | Terminology Settings | Where an administrator renames phases, terms, and gate wording for all categories or one. |
| Skill Settings | Skill Settings | Shows which of the skill's standard tools were edited or deleted, and puts them back to standard. |

Case Board and Witness Statement are set to never show in navigation. Block settings are ordinary attributes on the block classes. The Case Board reads its gate settings into `GateRequirementOptions` and falls back to the evaluator defaults when a value is blank. Both linked-page settings (`DetailPage` on the list, `WitnessStatementPage` on the board) are created by the migration itself.

## Data model

Six tables, prefix `_com_yesuchum_RockDetective_`:

- **CaseFile**: title, problem statement, CategoryId (Rock Category tree scoped to this entity type), mode (Simple or Advanced), classification, status (Open, Investigating, Solved, Closed, Reopened, Abandoned), current phase, PhaseStateJson, owner, postmortem fields, follow-up fields. The identifier people see (PDF, emails, page addresses, and the `CaseNumber` field) is the case's IdKey, not the entity Id. The case list leads with the issue date instead: `IssueDate` is the earliest Problem Start event on the timeline, or the created date when there is none. The Case Board header shows the name and status only.
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

`CaseFile.ParentAuthority` is the entity-type default, which the migration seeds for the Rock Detective Users role (View, Edit) and Rock Administration (View, Edit, Administrate). `IsAuthorized` adds one rule: the owner and every Rock-person participant except a Witness is granted View. Edit is never granted that way. Migration `_006` also adds a deny for All Users on View after the role rules, because Rock's `GlobalDefault` would otherwise allow View to anyone who can reach a page or endpoint. A witness therefore gets no View from being on the case, on the Case Board, the PDF, the REST API, or MCP.

The Case Board treats the owner as an editor too (`CanEdit` is Edit security or ownership), and every block action re-checks that server-side. The REST controller checks Edit security only, so an owner without Edit security can work a case on the board but not through the API. The witness page authorizes by matching the logged-in person to the participant's alias; anyone with Edit on the case can also open it.

## Method content and gates

`Method/MethodDefinition.cs` holds the nine phases, prompts, principles, and contextual tips as code; they are method content, not user data. `Method/GateEvaluator.cs` is a pure function over a `CaseAggregate` snapshot. It takes a `GateRequirementOptions` whose defaults match the method (3 facts Advanced, 1 Simple; 2 root hypotheses Advanced, 1 Simple; 2 timeline events; 1 experiment; attestations on). The evaluator is fully unit-tested in the Tests project (21 tests); run `dotnet test`.

Gate rules worth knowing:

- Simple mode auto-skips Define the Team and Timeline, and skips the attestations.
- Define the Team requires an owner and a Champion, or a recorded waiver (`ChampionWaived` plus a reason in phase state). The board has no control for setting the waiver yet.
- The timeline gate counts all events toward the minimum and separately requires one Problem Start.
- A root hypothesis counts only when its probability is above 0.
- An experiment counts when its outcome is not Pending and its changes are reverted, justified, or were never made.
- Reevaluate passes with a confirmed hypothesis that has a Supported experiment. A loop back does not satisfy it. With a Champion and that confirmed hypothesis, it also needs the Champion's sign-off.
- The Probability Tree count leaves out root hypotheses that are Ruled Out.
- Postmortem requires impact, solution, contributing factors, lessons learned, prevention measures, and action items. The problem summary line is met by the problem statement.

Hypothesis status changes are not gated; confirming one without a supporting experiment just leaves the Reevaluate gate unmet.

### Terminology

Names are a layer on top of the method, never part of it. `Method/MethodProfile.cs` resolves the wording for a case from three layers, later ones winning:

1. The built-in wording: `MethodProfile.DefaultTerms`, `MethodProfile.DefaultGateDescriptions`, and the phase names, guidance, and prompts in `MethodDefinition`.
2. The all-categories wording, stored as JSON in the global attribute `com_yesuchum_RockDetective_DefaultTerminology`.
3. The category's wording, stored as JSON in a Category attribute, `com_yesuchum_RockDetective_Terminology`, qualified to the Case File entity type.

The JSON is a `MethodProfileConfiguration`: `Phases` (keyed by CasePhase name, each with optional `Name`, `Guidance`, `Prompts`), `Terms` (keyed by `TermKey`, each with `Singular` and `Plural`), and `GateDescriptions` (keyed by `GateKey`, like `Facts.Minimum`). Anything missing falls through to the layer below. The settings page saves the singular only and the plural comes from Rock's `Pluralize()` string extension. A `Plural` in the JSON still wins if one is there, which is the way out for a word Rock pluralizes badly.

Guidance, prompts, and gate sentences are templates. `MethodProfile.Format` replaces `{term}` (lower case), `{Term}` (as entered), `{term+}` (plural), `{term#}` (plural unless the count is 1), and `{count}`. It also fixes "a" and "an" in front of a term token, so "An {owner}" reads right after Owner becomes Ministry Lead. The built-in guidance and gate sentences use the same tokens, which is why renaming a term carries through text nobody rewrote.

The gate evaluator did not change behavior. Each `GateRequirement` now carries a stable `Key` and an optional `Count` next to its original `Description`, and `MethodProfile.DescribeGate` picks the sentence. Code that evaluates gates without a profile still gets the original descriptions.

`CaseMapper.ToCaseFileBag` resolves the profile from the case's category and puts `PhaseDefinitions` and `Terms` on the case bag along with the reworded gate results. Every board action returns that bag, so a category change on the board swaps the wording without a reload. The case list, the PDF, and the MCP tools read the same profile. The system communications do not; they are Lava templates an administrator edits in Rock.

The Terminology Settings block reads and writes the two attributes. It requires Administrate on the block for everything: the initialization bag is empty without it, and both block actions return forbidden. The case list shows its Terminology Settings button under the same check. It drops blank entries, saves an empty string when nothing is customized, and removes the category from `CategoryCache` after a save. Both reads go through Rock's caches (`GlobalAttributesCache`, `CategoryCache`), so there is no table and no query per case. Rock's Category Manager does not show category attributes, which is why the block exists.

### Case lifecycle

All of these are block actions on the Case Board that re-check gates and permissions server-side. Never trust the client.

| Action | Effect |
|---|---|
| Create case | Status Open. Define the Problem and Classify are marked complete. Simple starts at Fact List, Advanced at Define the Team. |
| Complete Phase | Re-runs the gate, then moves to the next phase that is not auto-skipped. Open becomes Investigating at Fact List or later. Completing Reevaluate with a confirmed hypothesis sets Solved. |
| Set current phase | Moves the case. With Restrict Phase Jumping on (the default), it only allows the current phase or a phase at or before the furthest one the case has earned. `CaseRules.GetFurthestPhase` walks the phases in order and stops at the first that is not Complete, leaving out Simple mode's auto-skipped phases while they are untouched. A phase marked Complete at creation does not help while an earlier one is open. The stepper locks the rest, but the server check is the real gate. The same check guards Save Phase Form, so notes can't be used to start a locked phase. |
| Complete phase | Only the phase the case is on can be completed, whatever the setting. |
| Close | Only from Postmortem, whatever the setting; with the setting on, the earlier phases also have to be complete. |
| Loop back | Only from Reevaluate. Increments the loop count, clears the Champion sign-off, sets completed loop phases (5 to 8) back to in progress, and moves to Fact List. |
| Upgrade mode | Simple to Advanced only. |
| Save postmortem | Saves the six postmortem fields and the follow-up date without closing. Only on the Postmortem phase. The board shows the saved answers with an edit pencil, like the problem statement. |
| Close | Runs the Postmortem gate, saves the postmortem, sets Closed and an optional follow-up date. The board sends the saved answers; it no longer has a close form. |
| Reopen | Sets Reopened, clears the closed date, follow-up confirmation, and Champion sign-off, moves to Fact List, and sets completed phases 5 to 9 back to in progress so they are earned again. |
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

`api/com_yesuchum/rockdetective`, ApiControllerBase with `[Authenticate, Secured]` and a `[RestActionGuid]` on every action (without those attributes Rock registers the controller with zero actions). Every endpoint, with request fields, responses, and examples, is documented in `docs/RestApi.md`. The design goal is that a monitoring script can open a case, seed facts, and append experiment results without touching the UI.

Behavior to know about:

- Reads need View on the case and writes need Edit; a failed check returns 401.
- `PATCH /cases/{idKey}` re-runs the gate evaluator for each phase it advances past, using the default requirements rather than the Case Board's block settings.
- The same PATCH sets `status` as given, including Closed, without running the postmortem gate.
- A case created through the API has no phase state, so it never gets the attestations or classify scores the list modal records.

## MCP

There are two routes to the same 22 tools. Both call the same typed methods, so the logic and the security checks live in one place: the three read tools are on `Mcp/McpToolService.cs` (`ListCases`, `GetCase`, `SearchIisLogs`) and the nineteen that change things are on `Mcp/CaseWorkflowService.cs`.

### Case workflow service

`CaseWorkflowService` is the write path for both MCP routes. It mirrors the Case Board's block actions rather than calling them, because block actions need a block instance. The rules it keeps the same:

- A caller can change a case when they own it or have Edit on it, and never when it is Closed or Abandoned.
- Complete Phase runs `GateEvaluator` on the current phase and refuses with the unmet checklist lines, worded through the case's terminology profile. It then applies the same auto-skip, Investigating, and Solved transitions as the board.
- Gate minimums come from the Case Board block's own settings (looked up by the block guid the migration seeds), falling back to the evaluator defaults. The REST API's phase PATCH still uses defaults only.
- Close Case requires the last phase and a complete postmortem.
- Experiment learnings become one corroborated fact, as on the board.
- Champion Sign-Off requires a confirmed hypothesis with a Supported experiment, and the caller to be the Champion. When every Champion on the case is external (no PersonAliasId), the owner or someone with Edit records it. The rules are in `Data/CaseRules.cs` and the Case Board uses the same ones.
- `Data/CaseRules.cs` also holds the shared input checks: links must be absolute http or https (`IsSafeUrl`, applied on every write path, and `CaseMapper` drops anything else on the way out), probability 0 to 100, title up to 200 characters, and `ToRockLocalTime`, which converts a date that carries an offset into the Rock organization time zone using the offset in effect on that date. A date with no offset is stored as given.

Two things are specific to this path. Entries it adds get `CaseEntrySource.Agent` (3), which the board renders as "Added by an AI assistant" and the PDF as "AI Assistant". And every method returns a progress object (current phase, its guidance and prompts, the checklist with each line's state, whether the phase is ready to complete, and a next step), which is what lets an assistant run the method without a person steering it.

Reopen, resume, abandon, delete, and sending witness requests are deliberately not exposed. Those stay on the Case Board.

Three tools are for witnesses and follow the Witness Statement page's rule instead of the editor rule: `ListMyStatementRequests` returns only open requests whose participant is the caller, and `SubmitStatement` and `DeclineStatement` work when the request was addressed to the caller (or the caller has Edit on the case). They return a confirmation, not case progress, and complete the in-Rock reminder the same way the page does.

### Who can call what

`Mcp/CaseAccess.cs` scopes every tool to the caller, so one agent can serve witnesses, investigators, and administrators. Each direct-endpoint definition carries a `ToolAudience` (Everyone, Investigator, Administrator); the skill's Lava tools call the same `Execute`, so Rock's route gets the same checks.

- **Investigator** means `Authorization.Authorized( new CaseFile(), EDIT, person )`, which is the entity-type security the migration seeds for Rock Detective Users and Rock Administration. `create_case` and `list_categories` require it.
- **Reading a case** over MCP goes through `CaseAccess.CanReadCase`, which allows the owner, a participant in any role except Witness, or an investigator who passes the normal View check. `list_cases` and `get_case` use it. `CaseFile.IsAuthorized( VIEW )` follows the same rule for witnesses now, so the Case Board, PDF, and REST API agree with it.
- **Administrator** means Administrate on `GlobalDefault`. `search_iis_logs` requires it on both routes.
- The direct endpoint's `tools/list` is filtered by audience, and `Execute` refuses a tool outside the caller's audience. Rock's route lists whatever the agent has enabled, and a tool the caller may not use returns a refusal with the reason.

### Agent skill

The skill is a regular (non-code) Rock AI skill whose tools are Lava, so an administrator can edit all of it in Rock's AI Skills screen. Rock only lets a skill's tools be edited when the skill has no `CodeEntityTypeId`, and it rewrites a code skill from its class attributes on every start, so a code skill (an `AgentSkillComponent` subclass) can't be opened up this way. The plugin seeds rows instead.

The pieces:

- `Lava/RockDetectiveBlock.cs` is the `{% rockdetective tool:'name' %}{ json }{% endrockdetective %}` command. It renders its body, parses it as a JSON object, drops null properties (an argument the assistant left out renders as `null`), and calls `McpToolService.Execute( tool, arguments, currentPerson )`. The result is written as JSON. A `McpToolException` is written as `{"refused":true,"reason":"..."}`; anything else is logged and returned as a generic refusal. It is a secured command (`ILavaSecured`, permission `rockdetective`) and calls `IsAuthorized( context )` itself.
- `Agent/SkillWording.cs` holds the default text: the skill's description, purpose, usage, and guardrails, and a name, description, and purpose for each tool.
- `Agent/SkillToolCatalog.cs` builds the 22 default tools from the `McpToolService` definitions: the tool Guid, the name and wording from `SkillWording`, the parameters from the definition's JSON schema (string, number, or boolean, with required and allowed values), and the default Lava, which passes every parameter through `ToJSON`.
- `Agent/SkillSeeder.cs` writes them through `AISkillService` and `AISkillToolService`. Settings go in with `SetAdditionalSettings( key, object )` under Rock's keys: `SkillInstructionSettings` on the skill, and `PromptInformationSettings` (the Lava in `Prompt`, the parameters in `PromptParameters`), `ToolInstructionSettings`, and `ToolAdditionalSettings` on each tool. Rock's settings classes are internal, so the seeder writes anonymous objects with the same property names.
- `Agent/RockDetectiveStartup.cs` is an `IRockStartup` that calls `SkillSeeder.EnsureSkill()` on every start.

What `EnsureSkill` does, and does not do:

- It creates the skill if it is missing. The Guid is fixed (`2C5BE126-3BEC-4B15-A2F4-80FD685ABE84`) and so are the tool Guids, because an agent's list of enabled tools is a list of tool Guids.
- It adds a standard tool only if it has never added it before. The Guids it has added are kept on the skill under its own settings key, `RockDetectiveSeedSettings`. That is how a tool an administrator deleted stays deleted, and how a tool that is new in a later version still arrives.
- It never overwrites a tool that exists. The one exception is a tool still typed `ExecuteCode`, left over from a pre-release build where this was a code skill; those are converted in place.
- `RestoreTool` and `RestoreAll` (called from the Skill Settings block) are the only things that write defaults over an existing tool. `GetStatus` compares each tool to its default (name, description, purpose, usage, Lava, and parameters) and reports Standard, Edited, or Missing.

Things to know:

- Rock runs a Lava tool through its `LavaToolExecutor` with every Lava command enabled and the common merge fields, so `CurrentPerson` is the person the agent call is signed in as. The parameters are merge fields by name.
- Whatever the Lava outputs is the tool's result, as text. That is why a refusal is a JSON object with `refused` and `reason` instead of an error status.
- All permission and gate checks are in `McpToolService` and `CaseWorkflowService`, behind `Execute`. Nothing an administrator can type into a tool gets around them.
- The tool name on the wire is the skill name and tool name with spaces and punctuation removed: `RockDetective__ListCases`, `RockDetective__SearchIISLogs`.
- Rock's MCP server sends a tool's purposes, usages, and return description to the client, not its description. The seeder therefore also stores the description as the tool's one usage.
- A parameter typed Number arrives in Lava as a double, so an integer argument can render as `25.0`. The handlers read arguments with `Value<int?>`, which accepts that.
- `docs/SkillDefaults.md` is generated from the seeded rows of a clean install. Regenerate it when the defaults change.

### Direct endpoint

`Rest/RockDetectiveMcpController.cs` is a plugin-owned MCP server at `POST api/com_yesuchum/rockdetective/mcp`. It needs no agent and no AI setup in Rock, only a REST key.

It is stateless JSON-RPC 2.0 over HTTP, protocol version `2025-03-26`, with no SSE stream and no session:

- `initialize`, `ping`, `tools/list`, and `tools/call` return a JSON result.
- A message with no `id` (a notification such as `notifications/initialized`) returns 202 with no body.
- `GET` returns 405, which tells a client there is no stream to open.
- An unknown method returns JSON-RPC error -32601, and a body that isn't JSON returns -32700.

Auth is Rock's own: `[Authenticate, Secured]` on the POST action, so a client sends a REST key in the `Authorization-Token` header and the action is secured like any other under Security > REST Controllers.

Its tools are listed in `Mcp/McpToolService.cs` (read tools) and `Mcp/McpToolService.Write.cs` (the rest), one `McpToolDefinition` each (name, description, JSON schema, handler). To add a tool, write the typed method, add a definition, give it wording in `Agent/SkillWording.cs`, and give it a Guid in `Agent/SkillToolCatalog.cs` so it is seeded into the skill. A handler throws `McpToolException` for anything the caller should read; that comes back as a normal result with `isError: true`, which is what MCP expects for tool failures. Anything else is logged to the exception log and returned as a generic error.

| Tool | Notes |
|---|---|
| `list_cases` | Cases the caller can View, newest first. Optional `status` and `limit`. The phase name comes from the case's terminology profile. |
| `get_case` | The same case aggregate the REST API returns, by IdKey. Checks View on the case. |
| `search_iis_logs` | See below. |

## IIS logs

`Logs/IisLogReader.cs` reads W3C log files on demand. There is no job and no table; a search opens the newest files, parses them, and returns.

- The folder comes from the global attribute `com_yesuchum_RockDetective_IisLogPath`, or `%SystemDrive%\inetpub\logs\LogFiles` when that is blank. It searches subfolders. The folder is never a caller argument, so a caller cannot aim the reader at another directory.
- It reads the `#Fields:` header of each file to find columns, so custom field selections work. Lines that don't parse are skipped.
- Filters: minutes back, status code, path contains, method, and minimum time taken.
- Hard caps: 500 entries, 10 files, 7 days per search, and a range can start up to 30 days back. Files are opened with `FileShare.ReadWrite` so IIS can keep writing. Matches are counted, but only the newest entries up to the limit are held in memory.
- `RedactQuery` replaces the value of any query string parameter whose name contains a sensitive word (rckipid, token, key, code, password, secret, signature, auth, session, and a few more) with `[redacted]`. It runs in the reader, so the MCP tool and the Lava command both get redacted values.
- Log times are UTC on disk and are returned in server local time.
- A missing or unreadable folder returns a `Warning` on the result. It never throws, so the MCP tool and the Lava command both degrade to an explanation.

`Lava/IisLogsBlock.cs` exposes the same reader as `{% iislogs %}`, setting `logEntries` and `logResult` for its body. It is a secured command. Implementing `ILavaSecured` only names the permission; the block has to call `IsAuthorized( context )` itself in `OnRender`, and this one does. The permission name is the tag name, `iislogs`.

`LavaBlockBase` lives in `Rock.Lava.dll`, which has no NuGet package (only `Rock.Lava.Shared` does). A build against the Rock source tree uses project references. A build against the `RockRMS.*` packages needs a file reference to `Rock.Lava.dll` from a Rock `Bin` folder, with `Private` set to false.

## PDF

The cover prints the case's IdKey, not the entity Id. `Pdf/CaseFileReport.cs` builds plain HTML; `Rock.Pdf.PdfGenerator` (Rock's bundled headless Chromium) renders it. No added dependency. It is served from a block action (base64 to the browser) and from `GET /cases/{idKey}/pdf`. Both need View on the case.

## Migrations

`_001_CreateSchema` is squashed (pre-1.0 development iterations were folded in before release). It creates:

- the six tables and their indexes and foreign keys
- entity type registrations for the six entities and three blocks
- the four Case File categories
- the Rock Detective Users role (guarded insert; `AddSecurityRoleGroup` alone is not idempotent) and the entity auth rows
- the three block types, the page tree, the blocks, and the linked-page attributes
- the three system communications
- the two jobs and the Reminder Days job attribute

Version 1.1.0 added four more, numbered for Rock 19.7 so they only run there or later:

- `_002_AddIisLogPathSetting`: the Rock Detective IIS Log Folder global attribute.
- `_003_AddTerminologySettings`: the Category attribute for per-category wording (it looks up the Case File entity type Id for the qualifier) and the default terminology global attribute.
- `_004_AddTerminologySettingsPage`: the Terminology Settings block entity type, block type, page, and block. The entity type is registered before the block type, for the same clean-install reason as in `_001`.
- `_006_AddRoleAccess`: View on the Rock Detective page for the Rock Detective Users role (the child pages inherit it), and the All Users deny on Case File View described under security.
- `_005_AddSkillSettingsPage`: the Skill Settings block entity type, block type, page, and block. The skill itself is not in a migration; `SkillSeeder` adds it at startup.

Everything is add-or-update or existence-guarded, so a re-run over surviving rows is safe. Plugin migrations only run on a full Rock application restart. Each `Down()` reverses its own migration. Post-1.0 changes go in new higher-numbered migrations; never edit a shipped one.

## Frontend

The Obsidian project has one `.obs` file per block (five now, with `terminologySettings.obs` and `skillSettings.obs`), with partials for the Case Board panels (`phaseStepper`, `entriesPanel`, `hypothesesPanel`, `experimentsPanel`, `participantsPanel`) and the list's grid settings. Grid filters on the list are saved as person preferences.

The Case Board renders the phase stepper and a phase dropdown together and switches between them with CSS. Below 768 pixels the stepper is hidden and the dropdown shows, with a check mark on completed phases and disabled options for phases that Restrict Phase Jumping holds back. The board's secondary actions (Export PDF, Upgrade to Advanced, Abandon, Reopen, Audit Details) are `headerSecondaryActions` on the panel; only Resume and Fix Held stay as buttons, because they are the one thing to do on an abandoned or closed case. Timeline entries are sorted by event date in the entries panel; the other entry types keep their stored order.

Renamed terms reach the partials through provide and inject. `caseFileDetail.obs` builds a lookup from the case bag's `terms` with `createTermLookup` and provides it; each partial calls `useTerms()` and gets `t` (one), `tp` (more than one), and lower-case versions `tl` and `tpl`. The lookup reads the bag every time, so the words change as soon as an action returns a new bag. Panel titles for the fact list, timeline, probability tree, and experiments come from the phase names. `types.partial.ts` carries a copy of the standard terms as a fallback for a bag without any.

## Build and deploy

The project is built inside a Rock source checkout at 19.7 or later. Older source does not have the AI skill models the seeder writes to. The csproj references `..\Rock`, `..\Rock.Common`, `..\Rock.Enums`, `..\Rock.Rest`, `..\Rock.Lava`, and `..\Rock.Lava.Shared`, and the Obsidian build uses `..\Rock.JavaScript.Obsidian` and copies into `..\RockWeb`.

```
dotnet build com.yesuchum.RockDetective                    debug DLL, auto-copies to RockWeb\Bin
dotnet build com.yesuchum.RockDetective -c Release         release DLL for packaging
dotnet test com.yesuchum.RockDetective.Tests               gate evaluator tests

cd com.yesuchum.RockDetective.Obsidian
npm install
npm run build                                              dist\*.obs.js, auto-copies to RockWeb\Plugins\com_yesuchum\RockDetective
```

The `.plugin` package is a zip of `content/` (bin DLL plus Plugins output), `install/`, and `uninstall/` (`deletefile.lst` plus `run.sql`), named `com.yesuchum.RockDetective.{version}.plugin`. The version comes from the csproj.
