# Rock Detective REST API

Rock Detective has a REST API so other systems can work a case without anyone opening the Case Board. A monitoring script can open a case when an alert fires, seed the first facts, add hypotheses and experiments, record results as they come in, and pull the finished case file as a PDF.

Every endpoint here is in version 1.0.1.

## Basics

### Base URL

```
https://<your-rock-server>/api/com_yesuchum/rockdetective
```

All routes below are relative to that.

### Authentication

The API uses standard Rock REST authentication. For a script, create a REST key under Admin Tools > Security > REST Keys and send it in the `Authorization-Token` header. A browser that is already logged in to Rock also works, since the endpoints accept the Rock session.

### Permissions

There are two checks, and both have to pass.

1. **The REST controller.** Rock checks the Rock Detective controller's own security, under Admin Tools > Security > REST Controllers. Make sure the person tied to your REST key has access there.
2. **The case.** Reads need View on the case and writes need Edit, using the same per-case security as the case list's padlock. The list endpoint only returns cases the caller can view.

When either check fails the API returns `401 Unauthorized`.

On the Case Board, a case owner can edit their own case even without Edit security. The API checks Edit security only, so an owner without Edit can work the case on the board but not through the API.

### IDs

Cases, entries, hypotheses, experiments, participants, and statements are identified by their `IdKey`, the hashed Rock ID that every response returns. Use those values in routes and request bodies. `CaseNumber` on a case is the plain numeric ID, shown for people, not used in routes.

People and categories are referenced by Guid: `PersonAliasGuid` for participants and owners, a Person Guid for the `owner` filter, and a Category Guid for case categories.

These four case categories ship with the plugin:

| Category | Guid |
|---|---|
| Software Bug | `3A0A00D7-2167-4B4B-B75D-BE77DDAED45C` |
| Performance | `C66E56CB-25F7-4D98-A64A-A515F4A4E104` |
| Process | `157B8356-13A8-4571-810B-CAA767510245` |
| Ministry Operations | `A928CF42-B839-41A3-863B-B8ADAA603109` |

### JSON

- Request property names are not case sensitive; `title` and `Title` both work.
- Responses use Rock's v1 API style, with PascalCase names like `IdKey` and `ProblemStatement`.
- Enums are sent and returned as integers. The values are in the Enums section.
- Dates are ISO 8601 in Rock's server time zone.

### Errors

| Status | When |
|---|---|
| 200 | Success. Writes return the created or updated object. |
| 400 | A required field is missing or a phase move is blocked. The body has a message, like `Title and ProblemStatement are required.` |
| 401 | Not logged in, no access to the REST controller, or missing View or Edit on the case. |
| 404 | The `IdKey` doesn't match anything. |

## Enums

**CaseStatus:** 0 Open, 1 Investigating, 2 Solved, 3 Closed, 4 Reopened, 5 Abandoned

**CaseMode:** 0 Simple, 1 Advanced

**CaseClassification:** 0 Unclassified, 1 Easy, 2 Difficult

**CasePhase:** 1 Define the Team, 2 Define the Problem, 3 Classify, 4 Timeline, 5 Fact List, 6 Probability Tree, 7 Experiments, 8 Reevaluate, 9 Postmortem

**CaseEntryType:** 0 Fact, 1 Timeline Event, 2 Evidence

**TimelineEventType:** 0 Problem Start, 1 Last Known Good, 2 Change, 3 Activity, 4 Other

**CaseEntrySource:** 0 Direct, 1 Witness, 2 Experiment

**HypothesisStatus:** 0 Proposed, 1 Testing, 2 Ruled Out, 3 Confirmed

**ExperimentOutcome:** 0 Pending, 1 Supported, 2 Ruled Out, 3 Inconclusive

**ParticipantRole:** 0 Owner, 1 Champion, 2 Recorder, 3 Communicator, 4 Member, 5 Witness

**StatementRequestStatus:** 0 None, 1 Draft, 2 Requested, 3 Reminded, 4 Submitted, 5 Declined, 6 Manually Recorded

## Endpoints at a glance

