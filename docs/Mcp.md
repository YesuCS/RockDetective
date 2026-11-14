# Rock Detective MCP Guide

MCP (Model Context Protocol) is how an AI assistant like Claude talks to another system. Rock Detective plugs into it, so an assistant can do more than answer "what cases are open?". It can open a case, work it through every phase of the method, and close it with a postmortem, leaving a case file that documents what was found, what was decided, and what fixed it.

This guide covers the two ways to connect, the tools the assistant gets, how it works a case on its own, and the IIS log search. You don't need to know how MCP works to use it.

## Two ways to connect

| | Rock's MCP server | Direct endpoint |
|---|---|---|
| What it is | Rock Detective shows up as a skill you add to one of Rock's own AI agents. | An MCP endpoint that belongs to Rock Detective. |
| Address | `/api/v2/mcp/{your agent's slug}` | `/api/com_yesuchum/rockdetective/mcp` |
| Best for | Most churches. Rock Detective sits next to Rock's other skills (people, groups, giving) in one connection. | A quick connection with nothing but a REST key, or a script that only needs Rock Detective. |
| Who can search IIS logs | Rock administrators. | Whoever you allow on the REST action. |
| Choosing which tools are on | Per agent, tool by tool. | All tools, always. |

Both give the assistant the same tools and the same answers. Start with Rock's MCP server unless you have a reason not to.

Either way, you need Rock Detective 1.1.0 or later on Rock 19.7 or later.

## Option 1: Rock's MCP server

Rock registers a **Rock Detective Skill** on its own when the plugin is installed and Rock restarts. All you do is add it to an agent.

1. Go to **Admin Tools > System > AI Agents > Agents**.
2. Open the MCP agent you already use, or add a new agent, set its type to **MCP**, and give it a slug like `rock-detective`.
3. Add the **Rock Detective Skill** to the agent and choose which of its tools to turn on. Leave the tools that change cases off if the assistant should only read.
4. Save. The agent's address is your Rock address plus `/api/v2/mcp/` plus the slug:

```
https://rock.example.org/api/v2/mcp/rock-detective
```

Connect your assistant to that address the same way you connect to any other Rock MCP agent. Who can use it is controlled by the agent's security in Rock.

The tools show up to the assistant with names like **List Cases**, **Create Case**, and **Search IIS Logs**.

## Option 2: The direct endpoint

This one needs no agent. The assistant signs in with a Rock REST key and can only see what that key's person can see.

### Create a REST key

1. Go to **Admin Tools > Security > REST Keys** and add a key. Give it a name that says what it's for, like `Rock Detective MCP`.
2. Copy the key. You'll paste it into your MCP client below.
3. Put the key's person in the **Rock Detective Users** security role so it can view and edit cases. Leave it out of that role if the assistant should only see cases it has been added to.

### Allow the key to call the endpoint

Rock secures every REST endpoint separately.

1. Go to **Admin Tools > Security > REST Controllers** and open **RockDetectiveMcp**.
2. On the **POST** action, click the padlock and give your REST key's person (or a role they are in) **View** and **Edit**.

Rock Administration can already call it, so you can skip this if the key belongs to an administrator.

### Connect your assistant

The endpoint is your Rock address plus this path:

```
https://rock.example.org/api/com_yesuchum/rockdetective/mcp
```

The key goes in a header named `Authorization-Token`.

In Claude Code:

```bash
claude mcp add --transport http rock-detective https://rock.example.org/api/com_yesuchum/rockdetective/mcp --header "Authorization-Token: YOUR_REST_KEY"
```

In any other client, add a remote (HTTP) MCP server with that URL and that header. The tools show up with names like `list_cases`, `create_case`, and `search_iis_logs`.

## The tools

| Tool | What it does | What you can pass |
|---|---|---|
| List Cases | Lists the cases the person can view, newest first. | `status` (Open, Investigating, Solved, Closed, Reopened, Abandoned) and `limit` (default 25, up to 100). |
| Get Case | Returns one whole case: phases, gate checklists, facts, timeline, evidence, hypotheses, experiments, participants, and statements. | The case's IdKey, which comes from List Cases. |
| Search IIS Logs | Searches the web server's IIS logs for recent requests. | See the next section. |

