# Next Backlog Item

Select and begin the next backlog story, after validating any in-progress work.

## Instructions

Read `docs/backlog.md` and perform the following steps **in order**:

---

### Step 1: Parse Story Statuses

Each story in the backlog uses a **checkbox status line** immediately after the story header:

```
#### Story X.Y.Z: Title — `N SP`
- [x] **COMPLETED**          ← Done
- [~] **IN PROGRESS**        ← Currently being worked on
- [ ] (no status line)       ← Not started (default)
```

Parse every `#### Story ...` entry and determine its status:
- `- [x]` = Completed
- `- [~]` = In Progress
- No checkbox line = Pending (not started)

Print a **summary table** showing all stories grouped by status (completed count, in-progress count, pending count).

---

### Step 2: Validate In-Progress Stories

**WIP Limit: 1 story at a time.** There should be at most one story marked `- [~] **IN PROGRESS**`.

If an in-progress story exists:

1. **Read the acceptance criteria** for that story.
2. **Scan the codebase** for evidence of completion:
   - Check for expected files, folders, and code patterns described in the acceptance criteria.
   - Run relevant checks where possible (e.g., `dotnet build` succeeds, expected project files exist, test files exist, configuration files present).
   - Do NOT run the full test suite automatically — just check for structural evidence.
3. **Present findings** to the user:
   - List each acceptance criterion with a pass/fail/unknown status based on what was found.
   - Clearly state what evidence was found and what is missing.
4. **Ask the user** to confirm whether the story should be marked as completed.
5. If confirmed:
   - Update `docs/backlog.md`: change `- [~] **IN PROGRESS**` to `- [x] **COMPLETED**` for that story.
   - Print confirmation message.
6. If NOT confirmed:
   - Ask the user if they want to **continue working** on this story or **pause it** (revert to pending).
   - If continuing: stop here — the current story IS the next item. Print remaining acceptance criteria to work on.
   - If pausing: change `- [~] **IN PROGRESS**` back to no status line (remove the checkbox line entirely) and proceed to Step 3.

---

### Step 3: Select the Next Story

Follow these rules **strictly** to pick the next story:

#### Dependency Rules (Enforced)

The dependency graph is:
```
Epic 1 (Infrastructure) → ALL other epics
Epic 2 (Auth) → Epics 3, 4, 5, 6, 7
Epic 3 (Shell) → Epics 5, 6, 7 (sidebar navigation)
Epic 4 (Tasks) → Epics 5, 6, 7 (task components)
```

- **Never** start a story from a later epic until all stories in its prerequisite epics are completed.
- Within an epic, follow **feature order** (1.1 before 1.2, etc.).
- Within a feature, follow **story order** (1.1.1 before 1.1.2, etc.).

#### Selection Algorithm

1. Start from Epic 1, Feature 1.1, Story 1.1.1.
2. Find the first story (in order) with status = Pending (no checkbox).
3. Verify all prerequisite epics are fully completed (all stories `- [x]`).
4. If prerequisites are not met, **do not skip ahead** — report the blocker.
5. If prerequisites are met, this is the **next story**.

#### Present the Selection

Display to the user:
- **Story ID and title** (e.g., "Story 1.1.1: Initialize ASP.NET Core Web API Solution")
- **Story points**
- **Epic and Feature context** (which epic/feature this belongs to)
- **Full description** from the backlog
- **All acceptance criteria** as a checklist
- **Dependencies** — what must be done before this (if any blocking stories remain)

Ask the user: **"Shall I start working on this story?"**

---

### Step 4: Begin the Story

If the user confirms:

1. **Update `docs/backlog.md`**: Add `- [~] **IN PROGRESS**` immediately after the story's `####` header line.
2. **Print a work plan**: Break down the acceptance criteria into concrete implementation steps.
3. **Begin implementation** of the first step.

If the user declines:
- Ask if they want to pick a different story (show the next 3 candidates in order).
- Or stop without selecting anything.

---

## Status Line Format Reference

When updating `docs/backlog.md`, use exactly these formats:

**Marking in progress** — insert this line immediately after the `#### Story ...` header:
```markdown
- [~] **IN PROGRESS**
```

**Marking completed** — replace the in-progress line with:
```markdown
- [x] **COMPLETED**
```

**Reverting to pending** — remove the status checkbox line entirely (no line between header and description).

---

## Example Flow

```
> /next-item

📊 Backlog Status:
  Completed: 3 stories (16 SP)
  In Progress: 1 story (5 SP) — Story 1.1.2
  Pending: 50 stories (266 SP)

🔍 Validating Story 1.1.2: Configure Dependency Injection...
  ✅ Program.cs exists with DI setup
  ✅ CORS configuration found
  ✅ Exception handling middleware exists
  ✅ Swagger configured
  ✅ Health check endpoint returns 200
  ❓ Environment-based config override — not verified

All key criteria appear met. Mark Story 1.1.2 as completed? [y/n]
> y

✅ Story 1.1.2 marked as COMPLETED.

📋 Next Story: 1.1.3 — Set Up Unit and Integration Test Projects (5 SP)
   Epic 1: Project Infrastructure & Setup
   Feature 1.1: Backend Project Scaffolding

   [Full description and acceptance criteria shown]

   Shall I start working on this story? [y/n]
```