| Method | Route | Needs | Returns |
|---|---|---|---|
| GET | `/cases` | View | List of full cases |
| GET | `/cases/{idKey}` | View | Full case |
| POST | `/cases` | Logged in | Full case |
| PATCH | `/cases/{idKey}` | Edit | Full case |
| GET | `/cases/{idKey}/entries` | View | List of entries |
| POST | `/cases/{idKey}/entries` | Edit | Entry |
| POST | `/cases/{idKey}/hypotheses` | Edit | Hypothesis |
| PATCH | `/hypotheses/{idKey}` | Edit | Hypothesis |
| POST | `/cases/{idKey}/experiments` | Edit | Experiment |
| PATCH | `/experiments/{idKey}` | Edit | Experiment |
| POST | `/cases/{idKey}/participants` | Edit | Participant |
| POST | `/participants/{idKey}/request-statement` | Edit | Statement |
| GET | `/cases/{idKey}/pdf` | View | PDF file |

## Cases

### List cases

`GET /cases`

Returns every case the caller can view, each as a full case object. All filters are optional.

| Query parameter | Type | Description |
|---|---|---|
| `status` | int | Only cases with this CaseStatus. |
| `category` | Guid | Only cases in this category. |
| `owner` | Guid | Only cases owned by this person. This is the Person Guid, not a PersonAlias Guid. |

Each case comes back with everything in it (entries, hypotheses, experiments, participants, statements), so on a busy instance it's worth filtering by status.

```
GET /cases?status=1
```

### Get a case

`GET /cases/{idKey}`

Returns one full case. See Response shapes for the fields.

### Create a case

`POST /cases`

Creates a case owned by the caller and returns it.

| Field | Type | Required | Description |
|---|---|---|---|
| `title` | string | Yes | Up to 200 characters; longer titles are cut off. |
| `problemStatement` | string | Yes | One clear, concise statement of the problem. |
| `categoryGuid` | Guid | Yes | A Case File category; see the category Guids under IDs. |
| `mode` | int | No | 1 for Advanced. Anything else, including leaving it out, is Simple. |

What the API sets for you:

- Status is Open.
- Advanced cases are classified Difficult and start at Define the Team. Simple cases are classified Easy and start at Fact List.
- The caller is the owner.

```json
{
  "title": "Check-in kiosks slow on Sunday",
  "problemStatement": "Check-in at the north campus took over 10 seconds per family between 8:45 and 9:15 AM on Sunday.",
  "categoryGuid": "C66E56CB-25F7-4D98-A64A-A515F4A4E104",
  "mode": 1
}
```

### Update a case

`PATCH /cases/{idKey}`

Send only the fields you want to change.

| Field | Type | Description |
|---|---|---|
| `title` | string | Up to 200 characters. |
| `problemStatement` | string | Replaces the problem statement. |
| `status` | int | A CaseStatus. Setting 3 (Closed) also stamps the closed date. |
| `currentPhase` | int | A CasePhase to move the case to. |

How `currentPhase` works:

- **Moving forward** checks the gate of every phase being passed, skipping phases that are auto-skipped for the case's mode. If one isn't met, the request fails with `400` and a message like `Cannot advance past phase 5; its gate requirements are not met.` These checks use the default gate requirements, not the settings on your Case Board block.
- **Moving back** is always allowed.

`status` is set as given, with no gate check, so the API can close a case without going through the Postmortem phase. If you want the postmortem written, move the case through the phases on the board.

## Entries

Entries are facts, timeline events, and evidence.

### List entries

`GET /cases/{idKey}/entries`

Returns the case's entries in the order they were added. The Case Board sorts timeline events by date when it shows them.

### Add an entry

`POST /cases/{idKey}/entries`

| Field | Type | Required | Description |
|---|---|---|---|
| `entryType` | int | Yes | A CaseEntryType: 0 Fact, 1 Timeline Event, 2 Evidence. |
| `text` | string | Yes | The fact, event, or evidence description. |
| `eventDateTime` | date | No | When it happened. Use it for timeline events. |
| `timelineEventType` | int | No | A TimelineEventType, for timeline events. |
| `url` | string | No | A link, for evidence. |
| `isCorroborated` | bool | No | Whether the fact has been backed up by a second source. Defaults to false. |

