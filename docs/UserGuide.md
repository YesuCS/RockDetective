# Rock Detective User Guide

Rock Detective walks a problem through Jon Edmiston's Triumphant Problem Solving method as a case file. You open a case, work the nine phases, gather witness statements, test hypotheses with experiments, and close with a postmortem that goes into an archive.

It works for more than software bugs. Performance problems, broken processes, and ministry patterns (attendance skewed to one service, volunteers not returning) all fit; pick the category that matches.

## Install

There are two ways to install Rock Detective: from the Rock Shop, or by hand on the server Rock runs on. The plugin needs Rock 19.7 or later. Rock doesn't have a page for uploading a `.plugin` file, so a manual install happens on the server, not in the browser.

### From the Rock Shop

1. Go to **Admin Tools > Rock Shop** and search for `Rock Detective`.
2. Open the package and click **Install**.
3. Rock restarts after the install, and the plugin migration runs on that restart.

### Manual install on the server

Use this when the Rock Shop isn't an option, like a staging box or an instance without internet access. You need the `.plugin` file and access to the machine hosting Rock (the VM or server running IIS).

1. A `.plugin` file is a zip. Copy it to the server, change the extension to `.zip`, and extract it.
2. In IIS Manager, stop the Rock application pool.
3. Copy everything inside the package's `content` folder into the Rock web root, the folder the Rock site points to in IIS (the one with `web.config` and `Bin` in it), and merge it with the folders already there. When you're done, these should exist:
   ```
   Bin\com.yesuchum.RockDetective.dll
   Plugins\com_yesuchum\RockDetective\   (five .obs.js files)
   ```
4. Start the application pool. The plugin migration runs on startup and creates the tables, pages, security role, system communications, and jobs.

Only the `content` folder gets copied. Anything else in the package is for the Rock Shop, and none of it needs to run by hand. A manual install doesn't show up in the Rock Shop's list of installed plugins, so updates go the same way: stop the app pool, copy the new version's `content` files over the old ones, and start it again.

### What you get

- Pages under Admin Tools > Installed Plugins > Rock Detective: the case list, the Case Board, the Witness Statement page, Terminology Settings, and Skill Settings. The Case Board and Witness Statement page are hidden from navigation and open from the list and from emails.
- A **Rock Detective Users** security role with View and Edit on case files. Rock Administration gets View, Edit, and Administrate.
- Two scheduled jobs, **Rock Detective Witness Reminders** and **Rock Detective Follow-Up Checks**, both active by default.
- Three system communications you can reword under Admin Tools > Communications > System Communications (search "Rock Detective").
- Four case categories: Software Bug, Performance, Process, and Ministry Operations.
- A **Rock Detective** skill for Rock's AI agents, with tools you can edit, plus a direct MCP endpoint, so an AI assistant can open and work cases through every phase, document what it finds, and search the web server's IIS logs. See the [MCP Guide](Mcp.md).

People in the **Rock Detective Users** role can open the case list and the Case Board, and can view and edit cases. The pages sit under Admin Tools > Installed Plugins, which the rest of your staff can't browse to, so give your investigators a link or add the Rock Detective page to a menu they use. A witness who is not in the role needs View on the Witness Statement page to answer there; they can also answer through an AI assistant. Terminology Settings and Skill Settings are for administrators. Only people with Administrate on its block can see or change the wording, and everyone else gets a notice instead.

## The case list

The list shows every case you have access to. The filter icon on the grid opens **Case Filters**, where you narrow it down:

- **Show** picks which cases are listed: **Active** (the default) for cases still being worked, **Archived** for closed cases, or **All** for both.
- **My Involvement** narrows the list to your own work: **My Requests** for cases you own, or **My Assignments** for cases you are on. Leave it blank to see everything.
- **Category**, **Status**, and **Mode** narrow it further.

Your filters are remembered, and the filter icon is highlighted whenever one is on.

The three dots at the top right of the list have two built-in helps for people who are new to it:

