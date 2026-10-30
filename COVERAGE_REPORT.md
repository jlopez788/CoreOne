# CoreOne: Meaningful Code Coverage Gaps

**Measured:** October 29, 2026

**Result:** 930 passed, 0 failed, 0 skipped

**Execution:** NUnit/VSTest, Debug, .NET 10 test host, Coverlet 6.0.4

**Coverage scope:** `CoreOne.dll` only

## Executive summary

The highest-value additions are behavioral tests for:

1. OData query construction and advanced operators.
2. Result pipelines: short-circuiting, recovery, exception handling, and cursor metadata.
3. Semaphore ownership and queue completion under cancellation and contention.
4. Reflection-based invocation, conversion, and mapping.
5. Backing-field update ordering and cancellation.

Do not prioritize getters, constructors, identity/pass-through checks, static constants, or generated code merely to raise the headline percentage. Some high-line-coverage components still have important untested contracts.

No production code or tests were changed for this assessment. The run includes the existing changes in [UtilityJsonTests.cs](Tests/UtilityJsonTests.cs), which were uncommitted when measurement began. The existing test project also runs generator tests, but generator coverage is excluded. No Identity project tests were run or added.

## Measured baseline and limitations

### Raw collector totals

These are the totals recorded at the root of the Cobertura document:

| Metric | Covered / total | Coverage |
|---|---:|---:|
| Lines | 4,149 / 6,972 | 59.51% |
| Branches | 1,700 / 3,452 | 49.25% |

### Hand-written source view

The HTML report excludes `*RegexGenerator.g.cs`. Its source-level counts are:

| Metric | Covered / total | Coverage |
|---|---:|---:|
| Lines | 3,821 / 6,413 | 59.58% |
| Branches | 1,583 / 3,198 | 49.50% |
| Methods reached at least once | 815 / 1,396 | 58.38% |
| Methods with all instrumented lines reached | 743 / 1,396 | 53.22% |

Percentages here are calculated from the counts and rounded to two decimal places. ReportGenerator 5.5.1 truncates its displayed percentages to one decimal place, so the HTML summary shows 59.5% lines and 49.4% branches.

Important qualifications:

- Raw collector totals and ReportGenerator's source-level totals use different aggregation. In particular, do not interpret the branch-count difference as only generated-code removal. Compare subsequent runs using the same scope and reporting convention.
- Method coverage means execution, not correctness. Full line coverage of a method does not imply full branch coverage.
- Only the `net10.0` implementation was measured. This does not establish coverage of `net9.0`, `netstandard2.0`, or `netstandard2.1` conditional implementations.
- Debug-only behavior matters in [LoadingStore.cs](src/CoreOne/Services/LoadingStore.cs): its exception path can break into a debugger and invoke the callback again. Release error handling needs separate validation with symbols enabled.
- Rankings below combine measured gaps with source inspection. They are not production usage measurements or confirmed defect reports.
- File-level figures combine all types declared in that file. For example, the generic and non-generic subjects have very different coverage.

## Ranked backlog

Percentages in this table are rounded to one decimal place. Missing lines are opportunities, not targets to fill indiscriminately.

