# Working agreement

> **Archived 2026-09-26 by TASK-0039. This document is a record and not a rule.** The rules review
> of 2026-09-25 (`docs/reviews/2026-09-25-rules-review.md`, section 2.3) moved each live rule to
> `CLAUDE.md` or to `docs/templates.md`, and removed each rule that repeated a gate or another
> file. The text below is the document as TASK-0015 accepted it on 2026-09-20. It stays because its
> reasons, from the Blender, FreeCAD and Ondsel projects, explain the rules that remain.

**Status: Accepted** — 2026-09-20, TASK-0015.

This document tells you how to behave in this repository. `CLAUDE.md` tells you where things are
and what the boundaries are.

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section
"Language".

An agent writes most of the code in this repository. A person directs the work. This is a
decision, and it changes what the rules must do. A rule that needs judgement works with a person
who knows the context. It does not work with an agent that read four files. Therefore each rule
below is a rule that you can check, and the most important rules have a test.

---

## 1 The first principle

**The quality limit for work by an agent is higher than the quality limit for work by a person.**

This rule comes from the Blender project. Their reason is correct: the tool gives you time. Use
that time for tests and for special conditions. Do not use it to write more code.

This project is also a project to learn from. Therefore one more rule applies. If the owner
cannot understand the work well enough to keep it, the work failed. Green tests do not change
this. A large quantity of code is not progress. Nobody can keep a subsystem that nobody
understands.

---

## 2 Scope

**2.1** Stay inside the write-set. Each task declares the files that it creates, the files that it
changes, and the files that it must not touch. If you must change a different file, stop. Report
the problem.

**2.2** Do not improve a thing that nobody asked you to improve. Do not correct an adjacent file.
Do not change a name for consistency. These changes hide the real change. If you find a different
problem, add a register entry.

**2.3** Put one topic in one change. A refactor and a change of behaviour must not share a commit.

**2.4** Add a new file instead of a change to a file that many tasks use. If a new command needs a
change to a dispatch switch, the design is incorrect. Report the problem. Do not add the case.

---

## 3 Do not change the record

**3.1** Do not change the text of an approved ADR. You can add a new ADR. You can change a status
field. You must not change the text. An agent that receives the instruction "update the
documentation" can change a decision record to agree with new code. This action destroys the
value of the record.

**3.2** Do not change history in `CURRENT-STATE.md`. Entries are permanent. To correct an entry,
add a new entry.

**3.3** Do not close a register entry to make a gate pass. Use one of the four exits. The four
exits are in `docs/register.md`. Deletion of an entry destroys the register.

**3.4** If the documentation and the code disagree, stop. Do not change the documentation to agree
with the code. Do not change the code to agree with the documentation. One of the two holds a
decision. The other is an error. An agent cannot identify which is which. Report both.

---

## 4 Accuracy

**4.1** Report what you did not verify. "The build passes and the tests pass" is one statement. "I
think this is correct" is a different statement. Tell the reader which statement you make.

**4.2** Narrow tests are not sufficient. The gate is the standard. See `CLAUDE.md`, section "Test
discipline".

**4.3** Give the method for each large change. Three lines are sufficient:

- **Mechanical** — the work that needed no decision.
- **Judgement** — the work that needed a decision, and the decision that you made.
- **Weakest** — the part that is most probably incorrect, and the test that shows it.

This block tells the reader where to look. It costs one minute. It is difficult to write
incorrectly. It is necessary for each change that an agent made.

**4.4** Put each assumption in the register. If you made a guess about an important thing, that
guess is now a register entry with a date.

**4.5** Do not invent a reference. A path, a line number, an ADR section and an issue number are
all things that a person can check. An incorrect reference is worse than no reference.

---

## 5 Decisions

**5.1** Give the ADR number in the code, at the position that the ADR controls. Use the same
format as the adjacent code.

**5.2** Read the ADRs that control your write-set. Do not read the other ADRs. Each ADR declares
the paths that it controls.

**5.3** Read the rejected decisions before you propose a decision. Rejected ADRs, withdrawn ADRs
and closed register entries exist for this purpose. Do not propose a decision that the project
refused.

**5.4** If the roadmap does not contain a capability, the capability is out of scope. This rule
does not change if the capability looks correct.

---

## 6 Each rule needs a test

An agent can obey the words of a rule and break the purpose of the rule. Therefore:

**6.1** Each ADR declares how the project enforces it. The value is a test name, an analyzer rule,
or the text `UNENFORCED (accepted risk)`. The gate fails if the quantity of unenforced ADRs
increases.

**6.2** A rule in text only is a rule that an agent will break. If a boundary is important, write
a gate for it. `CLAUDE.md` declared the dependency direction rule for many months, but no check
exists. See register entry R-0004.

**6.3** Use a mechanical check, not a convention. Use this order of preference:

1. A compile error
2. A test that fails
3. A step in continuous integration (CI)
4. A convention in the documentation

Move a rule up this list after it fails two times.

---

## 7 Time

**7.1** Do not make a long branch for architecture work. Divide the work into parts. Each part
must merge to the main branch, and each part must keep the build green. The Ondsel team gave this
conclusion after the FreeCAD topological naming work. This rule is the most important rule here
for work in the evening.

**7.2** Leave the repository green. See `CLAUDE.md`, section "Workflow", for the conditions to end
a session. If you cannot reach that state in one session, the work was too large.

**7.3** Write for a reader who is not you. The owner reads this code three months later with no
memory of the session. The FreeCAD project could not use a correct repair for years, because only
its author could keep the code.

---

## 8 Parallel agents

**8.1** Two agents must not hold write-sets that intersect. Compare the write-sets before you
start the agents.

**8.2** A central list is a position where parallel work collides. Examples are a registration
list, an index and a ledger. Generate the list, or add to it only, or merge it with the union
strategy. Do not keep it as text that a person edits.

**8.3** Give each agent its own worktree. Keep each task short. Use hours, not days.

---

## Other documents

- `docs/conventions.md` gives the rules for code style, names and file layout.
- `CLAUDE.md` gives the boundaries and the dependency rules.
- An ADR gives the reason for a decision.

This document gives the rules for behaviour only.
