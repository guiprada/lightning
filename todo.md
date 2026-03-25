todo
====
Parser error syncCurrently zero error recovery — Error() just appends to a list and parsing continues blindly. "Error sync" would mean: on a bad token, skip until a safe resumption point (next newline, next var/fun/}) so the parser can report multiple real errors in one pass instead of cascading garbage. Not critical yet — defer to after language stabilization.
-> this is cool, lets create a plan in todo.md

This is a complete, coherent model. mut is not needed — the default is mutable, const opts into immutability. That's settled.
-> I see it now, we should have gone with mut as the function parameter marking. Even this would cause some cognitive model breakage. 
      -> We should consider choosing one model and sticking with it.
          ->(Transparent) -> All ref aliasing and passing is transformed in const, almost always, we might need a returning non const from a closing env should to be the exception.
          ->(Hand holding) -> (this is what we have, but need to fix the mut argument) All functions that need mutation should mark their arguments with 'mut'. Compiler detects when functions are not marked and mutates and errors on it, and VM enforces it at runtime it receives no const parameters. All ref aliasing and passing is const, in that sense, our const() in userspace is pointless. To break the hatch we are going to clone. We are going to need an optimal deep clone function.

test imported constsWhen you require() a module, you get back a plain Table. Any const variables declared inside the module are not const from the caller's perspective — the caller can rebind module.someConst = ... and nothing stops it. The question is: should require() return a frozen/const table? Or should the exporting module mark exported bindings as const? This needs a design decision before self-hosting — it's a module system semantic
-> yeah, this is general table semantics. not only required ones. We would have to mark the index as const for this to work. It is a form of aliasing after all. How do we deald with this?

try / exceptions / builtinCurrently: try is a C# intrinsic (Prelude.cs:216) — it spins up a sandbox VM, calls a function inside a C# try/catch, and returns a ResultUnit. It is NOT a keyword, NOT a VM opcode.
-> Ok, pretty sure i optimized this, but ... is not this crazy expensive and wastefull?

* "Remove exceptions" → the C# throw/catch path in the VM should never surface to the user; all errors should become ResultUnit
-> sure, how easy is that?

* "Convert try to builtin(parsed)" → already done conceptually, but "parsed" means it could get nicer call syntax (e.g. try func(args) vs try(func, args)) if you add it to the grammar
-> I see, looks cool, breaks syntactic minimalism coolness ;) - Worth it?


* No true exceptions in the language — ResultUnit is the error type. try just makes the sandbox. This is settled and good.
-> Cool right ;)

Constructors / Option / const
The const on constructor arguments (var const t = Table()) sets PROTECTION_CONST on the reference slot, but there's no constructor-level const enforcement. The open questions from todo.md:
* "Constructors in camelCase" — you have mixed: Table(), List() are PascalCase intrinsics
-> What are the other names?
* "Constructors don't create option" — currently Table() returns a TableUnit directly, not Option<TableUnit>. This was a design choice (fail fast vs option-returning)
-> Seems reasonable, is there any option returning one? Do you think we should do it?
* const + constructors: var const x = Table() works. fun f(const t) works. What doesn't work is imported const (above)
-> we have to fix that