Entries added through the API are marked with source Direct and go to the end of the list. File attachments can only be added on the Case Board.

```json
{
  "entryType": 1,
  "text": "Check-in label printer firmware updated",
  "eventDateTime": "2026-11-01T22:10:00",
  "timelineEventType": 2
}
```

## Hypotheses

### Add a hypothesis

`POST /cases/{idKey}/hypotheses`

| Field | Type | Required | Description |
|---|---|---|---|
| `name` | string | Yes | Short name of the possible cause. Up to 300 characters. |
| `description` | string | No | More detail. |
| `probability` | int | No | 0 to 100. Values outside that range are clamped. |
| `parentHypothesisIdKey` | string | No | Makes this a child of another hypothesis in the probability tree. |

New hypotheses start as Proposed.

### Update a hypothesis

`PATCH /hypotheses/{idKey}`

| Field | Type | Description |
|---|---|---|
| `probability` | int | 0 to 100, clamped. |
| `status` | int | A HypothesisStatus. |
| `ruledOutReason` | string | Why it was ruled out. |

The API doesn't require a supporting experiment to set a hypothesis to Confirmed. The Reevaluate gate still looks for one when the case moves past that phase.

## Experiments

### Add an experiment

`POST /cases/{idKey}/experiments`

| Field | Type | Required | Description |
|---|---|---|---|
| `hypothesisIdKey` | string | One of these two | The hypothesis being tested. |
| `hypothesisText` | string | One of these two | A free-text hypothesis, up to 300 characters, when it isn't in the tree. |
| `testPlan` | string | No | What you'll change and how. Keep it to one factor at a time. |
| `expectedResult` | string | No | What you expect to see if the hypothesis is right. |

New experiments start with outcome Pending.

### Record a result

`PATCH /experiments/{idKey}`

| Field | Type | Description |
|---|---|---|
| `actualResult` | string | What actually happened. |
| `outcome` | int | An ExperimentOutcome. |
| `changesMade` | string | Every change made during the experiment, so it can be reverted. |
| `changesReverted` | bool | Whether those changes were reverted. |
| `learnings` | string | What the experiment taught you. |

On the Case Board, learnings from an experiment with a recorded outcome are added to the fact list for you. The API saves learnings on the experiment only; add a Fact entry too if you want it on the fact list.

```json
{
  "actualResult": "Rolled the printer firmware back; check-in time dropped to 2 seconds per family.",
  "outcome": 1,
  "changesMade": "Firmware 4.2 to 4.1 on the north campus label printers.",
  "changesReverted": false,
  "learnings": "Firmware 4.2 adds a 6 second handshake on every label."
}
```

## Participants and witness statements

### Add a participant

`POST /cases/{idKey}/participants`

| Field | Type | Required | Description |
|---|---|---|---|
| `personAliasGuid` | Guid | One of these two | A person in Rock. |
| `externalName` | string | One of these two | Someone outside Rock, like a vendor contact. Up to 200 characters. |
| `externalContact` | string | No | Email or phone for an external participant. Up to 400 characters. |
| `role` | int | No | A ParticipantRole. Defaults to 0, which is Owner, so send it every time. |

When `personAliasGuid` matches a person, the external fields are ignored.

### Request a witness statement

`POST /participants/{idKey}/request-statement`

| Field | Type | Required | Description |
|---|---|---|---|
| `requestPrompt` | string | Yes | What you're asking the witness to describe. |

This creates a new statement in Draft status and sets the participant's role to Witness. Each call creates its own statement, so a witness can have several. No email goes out from the API; send the request from the Case Board, which also handles reminders.

```json
{ "requestPrompt": "Walk us through what you saw at the north campus kiosks Sunday morning." }
```

## Case file PDF

`GET /cases/{idKey}/pdf`

Returns the whole case file as a PDF download (`application/pdf`, named `Case-{CaseNumber}.pdf`), including witness statements.

## Response shapes

