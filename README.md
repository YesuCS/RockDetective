# Rock Detective

Self-contained Rock RMS Obsidian plugin that runs problems through Jon Edmiston's Triumphant Problem Solving method as case files. A case owner works nine gated phases; witnesses submit or have statements recorded; facts, a probability tree, and experiments live on a case board with a first-class 5-through-8 loop; closed cases become a searchable archive with a postmortem and PDF export. Any problem type fits: bugs, performance, process, ministry operations.

## Documentation

- [User Guide](docs/UserGuide.md). Install, opening a case, the Case Board, witness statements, closing and follow-up, block settings, security.
- [Technical Specification](docs/TechnicalSpec.md). Architecture, schema, entity plumbing gotchas, gate evaluator, witness flow, jobs, REST, PDF, migrations, build and deploy.
- [Uninstall Guide](docs/Uninstall.md). File removal, data removal SQL, Rock configuration housekeeping.

## Repo layout

```
com.yesuchum.RockDetective/           C# class library: entities, blocks,
                                      jobs, REST API, PDF export, plugin
                                      migration, gate evaluator.
com.yesuchum.RockDetective.Obsidian/  Vue 3 plus TypeScript Obsidian
                                      frontends for the three block types.
com.yesuchum.RockDetective.Tests/     MSTest unit tests for the gate
                                      evaluator.
docs/                                 User guide, technical spec, uninstall.
```

## Build

C# class library and tests:

```
dotnet build com.yesuchum.RockDetective
dotnet test com.yesuchum.RockDetective.Tests
```

Obsidian frontends (one-time `npm install`, then rebuild on each change):

```
cd com.yesuchum.RockDetective.Obsidian
npm install
npm run build
```

Outputs deploy into a Rock install as described in `docs/TechnicalSpec.md`; a Rock restart runs the plugin migration.

## REST API

All endpoints live under `/api/com_yesuchum/rockdetective`, require Rock REST authentication, and enforce entity security on the case (View for reads, Edit for writes). `idKey` values are Rock IdKey hashes as returned by the API itself. The intent: a monitoring script can open a case, seed facts, and later append experiment results without touching the UI.

| Method | Route | Description |
|---|---|---|
| GET | `/cases?status=&category=&owner=` | List cases the caller can view. `status` is the numeric CaseStatus (0 Open, 1 Investigating, 2 Solved, 3 Closed, 4 Reopened, 5 Abandoned); `category` is a Category guid; `owner` is a Person guid. |
| GET | `/cases/{idKey}` | Full case aggregate: case fields, phase states, gate results, entries, hypotheses, experiments, participants with statements. |
| POST | `/cases` | Create a case. Body: `{ title, problemStatement, categoryGuid, mode }` where mode is 0 Simple or 1 Advanced. The caller becomes the owner. |
| PATCH | `/cases/{idKey}` | Update `title`, `problemStatement`, `status`, or `currentPhase`. Advancing `currentPhase` past an unmet gate is rejected. |
| GET | `/cases/{idKey}/entries` | List entries (facts, timeline events, evidence). |
| POST | `/cases/{idKey}/entries` | Add an entry. Body: `{ entryType, text, eventDateTime?, timelineEventType?, url?, isCorroborated? }` where entryType is 0 Fact, 1 TimelineEvent, 2 Evidence. |
| POST | `/cases/{idKey}/hypotheses` | Add a hypothesis. Body: `{ name, description?, probability, parentHypothesisIdKey? }`. |
| PATCH | `/hypotheses/{idKey}` | Update `probability`, `status` (0 Proposed, 1 Testing, 2 RuledOut, 3 Confirmed), or `ruledOutReason`. |
| POST | `/cases/{idKey}/experiments` | Add an experiment. Body: `{ hypothesisIdKey?, hypothesisText?, testPlan?, expectedResult? }`. One of the hypothesis fields is required. |
| PATCH | `/experiments/{idKey}` | Update `actualResult`, `outcome` (0 Pending, 1 Supported, 2 RuledOut, 3 Inconclusive), `changesMade`, `changesReverted`, `learnings`. |
| POST | `/cases/{idKey}/participants` | Add a participant. Body: `{ personAliasGuid?, externalName?, externalContact?, role }` where role is 0 Owner through 5 Witness. One of personAliasGuid or externalName is required. |
| POST | `/participants/{idKey}/request-statement` | Create a witness statement request; returns the new statement. Body: `{ "requestPrompt": "..." }`. Each request is its own statement (a witness can have several); it starts as a draft and sending is done from the case board. |
| GET | `/cases/{idKey}/pdf` | The full case file as a PDF (`application/pdf` attachment). |

Example: open a case from a monitoring alert, then seed a fact.

```bash
curl -X POST https://rock.example.org/api/com_yesuchum/rockdetective/cases \
  -H "Authorization-Token: $ROCK_REST_KEY" -H "Content-Type: application/json" \
  -d '{"title":"API p95 latency spike","problemStatement":"p95 latency on /api/checkin rose from 180ms to 2.4s starting 06:12.","categoryGuid":"C66E56CB-25F7-4D98-A64A-A515F4A4E104","mode":1}'
```

```bash
curl -X POST https://rock.example.org/api/com_yesuchum/rockdetective/cases/{idKey}/entries \
  -H "Authorization-Token: $ROCK_REST_KEY" -H "Content-Type: application/json" \
  -d '{"entryType":0,"text":"Alert fired at 06:12; deploy 2026.07.31.1 completed at 06:07.","isCorroborated":true}'
```