Roadmap
-------
Phase 1 - Stabilize the language (current)
  - Unify TableUnit — DONE:
    - ListUnit merged into TableUnit (Elements + Map in one class)
    - UnitType.List and ListUnit class removed
    - ElemCount property added (null-safe, replaces verbose ternary pattern)
    - NEW_LIST and NEW_TABLE kept as distinct opcodes intentionally:
      different stack conventions (positional vs keyed), same TableUnit output.
      No speed gain from merging; extra branch would be a regression.
  - Fix line breaks and IIFE/compound Call constructs — DONE:
    - Newline after '}' in Statement(Block) does NOT eat trailing newlines
    - Compound calls must be on the same line; newline always breaks the chain
    - \{...}\n(arg) correctly does NOT chain (was falsely documented as a bug)
    - All cases covered and tested in compound_calls.ltn
  - Fix concurrency false contention — DONE:
    - Per-slot object[] globalLocks in VM and ModuleUnit (no shared TypeUnit sentinels)
    - Each global address gets its own independent lock object
  - tasks(n, func, args) — DONE:
    - returns n-sized result table (one slot per task, nil slots preserved)
    - freeze-on-entry: Table args frozen for duration of tasks(), unfrozen in finally
    - nested tasks() with same frozen table detected and rejected at runtime
    - is_null() intrinsic added (O(1) type check, no comparison needed)
    - switched to CallFunction (fail-fast: errors propagate out of tasks())
  - Define bytecode serialization format (.ltnc) — DONE:
    - Chunk.Save(path) / Chunk.Load(path, prelude) in Chunk.cs
    - Magic "LTNC" + version + flags (float32/64 mode encoded)
    - Serializes: data literals (Float, Integer, Bool, Char, String, Function, Closure, Void),
      global address map, and main program body (instructions + positions interleaved)
    - ClosureUnit serialises function body + upvalue template descriptors (addr/env or chained)
    - Auto-caching (Python __pycache__ style):
        interpreter script.ltn   → use .ltnc if newer, else compile + save + run
        interpreter --compile     → force recompile (use for tests / CI)
        interpreter script.ltnc  → load directly
    - require() also caches: saves .ltnc beside each module, honours ForceRecompile flag
  - Improve error messages — stack traces DONE; assert msg improved
  - Parser error sync — PLANNED (defer to post-Phase 2):
      Current: Error() appends to list, parsing continues blindly → cascading garbage errors
      Plan:
        1. Add Synchronize() method: consume tokens until newline / 'var' / 'fun' / '}' / EOF
        2. Call Synchronize() at the top of Statement() on unexpected token
        3. Call Synchronize() in Consume() when expected token is missing
        4. Gate: only sync if no error was reported in the last N tokens (avoid spin)
        Result: multiple real, independent errors reported per compile pass

  - Language stabilization — decisions made, implementation pending:

    MEMORY MODEL (chosen: Hand-holding / mut-opt-in):
      All function parameters are implicitly const (immutable reference).
      Parameters that mutate must be marked 'mut'. Compiler errors if unmarked param is mutated.
      VM enforces at runtime: non-mut params may not receive mutable references.
      To get a mutable copy: explicit clone() — need fast deep clone intrinsic.
      Consequences:
        - 'const' keyword on var decl stays (marks rebind-immutable slot)
        - 'const()' userspace intrinsic is REMOVED (redundant under new model)
        - All passing/aliasing of refs is implicitly const → no surprise mutation
        - Rejected alternative: Transparent (everything const, clone to mutate) —
          correct but requires clone story first; revisit for Phase 3+

    MODULE CONST / TABLE FIELD CONST:
      Problem: require() returns plain Table; caller can mutate module fields freely.
      Chosen approach: require() returns a const-flagged Table (whole-table freeze).
        - Caller cannot rebind the module var (already true if declared const)
        - Caller cannot SET fields on the module table (enforced by existing PROTECTION_CONST)
        - Per-field const (HashSet<string> of frozen keys) deferred — too costly
        - Same applies to any user-created "namespace" table: use const(table) to freeze it

    CONSTRUCTOR NAMING:
      Rename all constructors to lowercase: Table → table, List → list, ListInit → list_init
      Option/Result constructors: option(val), option(), result(val), result(), result_error(val)
      Fallible constructors (file I/O, etc.) return Option. Table/List cannot fail → no Option.
      Constructors in camelCase: REJECTED. All intrinsics use snake_case → stay consistent.

    TRY:
      Keep try(func, args) as plain intrinsic call — no grammar sugar.
      try() uses VM pool (GetVM/RecycleVM) — not wasteful, pool is reused.
      Next: make VM dispatch loop catch all internal throws and convert to ResultUnit,
        never letting C# exceptions escape to user code (medium refactor, ~32 throw sites in VM.cs).

Phase 2 - Self-hosted compiler
  - Write scanner + parser in Lightning
  - Write compiler in Lightning, targeting the bytecode format above
  - Bootstrap: use C# compiler to compile the Lightning compiler, then self-host

