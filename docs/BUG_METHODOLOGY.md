# Bug-Fix Methodology

## Principle

We cannot fix what we cannot see, reproduce, or understand.
Every bug fix must be driven by a failing test. Every fix must be validated by passing tests.

## Workflow

```
Reproduce → Understand → Test → Fix → Verify
```

### 1. Reproduce

Write the **minimal** Lightning script that triggers the bug.
Run it directly with the interpreter and observe:
- `Program returned ERROR!` (unhandled exception)
- `Program returned: <wrong value>` (wrong result)
- Wrong output in `_vm.log`

Always run from `lightning_programs/` so `require()` paths resolve correctly:
```sh
cd lightning_programs
../linux_builds/lightning_interpreter tests/my_test.ltn
```

Or use the provided test runner:
```sh
./run_test.sh
```

### 2. Understand

Read `lightning_programs/_vm.log` after a crash. It contains:
- The exception type and stack trace
- The Lightning function/module where it happened
- Position data (line, column)

The log is the primary diagnostic tool. Never skip it.

### 3. Test

Before writing any fix:
- Add the minimal reproducer as a new assert in the relevant `tests/*.ltn` file.
- Add **varieties**: edge cases, boundary conditions, related patterns that should also work.
- Run `run_test.sh` and confirm the new asserts **fail** (proving the test is live).
- Do NOT change an assertion to match broken behavior. If it fails, the code is wrong.

Test file conventions:
- Each test file returns `assert_counter.get_error_counter()`.
- Each assert includes a descriptive message string.
- Group related cases in `{}` blocks with a comment header.

### 4. Fix

Implement the fix. Keep it focused — do not refactor unrelated code.
Read the VM log to understand the failure mode before writing any code.

### 5. Verify

- Run `run_test.sh` → all tests must pass.
- The new tests from step 3 must now pass.
- No regressions in other test files.
- If fixing a crash: confirm `Program returned: <correct value>`, no ERROR.

## Known Bugs (tracked)

| # | Description | Test file | Status |
|---|-------------|-----------|--------|
| 1 | Upvalue chaining: innermost closure crashes when capturing from 2+ levels up | `tests/up_value.ltn`, `tests/compound_calls.ltn` | Failing test added, fix pending |
| 2 | Concurrency false contention: value-type globals share TypeUnit singleton as lock object | `tests/lock.ltn` | Failing test pending |
| 3 | Roslyn: `linux_builds/refs/` not set up — runtime assembly resolution fails | `tests/roslyn.ltn` | Environment setup pending |

## Logs

- `lightning_programs/_vm.log` — VM runtime log (exceptions, errors)
- `lightning_programs/_try.log` — try/catch log
- `lightning_programs/_parser.log` — parse errors (if any)

Always check the log after a failure before making any code change.