- **How a Case Works** is a rundown of the process. It shows the nine phases in their three groups with a one-line summary of each. Click a phase to open it up and see the questions to work through and what its gate asks for, in Advanced and in Simple mode. The standing principles are at the bottom.
- **Quick Guide** answers everyday questions step by step, like how to find a closed case, export a PDF, reopen a case, or ask someone for a statement.

The list leads with **Date | Name | Status** and sorts with the newest problem first. The date is when the problem started, taken from the Problem Start event on the case's timeline. Until a case has one, it shows the day the case was opened. Once you have a lot of cases, the date is the fastest way to find the one you're after.

## Opening a case

On the case list, click the plus button. One modal covers the first two phases:

1. **Define the Problem.** A title, one clear and concise problem statement with nothing unverified and nothing that is not germane, and a category. The two attestation checkboxes are there to make you actually check that.
2. **Classify.** Score impact, urgency, recurrence, and scope as Low, Medium, or High. A total of 8 or more recommends Advanced mode; anything lower recommends Simple. You can override the recommendation.

Simple mode collapses the flow for easy problems. The case starts at the Fact List phase, the Define the Team and Timeline phases are skipped, and the gate minimums drop. Advanced mode runs all nine phases and starts at Define the Team. A Simple case can be upgraded to Advanced from the board; it never goes back.

## The Case Board

### The header

The top of the board reads **Name | Status**, with the category and mode at the right. The date lives on the case list and in **Audit Details**, so the header stays easy to read.

### Phases

Export PDF, Upgrade to Advanced, Abandon, Reopen, and Audit Details are in the menu at the top right of the board (the three dots), so the board itself stays on the work.

The stepper across the top shows the phases in three groups: **Set up** (phases 1 to 4), **Investigate** (5 to 8), and **Close** (9). Each phase is a small numbered button. Green with a check is complete, and the highlighted one, the only one that shows its name, is where you are. Hover over any number to see that phase's name and what it's for, and click it to go there. In Simple mode, Define the Team and Timeline stay out of the stepper unless you started them. The Investigate phases (Fact List, Probability Tree, Experiments, Reevaluate) loop; keep cycling through them until a cause is confirmed beyond a reasonable doubt.

On a phone (screens under 768 pixels wide) the stepper becomes a dropdown. Completed phases carry a check mark, and locked phases are greyed out.

A case moves forward one way: by completing the phase it is on, which checks that phase's gate. You can always click back to a phase the case has already been through, to review it or add to it, and then return to where you were. Phases the case hasn't earned yet show a lock. That holds for everything on the board, including closing the case, which only works from the last phase with the phases before it complete. The **Restrict Phase Jumping** block setting controls this, and it is on by default.

Each phase shows its guidance, prompts to work through, a notes field, and a gate checklist. **Complete Phase** is enabled only when every line of the checklist is met. Completing a phase moves the case to the next one, and the case goes from Open to Investigating when it reaches the Fact List.

| Phase | Gate |
|---|---|
| 1. Define the Team | An owner is assigned, and a Champion is assigned. |
| 2. Define the Problem | A problem statement and a category. Advanced cases also need both attestations. |
| 3. Classify | The problem is classified as easy or difficult. |
| 4. Timeline | At least 2 timeline events, and one of them is a Problem Start. |
| 5. Fact List | At least 3 facts (Advanced) or 1 fact (Simple). |
| 6. Probability Tree | At least 2 root hypotheses (Advanced) or 1 (Simple), each with a probability above 0. |
| 7. Experiments | At least 1 experiment with a recorded outcome, where changes were reverted, justified, or never made. |
| 8. Reevaluate | A confirmed hypothesis with a supporting experiment. If nothing is confirmed, loop back for another round. If the case has a Champion, the Champion also signs off on the confirmed explanation. |
| 9. Postmortem | Impact, solution, contributing factors, lessons learned, prevention measures, and action items. |

The numbers are the defaults, which match the method's ideal. The minimums are block settings (see Settings below).