Phase 3 - Multiple VM targets
  - CIL backend via System.Linq.Expressions (expression trees -> .Compile())
    - Prefer over raw ILGenerator for readability; fall back for advanced control flow
    - NOT the DLR (too heavy, designed for C# dynamic keyword)
  - WASM backend (compile to WASM text format or use Emscripten on C VM)
  - Keep Roslyn as opt-in plugin (#if ROSLYN) - not a core language feature

Phase 4 - C VM
  - Port the dispatch loop + Unit struct to C
  - Unit in C: union { float f; int32_t i; char c; bool b; } + HeapUnit* heap;
  - TypeUnit sentinel trick ports directly: static HeapUnit objects for Float/Int/etc.
  - Keep the prelude/compiler in C# initially; call C VM via P/Invoke
  - Replace tasks() C# Parallel.For with pthreads or work-stealing queue in C VM
  - Roslyn stays C#-only, enabled via bridge when C# host is present

Notes
-----
  - Chained assignments (a = b += 1) exist in C/C++/Java; our postfix ++ semantics
    (always returns new value) are consistent. Prefix ++ was removed (use x+1 instead).
  - The module system implements a linker: MakeModule() relocates bytecode operands
    from imported VM address space to importing VM address space. Worth documenting.
  - The Unit struct dual representation (StructLayout Explicit + TypeUnit sentinel) is
    novel - heapUnitValue is never null, TypeUnit singletons carry type info for value
    types. Measured to be faster and more memory-efficient than boxed approaches.
    This design is C-ready.
  - Roslyn integration: roslyn.compile(name, arity, body) compiles C# at runtime and
    returns a Lightning-callable function. Extraordinary for game scripting hot paths.

To Think
--------
- Add typing tests, dont allow variables to change type
- readonly fields in Unit?

Unit protection flags design (protectionFlags field already added to Unit.cs):
  - Unit  ≅ virtual-memory mapping  → protectionFlags are page-table-entry bits (per-reference)
  - TableUnit.Frozen ≅ mprotect()   → per-object, needed for tasks() thread safety
  - Value types (Float/Int/Char/Bool): isHeapUnit==false, copied by value, no shared identity
      → their const-ness is compiler-only (refuse STORE to const slot, zero runtime cost, Lua style)
  - Heap types (Table, Closure, ...): isHeapUnit==true, protectionFlags are safe to use
  - PROTECTION_CONST (bit 0): reference slot cannot be rebound
  - sizeof(Unit) unchanged in both float32 and DOUBLE modes — field sits in existing padding
  - DO NOT set protectionFlags when isHeapUnit==false (would corrupt value union)


To be Done
----------
- add const keyword:
    heap units  → set PROTECTION_CONST in Unit.protectionFlags at runtime + compiler slot flag
    stack units → compiler slot flag only (value types, zero runtime cost, Lua style)
- Colocar mut na gramatica
- Eval should return result
- test imported consts behavior
- remove exceptions and try intrinsic
- Construtores em camel case
- Construtores nao criam option
- Create option default_action() intrinsic
- create separated math functions for integers and floats where it makes sense
- Create more option unwrap() tests

- Criar const, ou let
- convert try to builtin(parsed)
- nupple as internal class
- destructuring
- optional typing
- direct dispatch to typed functions
- function signatures
- parameters checking at compile time
- bounds check at compile time
- C# eval and company should return ResultUnit
- globals should be created with thread safe structure
- upvalues should be stored with thread safe structure
- remove call to this constructor in Unit?
  - is this faster?(less initialization?)
- Algebrical types and match
- Finish roslyn module(finish compile)
- Add logic to not recompile in roslyn
- generational aliasing
- linear types
- ffi


add table.flatten => merge with extension table and unset extension table
    add table.flatten_under => merge extension table with table and unset extension table
rreduce for list

mapping and reducing functions for Table

create an integer method "5.0":int

make a queue

file read and write test

improve function declaration - add method declaration?

global function override?

tuple class

add a make_closure(a_function, a_table_with_values) method to set up "protected" table variables

string could have an add method to sum string to string
table could have an add melhod

table and wrapper could have and add overridable add method
floor() ceil() and round()

better list sort tests

add Type to Table and Wrapper so they can be retrieved from lightining and overrided
properties, protected indexes and operator override
move bool back to heapval

add Get and Set? to Unit :)
convert StringUnit to use char[]

add load externally compiled prelude extensions

new VScode extension

Improvements
============
    - coroutines,wait, yield, thread and message passing
    - sobrecarda de operacoes
    - improve line counting
    - arrays, tuples and nuple Units
    - parameter passing with nuples
        nuples to call and return from try
        add check for arity before call
    - interface as a compile time check
    - protected variables as a compile time check

    - Memory Management
        - use unit value to count heapval references
        - out parameter

Optimizations
=============
    - tail call optimization
    - remover auto global lock

Parsec
======
    - elide more stuff on parser
    - parser generators and combinators
    - many(*), at_least_one(+), optional(?)
    - adicionar interfaces

Parser
======
    - error sync on statement
    - better parser error messages
    - add loading Value.Nil for missing parameters on function call, emit a warning

Done
====
    - nil support
    - nil var initialization
    - return without exp returns nil
    - function without return gets one
    - can we stop copyng the closure on declaration? no
    - function return should break nested envs
    - rets( return set ) could be done by the CALL op instruction, before the jump
    - funclose and closure close return to caller
    - proper bytecode decoder
    - make instruction type independent
    - flatten VM structures
    - if
    - while
    - fixed strings being mixed with function names
    - fixed var finding bug
    - for
    - clojures
    - added support for null prints
    - closures use shift based initialization
    - While and for execute condition just once and jump back autmatically
    - bool ops otimizations
    - flatten upValue registry
    - convert value to structs and interfaces, tested and gone back
    - when variables that store closures go out of scope we should clear their references
        -- captured closures are stored in their ValClosure and get collected with it
    - pretty printer
    - can you return a declaration? No
    - intrinsics
    - remove print from language def
    - list
    - table assignment by index works for elements
    - add prelude with count functions
    - lambda
    - table constructors should take identifiers
    - methods
    - Assertions
    - load tables with prelude
    - unify Dotted and mettod and plain in a enum
    - separated call for closures and intrinsics, it will break passing function pointers around
    - single quoted strings
    - better tests
    - global return
    - call specific function on script
    - added an eval :)
    - mudules, require function
    - fix error lines, add error line to valfunction and to Error reporting on VM
    - fix eval
    - do functions beleng in constants? yes
    - iterator
    - import CallFunction from backpack
    - import prelude add
    - rename prelude load
    - separate warnings and error on scanner, parser and chunker
    - mod, and random
    - list add, table add, and perhaps remove and removeRange
    - add list split, list copy and insert
    - add table methods, clear and copy
    - eval and requice should error check
    - ValNil and ValBool as structs
    - implement ++, -- += -= *= /=
    - separte error detection and warnings in parser
    - Write to file intrinsic
    - read intrinsic
    - char at intrinsic
    - string slice intrincic
    - Add Release VMs and ResourcesTrim to prelude.
    - Global lock tests.
    - Move Env out of VM.
    - Fix line counting and reporting
    - finish porting prelude and stack helpers
    - tuples  and nuples
    - methods on wrappers
    - rename Number to Float
    - Compiler diferentiates Integer and Float literals
    - add makeNumericIterator
    - added ASSIGN_IMPORTED_CONSTANT
    - Added chained function calls(fix open parentheses after call turns into compoundCall)
    - try , protected call
    - separate test suite in modules
    - fixed bug in instantiate variables in literal tables
    - fix double upvalue test
    - replace ContainsKey and Contains with TryGetValue
    - consolidate UpValueMatrix and UpValueRegistry
    - sync grammar with parser
    - cast methods to_float() to_integer() to_string()
    - table class
    - move Table Get and Set for negative indices to GetElement and SetElement
    - added async logger
    - merge chunk assignment and assignmentOp
    - rename constants data
    - create an indexNode, add index node pretty print
    - added PositionData
        fix eval inconstant naming
        replace lambda counter with moduleName + PositionData
    - added global variable data to chunk

    - Solve lone node quirk; perhaps add ";"  or enforce expression separating comas, ocaml style forbid parentheses on LHS
    - Added Newlines to grammar
    - Added Newline Eliding to parser
    - Test and fix logger
    - Split prelude
    - criar sistema de configuracao por json e remover valores hard_coded
    - Create option type
    - Fix writeln unescape "\n"
    - fix table and list literals to accept function call without parenthesis
    - add abort compilation on any error
    - convert try to return an option(should try even be needed now?)
    - remove null