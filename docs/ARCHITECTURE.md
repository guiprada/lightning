# Lightning Architecture

Lightning is a stack-based bytecode VM language implemented in C#. This document covers the major design decisions and their rationale.

---

## The Unit Struct

The central data type is `Unit` — an explicit-layout struct that is both a value type and a heap reference.

```csharp
[StructLayout(LayoutKind.Explicit)]
public struct Unit
{
    [FieldOffset(0)] public Float floatValue;
    [FieldOffset(0)] public Integer integerValue;
    [FieldOffset(0)] public char charValue;
    [FieldOffset(0)] public bool boolValue;
    [FieldOffset(4)] public HeapUnit heapUnitValue;
}
```

The four primitive fields **overlap at offset 0** (a C union). `heapUnitValue` sits at offset 4 (offset 8 with `#if DOUBLE`). For heap types (Table, Function, Closure, String...), `heapUnitValue` points to the live object. For value types (Float, Integer, Bool, Char), `heapUnitValue` is **never null** — it points to a static `TypeUnit` sentinel singleton (`TypeUnit.Float`, `TypeUnit.Integer`, etc.).

This means:
- `unit.Type` is always one pointer dereference: `unit.heapUnitValue.Type`
- `lock(unit.heapUnitValue)` is always valid — no null-check needed
- The layout ports directly to a C union for a future C VM

The tradeoff: all Floats in global scope share the same lock object (`TypeUnit.Float`), which causes false contention in parallel code. The planned fix is lock striping: `object[] lockStripes = new object[32]`, indexed by global slot address (`lockStripes[address & 31]`). This is a performance improvement, not a crash fix.

---

## Unified Table Model

There is a single heap collection type: **TableUnit**. It has two storage channels:

```csharp
public class TableUnit : HeapUnit
{
    public List<Unit> Elements { get; }      // positional, integer-indexed
    public Dictionary<Unit, Unit> Map { get; } // keyed entries
}
```

**Two constructors:**
- `TableUnit(Dictionary<Unit,Unit> map)` — map-only table, `Elements` is null. Created by `[key: val, ...]` literals.
- `TableUnit(List<Unit> elements, Dictionary<Unit,Unit> map)` — list-mode table, `Elements` is initialized. Created by `[val1, val2, ...]` literals and `List(n)` / `ListInit(n, default)`.

**`Get` priority:** integer key → Elements first (if non-null and in-range) → Map → ExtensionTable → methodTable.

**`Set` with integer key:** if Elements is non-null and index ≤ Elements.Count, writes/appends to Elements; otherwise falls through to Map.

All former `ListUnit` methods (`push`, `pop`, `remove`, `split`, `slice`, `reverse`, `sort`, `shuffle`, `map`, `pmap`, `rmap`, `reduce`, `init`, `list_iterator`, `index_iterator`, `list_to_string`) are now entries in `TableUnit.methodTable`.

`UnitType.List` no longer exists. Everything is `UnitType.Table`.

---

## Stack-Based VM

The VM (`VM.cs`) executes a flat list of `Instruction` structs (opcode + up to 3 operands: `opA`, `opB`, `opC`). There are ~54 opcodes.

Key dispatch cases:
- `NEW_TABLE` — pops N key/value pairs from stack, builds a map-only TableUnit
- `NEW_LIST` — pops N values from stack, builds a list-mode TableUnit
- `CALL` / `CALL_INTRINSIC` / `CALL_EXTERNAL` — dispatches to FunctionUnit, IntrinsicUnit, or ExternalFunctionUnit
- `LOAD_GLOBAL` / `ASSIGN_GLOBAL` — access the global slot array
- `LOAD_IMPORTED_GLOBAL` / `ASSIGN_IMPORTED_GLOBAL` — access a module's global slot array (post-relocation)

The stack is a typed array of `Unit` values.

---

## Module System (Linker)

`MakeModule()` in `Prelude.cs` implements **bytecode relocation** — it rewrites operands in a compiled function's bytecode so that `LOAD_GLOBAL` instructions targeting the imported VM's slot indices are rewritten to `LOAD_IMPORTED_GLOBAL` instructions targeting the importing VM's module table.

This is a full static linker, not a dynamic lookup:
- All `LOAD_GLOBAL N` targeting exported symbols → `LOAD_IMPORTED_GLOBAL idx, module_slot`
- Transitive imports are handled recursively
- Module globals are appended into a `ModuleUnit.Globals` list; the module is registered by index

The result: after `require("foo.ltn")`, calls into `foo`'s functions pay zero overhead for name resolution at runtime — it's a direct slot index.

---

## Concurrency

`tasks(n, func, args)` parallelizes over `n` workers using `Parallel.For`. The VM pool pattern:

```
1. Pre-allocate n VMs from pool (Stack<VM>) before Parallel.For
2. Each Parallel.For iteration gets its own pre-allocated VM — no lock on the pool during execution
3. After Parallel.For completes, recycle all n VMs back to pool
```

The `Stack<VM>` pool is never accessed concurrently. Parallel correctness depends on:
- Tasks not sharing mutable state (or the user ensuring external synchronization)
- The `args` Unit being treated as read-only (no enforcement; documented design constraint)

**Global variable locking in parallel mode:** When `parallelVM == true`, the VM wraps global reads/writes in `lock(heapUnitValue)`. Since `heapUnitValue` is never null (TypeUnit sentinel), this never crashes. The false contention issue (all Floats share `TypeUnit.Float` as lock object) is a performance bottleneck at high parallelism, to be addressed with lock striping.

---

## Roslyn Integration (`#if ROSLYN`)

`roslyn.compile(name, arity, body)` compiles a C# string at runtime using Roslyn (`CSharpCompilation`) and loads the resulting assembly into the process. It returns a Lightning-callable `ExternalFunctionUnit` that wraps the compiled `MethodInfo`.

`roslyn.csharpscript_compile(name, arity, body)` uses `CSharpScript.EvaluateAsync` to compile and evaluate a `Func<VM, Unit>` expression directly.

Both approaches give Lightning programs access to arbitrary .NET APIs at runtime without restarting the interpreter.

---

## Planned: CIL Backend

Phase 3 target: emit native CIL using `System.Reflection.Emit.ILGenerator` directly (not `System.Linq.Expressions` or the DLR). Rationale: `ILGenerator` maps 1:1 to CIL opcodes, avoids intermediate AST layers, and gives full control over calling conventions. Since Lightning's opcodes are close to CIL in structure (stack-based, typed operations), a direct translation is tractable.

Initial target: compile hot functions on demand (JIT-style) by detecting call frequency at the interpreter level. The compiled `DynamicMethod` replaces the interpreted dispatch for that function.

---

## Planned: WASM and C VM

Phase 3 also targets WebAssembly via Blazor or direct binary emission. Phase 4 is a C implementation of the VM, which is natural given the Unit struct's union layout ports directly to `union { double f; int64_t i; bool b; }` in C.

---

## Roadmap Summary

| Phase | Goal |
|-------|------|
| 1 | Stabilize: unified table model ✓, bytecode serialization (.ltnc), better error messages |
| 2 | Self-hosted compiler: parser and compiler written in Lightning itself |
| 3 | CIL JIT backend, WASM target |
| 4 | C VM: portable runtime from the bytecode format |