Phase 8 has two extra buttons. **Loop Back to Facts** sends the case back to phase 5 and reopens the loop phases you had completed. A loop back does not carry anything forward: the next time the case reaches phase 8 it still needs a confirmed hypothesis with a supporting experiment, a hypothesis you ruled out no longer counts toward the Probability Tree, and an earlier sign-off is cleared. **Champion Sign-Off** shows up for the person assigned as Champion once a hypothesis is confirmed. When the Champion is someone outside Rock, the case owner sees **Record Champion Sign-Off** and records it for them. Completing Reevaluate with a confirmed hypothesis marks the case Solved.

### Panels

- **Fact List.** Specific, correct, pertinent only. Mark corroboration honestly; witness statements are never fully trusted. Anything an AI assistant added says so under the entry.
- **Timeline.** When it started, last known good, what changed, what else was happening. Events sort by date, oldest first.
- **Evidence.** Protect the crime scene; capture logs, configs, and screenshots before anything changes. Each item can carry an attachment, a link, or both.
- **Probability Tree.** Brainstorm candidate causes first and assign probabilities second. Child nodes break a hypothesis down. A hypothesis is Proposed, Testing, Ruled Out (with a reason), or Confirmed. A confirmed hypothesis only satisfies the Reevaluate gate if an experiment testing it came out Supported. You can't delete a hypothesis that has children or linked experiments.
- **Experiments.** One factor at a time. Log every change so it can be reverted; the gate wants the changes reverted or a reason they stayed. When an experiment has an outcome and learnings, the learnings are added to the fact list once.
- **Team and Witnesses.** Roles are Champion, Recorder, Communicator, Member, and Witness, and a case has one Champion. The owner is whoever opened the case. The Champion is someone not emotionally tied to the problem who sanity-checks the final explanation.

The Standing Principles panel holds the method's reminders and where to look for evidence. **Audit Details** in the panel header shows who created and last changed the case.

## Witness statements

A witness can be a Rock person or an external person (a name plus contact text). Each request is its own statement, so one person can be asked several times. Creating a request makes the person a Witness on the case.

### Rock people

1. Create a request on their row in Team and Witnesses, with a prompt that says what you want to know. The request starts as a draft.
2. Send it. They get an email with a link to the Witness Statement page, and an in-Rock reminder that links to the same page.
3. They submit or decline, and can attach a file or a link. Declining can include a reason. An answer is final; once a request is submitted or declined it can't be changed from the page.

A witness sees the question, the case title, and who asked. They can't open the Case Board, the PDF, or the case through the API unless they are also on the team or in the Rock Detective Users role. A reminder for the same request can be sent once an hour at most.

Links on facts, evidence, timeline events, and statements have to be full web addresses that start with `http://` or `https://`. Anything else is refused.

A witness can also answer through an AI assistant instead of the page. They see only the requests addressed to them and can submit or decline, nothing else, even on the same agent your investigators use. See the [MCP Guide](Mcp.md).

The reminder completes on its own when the request is answered, declined, recorded, or deleted. If a request sits unanswered for the configured number of days, the reminder job emails them once. You can send more reminders by hand, and a request shows an overdue flag on the case list after 7 days.

The Case Board has a setting to send requests as soon as they are created instead of leaving them as drafts.

### External people

Nothing is sent. Gather their statement however you reach them and record it by hand, with an optional attachment and link.

### Recording a response

Any pending request also has a **Record their response** action, for when the witness answers you in person. It fills that request instead of creating a separate manual statement.

### Statements and facts

Any statement can be promoted to the fact list. Edit it down to the specific, pertinent fact first and mark corroboration deliberately.

## Closing, follow-up, and the archive

### Postmortem

**Close Case** (or **Run Postmortem and Close** on phase 9) opens the postmortem form: impact, solution, contributing factors, lessons learned, prevention measures, and action items. All six are required. That's the point of a postmortem.

