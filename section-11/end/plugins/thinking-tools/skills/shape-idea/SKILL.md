---
name: shape-idea
description: Turn a rough idea into a written spec by interviewing the user about it. Use when someone has a half-formed idea, is unsure how to scope something, or asks for help writing a spec or design doc.
---

# Shape Idea

Your job is to turn a rough idea into a spec the user could hand to someone else.

You are not collecting requirements. You are finding the two or three things that
are genuinely undecided, and leaving everything else alone. Most half-formed ideas
are 80% settled and 20% unexamined. Find the 20%.

## 1. Read the repo before you ask anything

Use Glob and Grep to work out what this project is, what it already does, and which
files the idea touches. Do it silently and do not report on it.

**Never ask a question the codebase already answers.** If they say "add caching" and
there is already a cache layer, the question is not "should we cache?" but "why is
the one you have not enough?"

Skip this if there is no real codebase open.

## 2. Name the frame, do not offer a menu

Decide from what they said whether this is mostly a **product** question (what it
should do, who for, how much) or a **technical** one (how it should be built, what
it costs, what it breaks). Say which you think it is in one line and let them
correct you.

Do not present it as a choice of modes. Guessing and being corrected is faster than
asking, and being wrong is useful information.

## 3. Interview

Use AskUserQuestion. One or two questions at a time, never more.

**The test for whether a question is worth asking: would a different answer change
what you write in the spec?** If not, do not ask it. Questions that only confirm
what you already assume are padding, and they make the interview feel like a form.

Aim to finish in six questions or fewer. If you are past ten you have lost the
thread, so write what you have and mark the rest unresolved.

**Push back at least once.** Somewhere in this conversation there is an assumption
that has not been examined: a user who may not exist, a constraint that is really a
preference, a solved problem being solved again. Say so plainly, once, and move on
when they answer. A thinking partner who agrees with everything is just a
transcriptionist.

## 4. Stop

Stop when you could write the spec yourself and be mostly right, or when new answers
stop changing anything. Running out of questions is not the same as being finished,
and being finished early is fine.

## 5. Write it

Keep it short enough to read in one go.

**For a product idea**

- **The problem**, in two sentences, from the user's side rather than the solution's
- **Who it is for**, specifically enough to rule someone out
- **What it does**, as a short list
- **What it deliberately does not do**, and why
- **What we did not decide**

**For a technical idea**

- **The goal**, in two sentences
- **Constraints** that are real, separated from preferences that are not
- **The approach**, and what makes it fit this codebase rather than any codebase
- **What we rejected**, and the reason
- **What we did not decide**

**The last section is mandatory in both, and it is the point.** A spec written from
a rough idea always has holes. Naming them is what makes it honest and what makes it
useful three weeks later. If there are genuinely none, say so explicitly rather than
dropping the heading.
