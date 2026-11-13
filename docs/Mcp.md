# Rock Detective MCP Guide

MCP (Model Context Protocol) is how an AI assistant like Claude talks to another system. Rock Detective has its own MCP endpoint, so you can ask an assistant things like "what cases are open?" or "show me the 500 errors from the last hour" and it gets the answer straight from your Rock server.

This guide covers turning it on, connecting an assistant, the tools it gets, and the IIS log search. You don't need to know how MCP works to use it.

## What you need

- Rock Detective 1.1.0 or later.
- A Rock REST key for the person the assistant will act as.
- An MCP client that can connect to a server by URL and send a custom header. Claude Code and Claude Desktop both can.

Rock Detective does not use Rock's built-in AI Agent MCP server. It ships its own endpoint, so it works the same on Rock 18.2, 19, and 20 and needs no AI provider set up in Rock.

## Step 1: Create a REST key

The assistant signs in to Rock with a REST key, and it can only see what that key's person can see.

1. Go to **Admin Tools > Security > REST Keys** and add a key. Give it a name that says what it's for, like `Rock Detective MCP`.
2. Copy the key. You'll paste it into your MCP client in step 3.
3. Put the key's person in the **Rock Detective Users** security role so it can view and edit cases. Leave it out of that role if the assistant should only see cases it has been added to.

## Step 2: Allow the key to call the endpoint

Rock secures every REST endpoint separately.

1. Go to **Admin Tools > Security > REST Controllers** and open **RockDetectiveMcp**.
2. On the **POST** action, click the padlock and give your REST key's person (or a role they are in) **View** and **Edit**.

Rock Administration can already call it, so you can skip this step if the key belongs to an administrator.

## Step 3: Connect your assistant

The endpoint is your Rock address plus this path:

```
https://rock.example.org/api/com_yesuchum/rockdetective/mcp
```

The key goes in a header named `Authorization-Token`.

In Claude Code:

```bash
claude mcp add --transport http rock-detective https://rock.example.org/api/com_yesuchum/rockdetective/mcp --header "Authorization-Token: YOUR_REST_KEY"
```

In any other client, add a remote (HTTP) MCP server with that URL and that header. Once it connects, the assistant lists three tools.

## The tools

| Tool | What it does | What you can pass |
|---|---|---|
| `list_cases` | Lists the cases the key's person can view, newest first. | `status` (Open, Investigating, Solved, Closed, Reopened, Abandoned) and `limit` (default 25, up to 100). |
| `get_case` | Returns one whole case: phases, gate checklists, facts, timeline, evidence, hypotheses, experiments, participants, and statements. | `idKey`, which comes from `list_cases`. |
| `search_iis_logs` | Searches the web server's IIS logs for recent requests. | See the next section. |

Phase names, terms, and gate wording come back the way that case's category has them set up under Terminology Settings, so the assistant uses the same words your team sees on the Case Board.

The tools are read-only. To open a case or add to one from a script, use the [REST API](RestApi.md).

## Searching IIS logs

IIS writes a line for every request that reaches your web server. That makes the logs some of the best evidence you have when a page is failing or slow, and `search_iis_logs` lets you ask for exactly the slice you need.

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

- The assistant sees what its REST key's person can see. Case security applies to every call.
- IIS logs include IP addresses and usernames. Anyone who can call the endpoint can search them, so give the endpoint to the same people you'd let read the logs on the server.
- The `iislogs` Lava command is off until an administrator enables it, the same as `execute` or `sql`.
