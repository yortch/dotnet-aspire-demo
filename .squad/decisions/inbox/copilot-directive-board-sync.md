# Directive: Board Sync

**Source:** User (Jorge Balderas)
**Date:** 2026-03-03
**Status:** Active

## Directive

> I'm not seeing project board being updated, can you make sure that work is reflected in the board as issues are being worked on

## Interpretation

Every issue state change MUST be reflected on the GitHub Project Board (#3) in real time:

1. **Issue assigned / work begins** → Move to **In Progress**
2. **PR opened** → Move to **In Review**
3. **PR merged / issue closed** → Move to **Done**

## Implementation

Use GraphQL mutations against the project board:
- Project ID: `PVT_kwHOAEXT9s4BQrvI`
- Status Field ID: `PVTSSF_lAHOAEXT9s4BQrvIzg-u3U0`
- Status options: Backlog (`0c668a1a`), In Progress (`bfd628d3`), In Review (`9b4e7601`), Done (`508eabd1`)

Ralph (Work Monitor) is responsible for issuing board updates at each state transition.
