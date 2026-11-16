# Rock Detective MCP Guide

MCP (Model Context Protocol) is how an AI assistant like Claude talks to another system. Rock Detective plugs into it, so an assistant can do more than answer "what cases are open?". It can open a case, work it through every phase of the method, and close it with a postmortem, leaving a case file that documents what was found, what was decided, and what fixed it.

This guide covers the two ways to connect, the tools the assistant gets, how it works a case on its own, how to make the skill your own, and the IIS log search. You don't need to know how MCP works to use it.

## Two ways to connect

| | Rock's MCP server | Direct endpoint |
|---|---|---|
| What it is | Rock Detective shows up as a skill you add to one of Rock's own AI agents. | An MCP endpoint that belongs to Rock Detective. |
| Address | `/api/v2/mcp/{your agent's slug}` | `/api/com_yesuchum/rockdetective/mcp` |
| Best for | Most churches. Rock Detective sits next to Rock's other skills (people, groups, giving) in one connection. | A quick connection with nothing but a REST key, or a script that only needs Rock Detective. |
| Who can search IIS logs | Rock administrators. | Rock administrators. |
| Choosing which tools are on | Per agent, tool by tool, on top of what each person is allowed. | Each person is offered only the tools they are allowed. |

Both give the assistant the same tools and the same answers. Start with Rock's MCP server unless you have a reason not to.

Either way, you need Rock Detective 1.1.0 or later on Rock 19.7 or later.

## Option 1: Rock's MCP server

The plugin adds a **Rock Detective** skill to Rock's AI skills on its own, the first time Rock starts after the install. All you do is add it to an agent.

1. Go to **Admin Tools > System > AI Agents > Agents**.
2. Open the MCP agent you already use, or add a new agent, set its type to **MCP**, and give it a slug like `rock-detective`.
3. Add the **Rock Detective** skill to the agent and turn its tools on. You can leave all of them on: the skill checks who is asking on every call, so a witness, an investigator, and an administrator can share one agent. Leave the tools that change cases off only if nobody on this agent should change cases.
4. Save. The agent's address is your Rock address plus `/api/v2/mcp/` plus the slug:

```
https://rock.example.org/api/v2/mcp/rock-detective
```

Connect your assistant to that address the same way you connect to any other Rock MCP agent. Who can reach the agent is controlled by the agent's security in Rock, and new agents are open to Rock Administration only, so give View to the people who should use it.

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
- **The Champion stays a person.** The assistant can only sign off as Champion if the person it is signed in as is the Champion on that case, and only once a hypothesis is confirmed with a supporting experiment. When the Champion is someone outside Rock, the case owner records their sign-off. On a small case with no Champion, it can waive the Champion with a reason.
- **A loop back is not a shortcut.** Loop Back only works from the reevaluate phase, and the next round has to earn its own confirmed hypothesis. Ruled-out hypotheses stop counting, and an earlier sign-off is cleared.
- **Bad input is refused, not fixed up.** A probability outside 0 to 100, a title over 200 characters, a date it can't read, or a link that isn't a web address all come back with a reason.
- **A closed case is closed.** The assistant can't change a closed or abandoned case. Reopening and resuming happen on the Case Board.

The assistant works as the person it signed in with, and can do exactly what that person can (see Who can do what below). To let an assistant read without changing anything, connect through Rock's MCP server and turn the changing tools off on the agent.

For a script that only needs to push data in, the [REST API](RestApi.md) is still there.

## Who can do what

One agent serves everyone. The skill checks who is signed in on every call, so you don't need separate agents for separate kinds of people.

| Who | What they can do |
|---|---|
| Someone who was asked for a statement (a witness) | See the requests addressed to them, and submit or decline. Nothing else. |
| Someone who works cases: a person in the Rock Detective Users role, a case's owner, or a team member on it | Everything a witness can, plus list and read the cases they have access to, open cases, and work them. |
| A Rock administrator | All of that, plus Search IIS Logs. |

