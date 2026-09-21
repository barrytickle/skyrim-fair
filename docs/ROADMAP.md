# Skyrim Fair roadmap

## Milestone 0: prove the toolchain

- [x] Code-first repository
- [x] Mutagen generator project
- [x] JSON fair configuration
- [x] Generate a smoke-test `SkyrimFair.esp`
- [ ] Build on Barry's machine
- [ ] Confirm Skyrim loads the generated plugin

## Milestone 1: put something in the world

- [ ] Inspect Barry's active Skyrim load order
- [ ] Choose a safe exterior test cell
- [ ] Resolve vanilla stage/platform statics by record, not guessed load-order FormIDs
- [ ] Place one unmistakable test object
- [ ] Verify position in game
- [ ] Replace the test object with a small stage

## Milestone 2: make the stage alive

- [ ] Two generic bards
- [ ] Two dancers
- [ ] Professional Dancer integration audit
- [ ] Performance start/stop orchestration
- [ ] Crowd markers and basic audience idles
- [ ] Add `Round the Green` as the prototype stage track

## Milestone 3: establish the fairground

- [ ] Select final flat exterior site
- [ ] Conflict scan against Barry's load order
- [ ] Build the dense Christmas-market-inspired main avenue and side trading rows
- [ ] Stalls, tents, fences, lanterns and seating
- [ ] Food lane: sweetroll-only stall, pies, mead/ale and other specialist food traders
- [ ] Trading rows: iron/steel, Imperial, Stormcloak, Dwemer, Elven, mage, alchemy and jewellery
- [ ] Put the Imperial and Stormcloak gear stalls opposite each other as rival traders
- [ ] Establish the stage square with dancing crowd and ambient spectators
- [ ] Fair opening hours / schedule
- [ ] Vendors and ambient NPCs
- [ ] Evening lighting state

## Milestone 4: activities

- [ ] Archery range
- [ ] Axe throwing
- [ ] Drinking game
- [ ] Prize/token system
- [ ] NPC tournament events

## Milestone 5: jousting

- [ ] Straight fenced lists
- [ ] Mounted NPC movement proof of concept
- [ ] Lance animation dependency audit
- [ ] Scripted hit / miss / unhorse outcomes
- [ ] Spectator reactions

## Principle

Prefer generated plugin data and small, testable increments. Do not introduce SKSE,
Papyrus, custom navmesh, or new animation authoring until a milestone actually needs it.