| Rank | Service / logic | Line coverage | Branch coverage | Uncovered lines | Why prioritize it |
|---|---|---:|---:|---:|---|
| 1 | [ODataBuilders](src/CoreOne/ODataBuilders) | 48.6% | 26.2% | 331 | Nested operators, parsing, type dispatch, query composition, and mutable builder state can silently change which records are returned. |
| 2 | [ResultExtensions](src/CoreOne/Extensions/ResultExtensions.cs) + [CursorResultExtensions](src/CoreOne/Extensions/CursorResultExtensions.cs) | 3.5% | 3.6% | 413 | Failure propagation, callbacks, recovery, mapping, and pagination metadata are almost entirely unexercised. |
| 3 | [SemaphoneSlimExtensions](src/CoreOne/Extensions/SemaphoneSlimExtensions.cs) + [AsyncTaskQueue](src/CoreOne/AsyncTaskQueue.cs) | 28.8% / 89.4% | 37.5% / 70.0% | 42 / 7 | Lock ownership, completion signals, and cancellation can fail despite a high line score. Queue `WhenAll` is entirely unexecuted. |
| 4 | [TypeUtility](src/CoreOne/Reflection/TypeUtility.cs), [InvokeCallback](src/CoreOne/Threading/Tasks/InvokeCallback.cs), [SafeTask](src/CoreOne/Threading/Tasks/SafeTask.cs) | 0.8% / 25.0% / 0.0% | 0.0% / 20.2% / 0.0% | 124 / 78 / 23 | Runtime constructor selection, argument conversion, nullability checks, and task adaptation have substantial branching. |
| 5 | [BackingField](src/CoreOne/Models/BackingField.cs) | 41.8% | 37.1% | 71 | Change cancellation, comparison, previous-value tracking, notifications, and serialized async updates form a state machine. |
| 6 | [FileStore](src/CoreOne/Services/FileStore.cs) | 0.0% | 0.0% | 59 | Persistence combines file access, serialization, encryption, cancellation, and result chaining. |
| 7 | [DebounceTrigger](src/CoreOne/Services/DebounceTrigger.cs) + [LoadingStore](src/CoreOne/Services/LoadingStore.cs) | 0.0% / 57.6% | 0.0% / 50.0% | 44 / 28 | Leading/trailing execution, cancellation, disposal, overlapping work, and busy-state restoration are user-visible behavior. |
| 8 | [HttpClientExtensions](src/CoreOne/Extensions/HttpClientExtensions.cs) | 11.8% | 66.7% | 45 | HTTP status, transport failures, malformed payloads, and cancellation cross an external boundary. |
| 9 | [CircularArray](src/CoreOne/Collections/CircularArray.cs) | 0.0% | 0.0% | 61 | Wrap-around indexing, removal, and enumeration need algorithmic invariants, not constructor tests. |
| 10 | Non-generic [Subject](src/CoreOne/Reactive/Subject.cs) | 0.0% for this type | 0.0% for this type | 32 in the file | The generic subject is well covered; the separate non-generic publication and cancellation implementation is not. |

### 1. OData: assert the generated query, not just serialization

Measured hotspots:

| Source | Covered lines | Covered branches |
|---|---:|---:|
| [ODataOperator.cs](src/CoreOne/ODataBuilders/ODataOperator.cs) | 92 / 205 | 21 / 134 |
| [ODataBuilder.cs](src/CoreOne/ODataBuilders/ODataBuilder.cs), including filter segments | 108 / 219 | 30 / 88 |
| [ODataPageRequestBuilder.cs](src/CoreOne/ODataBuilders/ODataPageRequestBuilder.cs) | 50 / 90 | 22 / 50 |
| [AdvancedSearchContext.cs](src/CoreOne/ODataBuilders/AdvancedSearchContext.cs) | 10 / 54 | 0 / 30 |

Start with these scenarios:

- Numeric/date `Between`: both endpoints, either endpoint absent, and invalid input placement. Assert the exact bounds and AND/OR grouping.
- String `Between`: assert a non-empty, correctly grouped prefix-range filter. Source inspection shows a segment is built in this path without being returned; test the intended contract rather than blessing a null result.
- `InArray`: empty, single, and multiple values; supported collection shapes; correct `eq` versus `in` syntax.
- String quoting/escaping, booleans, nullable values, dates, and culture-independent numeric formatting. Check exact query fragments against the intended OData contract.
- Global search across mixed field types versus field-specific filters; enum/nullable-enum handling; parsing failure; custom handlers and field interceptors.
- Copy and merge behavior: independent subsequent changes, selected fields, conflicting parameters, and whether merging is intended to mutate the source builder.
- Render a query, remove its filters, and render again. Assert removed filters do not survive in cached parameters.

Existing request/response round-trip tests are useful, but they do not prove that advanced operators generate correct query strings.

### 2. Result pipelines: verify routing and metadata

[ResultExtensionsTests.cs](Tests/Extensions/ResultExtensionsTests.cs) currently exercises a small set of `Select`/`SelectAsync` paths. Extend it around semantic families rather than writing one trivial test per overload.

Use a compact matrix:

