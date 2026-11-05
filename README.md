# Rock Detective

Rock Detective is a self-contained Rock RMS Obsidian plugin that runs problems through Jon Edmiston's Triumphant Problem Solving method as case files. A case owner works nine gated phases, and witnesses submit statements or have them recorded. Facts, a probability tree, and experiments live on a case board with a loop through phases 5 to 8. Closed cases go to an archive with a postmortem and a PDF export. Any problem type fits: bugs, performance, process, ministry operations.

The current version is 1.0.1, and it needs Rock 18.2 or later.

## Documentation

- [User Guide](docs/UserGuide.md): install, opening a case, the Case Board, witness statements, closing and follow-up, settings, security.
- [Technical Specification](docs/TechnicalSpec.md): architecture, schema, entity plumbing, gate evaluator, witness flow, jobs, PDF, migrations, build and deploy.
- [Uninstall Guide](docs/Uninstall.md): file removal, data removal SQL, Rock configuration cleanup.

## Repo layout

```
com.yesuchum.RockDetective/           C# class library: entities, blocks,
                                      jobs, REST API, PDF export, plugin
                                      migration, gate evaluator.
com.yesuchum.RockDetective.Obsidian/  Vue 3 and TypeScript Obsidian
                                      frontends for the three blocks.
com.yesuchum.RockDetective.Tests/     MSTest unit tests for the gate
                                      evaluator.
docs/                                 User guide, technical spec, uninstall.
```

## Build

The repo is built inside a Rock source checkout, because the projects reference the Rock source projects and `RockWeb` by relative path. C# class library and tests:

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

The builds copy their output into `RockWeb`, as described in the [Technical Specification](docs/TechnicalSpec.md). A Rock restart runs the plugin migration.

## REST API

All endpoints live under `/api/com_yesuchum/rockdetective` and require Rock REST authentication. They enforce entity security on the case: View for reads and Edit for writes, with a 401 when the check fails. `idKey` values are Rock IdKey hashes, as the API returns them. The point is that a monitoring script can open a case, seed facts, and later append experiment results without touching the UI.

The full reference, with every field, response shape, and enum value, is in [docs/RestApi.md](docs/RestApi.md).

| Method | Route | Description |
|---|---|---|
| GET | `/cases?status=&category=&owner=` | List the cases the caller can view, each as a full case aggregate. `status` is the numeric CaseStatus (0 Open, 1 Investigating, 2 Solved, 3 Closed, 4 Reopened, 5 Abandoned); `category` is a Category guid; `owner` is a Person guid. |
| GET | `/cases/{idKey}` | Full case aggregate: case fields, phase states, gate results, entries, hypotheses, experiments, participants with statements. |
| POST | `/cases` | Create a case. Body: `{ title, problemStatement, categoryGuid, mode }` where mode is 0 Simple or 1 Advanced. The caller becomes the owner. |
| PATCH | `/cases/{idKey}` | Update `title`, `problemStatement`, `status`, or `currentPhase`. Advancing `currentPhase` past an unmet gate is rejected, using the default gate requirements. `status` is set as given. |
| GET | `/cases/{idKey}/entries` | List entries (facts, timeline events, evidence). |
| POST | `/cases/{idKey}/entries` | Add an entry. Body: `{ entryType, text, eventDateTime?, timelineEventType?, url?, isCorroborated? }` where entryType is 0 Fact, 1 TimelineEvent, 2 Evidence. |
| POST | `/cases/{idKey}/hypotheses` | Add a hypothesis. Body: `{ name, description?, probability, parentHypothesisIdKey? }`. |
| PATCH | `/hypotheses/{idKey}` | Update `probability`, `status` (0 Proposed, 1 Testing, 2 RuledOut, 3 Confirmed), or `ruledOutReason`. |
| POST | `/cases/{idKey}/experiments` | Add an experiment. Body: `{ hypothesisIdKey?, hypothesisText?, testPlan?, expectedResult? }`. One of the hypothesis fields is required. |
| PATCH | `/experiments/{idKey}` | Update `actualResult`, `outcome` (0 Pending, 1 Supported, 2 RuledOut, 3 Inconclusive), `changesMade`, `changesReverted`, `learnings`. |
| POST | `/cases/{idKey}/participants` | Add a participant. Body: `{ personAliasGuid?, externalName?, externalContact?, role }` where role is 0 Owner through 5 Witness. One of personAliasGuid or externalName is required. |
| POST | `/participants/{idKey}/request-statement` | Create a witness statement request and return the new statement. Body: `{ "requestPrompt": "..." }`. Each request is its own statement (a witness can have several). It starts as a draft, and sending is done from the Case Board. |
| GET | `/cases/{idKey}/pdf` | The full case file as a PDF (`application/pdf` attachment). |

### Example

Open a case from a monitoring alert, then seed a fact.

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