### Case

| Field | Description |
|---|---|
| `IdKey`, `CaseNumber` | Hashed key for routes, and the plain numeric ID for people. |
| `Title`, `ProblemStatement` | |
| `CategoryGuid`, `CategoryName` | |
| `Mode`, `Classification`, `Status`, `CurrentPhase` | Integers; see Enums. |
| `OwnerName`, `OwnerPersonAliasGuid` | |
| `ImpactSummary`, `SolutionSummary`, `ContributingFactors`, `LessonsLearned`, `PreventionMeasures`, `ActionItems` | The postmortem. |
| `FollowUpDate`, `FollowUpConfirmedDateTime`, `ClosedDateTime`, `CreatedDateTime` | |
| `PhaseStates` | Per phase: `Phase`, `Progress`, `CompletedDateTime`, `Summary`, and `Data` (the phase form's saved answers). |
| `GateResults` | Per phase: `Phase`, `IsMet`, and `Requirements`, each with `Description` and `IsMet`. Graded against the default gate requirements. |
| `Entries`, `Hypotheses`, `Experiments`, `Participants` | Lists of the objects below. Statements are inside each participant. |

### Entry

`IdKey`, `EntryType`, `Text`, `EventDateTime`, `TimelineEventType`, `BinaryFileGuid`, `BinaryFileName`, `Url`, `Source`, `SourceParticipantIdKey`, `SourceParticipantName`, `SourceExperimentIdKey`, `IsCorroborated`, `Order`, `CreatedDateTime`

### Hypothesis

`IdKey`, `ParentHypothesisIdKey`, `Name`, `Description`, `Probability`, `Status`, `RuledOutReason`

### Experiment

`IdKey`, `HypothesisIdKey`, `HypothesisText`, `TestPlan`, `ExpectedResult`, `ActualResult`, `Outcome`, `ChangesMade`, `ChangesReverted`, `RevertJustification`, `Learnings`, `CreatedDateTime`

### Participant

`IdKey`, `PersonAliasGuid`, `DisplayName`, `ExternalContact`, `IsExternal`, `Role`, `Statements`

### Statement

`IdKey`, `ParticipantIdKey`, `RequestPrompt`, `RequestStatus`, `RequestDateTime`, `SentDateTime`, `SubmittedDateTime`, `Statement`, `BinaryFileGuid`, `BinaryFileName`, `Url`, `IsOverdue`

## Example: open a case from an alert

A monitoring script that opens a case when check-in slows down, then records what it knows.

Open the case:

```bash
curl -X POST https://rock.example.org/api/com_yesuchum/rockdetective/cases \
  -H "Authorization-Token: $ROCK_REST_KEY" -H "Content-Type: application/json" \
  -d '{"title":"Check-in kiosks slow","problemStatement":"Check-in took over 10 seconds per family at the north campus starting 8:45 AM.","categoryGuid":"C66E56CB-25F7-4D98-A64A-A515F4A4E104","mode":1}'
```

Take `IdKey` from the response, then seed the first fact:

```bash
curl -X POST https://rock.example.org/api/com_yesuchum/rockdetective/cases/$CASE/entries \
  -H "Authorization-Token: $ROCK_REST_KEY" -H "Content-Type: application/json" \
  -d '{"entryType":0,"text":"Alert fired at 8:47 AM; average check-in time 11.2 seconds over 40 families.","isCorroborated":true}'
```

Later, pull the case file:

```bash
curl -o case.pdf https://rock.example.org/api/com_yesuchum/rockdetective/cases/$CASE/pdf \
  -H "Authorization-Token: $ROCK_REST_KEY"
```

## Things to know

- A case created through the API starts without the Define the Problem attestations or the Classify scores that the case list's new-case form records. Open it on the board to fill those in.
- Gate checks in the API use the default gate requirements, not the settings on your Case Board block.
- Setting `status` skips the Postmortem gate, and confirming a hypothesis skips the experiment check. The board enforces both.
- Statement requests created through the API are drafts. Sending and reminders happen on the Case Board.
- Attachments can only be uploaded on the Case Board.