Phase names, terms, and gate wording come back the way that case's category has them set up under Terminology Settings, so the assistant uses the same words your team sees on the Case Board.

Those three only read. The rest change cases:

| Tool | What it does |
|---|---|
| List Categories | Lists the categories a case can be opened under. |
| Create Case | Opens a case in Simple or Advanced mode. The signed-in person becomes the owner. |
| Update Case | Changes the title, problem statement, or category, or marks the problem statement as verified. |
| Add Participant | Adds a team member or witness, either a Rock person or someone outside Rock. |
| Waive Champion | Records that the case will go without a Champion, with the reason. |
| Record Statement | Records what a participant said, and can add it to the fact list. |
| Add Entry | Adds a fact, a timeline event, or a piece of evidence. |
| Add Hypothesis | Adds a candidate cause to the probability tree, optionally under another one. |
| Update Hypothesis | Changes a probability, or marks a hypothesis Testing, Ruled Out, or Confirmed. |
| Add Experiment | Logs a planned test of one hypothesis. |
| Record Experiment Result | Records the outcome, the changes made, whether they were reverted, and what was learned. |
| Save Phase Notes | Saves the reasoning and decisions for a phase. |
| Complete Phase | Completes the current phase and moves to the next one. |
| Loop Back | Sends the case back to the fact list for another round. |
| Champion Sign-Off | Records the Champion's sign-off. Only works for the person who is the Champion. |
| Close Case | Closes the case with its postmortem and an optional follow-up date. |

And three are for witnesses:

| Tool | What it does |
|---|---|
| List My Statement Requests | Shows the signed-in person the requests waiting on them, with the question and who asked. Nothing else about the case. |
| Submit Statement | Submits their answer to a request that was addressed to them. |
| Decline Statement | Declines a request that was addressed to them, with an optional reason. |

## Letting an assistant work a case on its own

Every tool that changes a case answers with where the case stands: the current phase, that phase's guidance and prompts, its checklist with each line marked met or not, and the next step. That is what lets an assistant run the method without a person steering it. It opens a case, reads what the phase asks for, does that work, and moves on.

A request as simple as this is enough to start:

> Connect card submissions have been failing on Sunday mornings. Open a Rock Detective case, check the IIS logs, and work it through.

What keeps the result trustworthy:

- **The gates apply to the assistant too.** Complete Phase only works when every line of the phase's checklist is met. When it isn't, the assistant is told what is still needed and has to do that work first. The minimums are the same ones set on the Case Board block.
- **What the assistant adds is marked.** Facts, timeline events, and evidence it adds show "Added by an AI assistant" on the Case Board and "AI Assistant" as the source in the PDF.
- **It has to say what is verified.** A fact is only marked corroborated when the assistant says evidence backs it, and an Advanced case can't leave Define the Problem until the problem statement is marked verified.
- **The Champion stays a person.** The assistant can only sign off as Champion if the person it is signed in as is the Champion on that case. Otherwise it can waive the Champion with a reason when it opens a small case, or a person signs off on the Case Board.
- **A closed case is closed.** The assistant can't change a closed or abandoned case. Reopening and resuming happen on the Case Board.

The assistant works as the person it signed in with. It can open cases as that person, and it can only change cases that person owns or has Edit on. To let it read without changing anything, connect through Rock's MCP server and turn the changing tools off on the agent. The direct endpoint always offers every tool, so use it only where the assistant is meant to work cases.

For a script that only needs to push data in, the [REST API](RestApi.md) is still there.

## Letting a witness answer through an assistant

A witness doesn't need any access to the case to answer a statement request. The same is true over MCP: the three witness tools only ever act on requests addressed to the person who is signed in.

To give witnesses that and nothing more, set up a second agent just for them:

1. Go to **Admin Tools > System > AI Agents > Agents** and add an agent with the type **MCP** and a slug like `rock-detective-witness`.
2. Add the **Rock Detective Skill** and turn on only **List My Statement Requests**, **Submit Statement**, and **Decline Statement**.
3. On the agent's security, give View to the people who may be asked for statements. New agents are open to Rock Administration only.

A witness who connects to that agent can see what they were asked, answer it, or decline it. They can't list cases, read a case, search logs, or open anything. A request has to be sent from the Case Board before it shows up for them, and an external witness (someone who is not in Rock) can't use this at all, since there is no Rock person to sign in as.

