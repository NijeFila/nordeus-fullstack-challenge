# Acceptance Checklist

A checklist that maps directly to the challenge requirements. Items are unchecked until verified in a fresh build.

## Client

- [ ] Main Menu is present and allows starting a new run.
- [ ] Run Overview shows the player's monsters and current run state.
- [ ] Move Management screen lets the player inspect monster moves.
- [ ] Battle Screen renders both monsters, their health, and available moves.
- [ ] Player can select a move and see the result applied.
- [ ] Post-Battle flow shows outcome and returns the player to the run.
- [ ] Navigation between screens works without dead ends.

## Server

- [ ] `GET` endpoint for run configuration is implemented and reachable.
- [ ] `GET` endpoint for opponent move selection is implemented and reachable.
- [ ] Response payloads are documented with an example.
- [ ] Endpoints return sensible responses for invalid or missing input.
- [ ] Server runs locally with a single documented command.

## Game Systems

- [ ] Stats include Health, Attack, Defense, and Magic.
- [ ] Physical moves use Attack vs. Defense in damage calculation.
- [ ] Magic moves use Magic in damage calculation.
- [ ] Each monster has its own set of learned moves.
- [ ] Monsters gain experience from battles.
- [ ] Leveling up improves stats.
- [ ] New moves are learned through progression.

## Submission Polish

- [ ] Project builds and runs from a fresh clone using only the documented steps.
- [ ] README describes the project, stack, and setup.
- [ ] `docs/` contains challenge summary, architecture, implementation plan, and this checklist.
- [ ] Code is organized and readable.
- [ ] Repository is free of unneeded build artifacts and editor files.

## Optional Bonus

- [ ] Server-side validation of battle outcomes.
- [ ] Run persistence between sessions.
- [ ] Smarter opponent move selection (e.g., matchup-aware weighting).
- [ ] Audio or visual feedback on actions.
- [ ] Expanded move or type system.
