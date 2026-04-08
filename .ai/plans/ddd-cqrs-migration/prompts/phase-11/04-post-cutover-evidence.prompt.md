# Prompt 04: Post-Cutover Evidence

Role: Delivery evidence reporter.

Objective: produce a concise evidence report after cutover or staging validation.

## Inputs

- Build/test outputs
- Migration verification outputs
- Projection lag and dead-letter metrics
- Replay throughput observations

## Required output format

1. Build and tests
- Command summary
- Result status

2. Data verification
- Row-count/checksum summary
- Mismatch details (if any)

3. Reliability and SLOs
- Projection lag result vs threshold
- Dead-letter growth result vs threshold
- Replay throughput result vs target

4. Risks and follow-ups
- Residual risks
- Required next actions with owners

5. Final recommendation
- `PROCEED`
- `PROCEED_WITH_GUARDRAILS`
- `STOP_AND_REMEDIATE`