A closed case is read-only. **Reopen Case**, in the menu at the top right, puts it back at the Fact List phase with the status Reopened. The phases from Fact List on have to be completed again, and the Champion signs off again.

### Follow-up check

You can set a follow-up date when you close. On that date the case owner gets an email asking whether the fix held, with two links:

- **Fix held** stops the checks.
- **Reopen** puts the case back at the Fact List phase.

An unanswered check goes out again a week later. The **Fix Held** button on a closed case does the same as the first link.

### Archive and abandoned cases

Closed cases are archived. To see them, open **Case Filters** on the case list and set **Show** to **Archived**, which lists the solution and the closed date. **All** shows open and closed cases together.

Any case that isn't closed can be **abandoned**. It stays with the active cases, locked, with everything preserved and no postmortem. **Resume Case** unlocks it where it left off.

### Export PDF

**Export PDF**, in the menu at the top right of the board, produces the case file as a report. The cover shows the case's key (like `OX9mQWPQo8`), the same identifier the page address, the emails, and the MCP tools use. Rock Detective used to show the database Id there, which made a church's first ministry review look like the third of something. The report has the cover, problem statement, timeline, facts, evidence, probability tree, experiment log, witness statements, and postmortem. Sections with nothing in them are left out.

## Making it sound like your ministry

Out of the box, Rock Detective talks like a detective: case, witness, hypothesis, experiment. That fits a software bug. It can feel off when the "case" is a guest follow-up process and the "witnesses" are your volunteers. Terminology Settings lets you rename all of it.

Click **Terminology Settings** at the top right of the case list. The button only shows for people with Administrate on the Terminology Settings block, which Rock Administration has by default. To let someone else manage the wording, give them Administrate on that block from its security settings.

### Pick what it applies to

The **Applies To** dropdown at the top decides who gets the wording you're about to enter:

- **All categories** sets the wording for every case.
- **A single category**, like Ministry Operations, sets the wording for cases in that category only.

When both are set, the category's wording wins. That means you can leave software cases as they are and give your ministry categories their own language. A case picks up its wording from its category, so changing a case's category on the board changes its wording too.

To add a category of your own, go to **Admin Tools > System > Category Manager**, choose the **Case File** entity type, and add it there. It shows up in the Applies To dropdown right away. The naming itself is always set on Terminology Settings; the Category Manager doesn't show it.

### What you can rename

| Section | What it changes |
|---|---|
| Terms | The words for people and things: Case, Owner, Champion, Recorder, Communicator, Member, Witness, Fact, Timeline Event, Evidence, Hypothesis, Experiment, and Statement. Enter one of each, like Volunteer, and Rock works out the plural (Volunteers) on its own. |
| Phases | The name of each of the nine phases, the guidance shown under it, and its prompts (one per line). |
| Gate Wording | The checklist sentences on each phase. |

Leave a box blank to keep the standard wording, which shows in gray inside the box. **Reset to Standard Wording** clears everything for the selection in Applies To.

Renaming changes words only. The nine phases stay in the same order, and each gate still checks the same thing. The numbers behind the gates are block settings, covered under Settings below.

### Letting your terms carry through

If you rename Champion to Pastoral Advisor, the standard guidance and gate sentences pick that up on their own. Your own guidance, prompts, and gate sentences can do the same if you write the term in curly braces instead of typing the word:

| You type | You get (with Champion renamed to Pastoral Advisor, Fact to Observation) |
|---|---|
| `{Champion}` | Pastoral Advisor |
| `{champion}` | pastoral advisor |
| `{fact+}` | observations (always more than one) |
| `{fact#}` | observation or observations, matching the gate's number |
| `{count}` | the gate's required number |

So a Fact List gate written as `Record at least {count} {fact#} from the ministry floor.` reads "Record at least 3 observations from the ministry floor." on an Advanced case and "Record at least 1 observation from the ministry floor." on a Simple one. The names inside the braces are the standard terms with no spaces: `caseFile`, `owner`, `champion`, `recorder`, `communicator`, `member`, `witness`, `fact`, `timelineEvent`, `evidence`, `hypothesis`, `experiment`, and `statement`.

