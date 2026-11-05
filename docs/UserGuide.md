# Rock Detective User Guide

Rock Detective walks a problem through Jon Edmiston's Triumphant Problem Solving method as a case file. You open a case, work the nine phases, gather witness statements, test hypotheses with experiments, and close with a postmortem that goes into an archive.

It works for more than software bugs. Performance problems, broken processes, and ministry patterns (attendance skewed to one service, volunteers not returning) all fit; pick the category that matches.

## Install

There are two ways to install Rock Detective: from the Rock Shop, or by hand on the server Rock runs on. The plugin needs Rock 18.2 or later. Rock doesn't have a page for uploading a `.plugin` file, so a manual install happens on the server, not in the browser.

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
   Plugins\com_yesuchum\RockDetective\   (three .obs.js files)
   ```
4. Start the application pool. The plugin migration runs on startup and creates the tables, pages, security role, system communications, and jobs.

Only the `content` folder gets copied. Anything else in the package is for the Rock Shop, and none of it needs to run by hand. A manual install doesn't show up in the Rock Shop's list of installed plugins, so updates go the same way: stop the app pool, copy the new version's `content` files over the old ones, and start it again.

### What you get

- Pages under Admin Tools > Installed Plugins > Rock Detective: the case list, the Case Board, and the Witness Statement page. The last two are hidden from navigation and open from the list and from emails.
- A **Rock Detective Users** security role with View and Edit on case files. Rock Administration gets View, Edit, and Administrate.
- Two scheduled jobs, **Rock Detective Witness Reminders** and **Rock Detective Follow-Up Checks**, both active by default.
- Three system communications you can reword under Admin Tools > Communications > System Communications (search "Rock Detective").
- Four case categories: Software Bug, Performance, Process, and Ministry Operations.

The migration does not set any page security, so the pages inherit it from Installed Plugins. If people outside Rock Administration will use Rock Detective, give them View on the Rock Detective page and its two child pages, and put them in the Rock Detective Users role.

## Opening a case

On the case list, click the plus button. One modal covers the first two phases:

1. **Define the Problem.** A title, one clear and concise problem statement with nothing unverified and nothing that is not germane, and a category. The two attestation checkboxes are there to make you actually check that.
2. **Classify.** Score impact, urgency, recurrence, and scope as Low, Medium, or High. A total of 8 or more recommends Advanced mode; anything lower recommends Simple. You can override the recommendation.

Simple mode collapses the flow for easy problems. The case starts at the Fact List phase, the Define the Team and Timeline phases are skipped, and the gate minimums drop. Advanced mode runs all nine phases and starts at Define the Team. A Simple case can be upgraded to Advanced from the board; it never goes back.

## The Case Board

### Phases

The stepper across the top shows the phases. Green is complete and the highlighted one is where you are. In Simple mode, Define the Team and Timeline stay out of the stepper unless you started them. Phases 5 through 8 (Fact List, Probability Tree, Experiments, Reevaluate) loop; keep cycling through them until a cause is confirmed beyond a reasonable doubt.

On a phone (screens under 768 pixels wide) the stepper becomes a dropdown. Completed phases carry a check mark, and phases that Restrict Phase Jumping has locked are greyed out.

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
| 8. Reevaluate | A confirmed hypothesis with a supporting experiment, or a loop back for another round. If the case has a Champion and a confirmed hypothesis, the Champion also signs off. |
| 9. Postmortem | Impact, solution, contributing factors, lessons learned, prevention measures, and action items. |

The numbers are the defaults, which match the method's ideal. The minimums are block settings (see Settings below).

Phase 8 has two extra buttons. **Loop Back to Facts** sends the case back to phase 5 and reopens the loop phases you had completed. **Champion Sign-Off** shows up only for the person assigned as Champion. Completing Reevaluate with a confirmed hypothesis marks the case Solved.

### Panels

- **Fact List.** Specific, correct, pertinent only. Mark corroboration honestly; witness statements are never fully trusted.
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
3. They submit or decline, and can attach a file or a link. Declining can include a reason.

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

A closed case is read-only. **Reopen Case** puts it back at the Fact List phase with the status Reopened.

### Follow-up check

You can set a follow-up date when you close. On that date the case owner gets an email asking whether the fix held, with two links:

- **Fix held** stops the checks.
- **Reopen** puts the case back at the Fact List phase.

An unanswered check goes out again a week later. The **Fix Held** button on a closed case does the same as the first link.

### Archive and abandoned cases

Closed cases live on the **Archive** tab of the case list, which shows the solution and the closed date. The other tabs show cases that are still open.

Any case that isn't closed can be **abandoned**. It stays on the other tabs, locked, with everything preserved and no postmortem. **Resume Case** unlocks it where it left off.

### Export PDF

**Export PDF** on the board produces the case file as a report: cover, problem statement, timeline, facts, evidence, probability tree, experiment log, witness statements, and postmortem. Sections with nothing in them are left out.

## Settings

### Case Board block

| Setting | Default | What it does |
|---|---|---|
| Witness Statement Page | Witness Statement | The page witnesses land on. Required. |
| Send Requests Automatically | Off | Emails a witness request as soon as it is created. |
| Restrict Phase Jumping | Off | The stepper only allows phases already started; new ones unlock by completing the one before. |
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
| Default View | My Requests | The tab shown first: My Requests, My Assignments, or All. |
| Show All View To Everyone | Off | Widens the All tab past entity security. Opening a case still checks its own security. |

### Witness Reminders job

**Reminder Days** (default 3) is how long a request can sit unanswered before the job sends the reminder email. The job runs daily at 7:00 AM. The Follow-Up Checks job runs daily at 7:15 AM.

## Security

Case files use standard Rock entity security. Rock Detective Users can view and edit, and Rock Administration has full control. Override a single case from the padlock column on the list.

- **Viewing.** The owner and every Rock person on the Team and Witnesses panel can always view their case.
- **Editing.** Edit security on the case lets you change it. The owner can also work their own case on the Case Board without it. Other participants can only view.
- **Deleting.** The list's delete button needs Edit on the case and removes everything under it.
- **Witness page.** Only the person the request was sent to, or someone with Edit on the case, can open a statement request.
- **List tabs.** My Requests shows cases you own. My Assignments adds the cases where you are a participant. All shows every open case you can view.
