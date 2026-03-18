# Principal / FAANG Level — What You Need to Learn

> Personal reference. Go through these one by one when the technical debt is cleared.
> Each section explains what it is, why it matters, and how it applies to your project.

---

## 1. Runbooks

### What it is
A runbook is a step-by-step procedure that anyone on the team can follow at 2am when something breaks — without needing to understand the full system. It removes the "only Ivan knows how to fix this" problem.

### Why it matters
FAANG engineers write runbooks because systems break at the worst possible time. A runbook means a junior dev or on-call engineer can recover the system without waking up the one person who built it.

### What it looks like
```markdown
# Runbook: High Payment Failure Rate

## Symptoms
- Error rate on POST /api/payments/process > 10% in last 5 min
- Alert: "Payment failures spike" fires in monitoring

## Immediate steps
1. Check Stripe status page: https://status.stripe.com
2. Check logs for PAYMENT_AMOUNT_MISMATCH errors:
   grep "PAYMENT_AMOUNT_MISMATCH" logs/ecommerce-today.log
3. If Stripe is down: set maintenance banner on checkout page
4. If it's our code: check last deployment time vs when failures started

## Root cause investigation
- If failures started after a deploy: rollback with `git revert + redeploy`
- If random failures: check Stripe webhook logs in dashboard

## Escalation
- After 15 min unresolved: escalate to backend lead
```

### What you need to learn
- How to identify the 5-10 most critical failure scenarios in your system
- How to write steps clearly enough that someone unfamiliar can follow them
- How to link runbooks to alerts so on-call knows which runbook to open

### Applies to your project
Your most important runbooks to write eventually:
1. Payment service failing
2. Database connection exhausted
3. JWT secret compromised (mass token revocation)
4. High cart concurrency errors
5. Email delivery failing

---

## 2. SLOs / SLAs / Error Budgets

### What it is
- **SLA** (Service Level Agreement) — a promise to customers: "we guarantee 99.9% uptime"
- **SLO** (Service Level Objective) — your internal target, stricter than the SLA: "we aim for 99.95%"
- **SLI** (Service Level Indicator) — the actual measurement: "last month we had 99.97% uptime"
- **Error Budget** — how much failure you're allowed before breaching the SLO

### Why it matters
Without SLOs, every incident feels equally catastrophic. With SLOs, you know: "we have 0.05% error budget per month = 21 minutes of downtime allowed." If you still have budget, you ship. If you've burned it, you stop shipping features and fix reliability.

### The math
```
99.9% uptime  = 8.7 hours downtime allowed per year  = 43.8 min/month
99.95% uptime = 4.4 hours downtime allowed per year  = 21.9 min/month
99.99% uptime = 52 minutes downtime allowed per year = 4.4 min/month
```

### What error budget in practice looks like
```
Monthly error budget: 21 minutes
Used so far this month: 8 minutes (deploy on the 5th took 4 min, plus a blip)
Remaining: 13 minutes

Decision: we can ship the Stripe integration this sprint.
If remaining was < 5 min: freeze feature work, fix reliability first.
```