| Input / event | Observable assertion |
|---|---|
| Success with a model | The selected callback runs exactly once and produces the expected output. |
| Failure or exception result | Success-only callbacks do not run; status and message survive. |
| Success with a null reference model | The appropriate fallback/recovery path runs; no invalid dereference occurs. |
| Callback throws or returns a faulted task | The documented exception-to-result or exception propagation behavior is preserved. |
| Recovery succeeds or fails | The resulting model, status, and message describe the recovery outcome. |
| Predicate rejects a model | Failure status and error-message precedence are correct. |
| Cursor mapping succeeds or fails | Item mapping, next cursor, message, and result type follow the intended contract. |

Focus first on `Where`/`WhereAsync`, `OnFailSelect*`, `SelectResult*`, and cursor transforms. Check sync/async parity only where different implementations make it valuable. Some overloads capture callback exceptions while others propagate them; decide the desired contract before encoding that distinction.

### 3. Async coordination: small gaps with high consequence

For [SemaphoneSlimExtensions.cs](src/CoreOne/Extensions/SemaphoneSlimExtensions.cs):

- Cancel before acquiring a semaphore whose only permit is held elsewhere.
- Assert the callback never starts and the cancelled waiter does not release another caller's permit.
- Throw inside the protected callback; assert exactly one release and that the next waiter can proceed.
- Check finalizer-callback behavior on success, callback failure, and acquisition cancellation.
- Exercise both sync and async core implementations, not every forwarding wrapper.

The current implementations call `Release` in `finally` even if acquisition fails. A cancellation/ownership test is more valuable here than covering another successful wrapper.

For [AsyncTaskQueue.cs](src/CoreOne/AsyncTaskQueue.cs):

- `WhenAll` on an initially empty queue.
- `WhenAll` with one blocked worker and multiple queued workers: it must neither finish early nor remain incomplete after all accepted work settles.
- Enqueue additional work while a drain is in progress, then reuse the queue after it becomes idle.
- Combine worker failure/cancellation with later successful work and verify the drain completion contract.
- Establish the intended meaning of the `concurrency` argument; measure maximum simultaneously active callbacks with synchronization barriers.

The existing concurrency test uses a broad start-time window. That is not strong evidence that the configured parallelism is actually honored.

### 4. Reflection and task adaptation

- [TypeUtility.cs](src/CoreOne/Reflection/TypeUtility.cs): constructor activation for zero through three arguments, cache isolation by argument signature, missing constructors, inherited/nested property expressions, converted expressions, and invalid expression shapes.
- Mapping: copy actual data across matching properties, nested types, incompatible assignments, and setters that throw. Assert target contents and error semantics, not merely non-null results.
- [InvokeCallback.cs](src/CoreOne/Threading/Tasks/InvokeCallback.cs): ordinary delegates as well as custom delegate types, static versus instance invocation, argument merging, insufficient arguments, non-nullable parameters, and implicit conversions declared on either the input or target type.
- [SafeTask.cs](src/CoreOne/Threading/Tasks/SafeTask.cs): plain values, `Task`, `Task<T>`, faults, and cancellation. Assert completion state and original failure semantics, not just that awaiting returns.

### 5. Backing-field state transitions

For [BackingField.cs](src/CoreOne/Models/BackingField.cs):

- Cancel from a changing subscriber and from the async pre-change callback.
- Assert value, previous value, change flags, and event sequence remain coherent after cancellation.
- Exercise custom comparers, reference comparison, initial assignment, equal updates, and ignored nulls.
- Block one async update, start another, and prove the second rechecks state after acquiring the semaphore.
- Throw during pre-change work; verify later updates can still proceed.

### 6. Persistence and stream boundaries

For [FileStore.cs](src/CoreOne/Services/FileStore.cs), use a unique temporary directory and injected serializer/cypher implementations:

- Save/load a nontrivial model and verify its contents.
- Missing file, truncated or malformed content, decryption failure, serialization failure, and an incompatible deserialized type.
- Verify failed decryption prevents deserialization and failed serialization prevents a false success result.
- Cancellation during async reads/writes; validate the result contract and file state, not just whether an exception was thrown.
- Compare sync and async load semantics and explicit-path versus configured-path behavior.