Keep witnesses off the direct endpoint. It always offers every tool, and the witness agent is the only way to offer three.

## Searching IIS logs

IIS writes a line for every request that reaches your web server. That makes the logs some of the best evidence you have when a page is failing or slow, and Search IIS Logs lets you ask for exactly the slice you need.

| Option | What it does |
|---|---|
| `minutes` | How far back to look. Default 60, up to 10080 (7 days). |
| `status` | Only requests with this HTTP status, like `500`. |
| `pathContains` | Only requests whose URL path contains this text. Case doesn't matter. |
| `method` | Only this HTTP method, like `POST`. |
| `minTimeTakenMs` | Only requests that took at least this many milliseconds. |
| `limit` | The most entries to return. Default 100, up to 500. |

Options combine, so "POSTs to the connect card that took over 5 seconds in the last hour" is one call. Each entry comes back with the time, method, path, query string, status, time taken, client IP, username, and browser.

Things to ask your assistant once it's connected:

- "Show me every 500 error from the last hour."
- "Which requests took longer than 5 seconds today?"
- "What hit the connect card page in the last 30 minutes?"
- "Read the guest follow-up case, then check the logs for anything that lines up with its timeline."

### Where the logs are read from

Rock Detective reads from the standard IIS location, `C:\inetpub\logs\LogFiles`, including the folders under it. If your logs live somewhere else:

1. Go to **Admin Tools > General > Global Attributes**.
2. Find **Rock Detective IIS Log Folder** and enter the folder path.

The folder is a server setting on purpose. Nobody calling the tool can point it at a different folder.

### If it comes back empty

The result includes a `warning` line when something is off. The two you'll see most:

- **The folder doesn't exist or isn't accessible.** The account your Rock application pool runs as needs Read access to the log folder. Give it that in the folder's security settings, or point the global attribute at a folder it can read.
- **No log files were modified in that window.** Widen `minutes`, or check that IIS logging is turned on for the site.

A few more things worth knowing:

- IIS writes log times in UTC. Rock Detective converts them, so the times you get back are in the server's local time.
- IIS holds log lines in memory for a bit before writing them, so the last minute or so may not be there yet.
- On a web farm each server keeps its own logs. The tool reads the logs of whichever server answered the call, unless you point the setting at a shared folder that all of them write to.

## Using the logs in Lava

The same search is available as a Lava command, for when you want log data on a page, in a report, or in a workflow instead of through an assistant.

It has to be enabled first, like any secured Lava command: add `iislogs` to the enabled Lava commands on the block or template that uses it.

```
{% iislogs status:'500' minutes:'60' limit:'25' %}
    <p>{{ logResult.MatchedCount }} errors in the last hour.</p>
    <ul>
    {% for entry in logEntries %}
        <li>{{ entry.DateTime }} {{ entry.Method }} {{ entry.UriStem }} ({{ entry.TimeTakenMs }} ms)</li>
    {% endfor %}
    </ul>
{% endiislogs %}
```

| Parameter | Matches the tool option |
|---|---|
| `minutes` | `minutes` |
| `status` | `status` |
| `path` | `pathContains` |
| `method` | `method` |
| `mintimetaken` | `minTimeTakenMs` |
| `limit` | `limit` |

Inside the block, `logEntries` is the list of entries and `logResult` has `MatchedCount`, `FilesScanned`, `IsTruncated`, `LogPath`, and `Warning`. Each entry has `DateTime`, `Method`, `UriStem`, `UriQuery`, `Status`, `SubStatus`, `TimeTakenMs`, `ClientIp`, `UserName`, and `UserAgent`.

## Security

- The assistant sees what its signed-in person can see, and can change what that person can change. Case security applies to every call, on both routes.
- IIS logs include IP addresses and usernames. Through Rock's MCP server, only Rock administrators can search them. Through the direct endpoint, anyone you allow on the REST action can, so give that to the same people you'd let read the logs on the server.
- If you only want one route, leave the other closed: don't add the skill to an agent, or don't grant anyone the REST action.
- The `iislogs` Lava command is off until an administrator enables it, the same as `execute` or `sql`.