### An example

A Ministry Process category set up for reviewing how a ministry process is working might look like this:

| Standard | Renamed |
|---|---|
| Case | Process Review |
| Owner | Ministry Lead |
| Champion | Pastoral Advisor |
| Witness | Volunteer |
| Fact | Observation |
| Hypothesis | Possible Cause |
| Experiment | Trial Run |
| Fact List (phase 5) | What We Know |
| Probability Tree (phase 6) | Possible Causes |
| Postmortem (phase 9) | Lessons and Next Steps |

### Where the wording shows up

The Case Board, the phase column on the case list, the PDF export, and the MCP tools all use it. The three system communications do not. They are ordinary Rock communications, so reword those under Admin Tools > Communications > System Communications.

Saved wording applies the next time a case loads. Nothing about the case's data changes, so you can rename things on cases that are already in progress.

## Settings

### Case Board block

| Setting | Default | What it does |
|---|---|---|
| Witness Statement Page | Witness Statement | The page witnesses land on. Required. |
| Send Requests Automatically | Off | Emails a witness request as soon as it is created. |
| Restrict Phase Jumping | On | A case only moves forward by completing its current phase, which checks the gate. Going back to earlier phases is always allowed. Turn it off and any phase can be selected at any time, which makes the gates advice instead of rules. AI assistants follow the gates either way. |
| Minimum Facts (Advanced) | 3 | Facts needed to complete the Fact List phase. |
| Minimum Facts (Simple) | 1 | Same, for Simple cases. |
| Minimum Root Hypotheses (Advanced) | 2 | Root hypotheses with probabilities needed to complete the Probability Tree phase. |
| Minimum Root Hypotheses (Simple) | 1 | Same, for Simple cases. |
| Minimum Timeline Events | 2 | Timeline events needed to complete the Timeline phase, one of which must be a Problem Start. |
| Minimum Completed Experiments | 1 | Experiments with an outcome and reverted or justified changes. |
| Require Problem Statement Attestations | On | Advanced cases must check both attestations to complete Define the Problem. |

The five minimums and the attestation setting sit under the **Gate Requirements** category, and each description states the method's ideal. Loosen them only if you know why.

### Case File List block

| Setting | Default | What it does |
|---|---|---|
| Detail Page | Case Board | The page a case opens on. Required. |
| Default View | Every Case | What a person sees before they set their own My Involvement filter: Every Case, My Requests, or My Assignments. |
| Show All View To Everyone | Off | With no My Involvement filter, widens the list past entity security. Opening a case still checks its own security. |

### Global attributes

These are under Admin Tools > General > Global Attributes.

| Setting | Default | What it does |
|---|---|---|
| Rock Detective IIS Log Folder | Blank | The folder the IIS log search reads from, including its subfolders. Blank means `C:\inetpub\logs\LogFiles`. See the [MCP Guide](Mcp.md). |
| Rock Detective Default Terminology | Blank | Where the All categories wording is stored. Change it on Terminology Settings, not here. |

### Witness Reminders job

**Reminder Days** (default 3) is how long a request can sit unanswered before the job sends the reminder email. The job runs daily at 7:00 AM. The Follow-Up Checks job runs daily at 7:15 AM.

## Security

Case files use standard Rock entity security. Rock Detective Users can view and edit, and Rock Administration has full control. Override a single case from the padlock column on the list.

- **Viewing.** The owner and every Rock person on the Team and Witnesses panel can always view their case.
- **Editing.** Edit security on the case lets you change it. The owner can also work their own case on the Case Board without it. Other participants can only view.
- **Deleting.** The list's delete button needs Edit on the case and removes everything under it.
- **Witness page.** Only the person the request was sent to, or someone with Edit on the case, can open a statement request.
- **My Involvement filter.** My Requests shows cases you own. My Assignments shows the cases where you are a participant. Left blank, the list shows every case you can view.
