# Rock Detective Skill Defaults

This is the Rock Detective skill the way it ships: what the assistant is told about the skill, and every tool with its description, purpose, parameters, and Lava.

The skill is a regular Rock AI skill, so all of it is yours to change in Rock under **Admin Tools > System > AI Agents > AI Skills > Rock Detective**. Use this page to see what you started with, or to copy a piece back after changing it. The **Skill Settings** page (the button at the top right of the case list) shows which tools you have edited and puts any of them back to standard with one click.

Editing a tool changes what the assistant is told and what it sends. It does not change what is allowed: the checklists, case security, and who may use which tool are enforced by Rock Detective every time a tool runs. The [MCP Guide](Mcp.md#making-the-skill-your-own) explains how the tools work and how to write your own.

This page is generated from a clean install, so it matches the version in this repo (currently 1.1.0).

## The skill

These apply to the skill as a whole. The assistant reads the purpose, how to use it, and the guardrails when it connects. The description is what an administrator sees for the skill in Rock.

### Name

Rock Detective

### Description

Works Rock Detective problem-solving case files from start to finish: opens a case, gathers facts and evidence, tests possible causes, and closes with a postmortem. Also lets a witness answer a statement request, and searches the web server's IIS logs for evidence.

### Purpose

- Lets the agent diagnose a problem methodically and document the decisions, findings, and fix in a case file.

### How to use it

- Find a case with the list cases tool and read it with the get case tool.
- To work a problem, open a case and follow the phase it reports. Every tool that changes a case returns the current phase, its guidance, its checklist, and the next step. Do the work the checklist asks for, save your reasoning as phase notes, then complete the phase. Finish by closing the case with its postmortem.
- Search the IIS logs when investigating errors or slow pages.

### Guardrails

- Phase names, terms, and gate wording come back the way the case's category has them set up. Use those words when talking about the case.
- Record only what is verified as a corroborated fact. Mark anything a person reported, or anything inferred, as not corroborated until evidence backs it.
- Change one thing at a time in an experiment, log every change, and revert it or say why it stays.
- Do not confirm a hypothesis without an experiment that supports it.
- Never try to get around a checklist. If something is still needed, do that work first.
- What a person may do depends on who they are. Someone who was only asked for a statement can list, submit, and decline their own statement requests, and nothing else. If a tool says it is for people who work cases, do not look for another way to do it.

## The tools

There are 22. Every one is a **Lava** tool: Rock fills in the parameters the assistant sent, runs the Lava, and hands the output back to the assistant. Each standard tool's **Usage** is the same text as its description, and each one's **Returns** reads: "A JSON object. When the tool could not do what was asked it returns refused true and a reason that says what is needed."

| Tool | Who can use it |
|---|---|
| [List Cases](#list-cases) | People who work cases |
| [Get Case](#get-case) | People who work cases |
| [Search IIS Logs](#search-iis-logs) | Rock administrators |
| [List My Statement Requests](#list-my-statement-requests) | Anyone who was asked for a statement |
| [Submit Statement](#submit-statement) | Anyone who was asked for a statement |
| [Decline Statement](#decline-statement) | Anyone who was asked for a statement |
| [List Categories](#list-categories) | People who work cases |
| [Create Case](#create-case) | People who work cases |
| [Update Case](#update-case) | People who work cases |
| [Add Participant](#add-participant) | People who work cases |
| [Waive Champion](#waive-champion) | People who work cases |
| [Record Statement](#record-statement) | People who work cases |
| [Add Entry](#add-entry) | People who work cases |
| [Add Hypothesis](#add-hypothesis) | People who work cases |
| [Update Hypothesis](#update-hypothesis) | People who work cases |
| [Add Experiment](#add-experiment) | People who work cases |
| [Record Experiment Result](#record-experiment-result) | People who work cases |
| [Save Phase Notes](#save-phase-notes) | People who work cases |
| [Complete Phase](#complete-phase) | People who work cases |
| [Loop Back](#loop-back) | People who work cases |
| [Champion Sign-Off](#champion-sign-off) | People who work cases |
| [Close Case](#close-case) | People who work cases |

### List Cases

**Description:** Lists the Rock Detective case files the current person can read, newest first.

**Purpose:** Finds case files and their keys.

| Parameter | Type | Required | Instructions |
|---|---|---|---|
| `status` | Text | No | Only return cases with this status. Allowed values: Open, Investigating, Solved, Closed, Reopened, Abandoned. |
| `limit` | Number | No | Maximum number of cases to return. Default 25, max 100. |

```liquid
{% rockdetective tool:'list_cases' %}
{
  "status": {{ status | ToJSON }},
  "limit": {{ limit | ToJSON }}
}
{% endrockdetective %}
```

### Get Case

**Description:** Gets one Rock Detective case file in full: phases, gate checklists, facts, timeline, evidence, hypotheses, experiments, participants, and statements.

**Purpose:** Retrieves the full detail of a single case file.

| Parameter | Type | Required | Instructions |
|---|---|---|---|
| `idKey` | Text | Yes | The IdKey of the case file, from the list cases tool. |

```liquid
{% rockdetective tool:'get_case' %}
{
  "idKey": {{ idKey | ToJSON }}
}
{% endrockdetective %}
```

### Search IIS Logs

**Description:** Searches the web server's IIS log files for requests in a time range. Narrow it with the range and filters, because results are capped. Times are in the server's local time zone, and the values of sensitive query string parameters are redacted. Only Rock administrators can use it.

**Purpose:** Gathers request-level evidence such as errors and slow pages.

| Parameter | Type | Required | Instructions |
|---|---|---|---|
| `minutes` | Number | No | How many minutes back to search. Default 60, max 10080 (7 days). Ignored when start is given. |
| `start` | Text | No | Start of a time range in the server's local time, as ISO 8601, like 2026-11-08T08:00:00. Up to 30 days back. |
| `end` | Text | No | End of the time range. Defaults to now. A range can span up to 7 days. |
| `status` | Number | No | Only return requests with this HTTP status code (e.g. 500). |
| `pathContains` | Text | No | Only return requests whose URL path contains this text (case-insensitive). |
| `method` | Text | No | Only return requests with this HTTP method (e.g. POST). |
| `minTimeTakenMs` | Number | No | Only return requests that took at least this many milliseconds. |
| `limit` | Number | No | Maximum number of entries to return, newest first. Default 50, max 500. |
| `includeUserAgent` | True or false | No | Also return each request's browser user agent. Defaults to false to keep results small. |

```liquid
{% rockdetective tool:'search_iis_logs' %}
{
  "minutes": {{ minutes | ToJSON }},
  "start": {{ start | ToJSON }},
  "end": {{ end | ToJSON }},
  "status": {{ status | ToJSON }},
  "pathContains": {{ pathContains | ToJSON }},
  "method": {{ method | ToJSON }},
  "minTimeTakenMs": {{ minTimeTakenMs | ToJSON }},
  "limit": {{ limit | ToJSON }},
  "includeUserAgent": {{ includeUserAgent | ToJSON }}
}
{% endrockdetective %}
```

### List My Statement Requests

**Description:** For a witness: lists the statement requests that are waiting on the signed-in person, with the question being asked. It shows nothing else about the case.

**Purpose:** Shows a witness what they have been asked.

**Parameters:** none.

```liquid
{% rockdetective tool:'list_my_statement_requests' %}{% endrockdetective %}
```

### Submit Statement

**Description:** For a witness: submits the signed-in person's answer to a statement request that was addressed to them. It needs no other access to the case. Submit the person's own words without adding to or rewording them.

**Purpose:** Lets a witness answer a statement request.

| Parameter | Type | Required | Instructions |
|---|---|---|---|
| `statementIdKey` | Text | Yes | The IdKey of the request, from the list my statement requests tool. |
| `statement` | Text | Yes | What the person saw or knows, in their own words. |
| `url` | Text | No | An optional link that goes with the statement. |

```liquid
{% rockdetective tool:'submit_statement' %}
{
  "statementIdKey": {{ statementIdKey | ToJSON }},
  "statement": {{ statement | ToJSON }},
  "url": {{ url | ToJSON }}
}
{% endrockdetective %}
```

### Decline Statement

**Description:** For a witness: declines a statement request that was addressed to the signed-in person, with an optional reason.

**Purpose:** Lets a witness decline a statement request.

| Parameter | Type | Required | Instructions |
|---|---|---|---|
| `statementIdKey` | Text | Yes | The IdKey of the request, from the list my statement requests tool. |
| `reason` | Text | No |  |

```liquid
{% rockdetective tool:'decline_statement' %}
{
  "statementIdKey": {{ statementIdKey | ToJSON }},
  "reason": {{ reason | ToJSON }}
}
{% endrockdetective %}
```

### List Categories

**Description:** Lists the case categories a case can be opened under. Each category can have its own names for phases and terms.

**Purpose:** Finds the category to open a case under.

**Parameters:** none.

```liquid
{% rockdetective tool:'list_categories' %}{% endrockdetective %}
```

### Create Case

**Description:** Opens a new case file. The signed-in person becomes the owner. Simple mode starts at the fact list; Advanced mode runs all nine phases. Returns where the case stands and the next step.

**Purpose:** Opens a case for a problem that needs to be worked methodically.

| Parameter | Type | Required | Instructions |
|---|---|---|---|
| `title` | Text | Yes | A short handle for the case. |
| `problemStatement` | Text | Yes | One clear, concise statement of the problem with nothing unverified and nothing that is not germane. |
| `category` | Text | Yes | The category name or guid, from the list categories tool. |
| `mode` | Text | No | Simple for easy problems, Advanced for difficult ones. Defaults to Simple. Allowed values: Simple, Advanced. |
| `isProblemStatementVerified` | True or false | No | True only if every detail in the problem statement has been verified and all of it is germane. Advanced cases need this to complete the problem definition phase. |

```liquid
{% rockdetective tool:'create_case' %}
{
  "title": {{ title | ToJSON }},
  "problemStatement": {{ problemStatement | ToJSON }},
  "category": {{ category | ToJSON }},
  "mode": {{ mode | ToJSON }},
  "isProblemStatementVerified": {{ isProblemStatementVerified | ToJSON }}
}
{% endrockdetective %}
```

### Update Case

**Description:** Changes a case's title, problem statement, category, or whether the problem statement is verified. Leave out anything that should stay as it is.

**Purpose:** Refines the problem definition as understanding improves.

| Parameter | Type | Required | Instructions |
|---|---|---|---|
| `caseIdKey` | Text | Yes | The IdKey of the case file, from the list cases tool or the create case tool. |
| `title` | Text | No |  |
| `problemStatement` | Text | No |  |
| `category` | Text | No | The category name or guid. |
| `isProblemStatementVerified` | True or false | No | True only if every detail in the problem statement has been verified and all of it is germane. |

```liquid
{% rockdetective tool:'update_case' %}
{
  "caseIdKey": {{ caseIdKey | ToJSON }},
  "title": {{ title | ToJSON }},
  "problemStatement": {{ problemStatement | ToJSON }},
  "category": {{ category | ToJSON }},
  "isProblemStatementVerified": {{ isProblemStatementVerified | ToJSON }}
}
{% endrockdetective %}
```

### Add Participant

**Description:** Adds a person to the case team or as a witness, either a Rock person or someone outside Rock. A case has one Champion.

**Purpose:** Builds the team and records who has information about the problem.

| Parameter | Type | Required | Instructions |
|---|---|---|---|
| `caseIdKey` | Text | Yes | The IdKey of the case file, from the list cases tool or the create case tool. |
| `role` | Text | No | Defaults to Member. Allowed values: Champion, Recorder, Communicator, Member, Witness. |
| `personAliasGuid` | Text | No | The person alias guid of a Rock person. |
| `externalName` | Text | No | The name of someone who is not in Rock. |
| `externalContact` | Text | No | How to reach an external person. |

```liquid
{% rockdetective tool:'add_participant' %}
{
  "caseIdKey": {{ caseIdKey | ToJSON }},
  "role": {{ role | ToJSON }},
  "personAliasGuid": {{ personAliasGuid | ToJSON }},
  "externalName": {{ externalName | ToJSON }},
  "externalContact": {{ externalContact | ToJSON }}
}
{% endrockdetective %}
```

### Waive Champion

**Description:** Records that this case will go without a Champion, with the reason. Use it for problems small enough not to need an independent sanity check. It is refused when the case already has a Champion.

**Purpose:** Satisfies the team gate when no Champion is needed.

| Parameter | Type | Required | Instructions |
|---|---|---|---|
| `caseIdKey` | Text | Yes | The IdKey of the case file, from the list cases tool or the create case tool. |
| `reason` | Text | Yes | Why a Champion is not needed. |

```liquid
{% rockdetective tool:'waive_champion' %}
{
  "caseIdKey": {{ caseIdKey | ToJSON }},
  "reason": {{ reason | ToJSON }}
}
{% endrockdetective %}
```

### Record Statement

**Description:** Records what a participant or witness said. Optionally adds it to the fact list. Statements are never fully trusted, so only mark a fact corroborated when evidence backs it.

**Purpose:** Documents what people reported about the problem.

| Parameter | Type | Required | Instructions |
|---|---|---|---|
| `caseIdKey` | Text | Yes | The IdKey of the case file, from the list cases tool or the create case tool. |
| `participantIdKey` | Text | Yes | The IdKey of the participant, from the add participant tool or the get case tool. |
| `statement` | Text | Yes |  |
| `url` | Text | No | An optional link that goes with the statement. |
| `addToFacts` | True or false | No | Also add the statement to the fact list. Defaults to false. |
| `isCorroborated` | True or false | No | Marks the added fact as corroborated by evidence. Defaults to false. |

```liquid
{% rockdetective tool:'record_statement' %}
{
  "caseIdKey": {{ caseIdKey | ToJSON }},
  "participantIdKey": {{ participantIdKey | ToJSON }},
  "statement": {{ statement | ToJSON }},
  "url": {{ url | ToJSON }},
  "addToFacts": {{ addToFacts | ToJSON }},
  "isCorroborated": {{ isCorroborated | ToJSON }}
}
{% endrockdetective %}
```

### Add Entry

**Description:** Adds a fact, a timeline event, or a piece of evidence to the case. Facts must be specific, correct, and pertinent. Only facts count toward the fact list checklist; evidence and timeline events do not. The entry is marked as added by an AI assistant.

**Purpose:** Records what is known, when things happened, and the evidence for it.

| Parameter | Type | Required | Instructions |
|---|---|---|---|
| `caseIdKey` | Text | Yes | The IdKey of the case file, from the list cases tool or the create case tool. |
| `entryType` | Text | No | Defaults to Fact. Allowed values: Fact, TimelineEvent, Evidence. |
| `text` | Text | Yes |  |
| `eventDateTime` | Text | No | When a timeline event happened, as an ISO 8601 date and time. Without an offset it is read as the church's local time. Required for timeline events. |
| `timelineEventType` | Text | No | For timeline events. The timeline needs one ProblemStart. Allowed values: ProblemStart, LastKnownGood, Change, Activity, Other. |
| `url` | Text | No | An optional link, mostly for evidence. It has to start with http:// or https://. |
| `isCorroborated` | True or false | No | For facts: true only when evidence backs the fact. Defaults to false. |

```liquid
{% rockdetective tool:'add_entry' %}
{
  "caseIdKey": {{ caseIdKey | ToJSON }},
  "entryType": {{ entryType | ToJSON }},
  "text": {{ text | ToJSON }},
  "eventDateTime": {{ eventDateTime | ToJSON }},
  "timelineEventType": {{ timelineEventType | ToJSON }},
  "url": {{ url | ToJSON }},
  "isCorroborated": {{ isCorroborated | ToJSON }}
}
{% endrockdetective %}
```

### Add Hypothesis

**Description:** Adds a candidate cause to the probability tree. Brainstorm causes first and assign probabilities second. A parent hypothesis key breaks a broad hypothesis into a child node.

**Purpose:** Builds the list of possible causes.

| Parameter | Type | Required | Instructions |
|---|---|---|---|
| `caseIdKey` | Text | Yes | The IdKey of the case file, from the list cases tool or the create case tool. |
| `name` | Text | Yes | The candidate cause, stated so it can be tested. |
| `description` | Text | No |  |
| `probability` | Number | No | How likely this cause is, 0 to 100. A root hypothesis only counts toward the gate when this is above 0. |
| `parentHypothesisIdKey` | Text | No |  |

```liquid
{% rockdetective tool:'add_hypothesis' %}
{
  "caseIdKey": {{ caseIdKey | ToJSON }},
  "name": {{ name | ToJSON }},
  "description": {{ description | ToJSON }},
  "probability": {{ probability | ToJSON }},
  "parentHypothesisIdKey": {{ parentHypothesisIdKey | ToJSON }}
}
{% endrockdetective %}
```

### Update Hypothesis

**Description:** Changes a hypothesis's probability or status. Ruling one out needs a reason. Confirming one only satisfies the reevaluate gate when an experiment testing it came out Supported.

**Purpose:** Keeps the probability tree current as evidence comes in.

| Parameter | Type | Required | Instructions |
|---|---|---|---|
| `caseIdKey` | Text | Yes | The IdKey of the case file, from the list cases tool or the create case tool. |
| `hypothesisIdKey` | Text | Yes |  |
| `probability` | Number | No | 0 to 100. |
| `status` | Text | No | Allowed values: Proposed, Testing, RuledOut, Confirmed. |
| `ruledOutReason` | Text | No |  |

```liquid
{% rockdetective tool:'update_hypothesis' %}
{
  "caseIdKey": {{ caseIdKey | ToJSON }},
  "hypothesisIdKey": {{ hypothesisIdKey | ToJSON }},
  "probability": {{ probability | ToJSON }},
  "status": {{ status | ToJSON }},
  "ruledOutReason": {{ ruledOutReason | ToJSON }}
}
{% endrockdetective %}
```

### Add Experiment

**Description:** Logs a planned experiment that tests one hypothesis, changing one factor at a time. Link it to a hypothesis in the probability tree or describe the hypothesis in text.

**Purpose:** Plans a test of a possible cause.

| Parameter | Type | Required | Instructions |
|---|---|---|---|
| `caseIdKey` | Text | Yes | The IdKey of the case file, from the list cases tool or the create case tool. |
| `hypothesisIdKey` | Text | No |  |
| `hypothesisText` | Text | No | Use when the hypothesis is not in the probability tree. |
| `testPlan` | Text | No |  |
| `expectedResult` | Text | No |  |

```liquid
{% rockdetective tool:'add_experiment' %}
{
  "caseIdKey": {{ caseIdKey | ToJSON }},
  "hypothesisIdKey": {{ hypothesisIdKey | ToJSON }},
  "hypothesisText": {{ hypothesisText | ToJSON }},
  "testPlan": {{ testPlan | ToJSON }},
  "expectedResult": {{ expectedResult | ToJSON }}
}
{% endrockdetective %}
```

### Record Experiment Result

**Description:** Records what an experiment showed. Log every change made so it can be reverted; the gate wants changes reverted, justified, or never made. Learnings are added to the fact list once.

**Purpose:** Documents the result of a test and what was learned.

| Parameter | Type | Required | Instructions |
|---|---|---|---|
| `caseIdKey` | Text | Yes | The IdKey of the case file, from the list cases tool or the create case tool. |
| `experimentIdKey` | Text | Yes |  |
| `actualResult` | Text | No |  |
| `outcome` | Text | Yes | Allowed values: Supported, RuledOut, Inconclusive. |
| `changesMade` | Text | No | Every change made during the experiment. Leave out if nothing was changed. |
| `changesReverted` | True or false | No | True when all changes have been reverted. Defaults to false. |
| `revertJustification` | Text | No | Why changes remain in place, when they were not reverted. |
| `learnings` | Text | No | What was learned. This becomes a corroborated fact. |

```liquid
{% rockdetective tool:'record_experiment_result' %}
{
  "caseIdKey": {{ caseIdKey | ToJSON }},
  "experimentIdKey": {{ experimentIdKey | ToJSON }},
  "actualResult": {{ actualResult | ToJSON }},
  "outcome": {{ outcome | ToJSON }},
  "changesMade": {{ changesMade | ToJSON }},
  "changesReverted": {{ changesReverted | ToJSON }},
  "revertJustification": {{ revertJustification | ToJSON }},
  "learnings": {{ learnings | ToJSON }}
}
{% endrockdetective %}
```

### Save Phase Notes

**Description:** Saves working notes for a phase: the answers to its prompts, decisions made, and why. Defaults to the current phase and replaces that phase's earlier notes.

**Purpose:** Documents decisions and reasoning as the case moves along.

| Parameter | Type | Required | Instructions |
|---|---|---|---|
| `caseIdKey` | Text | Yes | The IdKey of the case file, from the list cases tool or the create case tool. |
| `phase` | Number | No | The phase number, 1 to 9. Defaults to the current phase. |
| `notes` | Text | Yes |  |

```liquid
{% rockdetective tool:'save_phase_notes' %}
{
  "caseIdKey": {{ caseIdKey | ToJSON }},
  "phase": {{ phase | ToJSON }},
  "notes": {{ notes | ToJSON }}
}
{% endrockdetective %}
```

### Complete Phase

**Description:** Completes the case's current phase and moves to the next one. It only works when every item on the phase's checklist is met; otherwise it says what is still needed.

**Purpose:** Moves the case forward once a phase's requirements are met.

| Parameter | Type | Required | Instructions |
|---|---|---|---|
| `caseIdKey` | Text | Yes | The IdKey of the case file, from the list cases tool or the create case tool. |

```liquid
{% rockdetective tool:'complete_phase' %}
{
  "caseIdKey": {{ caseIdKey | ToJSON }}
}
{% endrockdetective %}
```

### Loop Back

**Description:** Sends the case back to the fact list for another round through facts, the probability tree, experiments, and reevaluation. Use it from the reevaluate phase when no cause is confirmed yet.

**Purpose:** Starts another round of investigation.

| Parameter | Type | Required | Instructions |
|---|---|---|---|
| `caseIdKey` | Text | Yes | The IdKey of the case file, from the list cases tool or the create case tool. |

```liquid
{% rockdetective tool:'loop_back' %}
{
  "caseIdKey": {{ caseIdKey | ToJSON }}
}
{% endrockdetective %}
```

### Champion Sign-Off

**Description:** Records the Champion's sign-off on the confirmed explanation. It works once a hypothesis is confirmed with a supporting experiment, for the person who is the Champion. When the Champion is someone outside Rock, the case owner records it for them.

**Purpose:** Lets the Champion approve the confirmed explanation.

| Parameter | Type | Required | Instructions |
|---|---|---|---|
| `caseIdKey` | Text | Yes | The IdKey of the case file, from the list cases tool or the create case tool. |

```liquid
{% rockdetective tool:'champion_sign_off' %}
{
  "caseIdKey": {{ caseIdKey | ToJSON }}
}
{% endrockdetective %}
```

### Close Case

**Description:** Closes the case with its postmortem and sends it to the archive. The case must be on its last phase and all six postmortem fields are required.

**Purpose:** Documents the outcome: what happened, what fixed it, and how to prevent it.

| Parameter | Type | Required | Instructions |
|---|---|---|---|
| `caseIdKey` | Text | Yes | The IdKey of the case file, from the list cases tool or the create case tool. |
| `impactSummary` | Text | Yes | What the problem cost or affected. |
| `solutionSummary` | Text | Yes | What fixed it. |
| `contributingFactors` | Text | Yes |  |
| `lessonsLearned` | Text | Yes |  |
| `preventionMeasures` | Text | Yes | What keeps it from happening again. |
| `actionItems` | Text | Yes | Follow-up work that remains. |
| `followUpDate` | Text | No | Optional ISO 8601 date on which the owner is asked whether the fix held. |

```liquid
{% rockdetective tool:'close_case' %}
{
  "caseIdKey": {{ caseIdKey | ToJSON }},
  "impactSummary": {{ impactSummary | ToJSON }},
  "solutionSummary": {{ solutionSummary | ToJSON }},
  "contributingFactors": {{ contributingFactors | ToJSON }},
  "lessonsLearned": {{ lessonsLearned | ToJSON }},
  "preventionMeasures": {{ preventionMeasures | ToJSON }},
  "actionItems": {{ actionItems | ToJSON }},
  "followUpDate": {{ followUpDate | ToJSON }}
}
{% endrockdetective %}
```
