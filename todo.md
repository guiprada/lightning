todo
====

Roadmap
-------
Phase 1 - Stabilize the language (current)
  - Unify TableUnit: restore List<Unit> Elements + Dictionary<Unit,Unit> Map in one class
    - Merge ListUnit metatable methods into TableUnit
    - Collapse NEW_LIST opcode -> NEW_TABLE
    - Remove UnitType.List and ListUnit class
    - This fixes the inheritance/prototype chain (everything is a table again)

    - the pattern:
      int size = this_table.Elements != null ? this_table.Elements.Count : 0;
      should be build in .Elements.Count() - if Elements == null -> return 0
      - Remove this patter where it was added :) (Nuple.cs, TableUnit.cs:26, )
  - Fix line breaks and IIFE/coumpound Call constructs.
    - I think the solution is to double down on the newlines, there should not be newlines in a compount Call - they have to be on the same line; It it better this way 'cos this is the 'special' construct - spreading calls in lines should bring no suprises.
  - Fix concurrency false contention: value-type globals (Float/Integer/Bool) all share
    TypeUnit.Float etc. as their lock object. Replace with per-address lock array
    object[] globalLocks to give independent locking per global slot.
  - tasks(n, func, args) — DONE:
    - returns n-sized result table (one slot per task, nil slots preserved)
    - freeze-on-entry: Table args frozen for duration of tasks(), unfrozen in finally
    - nested tasks() with same frozen table detected and rejected at runtime
    - is_null() intrinsic added (O(1) type check, no comparison needed)
    - switched to CallFunction (fail-fast: errors propagate out of tasks())
  - Define bytecode serialization format (.ltnc) so Chunk can be saved/loaded portably
    - This is the bridge to multi-VM targets
  - Improve error messages (parser error sync, stack traces, assert error messages)
  - Remove DUP and STASH opcodes if unused (already in todo below)

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


To be Done
----------
- Colocar mut na gramatica
- Eval should return result
- function declaration should be const!
- test imported consts behavior
- remove exceptions and try intrinsic
- Construtores em camel case
- Construtores nao criam option
- Create option default_action() intrinsic
- create separated math functions for integers and floats where it makes sense
- Create more option unwrap() tests

- Criar const, ou let
- stack trace
  - fix assert error msg
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

remove DUP and STASH

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