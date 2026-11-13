# Rock Detective MCP Guide

MCP (Model Context Protocol) is how an AI assistant like Claude talks to another system. Rock Detective plugs into it, so you can ask an assistant things like "what cases are open?" or "show me the 500 errors from the last hour" and it gets the answer straight from your Rock server.

This guide covers the two ways to connect, the tools the assistant gets, and the IIS log search. You don't need to know how MCP works to use it.

## Two ways to connect

| | Rock's MCP server | Direct endpoint |
|---|---|---|
| What it is | Rock Detective shows up as a skill you add to one of Rock's own AI agents. | An MCP endpoint that belongs to Rock Detective. |
| Address | `/api/v2/mcp/{your agent's slug}` | `/api/com_yesuchum/rockdetective/mcp` |
| Best for | Most churches. Rock Detective sits next to Rock's other skills (people, groups, giving) in one connection. | A quick connection with nothing but a REST key, or a script that only needs Rock Detective. |
| Who can search IIS logs | Rock administrators. | Whoever you allow on the REST action. |

Both give the assistant the same three tools and the same answers. Start with Rock's MCP server unless you have a reason not to.

Either way, you need Rock Detective 1.1.0 or later on Rock 19.7 or later.

## Option 1: Rock's MCP server

Rock registers a **Rock Detective** skill on its own when the plugin is installed and Rock restarts. All you do is add it to an agent.

1. Go to **Admin Tools > System > AI Agents > Agents**.
2. Open the MCP agent you already use, or add a new agent, set its type to **MCP**, and give it a slug like `rock-detective`.
3. Add the **Rock Detective** skill to the agent and choose which of its three tools to turn on.
4. Save. The agent's address is your Rock address plus `/api/v2/mcp/` plus the slug:

```
https://rock.example.org/api/v2/mcp/rock-detective
```

Connect your assistant to that address the same way you connect to any other Rock MCP agent. Who can use it is controlled by the agent's security in Rock.

The tools show up to the assistant as **List Cases**, **Get Case**, and **Search IIS Logs**.

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

In any other client, add a remote (HTTP) MCP server with that URL and that header. The tools show up as `list_cases`, `get_case`, and `search_iis_logs`.

## The tools

| Tool | What it does | What you can pass |
|---|---|---|
| List Cases | Lists the cases the person can view, newest first. | `status` (Open, Investigating, Solved, Closed, Reopened, Abandoned) and `limit` (default 25, up to 100). |
| Get Case | Returns one whole case: phases, gate checklists, facts, timeline, evidence, hypotheses, experiments, participants, and statements. | The case's IdKey, which comes from List Cases. |
| Search IIS Logs | Searches the web server's IIS logs for recent requests. | See the next section. |

Phase names, terms, and gate wording come back the way that case's category has them set up under Terminology Settings, so the assistant uses the same words your team sees on the Case Board.

The tools are read-only. To open a case or add to one from a script, use the [REST API](RestApi.md).

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

- The assistant sees what its signed-in person can see. Case security applies to every call, on both routes.
- IIS logs include IP addresses and usernames. Through Rock's MCP server, only Rock administrators can search them. Through the direct endpoint, anyone you allow on the REST action can, so give that to the same people you'd let read the logs on the server.
- If you only want one route, leave the other closed: don't add the skill to an agent, or don't grant anyone the REST action.
- The `iislogs` Lava command is off until an administrator enables it, the same as `execute` or `sql`.