### What you need to learn
- How to define SLIs for your system (latency, error rate, availability)
- How to set realistic SLOs (not "100% uptime" — that's impossible)
- How error budgets change the conversation from "can we ship?" to "do we have budget?"
- The difference between availability SLO and latency SLO

### Applies to your project
Your most important SLOs to define:
- Checkout flow: 99.9% success rate, P99 < 3 seconds
- Product listing: 99.95% availability, P99 < 1 second
- Auth endpoints: 99.9% availability

---

## 3. Domain Model

### What it is
A document that captures the *business rules* of your system in plain language — separate from how the code implements them. It answers "what are the rules this business must never violate?"

This comes from **Domain-Driven Design (DDD)** — specifically: aggregate roots, invariants, and domain events.

### Key concepts

**Aggregate Root** — an entity that owns and protects a cluster of related objects.
In your project: `Order` is an aggregate root. It owns `OrderItems`. You never add/remove items directly — you go through the Order.

**Invariant** — a rule that must always be true.
Examples from your project:
- An Order's `TotalAmount` must always equal `Subtotal + ShippingAmount + TaxAmount - DiscountAmount`
- A Cart can have at most one item per product (enforced by unique index)
- A PromoCode's `UsedCount` can never exceed `MaxUses`
- An Order in `Shipped` status cannot be cancelled

**Domain Event** — something that happened in the domain that other parts of the system care about.
Examples from your project:
- `OrderPlaced` → trigger: decrement stock, send confirmation email
- `OrderShipped` → trigger: send shipping notification, set `ShippedAt`
- `UserRegistered` → trigger: send welcome email, send verification email
- `PaymentFailed` → trigger: revert order to Pending, notify customer

### What you need to learn
- What Domain-Driven Design (DDD) is — read the "blue book" (Eric Evans) or at minimum the "red book" (Vaughn Vernon)
- How to identify aggregates vs entities vs value objects
- How to write invariants as rules, not as code
- How domain events lead to eventual consistency and decoupled services

### Applies to your project
Your main aggregate roots: `Order`, `Cart`, `Product` (controls stock), `User`
Writing down their invariants is the first step to never having data consistency bugs.

---

## 4. Scalability & Capacity Analysis

### What it is
A structured answer to: "how much traffic can this system handle, and what breaks first?"

### Why it matters
Without this you don't know:
- When to scale horizontally (add more API servers)
- When to scale vertically (bigger DB server)
- When you need a cache (Redis)
- When you need a read replica
- What your ceiling is before you need to redesign

### How to think about it — the bottleneck chain
Every system has a bottleneck. Find it before your users do.

```
Request → API Server → Service → Repository → Database
                                              ↑
                                         Usually here
```

For your project, the likely bottleneck order:
1. **Database connection pool** — EF Core holds connections. Under high load, requests queue waiting for a connection. Fix: increase pool size, add read replica.
2. **N+1 queries on product listing** — 100 products = 100 extra queries for images. Fix: `.Include()`.
3. **Cart concurrency** — optimistic lock retries under high load. Fix: retry policy + exponential backoff.
4. **Memory on API server** — AutoMapper allocates a lot under load. Fix: projection instead of mapping.

### Key numbers to know for your stack
- PostgreSQL: handles ~1000-5000 connections, ~10,000 simple queries/second per core
- .NET API: easily handles 10,000+ requests/second per core with async code
- React frontend: static files, scales infinitely via CDN

### What you need to learn
- How to read a database `EXPLAIN ANALYZE` output (PostgreSQL query plan)
- What connection pooling is and how to tune it (PgBouncer)
- When a read replica helps vs when it doesn't
- What Redis solves (session cache, rate limiting, distributed locks, cart cache)
- How to do a back-of-the-envelope capacity calculation
- What horizontal vs vertical scaling means and when each applies

---

## 5. Incident Response Playbook

### What it is
A predefined process for handling production outages. Who does what, in what order, with what communication.

### Why it matters
During an incident, adrenaline is high and thinking is impaired. A playbook removes decisions — you just follow the steps.

### The standard structure

**Severity levels:**
| Level | Definition | Response time | Example |
|-------|-----------|---------------|---------|
| P0 | System completely down, data at risk | Immediate | DB is down, all requests failing |
| P1 | Major feature broken for all users | 15 min | Checkout not working |
| P2 | Feature broken for some users | 1 hour | Payment failing for Visa cards |
| P3 | Minor issue, workaround exists | Next business day | Promo code validation slow |

**The incident process:**
1. **Detect** — monitoring alert fires or user reports
2. **Acknowledge** — someone owns it within SLA response time
3. **Communicate** — post in #incidents: "Investigating reports of checkout failures"
4. **Mitigate** — restore service (rollback, feature flag off, restart)
5. **Resolve** — root cause fixed
6. **Post-mortem** — written within 48 hours (blameless)

**Blameless post-mortem** — the most important concept. The goal is never to blame a person. The goal is to find what process or system allowed the incident to happen, and fix that.

### What you need to learn
- How to write a post-mortem (timeline, root cause, contributing factors, action items)
- The concept of "blameless culture" — why blaming people makes systems less safe
- How feature flags allow you to turn off a broken feature without deploying
- What a status page is and why customers need one

---

## 6. API Versioning & Deprecation Policy

### What it is
A strategy for how you change your API without breaking existing clients.

### Why it matters
Once an API is live and clients depend on it, you can't just change it. Removing a field, renaming an endpoint, or changing a response shape will break every client. You need a plan.

### The main strategies

**URL versioning** (most common, simplest):
```
/api/v1/products   ← old version, kept alive
/api/v2/products   ← new version with breaking changes
```

**Header versioning:**
```
GET /api/products
API-Version: 2
```

**Deprecation process:**
1. Ship v2 alongside v1
2. Add `Deprecation` response header to v1: `Deprecation: true`
3. Document the sunset date: "v1 will be removed on 2027-01-01"
4. Monitor v1 traffic — when it drops to zero, remove it

### Versioning in .NET
ASP.NET Core has a `Asp.Versioning` NuGet package that handles this cleanly. This is listed as a future improvement in your `.ai/tracking/` docs.

### What you need to learn
- What constitutes a "breaking change" (removing fields, changing types, changing auth requirements)
- What does NOT constitute a breaking change (adding new optional fields, adding new endpoints)
- How to use the `Asp.Versioning` NuGet package
- How to write a deprecation notice
- Semantic versioning (SemVer): MAJOR.MINOR.PATCH

---

## 7. Dependency Risk Matrix

### What it is
A structured analysis of every external dependency your system has, what happens when it fails, and what your mitigation is.

### Why it matters
Every external service you depend on WILL fail eventually. Stripe has outages. SendGrid has deliverability issues. Your DB can run out of connections. Knowing your failure modes in advance means you can design gracefully degraded experiences instead of total outages.

### What it looks like for your project

| Dependency | What breaks if it fails | Current mitigation | What's missing |
|---|---|---|---|
| PostgreSQL | Everything — no reads or writes | Polly retry (3 attempts) | No read replica, no failover |
| Stripe | Payments — checkout blocked | Currently mocked | Graceful degradation message needed |
| SendGrid | Emails not sent | SMTP fallback registered | Email queue/retry not implemented |
| SMTP server | Emails not sent | SendGrid fallback | Need to pick one as primary |
| JWT secret | All tokens invalid if rotated | N/A | Mass revocation procedure needed |

### Key concept: graceful degradation
If Stripe is down, should your entire checkout page break? Or should it show "payment processing is temporarily unavailable, please try again in a few minutes"?

That's graceful degradation — the system stays partially functional instead of fully broken.

### What you need to learn
- Circuit breaker pattern (already in your Polly config — learn how it works)
- Retry with exponential backoff (also in your Polly config)
- What a fallback is and when to use one
- Timeout budgets — how long to wait before giving up on an external call
- The concept of "fail fast" vs "retry"

---

## 8. Data Retention & GDPR

### What it is
Rules for how long you keep personal data, what happens when a user asks you to delete it, and what a data breach requires you to do.

### Why it matters
GDPR (EU) and similar laws (CCPA in California, LGPD in Brazil) give users rights over their data:
- **Right to access** — "show me everything you have about me"
- **Right to erasure** ("right to be forgotten") — "delete all my data"
- **Right to portability** — "give me my data in a machine-readable format"

Violating these can result in fines up to 4% of global annual revenue.

### The hard problem in your project
Users have Orders. Orders have financial/legal significance — you may be legally required to keep them for 7 years for tax purposes. But the user has a right to erasure.

The solution: **anonymisation, not deletion**.
```sql
-- Don't delete the order
-- Anonymise the personal data
UPDATE Orders SET GuestEmail = NULL WHERE UserId = @userId;
UPDATE Users SET
    Email = 'deleted-' + Id + '@deleted.invalid',
    FirstName = 'Deleted',
    LastName = 'User',
    Phone = NULL
WHERE Id = @userId;
-- Keep the order record for accounting
-- Keep the order items for inventory history
```

### Key concepts to learn
- What constitutes PII (Personally Identifiable Information) — your columns: Email, FirstName, LastName, Phone, Address, IP addresses in logs
- Data retention periods by data type (financial records: 7 years; session data: 30 days; logs: 90 days)
- What a Data Processing Agreement (DPA) is — needed if you use processors like Stripe, SendGrid
- What a data breach notification requirement is (GDPR: notify within 72 hours)
- The difference between deletion and anonymisation

### Applies to your project
Your PII columns:
- `Users`: Email, FirstName, LastName, Phone, GoogleId, FacebookId, AvatarUrl
- `Addresses`: All fields
- `Orders`: GuestEmail, ShippingAddress, BillingAddress
- `Reviews`: linked to UserId
- Logs: any request that contains email or user ID

---

## 9. Zero-Downtime Deployment

### What it is
A deployment strategy where you ship new code without any users experiencing downtime or errors.

### Why it matters
If you stop the server to deploy, users get errors during that window. For a checkout flow, even 30 seconds of downtime during peak hours loses real money.

### The strategies

**Rolling deployment** (simplest):
1. Start new version alongside old
2. Health check new version
3. Route traffic to new version
4. Shut down old version

**Blue-Green deployment** (safest):
```
Blue environment  → currently live (v1.0)
Green environment → deploy new version (v1.1)
                  → run smoke tests
                  → switch load balancer from Blue to Green
                  → if problems: switch back to Blue (instant rollback)
                  → Blue becomes the next staging environment
```

**The database migration problem** — the hardest part:
You can't rename a column while old code is still running — old code will break because the column doesn't exist anymore.

The solution: **expand-contract migrations**:
```
Step 1 (expand):    Add new column, keep old column. Deploy code that writes to both.
Step 2 (migrate):   Backfill old column data into new column.
Step 3 (contract):  Deploy code that reads only from new column. Drop old column.
```
This takes 3 deploys but has zero downtime.

### What you need to learn
- How Docker and Kubernetes enable rolling deployments
- The expand-contract (also called parallel change) migration pattern
- What a health check endpoint is used for in deployments (your `/health/ready` already exists for this)
- What a canary deployment is (route 5% of traffic to new version, watch metrics, then 100%)
- How feature flags let you deploy code without activating features

---

## 10. Load Testing

### What it is
Simulating real user traffic on your system to find where it breaks before real users do.

### Why it matters
You built the system. You know what it should do. Load testing answers: "what does it actually do under 100 concurrent users? 1,000? 10,000?"

### The types
- **Load test** — simulate expected normal traffic (e.g. 100 users/second)
- **Stress test** — push until it breaks — find the breaking point
- **Spike test** — sudden burst (e.g. flash sale: 0 → 5,000 users in 30 seconds)
- **Soak test** — normal load for 24 hours — finds memory leaks and slow degradation

### Tools to know
- **k6** (JavaScript, modern, great for REST APIs) — recommended for your stack
- **Locust** (Python) — good for complex scenarios
- **JMeter** (Java) — enterprise standard, more complex
- **Artillery** (Node.js) — simpler alternative to k6

### What a k6 test looks like
```javascript
import http from 'k6/http';
import { check } from 'k6';

export const options = {
  vus: 100,          // 100 virtual users
  duration: '30s',   // for 30 seconds
};

export default function () {
  const res = http.get('http://localhost:5000/api/products?page=1&pageSize=20');
  check(res, { 'status is 200': (r) => r.status === 200 });
}
```

### Key metrics to measure
- **Throughput** — requests per second at target load
- **P50 / P95 / P99 latency** — median, 95th percentile, 99th percentile response time
- **Error rate** — % of requests that fail under load
- **Breaking point** — RPS at which error rate exceeds SLO

### What you need to learn
- How to install and run k6
- How to write realistic test scenarios (login → browse → add to cart → checkout, not just GET /products)
- How to read the output and identify the bottleneck
- How to run load tests in staging, never production
- How to correlate load test results with DB metrics (connection pool usage, query time)

---

## Suggested Learning Order

Go in this order — each one builds on the previous:

```
1. Domain Model          ← understand your business rules first
2. SLOs / Error Budgets  ← define what "working" means
3. Load Testing          ← measure your system's actual limits
4. Scalability Analysis  ← interpret what you measured
5. Zero-Downtime Deploy  ← ship without fear
6. Runbooks              ← operate without panic
7. Incident Response     ← respond without chaos
8. Dependency Risk       ← design for failure
9. API Versioning        ← evolve without breaking
10. GDPR / Data Retention ← protect your users and yourself
```

---

## Key Books & Resources

| Topic | Resource |
|-------|---------|
| Domain-Driven Design | *Domain-Driven Design* — Eric Evans ("the blue book") |
| DDD accessible | *Implementing Domain-Driven Design* — Vaughn Vernon |
| SRE / SLOs | *Site Reliability Engineering* — Google (free at sre.google/books) |
| Incident response | *The Phoenix Project* — Gene Kim (novel format, very readable) |
| System design | *Designing Data-Intensive Applications* — Martin Kleppmann |
| Load testing | k6 docs — k6.io/docs |
| GDPR practical | gdpr.eu/what-is-gdpr |

**Start here:** Site Reliability Engineering by Google — free online, invented most of these concepts.
