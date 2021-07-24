todo
====

To Think
--------
rethink typing system
strong typed dynamic language
add typing tests
examine overflows in numeric operations


To be Done
----------
sync grammar with parser
add a make_closure(a_function, a_table_with_values) method to set up "protected" table variables

string could have an add method to sum string to string
cast methods float() integer() string()
table could have an add melhod
table and wrapper could have and add overridable add method
floor() ceil() and round()

improve Peek2()
list sort tests
table class
tuple and nuple class

add Type to Table and Wrapper so they can be retrieved from lightining and overrided
properties, protected indexes and operator override
move bool back to heapval

add Get and Set? to Unit :)
convert StringUnit to use char[]

add load externally compiled prelude extensions

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
        -
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
    - when variables that store closures go out of scope we should clear their references, yes -- captured closures are stored in ther ValClosure and get collected with it
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