---
description: 'Enforces the CoreOne IResult pattern over throwing exceptions across all C# files in the project.'
applyTo: '**/*.cs'
---

# CoreOne `IResult` Coding Standard

Follow the `CoreOne.Results` and `CoreOne.Extensions` functional result pattern instead of throwing exceptions for control flow. Consult [../skills/coreone/SKILL.md](../skills/coreone/SKILL.md) and [../skills/coreone/references/results.md](../skills/coreone/references/results.md) for exact signatures.

## Mandatory Rules

1. **Return `IResult` Instead of Throwing**:
   - Never throw exceptions for validation failures, missing records, unauthorized checks, or expected domain/IO errors.
   - Return `IResult` (void outcome), `IResult<T>` (single value), `ICollectionResult<T>` (collections), or `HttpResult<T>` / `HttpResult<TModel, TError>` (HTTP calls).
   - Use `Result.Ok`, `Result.Fail("reason")`, `Result.Fail<T>("reason")`, or `new CollectionResult<T>(items)`.

2. **Wrap Throwing Calls in `Utility.Try` / `Utility.TryResult`**:
   - Avoid manual `try { ... } catch (Exception ex)` blocks when producing an `IResult`.
   - Use `Utility.Try(() => ...)` for actions/values and `Utility.TryResult(() => ...)` when calling a delegate that already returns `IResult<T>`.
   - Note: `Utility.Try<T>` treats a `null` return value as `ResultType.Fail`. If `null` is a valid success state, return `new Result<T?>(null, ResultType.Success)` explicitly.

3. **Compose Pipelines Fluently (`CoreOne.Extensions`)**:
   - Chain transformations with `.Select(...)` / `.SelectAsync(...)`.
   - Chain nested `IResult<T>` calls with `.SelectResult(...)` / `.SelectResultAsync(...)` to prevent nested `IResult<IResult<T>>`.
   - Validate preconditions with `.Where(predicate, "Error message")` / `.WhereAsync(...)`.
   - Run side effects with `.OnSuccess(...)` / `.OnSuccessAsync(...)` and `.OnFailAsync(...)`.
   - Recover from failures with `.OnFailSelectAsync(...)` or `.OnFailSelectResultAsync(...)`.

4. **Unwrap Only at Application Boundaries**:
   - Keep `IResult<T>` intact through repositories, domain services, and application handlers.
   - Unwrap at the edge (API controllers, Minimal API endpoints, UI/Blazor handlers) using `.Match(...)` or `.MatchAsync(...)`.