A few details behind that table:

- **A witness can't read the case they are a witness on.** They get the question they were asked, the case title, and who asked. The same goes for the Case Board, the PDF, and the REST API: a summary of the whole case would taint the statement you want from them.
- **Opening a case takes the Rock Detective Users role** (or Rock Administration).
- **Changing a case takes Edit on it, or being its owner**, the same as on the Case Board.
- **A statement request has to be sent** from the Case Board before the witness sees it.
- **An external witness** (someone who is not in Rock) can't use this, since there is no Rock person to sign in as.

On the direct endpoint, the list of tools itself is trimmed to what the person may use. On Rock's MCP server the agent lists every tool you turned on, and a tool the person isn't allowed to use answers with a short explanation instead of doing anything.

## Making the skill your own

The Rock Detective skill is a regular Rock AI skill, and it's yours to change. Go to **Admin Tools > System > AI Agents > AI Skills** and open **Rock Detective**. From there you can:

- Edit what the assistant is told about the skill as a whole: its description, purposes, usages, and guardrails.
- Edit any tool: its name, description, instructions, parameters, and Lava.
- Add tools of your own.
- Delete tools you don't want.

This applies to Rock's MCP server. The direct endpoint doesn't go through the skill, so it always offers the standard tools.

### How a tool works

Each tool is a short piece of Lava. When the assistant calls a tool, Rock fills in the parameters the assistant sent, runs the Lava, and hands the output back to the assistant. This is Create Case as it ships:

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

The `rockdetective` command does the work. You give it the name of one of Rock Detective's actions and a JSON object of arguments. It runs the action as the signed-in person and outputs the result as JSON. When the action can't do what was asked, the output is `{"refused": true, "reason": "..."}`, and the reason says what is still needed.

The action names are the same ones the direct endpoint uses (`list_cases`, `create_case`, `add_entry`, and so on). [Skill Defaults](SkillDefaults.md) lists all 22 with their arguments.

### What you can do with that

- **Use your church's language.** Rename a tool, or rewrite its description and parameter instructions so the assistant talks the way your team does.
- **Decide something for the assistant.** Replace `{{ mode | ToJSON }}` with `"Advanced"` and remove the `mode` parameter, and every case the assistant opens runs all nine phases.
- **Add your own Lava.** Look something up in Rock before the command and pass it in, or add to the output after it.
- **Write a new tool.** Call an action in a way that fits how you work, or write a tool that doesn't use `rockdetective` at all.

### What editing can't change

The rules live in the `rockdetective` command, not in the tool's wording. However a tool is reworded or rewritten, it still can't complete a phase with an unmet checklist, change a closed case, let a witness read a case, or let someone who isn't a Rock administrator search the IIS logs. So you can open the skill up to the people who run your AI agents without opening up your cases.

### Things to know

- **Your edits last.** The plugin adds each standard tool once. After that it never overwrites a tool, through Rock restarts and plugin updates, and a standard tool you delete stays deleted.
- **A new tool has to be turned on for the agent.** A Rock agent keeps its own list of which tools are on. Open the agent, find the Rock Detective skill, and turn the new tool on. That goes for tools you add and for new standard tools that arrive with a later version. Tools that were already on stay on when you edit them.
- **Renaming changes what the assistant sees.** The assistant knows a tool as the skill name plus the tool name, like `RockDetective__CreateCase`. Rename either and the name changes, so an assistant that is already connected needs to reconnect.
- **`rockdetective` only works inside a skill tool.** It is there for the Rock Detective skill and nothing else. Used anywhere else in Rock, such as a page, an email, or a workflow, it does nothing and answers that it only works inside a Rock Detective AI skill tool. It does not show up in Rock's lists of Lava commands to enable, because there is nothing for you to enable.
- **Lava in a skill tool can use every Lava command.** Rock allows that because only people who can edit AI skills can write one.

