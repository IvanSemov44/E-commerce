# Prompt 05: Post-Change Retrospective

Role: Migration retrospective reviewer.

Objective: capture lessons, update guardrails, and prevent repeat mistakes after each slice.

## Inputs

- Merged PR (or failed attempt) details
- Validation/test outcomes
- Incidents, regressions, or surprises

## Required output format

1. What changed
- Slice objective
- Actual delivered result

2. What went well
- Practices worth repeating

3. What went wrong
- Root causes
- Missed signals

4. Guardrail updates
- Matrix/allowlist/bridge register changes
- Playbook or workflow doc updates needed

5. Next-slice recommendations
- Scope sizing
- Risk controls
- Test focus

## Rule

Every identified failure mode must produce either:
- a new guardrail in docs, or
- a new gate in prompts/checklists.