Related follow-up: [StreamExtensions.cs](src/CoreOne/Extensions/StreamExtensions.cs) is entirely uncovered. Test short reads and stream failures. [IOUtility.cs](src/CoreOne/IO/IOUtility.cs) is also uncovered; prioritize multi-buffer copy integrity and byte I/O failure semantics over directory-existence plumbing.

### 7-10. Next wave

- [DebounceTrigger.cs](src/CoreOne/Services/DebounceTrigger.cs): first call fires immediately; later calls collapse to the latest payload; superseded work receives cancellation; disposing cancels pending work.
- [LoadingStore.cs](src/CoreOne/Services/LoadingStore.cs): overlapping busy scopes, repeated scope disposal, cancellation/failure cleanup, and notifications. Keep the existing delayed-visibility tests; do not duplicate them.
- [HttpClientExtensions.cs](src/CoreOne/Extensions/HttpClientExtensions.cs): use an in-memory `HttpMessageHandler`, not live network services. Verify method/body/content type, HTTP failure metadata, malformed JSON, transport errors, and cancellation.
- [CircularArray.cs](src/CoreOne/Collections/CircularArray.cs): capacity-one and repeated-wrap behavior, removal at each logical position after wrapping, duplicates/nulls, index bounds, and enumeration order relative to indexing. Clarify the intended enumeration order before asserting it.
- Non-generic [Subject.cs](src/CoreOne/Reactive/Subject.cs): cancelled subscribers must not run; live subscribers run once per publication; adding a subscriber during publication must respect snapshot semantics.

## Lower-priority coverage

Do not start by filling out static constants, marker attributes, clocks that merely return a value, identity/equality boilerplate, or generated regex internals.

Do not infer that every zero-covered service is trivial: logging/interception and policy evaluation can merit contract tests when they are important to consumers. They rank below the algorithmic and stateful gaps above, not outside the testing strategy.

The existing [UtilityJsonTests.cs](Tests/UtilityJsonTests.cs) provides meaningful operation-state and service-shaped round trips. Hand-written [Utility.json.cs](src/CoreOne/Utility.json.cs) is already at 47/48 lines, or 97.9%. Its remaining uncovered line is in the stream-serialization exception handler; a throwing/non-writable stream is a useful focused addition, but more ordinary round trips are not the best next investment.

Likewise, avoid expanding the already well-covered delayed busy-state implementation merely to reach 100%; focus on [LoadingStore.cs](src/CoreOne/Services/LoadingStore.cs)'s distinct lifecycle paths.

## Suggested delivery order and acceptance criteria

1. Add OData operator/composition regression tests and semaphore/queue ownership-completion tests.
2. Add result-pipeline routing and metadata tests using a small behavioral matrix.
3. Add reflection invocation/conversion tests and backing-field transition tests.
4. Add persistence, HTTP boundary, and remaining timing/collection scenarios.

For each batch:

- Assert the externally observable contract, including negative assertions such as callbacks not running.
- Use `TaskCompletionSource`/barriers to arrange interleavings. Use timeouts as failure bounds, not loose timing-based evidence of correctness.
- Resolve surprising behavior instead of writing assertions that merely reproduce the implementation.
- Re-run the focused tests, then collect a comparable full baseline.
- Use local branch coverage as a diagnostic; do not make a blanket global percentage the definition of success.

## Reproducing this measurement

From the repository root in PowerShell:

```powershell
dotnet test .\Tests\Tests.csproj --no-restore `
    --collect 'XPlat Code Coverage' `
    --results-directory .\TestResults `
    --logger 'trx;LogFileName=coreone-tests.trx' `
    -- 'DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Include=[CoreOne]*' `
       'DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura'

reportgenerator `
    '-reports:.\TestResults\**\coverage.cobertura.xml' `
    '-targetdir:.\TestResults\CoverageReport' `
    '-reporttypes:Html;TextSummary;JsonSummary' `
    '-filefilters:-*RegexGenerator.g.cs'
```

Use a fresh results directory for each baseline so the report glob does not merge older runs. The measurement used the already installed SDK, dependencies, and ReportGenerator; no packages or tools were installed.
