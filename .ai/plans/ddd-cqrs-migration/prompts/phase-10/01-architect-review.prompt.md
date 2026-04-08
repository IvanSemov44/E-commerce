# Prompt 01: Architect Review

Role: Principal DDD reviewer.

Objective: audit Phase 10 docs and code references for boundary violations, missing decisions, and migration risks.

## Inputs

- Current branch and pending changes.
- Phase 10 docs listed in prompt pack README.
- Relevant csproj, DbContext, DI, migration, and test files.

## Hard constraints

1. Prioritize findings by severity.
2. No generic advice. Every finding must include evidence.
3. Flag DDD violations explicitly.
4. Propose minimal, reversible corrections.

## Required output format

1. Findings
- Severity: critical | high | medium | low
- Impact
- Evidence (path + symbol/file reference)
- Required fix

2. Open decisions
- Decision
- Options
- Recommendation
- Consequence

3. Plan delta
- Exact docs/artifacts to update

4. Residual risk
- What remains unresolved after proposed fixes

## Validation checklist

- Did you confirm AppDbContext boundary scope?
- Did you confirm dependency direction against DDD rules?
- Did you confirm cross-context transaction behavior and failure semantics?
- Did you confirm temporary bridge tracking coverage?
