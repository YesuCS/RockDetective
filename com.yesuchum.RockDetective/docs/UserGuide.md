# Rock Detective User Guide

Rock Detective walks a problem through Jon Edmiston's Triumphant Problem Solving method as a case file. You open a case, work the nine phases, gather witness statements, test hypotheses with experiments, and close with a postmortem that lands in a searchable archive.

It is not just for software bugs. Performance problems, broken processes, and ministry patterns (attendance skewed to one service, volunteers not returning) all fit; pick the category that matches.

## Install

Install from the Rock Shop, or upload the `.plugin` file through Admin Tools > Power Tools > Install Plugin. Restart Rock after install; the plugin migration creates its tables, pages, security role, system communications, and jobs on startup.

You get:

- Pages under Admin Tools > Installed Plugins > Rock Detective (case list, Case Board, Witness Statement).
- A **Rock Detective Users** security role seeded with View and Edit on case files. Put your staff in it, or widen access with per-case security from the list grid's padlock.
- Two scheduled jobs: witness reminders and follow-up checks.
- Three system communications you can reword under Communications > System Communications (search "Rock Detective").

## Opening a case

From the case list, hit the plus. Two things happen in one modal:

1. **Define the Problem.** One clear, concise statement; nothing unverified, nothing that is not germane. The two attestation checkboxes are there to make you actually do that. Pick a category.
2. **Classify.** Score impact, urgency, recurrence, and scope. The block recommends Simple or Advanced mode; you can override it. Simple collapses the flow for easy problems (team and timeline phases skip, minimums drop). Advanced runs all nine phases with full gates. A Simple case can upgrade to Advanced later; it never downgrades.

## The Case Board

The board is the workspace. The stepper across the top shows the nine phases; green is complete, the highlighted one is where you are. Phases 5 through 8 (Facts, Probability Tree, Experiments, Reevaluate) loop; keep cycling them until a cause is confirmed beyond a reasonable doubt.

Each phase shows its guidance, prompts to work through, a notes field, and a gate checklist. **Complete Phase** is enabled only when the gate is satisfied. Gates are configurable per block (see Settings below).

Panels:

- **Fact List.** Specific, correct, pertinent only. Track corroboration honestly; witness statements are never fully trusted.
- **Timeline.** When it started, last known good, what changed, what else was happening.
- **Evidence.** Protect the crime scene; capture logs, configs, and screenshots before anything changes. Attachments and links both work.
- **Probability Tree.** Brainstorm candidate causes first, assign probabilities second. Child nodes break a hypothesis down. Confirming a hypothesis requires a supporting experiment.
- **Experiments.** One factor at a time. Log every change so it can be reverted; the gate makes you revert or justify. Learnings with a recorded outcome append to the fact list automatically.
- **Team and Witnesses.** Roles beyond the owner are optional. The Champion is someone not emotionally tied to the problem who sanity-checks the final explanation; the Reevaluate gate requires their sign-off when one is assigned.

## Witness statements

A witness can be a Rock person or an external person (name plus contact text). Each request is its own statement, so one person can be asked several times.

- **Rock people**: create a request with a prompt, then send it. They get an email with a link to the Witness Statement page where they submit or decline, optionally attaching a file or link, and an in-Rock reminder in the bell menu that links straight to the same page. The reminder completes automatically when the request is answered, declined, recorded, or deleted. Unanswered requests get an email reminder from the job after the configured number of days, and show an overdue flag on the case list.
- **External people**: no communication is sent. Gather their statement however you reach them and record it manually, with optional attachment and link.

Any pending request also has a **Record their response** action on the board for when the witness answers you in person; it fills that request rather than creating a separate manual statement.

Any statement can be promoted to the fact list; edit it down to the specific, pertinent fact first and mark corroboration deliberately.

## Closing, follow-up, and the archive

Closing runs the postmortem: impact, solution, contributing factors, lessons learned, prevention measures, action items. All fields are required; that is the point of a postmortem.

Optionally set a follow-up date. On that date the owner gets an email asking whether the fix held, with one-click links: **fix held** stops the checks, **reopen** puts the case back at the Fact List phase. Unanswered checks re-send weekly.

Closed cases live in the **Archive** tab of the case list; search covers problem statements, titles, solutions, and lessons learned. Any case can also be **abandoned**, which locks it in place without a postmortem; resume it later and it picks up exactly where it left off.

**Export PDF** on the board produces the full case file as a clean report: cover, problem statement, timeline, facts, probability tree, experiments, witness statements, postmortem.

## Settings worth knowing

On the **Case Board** block:

- **Gate Requirements** category: minimum facts, root hypotheses, timeline events, and completed experiments per mode, plus whether the problem statement attestations are required. Each setting's description states the method's ideal; loosen them only if you know why.
- **Send Requests Automatically**: when on, creating a witness request emails immediately instead of starting as a draft.
- **Restrict Phase Jumping**: when on, the stepper only allows phases that have already been started; new phases unlock by completing the previous one. When off (the default), any phase can be selected at any time.

On the **Case File List** block:

- **Default View**: My Requests, My Assignments, or All.
- **Show All View To Everyone**: widens the All tab past entity security. Opening a case still enforces its own security.

On the **Rock Detective Witness Reminders** job: **Reminder Days** (default 3).

## Security

Standard Rock entity security, seeded so Rock Detective Users can view and edit and Rock Administration has full control. Override per case from the padlock column on the list. Participants always see their own cases. Deleting a case requires Edit on that case and removes everything under it.
