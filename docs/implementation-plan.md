# Implementation Plan

The work was broken into phases. Each phase ended in a runnable state so I could verify progress before moving on. This is the plan I started from; the project hit each phase and then continued through a bonus backlog (see `docs/feature-checklist.md`).

## Phase 1: Project Setup

**Goal:** Establish the monorepo foundation so both projects can be built and run locally.

**Outputs:**
- Repository structure with `/client-unity`, `/server`, and `/docs`.
- `.gitignore`, `.editorconfig`, and a baseline `README.md`.
- Empty Unity project scaffolded under `/client-unity`.
- Empty ASP.NET Core Web API scaffolded under `/server`.
- Local run instructions verified for both sides.

**Risks:**
- Unity and .NET tooling versions drifting between machines. I pinned SDK versions in the documentation.

## Phase 2: Backend Contracts

**Goal:** Define and implement the two required endpoints with representative data.

**Outputs:**
- Data models for monsters, stats, and moves.
- A `GET` run configuration endpoint returning the full run payload.
- A `GET` opponent-move endpoint that accepts battle state and returns a move choice.
- A documented example payload for each endpoint.

**Risks:**
- Designing a battle-state shape that is either too thin to support move selection or too heavy to be practical. I iterated the shape alongside early client integration.

## Phase 3: Client Foundations

**Goal:** Stand up the client screens and wire them to the server.

**Outputs:**
- Main Menu and navigation skeleton.
- Run Overview screen consuming the run configuration endpoint.
- Move Management screen reading from the configured data.
- HTTP layer for talking to the server with clear error handling.

**Risks:**
- Scope creep in UI polish before functionality is in place. I kept visuals minimal until later phases.

## Phase 4: Battle Loop

**Goal:** Implement turn-based combat end to end.

**Outputs:**
- Battle Screen with player and opponent monsters, HP, and move selection.
- Damage resolution for physical and magic moves using Attack, Defense, and Magic.
- Opponent turns driven by the server's move-selection endpoint.
- Post-Battle screen showing outcome and transitioning back into the run.

**Risks:**
- Subtle mismatches between client-side damage calculations and the values implied by the server's configuration. I kept a single shared definition of the formulas, documented in one place.

## Phase 5: Progression

**Goal:** Make runs feel like a progression, not a single fight.

**Outputs:**
- Experience awarded after battles.
- Level-ups that update stats.
- Learned moves expanding over time.
- Run state carried between battles until the run ends.

**Risks:**
- Progression formulas that trivialise or stall the run. I tuned against a small number of representative battles before broadening.

## Phase 6: Polish and Submission

**Goal:** Prepare the submission.

**Outputs:**
- A pass over UI readability, screen transitions, and basic feedback.
- Verification against the acceptance checklist.
- Final README and docs review.
- Clean commit history and a runnable project from a fresh clone.

**Risks:**
- Last-minute changes introducing regressions. I froze new features once Phase 5 landed and limited final commits to polish and fixes.

## Bonus Backlog (after Phase 6)

Once the core was stable I worked through a longer list of bonus items: combat depth, environments, level-up choices, items, item management, shop, localization, expanded enemies and moves, hero classes, non-linear map, endless mode, save and exit, and a settings panel. Each landed on its own feature branch so it could be reviewed in isolation. The current state is summarised in `docs/feature-checklist.md`.
