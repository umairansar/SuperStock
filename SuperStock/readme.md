# Inventory Counter Service

A concise, implementation-ready checklist describing **what** to use, **how** the parts fit, and **what to watch**—without code.

---

## Testing
Install Siege

`brew install siege`

To simulate 5 concurrent users hitting this endpoint repeatedly for 2 seconds.

`siege -c5 -t2s -b 'http://localhost:5059/api/v1/Endpoints'`

## 1) Purpose & Scope

* Provide a high-throughput in-memory inventory counter per SKU.
* 98% of requests **decrement** (purchase/reserve). ~2% **increment** (refund/restore).
* Periodically **sync** changes to a database (write-behind deltas).
* Emit an **event** only when stock is **incremented by a user of type A**.
* No explicit locks on the hot path.

Out of scope (this doc): business rules like pricing, reservations expiry, partial shipments.

---

## 2) Building Blocks (What to Use)

* **Atomic ops:** `Interlocked` (CompareExchange, Add, Increment, Exchange).
* **Visibility:** `Volatile.Read/Write` for reading/writing the shared counters and flags.
* **Per-SKU map:** `ConcurrentDictionary<string, CounterState>` (or sharded dictionaries for very high contention).
* **Periodic work:** `PeriodicTimer` (or a hosted background loop) for DB syncing and optional periodic rebases.
* **Event stream:** `System.Threading.Channels` for publish/subscribe fan-out; **bounded** channel recommended for backpressure.
* **One-shot signal:** `ManualResetEventSlim` for "first Type‑A increment" notifications (optional admin/test hook).
* **Persistence model:** DB layer capable of delta writes and conditional updates (see Multi-Instance strategy).
* **Durability (optional but recommended):** lightweight write-ahead log (WAL) for deltas before applying in memory.

---

## 3) Data Model & Invariants (Per SKU)

**State fields** (no code – conceptual):

* `current` (long): latest in-memory count.
* `dirtyDelta` (long): net change since last successful DB sync.
* `version` (long): monotonic change counter for ordering & observability.
* `max` (long): optional upper bound (cap on increments/refunds).
* Optional `baseFromDb` + `deltaSinceBase` (if using epoch/rebase model).

**Invariants**

* `0 ≤ current ≤ max` (if `max` enforced).
* Decrement is rejected if it would make `current < 0`.
* Every successful mutation must update `dirtyDelta` and `version` atomically with the change.

---

## 4) Hot Path Operations (Lock-Free Policy)

* **Decrement (98% case):**

  * Compute if `current - n < 0` → **reject**.
  * On success, apply change atomically and adjust `dirtyDelta` by `-n`; bump `version`.
* **Increment (refund, 2% case):**

  * Add `n` with an optional cap at `max` (excess ignored or handled by policy).
  * On success, adjust `dirtyDelta` by `+n`; bump `version`.
  * **Publish event** only when the requester is **UserType A**.

**Notes**

* Reads use `Volatile.Read` for up-to-date values.
* Keep the mutation section minimal to reduce contention.

---

## 5) Syncing to DB While Serving Requests

**Recommended (Single Instance): Write-Behind Deltas**

* Continue serving requests during sync; do **not** queue or block.
* On each period tick:

  * Atomically capture `dirtyDelta` and reset it to 0.
  * Persist the captured delta along with telemetry (`sku`, `current`, `version`).
  * On failure, **add the captured delta back** and retry on next tick.

**Optional: Epoch/Rebase (Consistent Refresh)**

* Maintain `baseFromDb` and `deltaSinceBase` (so `current = base + delta`).
* During a refresh: snapshot & zero the delta buffer, load new base from DB, then re-apply the snapshot to the new base or persist it first.
* Readers and writers never pause; state remains consistent.

**Queue During Full Replace (Least Preferred)**

* Momentarily enqueue updates while performing a full state replacement. Adds latency and complexity; avoid unless necessary for a rare snapshot scenario.

---

## 6) Reconciling New vs Old Counter

* **With write-behind:** periodically compare DB value to `current`. If drift is detected (should be zero in single-writer designs), apply a **correction delta** and include it in the next DB sync so both converge.
* **With epoch/rebase:** reconciliation is part of the rebase process (no drift if the old delta is applied properly).

---

## 7) Multi-Instance / Cross-Process Strategy (Choose One)

If more than one process can mutate a SKU, **local in-memory atomicity is not sufficient**:

1. **DB Conditional Updates (Recommended)**

   * Decrement: conditional SQL/command that only succeeds if `qty ≥ n`.
   * Increment: bounded increment (respecting `max`).
   * Apply local mirror only after DB confirms success; on DB reject, return failure without local mutation.

2. **Distributed Atomic Store** (e.g., Redis Lua/CAS)

   * Perform atomic decrement/increment with bounds in the store; mirror result locally.

3. **Single Writer per Key via Queue/Shard**

   * Route all mutations for a given SKU to a single partition/consumer which holds the authoritative in-memory counter.

**Rule of thumb:** If not guaranteed single-writer-per-SKU, use 1) or 2).

---

## 8) Event Stream Semantics

* **Trigger condition:** publish only on **increments** and only when the **user type = A**.
* **Transport:** process-local `Channel` with **bounded** capacity for backpressure.
* **Ordering:** include `sku` and `version`; consumers order by `(sku, version)`.
* **One-shot signal:** `ManualResetEventSlim` can be used for "first Type-A increment" observers (testing/admin). Reset to re-arm.
* **Backpressure policy:** choose one — block producer, drop oldest, or spill to durable queue (log and alert on drops).

---

## 9) Failure Modes & Mitigations

* **Local decrement succeeded but DB already 0 (oversell):**

  * Happens in multi-instance without conditional DB updates.
  * **Mitigate:** push atomicity to DB/Redis. On DB reject (no rows updated), do **not** mutate locally and fail the request.

* **DB sync failure loses a captured delta:**

  * **Mitigate:** re-credit the delta upon failure; ideally use a small **WAL** to durably record deltas before applying.

* **Event stream overload:**

  * **Mitigate:** bounded channel + clear drop/block policy; publish fewer events (filter to Type A only).

* **Negative counts due to bugs:**

  * **Mitigate:** guard decrements with bounds; invariant checker in the sync loop; alert if `current < 0` or `current > max`.

* **Concurrent refresh races:**

  * **Mitigate:** single refresh worker per SKU or a per-SKU guard flag to allow only one rebase at a time.

---

## 10) Configuration Knobs

* Sync period (e.g., 250–1000 ms).
* Max per-op decrement/increment.
* Global `max` per SKU (cap) and policy when cap reached (ignore extra, queue, or reject).
* Channel capacity and backpressure policy for events.
* Rebase cadence (e.g., every N minutes or on admin signal).
* Timeouts/retries for DB operations; jitter/backoff settings.

---

## 11) Observability & Alerts

**Metrics**

* Successful decrements/increments; rejected decrements (insufficient stock).
* `dirtyDelta` backlog; sync success/fail counts; retry counts.
* Event publishes vs drops; channel occupancy.
* Max CAS retries per op (optional diagnostic); average/percentile op latency.
* Current vs DB divergence (absolute and percentage).

**Logs**

* Sync attempt outcomes; correction deltas applied; rebase events.

**Alerts**

* Repeated sync failures.
* Non-zero divergence beyond threshold.
* Event drops > threshold over window.
* Invariant breaches (`current < 0`, `current > max`).

---

## 12) Testing Plan (No Code)

* **Unit:** bounds enforcement, delta accounting, version monotonicity, event filtering (only Type A increments).
* **Property-based:** a stream of random ops never yields negative `current` and total equals initial + net applied deltas.
* **Soak/Load:** sustained 100–1000+ ops/s; monitor contention metrics and latency.
* **Chaos:** inject DB failures during sync; verify delta re-credit and eventual consistency.
* **Multi-instance (if applicable):** simulate concurrent decrements; confirm DB conditional updates prevent oversell.
* **Rebase:** continuous traffic during rebase shows no lost updates; `current` remains stable (modulo in-flight ops).

---

## 13) Operational Runbook

* **Startup:**

  * Optionally replay WAL to reconstruct `current` & `dirtyDelta`.
  * Start periodic sync and event consumers.

* **Shutdown:**

  * Stop intake; flush last `dirtyDelta`; close channels; persist final state.

* **Deploy/Rollback:**

  * Use rolling or blue/green. If multi-instance with DB conditional ops, deploy order is flexible; ensure schema is ready first.

---

## 14) Acceptance Criteria

* Hot path free of explicit locks; ops succeed under 100+ consecutive requests without stalls.
* No negative inventory; decrements rejected when insufficient.
* DB sync eventual consistency within configured SLO (e.g., ≤ 2 sync periods).
* Event stream publishes only on increments by Type A; zero false positives.
* Metrics and alerts wired and tested.