### Putting things back

Click **Skill Settings** at the top right of the case list (it shows for people with Administrate on that block). The page lists every standard tool and marks it **Standard**, **Edited**, or **Missing**.

- **Restore** puts one edited tool back to its standard name, description, parameters, and Lava.
- **Add Back** brings back a standard tool you deleted.
- **Restore Everything to Standard** does both for the whole skill, including what the assistant is told about the skill itself.

None of these touch tools you added yourself. The standard version of everything is in [Skill Defaults](SkillDefaults.md), so you can also copy a single piece back by hand.

## Searching IIS logs

IIS writes a line for every request that reaches your web server. That makes the logs some of the best evidence you have when a page is failing or slow, and Search IIS Logs lets you ask for exactly the slice you need.

| Option | What it does |
|---|---|
| `minutes` | How far back to look. Default 60, up to 10080 (7 days). Ignored when `start` is given. |
| `start` | The start of a time range, in the server's local time, like `2026-11-08T08:00:00`. Up to 30 days back. |
| `end` | The end of the range. Defaults to now. A range can span up to 7 days. |
| `status` | Only requests with this HTTP status, like `500`. |
| `pathContains` | Only requests whose URL path contains this text. Case doesn't matter. |
| `method` | Only this HTTP method, like `POST`. |
| `minTimeTakenMs` | Only requests that took at least this many milliseconds. |
| `limit` | The most entries to return, newest first. Default 50, up to 500. |
| `includeUserAgent` | Also return each request's browser. Off by default to keep results small. |

Options combine, so "POSTs to the connect card that took over 5 seconds between 8 and noon last Sunday" is one call. Each entry comes back with the time, method, path, query string, status, time taken, client IP, and username. The result also says how many requests matched in all, so the assistant knows when to narrow the search.

Query strings can carry secrets, like the `rckipid` token in a personalized email link. Rock Detective replaces the value of any parameter whose name looks sensitive (`rckipid`, and names containing token, key, code, password, secret, signature, auth, or session) with `[redacted]` before the result leaves the server. The parameter names stay, so you can still see what kind of link was followed.

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

It has to be enabled first, like any secured Lava command: tick **IIS Logs** in the Enabled Lava Commands on the block or template that uses it.

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
| `start` | `start` |
| `end` | `end` |

Inside the block, `logEntries` is the list of entries and `logResult` has `MatchedCount`, `FilesScanned`, `IsTruncated`, `LogPath`, and `Warning`. Each entry has `DateTime`, `Method`, `UriStem`, `UriQuery`, `Status`, `SubStatus`, `TimeTakenMs`, `ClientIp`, `UserName`, and `UserAgent`.

## Security

- The assistant sees what its signed-in person can see, and can change what that person can change. Case security applies to every call, on both routes.
- IIS logs include IP addresses and usernames, so only Rock administrators can search them, on both routes. Sensitive query string values are redacted before they reach the assistant.
- The direct endpoint only accepts `application/json`, which keeps a web page in someone's browser from posting to it behind their back.
- If you only want one route, leave the other closed: don't add the skill to an agent, or don't grant anyone the REST action.
- The `iislogs` Lava command is off until an administrator enables it, the same as `execute` or `sql`. It shows up as **IIS Logs** wherever Rock lists Lava commands to enable. Rock builds that list from every secured command it finds, so seeing it there is expected, and the plugin never ticks it for you. The one exception is Lava inside an AI skill tool, where Rock turns every command on.
- The `rockdetective` Lava command is not in that list and can't be enabled anywhere. It only runs inside a Rock Detective skill tool, as the signed-in person, under the same gates and permissions as everything else.
- Editing the skill takes access to Rock's AI Skills pages, which is Rock Administration unless you open it up. Whoever edits a tool is writing Lava that runs on your server, so treat that access the way you treat access to the `sql` command.
