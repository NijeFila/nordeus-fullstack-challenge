# Implementation Plan

The work is broken into phases. Each phase ends in a runnable state so that progress can be verified before moving on.

## Phase 1 — Project Setup

**Goal:** Establish the monorepo foundation so both projects can be built and run locally.

**Outputs:**
- Repository structure with `/client-unity`, `/server`, and `/docs`.
- `.gitignore`, `.editorconfig`, and baseline `README.md`.
- Empty Unity project scaffolded under `/client-unity`.
- Empty ASP.NET Core Web API scaffolded under `/server`.
- Local run instructions verified for both sides.

**Risks:**
- Unity and .NET tooling versions drifting between machines. Mitigated by pinning SDK versions in documentation.

## Phase 2 — Backend Contracts

**Goal:** Define and implement the two required endpoints with representative data.

**Outputs:**
- Data models for monsters, stats, and moves.
- `GET` run configuration endpoint returning the full run payload.
- `GET` opponent-move endpoint that accepts battle state and returns a move choice.
- A simple, documented example payload for each endpoint.

**Risks:**
- Designing a battle-state shape that is either too thin to support move selection or too heavy to be practical. Mitigated by iterating the shape alongside early client integration.

## Phase 3 — Client Foundations

**Goal:** Stand up the client screens and wire them to the server.

**Outputs:**
- Main Menu and navigation skeleton.
- Run Overview screen consuming the run configuration endpoint.
- Move Management screen reading from the configured data.
- HTTP layer for talking to the server with clear error handling.

**Risks:**
- Scope creep in UI polish before functionality is in place. Mitigated by keeping visuals minimal until later phases.

## Phase 4 — Battle Loop

**Goal:** Implement turn-based combat end to end.

**Outputs:**
- Battle Screen with player and opponent monsters, HP, and move selection.
- Damage resolution for physical and magic moves using Attack, Defense, and Magic.
- Opponent turns driven by the server's move-selection endpoint.
- Post-Battle screen showing outcome and transitioning back into the run.

**Risks:**
- Subtle mismatches between client-side damage calculations and the values implied by the server's configuration. Mitigated by a single shared definition of the formulas, documented in one place.

## Phase 5 — Progression

**Goal:** Make runs feel like a progression, not a single fight.

**Outputs:**
- Experience awarded after battles.
- Level-ups that update stats.
- Learned moves expanding over time.
- Run state carried between battles until the run ends.

**Risks:**
- Progression formulas that trivialize or stall the run. Mitigated by tuning against a small number of representative battles before broadening.

## Phase 6 — Polish and Submission

**Goal:** Prepare the submission.

**Outputs:**
- Pass over UI readability, screen transitions, and basic feedback.
- Verification against the acceptance checklist.
- Final README and docs review.
- Clean commit history and a runnable project from a fresh clone.

**Risks:**
- Last-minute changes introducing regressions. Mitigated by freezing new features once Phase 5 lands and limiting final commits to polish and fixes.