---

## 15) Quick Decision Matrix

| Constraint                       | Strategy                                                                             |
| -------------------------------- | ------------------------------------------------------------------------------------ |
| Single writer per SKU            | In-proc atomics + write-behind deltas; optional epoch/rebase.                        |
| Many writers per SKU             | DB conditional updates or distributed atomic store; local mirror only after success. |
| Require periodic full refresh    | Epoch/rebase without pausing hot path.                                               |
| Must notify only on refunds by A | Filter at publish; bounded channel; include `version` for ordering.                  |
| Avoid data loss on crash         | Add WAL for deltas and replay at startup.                                            |

---

### Glossary

* **CAS**: Compare-and-swap (atomic read-modify-write using `Interlocked.CompareExchange`).
* **Rebase/Epoch**: Technique separating DB base value from live in-memory deltas.
* **WAL**: Write-Ahead Log used for crash recovery.
* **Backpressure**: Mechanism to slow or drop producers when consumers can’t keep up.

## 16) Downward Correction Handling (DB → Cache)

**Scenario:** DB emits a correction for a SKU (actual stock is **lower** than your cache). You run a **single instance** today, serve requests in parallel, and will add multiple instances later.

### Recommended defaults

* Keep serving requests; apply the correction as an **authoritative in‑memory adjustment** (like a large decrement) using the same atomic CAS pattern.
* **Do not** add the admin correction to `dirtyDelta` (it already happened in DB).
* Use **optimistic concurrency** when flushing to DB (`version`/ETag).

### Option A — Non‑blocking CAS correction (fast, tiny slip allowed)

1. Receive `{sku, correctedQty=T, dbVersion}`.
2. Atomically set `current → T` via CAS; concurrent decrements may win/lose during a very small window.
3. Leave `dirtyDelta` unchanged (only reflects your accepted requests).
4. Set `lastSeenDbVersion = dbVersion` for the next flush.

**When to choose:** admin corrections are rare; sub‑millisecond slip is acceptable.

### Option B — Brief decrement gate (strict, still low‑latency)

1. Close a tiny **decrement gate** (e.g., `volatile bool correctionInProgress` or a `ManualResetEventSlim`), affecting **decrements only**.
2. CAS `current → T`.
3. Do **not** touch `dirtyDelta`; set `lastSeenDbVersion`.
4. Open the gate.

**Decrement behavior while gated:** either (a) wait a few ms then proceed, or (b) fail fast with “retry”. Increments/refunds may bypass the gate.

### Flush & rebase after a correction

* **Snapshot flush:** `UPDATE … SET qty=@current, version=version+1 WHERE version=@lastSeenDbVersion`.
* **Delta flush:** `UPDATE … SET qty=qty+@dirtyDelta, version=version+1 WHERE version=@lastSeenDbVersion`; then zero `dirtyDelta`.
* **On 0 rows updated (conflict):**

  1. Read `{dbQty, dbVersion}`.
  2. Treat as a new correction: set `current = dbQty` atomically (do not fold into `dirtyDelta`).
  3. Retry flush against the fresh `dbVersion` (snapshot or delta). Your `dirtyDelta` remains the set of locally accepted ops since the last success.

### Why corrections aren’t added to `dirtyDelta`

`dirtyDelta` should represent **only** this instance’s accepted operations that still need persistence. Admin/db corrections already exist in DB; adding them to `dirtyDelta` would double‑apply or undo them on the next flush.

### Behavior of new requests during correction

* **Option A:** requests continue; a few decrements may slip through on the old value during CAS races—window is tiny and still bounded by `current ≥ 0` guards.
* **Option B:** decrements wait or fail fast while the gate is closed, so none are accepted against stale stock.
* **Increments** can pass either way; they reduce oversell risk.

### Safeguards & edge cases

* **No negatives:** decrement path **must** reject `current − n < 0` before CAS.
* **Crash between correction and flush:** safe—the DB already holds the corrected value; on restart you rebuild from DB.
* **Visibility:** use `Volatile.Read` for readers; they see either old or new `current`, never torn.
* **Metrics:** count corrections applied, gate hold time, conflicts on flush, and any “retry later” responses while gated.

### Looking ahead to multi‑instance

* Keep the same local handling for speed, but move atomicity to a shared authority:

  * **DB conditional updates** (qty bounds + version) **or** a **distributed CAS** (e.g., Redis Lua).
* Keep the decrement gate pattern for local strictness; rely on DB/version checks so no node overwrites another node’s correction.
