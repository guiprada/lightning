using System;
using System.Collections.Generic;

using Operand = System.UInt16;

#if DOUBLE
    using Float = System.Double;
    using Integer = System.Int64;
#else
    using Float = System.Single;
    using Integer = System.Int32;
#endif

namespace lightning
{
    public class Chunker
    {
        enum ValueType
        {
            UpValue,
            Global,
            Local
        }

        struct Variable
        {
            public string name;
            public int address;
            public int envIndex;
            public ValueType type;
            public Variable(string p_name, int p_address, int p_envIndex, ValueType p_type)
            {
                name = p_name;
                address = p_address;
                envIndex = p_envIndex;
                type = p_type;
            }
        }

        private Node ast;
        private Chunk chunk;
        private string moduleName;

        public bool HasChunked { get; private set; }
        private int instructionCounter;
        private List<object> constants;
        private List<List<string>> env;
        private List<string> globals;
        private Stack<List<Variable>> upvalueStack;
        private Stack<int> funStartEnv;
        int lambdaCounter;
        public List<string> Errors { get; private set; }

        public Chunk Chunk
        {
            get
            {
                if (HasChunked == false)
                {
                    try
                    {
                        ChunkIt(ast);
                        HasChunked = true;
                    }
#if DEBUG
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        foreach(string error in Errors)
                        Console.WriteLine(error);
                    }
#else
                    catch
                    {
                        foreach(string error in Errors)
                        Console.WriteLine(error);
                    }
#endif
                }
                return chunk;
            }
        }

        public Chunker(Node p_ast, string p_moduleName, Library p_prelude)
        {
            chunk = new Chunk(p_moduleName, p_prelude);
            moduleName = p_moduleName;
            ast = p_ast;
            instructionCounter = 0;
            HasChunked = false;
            Errors = new List<string>();
            globals = new List<string>();
            constants = new List<dynamic>();
            upvalueStack = new Stack<List<Variable>>();
            env = new List<List<string>>();
            env.Add(globals);// set env[0] to globals
            funStartEnv = new Stack<int>();
            lambdaCounter = 0;

            // place prelude functions on constans
            foreach (IntrinsicUnit v in p_prelude.intrinsics)
            {
                SetGlobalVar(v.Name);
            }

            // load prelude tables
            foreach (KeyValuePair<string, TableUnit> entry in p_prelude.tables)
            {
                SetGlobalVar(entry.Key);

            }
        }

        private void ChunkIt(Node p_node)
        {
            NodeType this_type = p_node.Type;
            switch (this_type)
            {
                case NodeType.PROGRAM:
                    ChunkProgram(p_node as ProgramNode);
                    break;
                case NodeType.BINARY:
                    ChunkBinary(p_node as BinaryNode);
                    break;
                case NodeType.UNARY:
                    ChunkUnary(p_node as UnaryNode);
                    break;
                case NodeType.LITERAL:
                    ChunkLiteral(p_node as LiteralNode);
                    break;
                case NodeType.GROUPING:
                    ChunkGrouping(p_node as GroupingNode);
                    break;
                case NodeType.STMT_EXPR:
                    ChunkStmtExpr(p_node as StmtExprNode);
                    break;
                case NodeType.IF:
                    ChunkIf(p_node as IfNode);
                    break;
                case NodeType.WHILE:
                    ChunkWhile(p_node as WhileNode);
                    break;
                case NodeType.VARIABLE:
                    ChunkVariable(p_node as VariableNode);
                    break;
                case NodeType.VAR_DECLARATION:
                    ChunkVarDeclaration(p_node as VarDeclarationNode);
                    break;
                case NodeType.ASSIGMENT:
                    Chunkassignment(p_node as AssignmentNode);
                    break;
                case NodeType.ASSIGMENT_OP:
                    ChunkassignmentOp(p_node as AssignmentOpNode);
                    break;
                case NodeType.LOGICAL:
                    ChunkLogical(p_node as LogicalNode);
                    break;
                case NodeType.BLOCK:
                    ChunkBlock(p_node as BlockNode);
                    break;
                case NodeType.FUNCTION_CALL:
                    ChunkFunctionCall(p_node as FunctionCallNode);
                    break;
                case NodeType.ANONYMOUS_METHOD_CALL:
                    ChunkAnonymousMethodCall(p_node as AnonymousMethodCallNode);
                    break;
                case NodeType.RETURN:
                    ChunkReturn(p_node as ReturnNode);
                    break;
                case NodeType.FUNCTION_DECLARATION:
                    ChunkFunctionDeclaration(p_node as FunctionDeclarationNode);
                    break;
                case NodeType.FUNCTION_EXPRESSION:
                    ChunkFunctionExpression(p_node as FunctionExpressionNode);
                    break;
                case NodeType.FOR:
                    ChunkFor(p_node as ForNode);
                    break;
                case NodeType.TABLE:
                    ChunkTable(p_node as TableNode);
                    break;
                default:
                    Error("Received unkown node." + this_type.ToString(), p_node.Line);
                    break;
            }
        }

        private void ChunkProgram(ProgramNode p_node)
        {
            int line = p_node.Line;
            if (p_node.Statements != null)
            {
                foreach (Node n in p_node.Statements)
                {
                    ChunkIt(n);
                }

                if (p_node.Statements.Count > 1)
                    line = p_node.Statements[p_node.Statements.Count - 1].Line;
            }
            Add(OpCode.EXIT, line);
        }

        private void ChunkBinary(BinaryNode p_node)
        {
            ChunkIt(p_node.Left);
            ChunkIt(p_node.Right);

            OpCode this_opcode;
            switch (p_node.Op)
            {
                case OperatorType.SUBTRACTION:
                    this_opcode = OpCode.SUBTRACT;
                    break;
                case OperatorType.ADDITION:
                    this_opcode = OpCode.ADD;
                    break;
                case OperatorType.DIVISION:
                    this_opcode = OpCode.DIVIDE;
                    break;
                case OperatorType.MULTIPLICATION:
                    this_opcode = OpCode.MULTIPLY;
                    break;
                case OperatorType.EQUAL:
                    this_opcode = OpCode.EQUALS;
                    break;
                case OperatorType.NOT_EQUAL:
                    this_opcode = OpCode.NOT_EQUALS;
                    break;
                case OperatorType.GREATER_EQUAL:
                    this_opcode = OpCode.GREATER_EQUALS;
                    break;
                case OperatorType.LESS_EQUAL:
                    this_opcode = OpCode.LESS_EQUALS;
                    break;
                case OperatorType.GREATER:
                    this_opcode = OpCode.GREATER;
                    break;
                case OperatorType.LESS:
                    this_opcode = OpCode.LESS;
                    break;
                case OperatorType.APPEND:
                    this_opcode = OpCode.APPEND;
                    break;
                default:
                    Error("Unkown Binary operator " + p_node.Op.ToString(), p_node.Line);
                    this_opcode = OpCode.EXIT;
                    break;
            }

            Add(this_opcode, p_node.Line);
        }

        private void ChunkUnary(UnaryNode p_node)
        {
            ChunkIt(p_node.Right);

            OpCode this_opcode;
            switch (p_node.Op)
            {
                case OperatorType.NOT:
                    this_opcode = OpCode.NOT;
                    Add(this_opcode, p_node.Line);
                    break;
                case OperatorType.SUBTRACTION:
                    this_opcode = OpCode.NEGATE;
                    Add(this_opcode, p_node.Line);
                    break;
                case OperatorType.INCREMENT:
                    this_opcode = OpCode.INCREMENT;
                    Add(this_opcode, p_node.Line);
                    break;
                case OperatorType.DECREMENT:
                    this_opcode = OpCode.DECREMENT;
                    Add(this_opcode, p_node.Line);
                    break;
                default:
                    Error("Unkown Unary operator " + p_node.Op.ToString(), p_node.Line);
                    break;
            }
        }

        private void ChunkTable(TableNode p_node)
        {
            int n_elements = 0;
            if (p_node.Elements != null)
            {
                p_node.Elements.Reverse();
                foreach (Node n in p_node.Elements)
                {
                    ChunkIt(n);
                    n_elements++;
                }
            }

            int n_table = 0;
            if (p_node.Table != null)
            {
                foreach (KeyValuePair<Node, Node> entry in p_node.Table)
                {
                    ChunkIt(entry.Key);
                    ChunkIt(entry.Value);
                    n_table++;
                }
            }
            Add(OpCode.NEW_TABLE, (Operand)n_elements, (Operand)n_table, p_node.Line);
        }

        private void ChunkLiteral(LiteralNode p_node)
        {
            if (p_node.ValueType == typeof(bool))
            {
                if ((bool)p_node.Value == true)
                {
                    Add(OpCode.LOAD_TRUE, p_node.Line);
                }
                else if ((bool)p_node.Value == false)
                {
                    Add(OpCode.LOAD_FALSE, p_node.Line);
                }
            }
            else if (p_node.ValueType == typeof(Float))
            {
                int address = AddConstant((Float)p_node.Value);
                Add(OpCode.LOAD_CONSTANT, (Operand)address, p_node.Line);
            }
            else if (p_node.ValueType == typeof(Integer))
            {
                int address = AddConstant((Integer)p_node.Value);
                Add(OpCode.LOAD_CONSTANT, (Operand)address, p_node.Line);
            }
            else if (p_node.ValueType == typeof(string))
            {
                int address = AddConstant((string)p_node.Value);
                Add(OpCode.LOAD_CONSTANT, (Operand)address, p_node.Line);
            }
            else if (p_node.ValueType == typeof(char))
            {
                int address = AddConstant((char)p_node.Value);
                Add(OpCode.LOAD_CONSTANT, (Operand)address, p_node.Line);
            }
            else if ((string)p_node.Value == null)
            {
                Add(OpCode.LOAD_NIL, p_node.Line);
            }
        }

        private void ChunkGrouping(GroupingNode p_node)
        {
            ChunkIt(p_node.Expr);
        }

        private void ChunkStmtExpr(StmtExprNode p_node)
        {
            ChunkIt(p_node.Expr);
            Add(OpCode.POP, p_node.Line);
        }

        private void ChunkIf(IfNode p_node)
        {
            ChunkIt(p_node.Condition);
            int then_address = instructionCounter;
            Add(OpCode.JUMP_IF_NOT_TRUE, 0, p_node.Line);
            ChunkIt(p_node.ThenBranch);
            int else_address = instructionCounter;
            Add(OpCode.JUMP, 0, p_node.Line);
            chunk.FixInstruction(then_address, null, (Operand)(instructionCounter - then_address), null, null);
            if (p_node.ElseBranch != null)
            {
                ChunkIt(p_node.ElseBranch);
            }
            chunk.FixInstruction(else_address, null, (Operand)(instructionCounter - else_address), null, null);
        }

        private void ChunkFor(ForNode p_node)
        {
            Add(OpCode.OPEN_ENV, p_node.Line);
            env.Add(new List<string>());

            ChunkIt(p_node.Initializer);

            int condition_address = instructionCounter;
            ChunkIt(p_node.Condition);

            int start_address = instructionCounter;

            Add(OpCode.JUMP_IF_NOT_TRUE, 0, p_node.Line);
            if (p_node.Body != null)
            {
                ChunkIt(p_node.Body);
            }
            if (p_node.Finalizer != null)
            {
                ChunkIt(p_node.Finalizer);
                Add(OpCode.POP, p_node.Line);
            }
            //ChunkIt(p_node.Condition);

            int go_back_address = instructionCounter;
            Add(OpCode.JUMP_BACK, 0, p_node.Line);

            int exit_address = instructionCounter;
            Add(OpCode.CLOSE_ENV, p_node.Line);
            env.RemoveAt(env.Count - 1);

            chunk.FixInstruction(start_address, null, (Operand)(exit_address - start_address), null, null);
            chunk.FixInstruction(go_back_address, null, (Operand)(go_back_address - condition_address), null, null);

        }

        private void ChunkWhile(WhileNode p_node)
        {
            int condition_address = instructionCounter;
            ChunkIt(p_node.Condition);
            int body_address = instructionCounter;
            Add(OpCode.JUMP_IF_NOT_TRUE, 0, p_node.Line);

            ChunkIt(p_node.Body);

            int go_back_address = instructionCounter;
            Add(OpCode.JUMP_BACK, 0, p_node.Line);
            //int body_end = instructionCounter;

            chunk.FixInstruction(body_address, null, (Operand)(instructionCounter - body_address), null, null);
            chunk.FixInstruction(go_back_address, null, (Operand)(go_back_address - condition_address), null, null);
        }

        private void LoadVariable(Variable p_variable, int line){
            switch (p_variable.type)
            {
                case ValueType.Local:
                    Add(OpCode.LOAD_VARIABLE, (Operand)p_variable.address, (Operand)p_variable.envIndex, line);
                    break;
                case ValueType.Global:
                    Add(OpCode.LOAD_GLOBAL, (Operand)p_variable.address, line);
                    break;
                case ValueType.UpValue:
                    int this_index = upvalueStack.Peek().IndexOf(p_variable);
                    Add(OpCode.LOAD_UPVALUE, (Operand)this_index, line);
                    break;
            }
        }
        private void LoadIndex(VariableNode p_node){
            if(p_node.IsExpression)
                ChunkIt(p_node.Expression);
            else{
                Operand string_address = (Operand)AddConstant(p_node.Name);
                Add(OpCode.LOAD_CONSTANT, string_address, p_node.Line);
            }
        }
        private void LoadIndexes(VariableNode p_node){
            foreach (Node n in p_node.Indexes){
                LoadIndex((VariableNode)n);
            }
        }

        private void ChunkVariable(VariableNode p_node)
        {
            Nullable<Variable> maybe_var = GetVar(p_node.Name);
            if (maybe_var.HasValue){
                Variable this_var = maybe_var.Value;
                LoadVariable(this_var, p_node.Line);

                if (p_node.Indexes.Count > 0){// it is a compoundVar
                    LoadIndexes(p_node);
                    Add(OpCode.TABLE_GET, (Operand)p_node.Indexes.Count, p_node.Line);
                }
            }else{
                Error("Variable " + p_node.Name + " not found!", p_node.Line);
            }

        }

        private void ChunkVarDeclaration(VarDeclarationNode p_node)
        {
            if (p_node.Initializer == null){
                Add(OpCode.LOAD_NIL, p_node.Line);
            }else
                ChunkIt(p_node.Initializer);

            Nullable<Variable> maybe_var = SetVar(p_node.Name);
            if (maybe_var.HasValue){
                Variable this_var = maybe_var.Value;
                if (this_var.type == ValueType.Global)
                    Add(OpCode.DECLARE_GLOBAL, p_node.Line);
                else
                    Add(OpCode.DECLARE_VARIABLE, p_node.Line);
            }else
                Error("Variable Name has already been used!", p_node.Line);
        }

        private void Chunkassignment(AssignmentNode p_node)
        {
            ChunkIt(p_node.Value);
            Nullable<Variable> maybe_var = GetVar(p_node.Assigned.Name);
            Operand op = (Operand)AssignmentOperatorType.ASSIGN;
            if (maybe_var.HasValue){
                Variable this_var = maybe_var.Value;
                if (p_node.Assigned.Indexes.Count == 0){
                    switch (this_var.type){
                        case ValueType.Local:
                            Add(OpCode.ASSIGN_VARIABLE, (Operand)this_var.address, (Operand)this_var.envIndex, op, p_node.Line);
                            break;
                        case ValueType.Global:
                            Add(OpCode.ASSIGN_GLOBAL, (Operand)this_var.address, op, p_node.Line);
                            break;
                        case ValueType.UpValue:
                            int this_index = upvalueStack.Peek().IndexOf(this_var);
                            Add(OpCode.ASSIGN_UPVALUE, (Operand)this_index, op, p_node.Line);
                            break;

                    }
                }else{//  it is a compoundVar
                    LoadVariable(this_var, p_node.Line);
                    LoadIndexes(p_node.Assigned);
                    Add(OpCode.TABLE_SET, (Operand)p_node.Assigned.Indexes.Count, op, p_node.Line);
                }
            }else{
                Error("assignment to non existing variable!", p_node.Line);
            }
        }

        private void ChunkassignmentOp(AssignmentOpNode p_node)
        {
            ChunkIt(p_node.Value);
            Nullable<Variable> maybe_var = GetVar(p_node.Assigned.Name);

            Operand op = (Operand) p_node.Op;

            if (maybe_var.HasValue){
                Variable this_var = maybe_var.Value;
                if (p_node.Assigned.Indexes.Count == 0){
                    switch (this_var.type){
                        case ValueType.Local:
                            Add(OpCode.ASSIGN_VARIABLE, (Operand)this_var.address, (Operand)this_var.envIndex, op, p_node.Line);
                            break;
                        case ValueType.Global:
                            Add(OpCode.ASSIGN_GLOBAL, (Operand)this_var.address, op, p_node.Line);
                            break;
                        case ValueType.UpValue:
                            int this_index = upvalueStack.Peek().IndexOf(this_var);
                            Add(OpCode.ASSIGN_UPVALUE, (Operand)this_index, op, p_node.Line);
                            break;

                    }
                }else{//  it is a compoundVar
                    LoadVariable(this_var, p_node.Line);
                    LoadIndexes(p_node.Assigned);
                    Add(OpCode.TABLE_SET, (Operand)p_node.Assigned.Indexes.Count, op, p_node.Line);
                }
            }else{
                Error("assignment to non existing variable!", p_node.Line);
            }
        }

        private void ChunkLogical(LogicalNode p_node)
        {
            ChunkIt(p_node.Left);
            ChunkIt(p_node.Right);

            OpCode this_opcode;
            switch (p_node.Op){
                case OperatorType.AND:
                    this_opcode = OpCode.AND;
                    break;
                case OperatorType.OR:
                    this_opcode = OpCode.OR;
                    break;
                case OperatorType.XOR:
                    this_opcode = OpCode.XOR;
                    break;
                case OperatorType.NAND:
                    this_opcode = OpCode.NAND;
                    break;
                case OperatorType.NOR:
                    this_opcode = OpCode.NOR;
                    break;
                case OperatorType.XNOR:
                    this_opcode = OpCode.XNOR;
                    break;
                default:
                    Error("Unkown Logical operator " + p_node.Op.ToString(), p_node.Line);
                    this_opcode = OpCode.EXIT;
                    break;
            }

            Add(this_opcode, p_node.Line);
        }

        private void ChunkBlock(BlockNode p_node)
        {
            Add(OpCode.OPEN_ENV, p_node.Line);
            env.Add(new List<string>());
            foreach (Node n in p_node.Statements)
                ChunkIt(n);
            env.RemoveAt(env.Count - 1);
            Add(OpCode.CLOSE_ENV, p_node.Line);
        }

        private void ChunkAnonymousMethodCall(AnonymousMethodCallNode p_node)
        {
            ChunkIt(p_node.Declaration);
            ChunkIt(p_node.FunctionCall);
        }

        private void ChunkFunctionCall(FunctionCallNode p_node)
        {
            //push parameters on stack
            p_node.Calls[0].Reverse();
            foreach (Node n in p_node.Calls[0])
                ChunkIt(n);

            // the first call, we need to decode the function name
            Nullable<Variable> maybe_call = GetVar(p_node.Variable.Name);

            if (maybe_call.HasValue){
                Variable this_call = maybe_call.Value;

                if (p_node.Variable.Indexes.Count == 0)
                    LoadVariable(this_call, p_node.Line);
                else{// it is a compoundCall/method/IndexedAccess
                    // is it a method?
                    if ((p_node.Variable.Indexes[p_node.Variable.Indexes.Count - 1] as VariableNode).AccessType == VarAccessType.METHOD)
                    {// it is a method so we push the table again, to be used as parameter
                        LoadVariable(this_call, p_node.Line);

                        for (int i = 0; i < p_node.Variable.Indexes.Count - 1; i++)
                        {
                            LoadIndex((VariableNode)p_node.Variable.Indexes[i]);
                        }

                        Add(OpCode.TABLE_GET, (Operand)(p_node.Variable.Indexes.Count - 1), p_node.Line);
                    }

                    LoadVariable(this_call, p_node.Line);
                    LoadIndexes(p_node.Variable);
                    Add(OpCode.TABLE_GET, (Operand)p_node.Variable.Indexes.Count, p_node.Line);
                }

                // Call
                Add(OpCode.CALL, p_node.Line);
                // Does it have IndexedAccess?
                if (p_node.IndexedAccess[0] != null)
                {
                    LoadIndexes(p_node.IndexedAccess[0]);
                    Add(OpCode.TABLE_GET, (Operand)p_node.IndexedAccess[0].Indexes.Count, p_node.Line);
                }

                // Is it a compound call?
                for (int i = 1; i < p_node.Calls.Count; i++)
                {
                    Add(OpCode.PUSH_STASH, p_node.Line);

                    p_node.Calls[i].Reverse();
                    foreach (Node n in p_node.Calls[i])
                    {
                        ChunkIt(n);
                    }

                    Add(OpCode.POP_STASH, p_node.Line);
                    Add(OpCode.CALL, p_node.Line);

                    if (p_node.IndexedAccess[i] != null)
                    {
                        LoadIndexes(p_node.IndexedAccess[i]);
                        Add(OpCode.TABLE_GET, (Operand)p_node.IndexedAccess[i].Indexes.Count, p_node.Line);
                    }
                }
            }else
                Error("Tried to call unkown function!", p_node.Line);
        }

        private void ChunkReturn(ReturnNode p_node)
        {
            if (p_node.Expr == null){
                Add(OpCode.LOAD_NIL, p_node.Line);
            }else{
                ChunkIt(p_node.Expr);
            }
            Add(OpCode.RETURN, p_node.Line);
        }

        private void ChunkFunctionExpression(FunctionExpressionNode p_node)
        {
            //set the address

            CompileFunction("lambda" + lambdaCounter, p_node.Line, p_node, false);
            lambdaCounter++;
        }

        private void ChunkFunctionDeclaration(FunctionDeclarationNode p_node)
        {
            //register name
            if (globals.Contains(p_node.Name))
                Error("Local functions cant have the same name as global ones, yet :)", p_node.Line);

            Nullable<Variable> maybe_name = SetVar(p_node.Name);

            if (maybe_name.HasValue){
                Variable this_function = maybe_name.Value;

                if (this_function.type == ValueType.Global)
                    CompileFunction(this_function.name, p_node.Line, p_node, true);
                else
                    CompileFunction(this_function.name, p_node.Line, p_node, false);
            }else
                Error("Function name has already been used!", p_node.Line);
        }

        private void CompileFunction(string p_name, int p_line, FunctionExpressionNode p_node, bool p_isGlobal)
        {

            FunctionUnit new_function = new FunctionUnit(p_name, moduleName);
            Operand this_address = (Operand)AddConstant(new_function);

            if (p_node.GetType() == typeof(FunctionExpressionNode))
                Add(OpCode.DECLARE_FUNCTION, 0, 1, this_address, p_line);
            else{
                if (p_isGlobal)
                    Add(OpCode.DECLARE_FUNCTION, 0, 0, this_address, p_node.Line);// zero for gloabal
                else
                    Add(OpCode.DECLARE_FUNCTION, 1, 0, this_address, p_node.Line);// one for current env
            }

            // body
            int function_start = instructionCounter;

            // env
            env.Add(new List<string>());
            Add(OpCode.OPEN_ENV, p_line);
            //Add funStartEnv
            funStartEnv.Push(env.Count - 1);

            int exit_instruction_address = instructionCounter;
            Add(OpCode.RETURN_SET, 0, p_line);

            Operand arity = 0;
            if(p_node.Parameters != null)
                foreach (string p in p_node.Parameters){
                    SetVar(p);// it is always local
                    Add(OpCode.DECLARE_VARIABLE, p_line);
                    arity++;
                }

            upvalueStack.Push(new List<Variable>());
            foreach (Node n in p_node.Body)
                ChunkIt(n);

            bool is_closure = false;
            if (upvalueStack.Peek().Count != 0){
                is_closure = true;
                List<Variable> this_variables = upvalueStack.Pop();
                List<UpValueUnit> new_upvalues = new List<UpValueUnit>();
                foreach (Variable v in this_variables){
                    new_upvalues.Add(new UpValueUnit((Operand)v.address, (Operand)v.envIndex));
                }
                ClosureUnit new_closure = new ClosureUnit(new_function, new_upvalues);
                chunk.SwapConstant(this_address, new Unit(new_closure));
            }else
                upvalueStack.Pop();

            // fix the exit address
            chunk.FixInstruction(exit_instruction_address, null, (Operand)(instructionCounter - exit_instruction_address), null, null);

            if (is_closure == true)
                Add(OpCode.CLOSE_CLOSURE, p_line);
            else
                Add(OpCode.CLOSE_FUNCTION, p_line);

            new_function.Set(
                arity,
                chunk.Slice(function_start, instructionCounter),
                chunk.LineCounter.Slice(function_start, instructionCounter),
                (Operand)function_start);
            instructionCounter = function_start;

            //Console.WriteLine("Removed env");
            env.RemoveAt(env.Count - 1);

            //pop fun start env
            funStartEnv.Pop();
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        private Nullable<Variable> GetVar(string p_name)
        {
            int top_index = env.Count - 1;
            for (int i = top_index; i >= 0; i--){
                var this_env = env[i];
                if (this_env.Contains(p_name)){
                    bool is_in_function = upvalueStack.Count != 0;// if we are compiling a function this stack will not be empty
                    bool isGlobal = (i == 0);
                    if (!isGlobal){
                        if (is_in_function && i < funStartEnv.Peek()){// not global
                            List<Variable> this_upvalues = upvalueStack.Peek();
                            Variable this_upvalue = GetOrSetUpValue(p_name, this_env.IndexOf(p_name), i, this_upvalues);
                            return this_upvalue;
                        }
                        // local var
                        return new Variable(p_name, this_env.IndexOf(p_name), top_index - i, ValueType.Local);
                    }
                    if (isGlobal && globals.Contains(p_name)){// Sanity check
                        return new Variable(p_name, this_env.IndexOf(p_name), 0, ValueType.Global);
                    }
                    Console.WriteLine("Var finding gone mad!");
                    return null;
                }
            }
            return null;
        }

        private Variable GetOrSetUpValue(string p_name, int p_address, int p_env, List<Variable> p_list)
        {
            foreach (Variable v in p_list)
                if (p_name == v.name) return v;

            Variable this_upvalue = new Variable(p_name, p_address, p_env, ValueType.UpValue);
            p_list.Add(this_upvalue);
            return this_upvalue;
        }

        private bool IsLocalVar(string p_name)
        {
            int top_index = env.Count - 1;
            var this_env = env[top_index];

            if (this_env.Contains(p_name))
                return true;
            else
                return false;
        }

        private Nullable<Variable> SetVar(string p_name)
        {
            if (env.Count == 1)// we are in global scope
                return SetGlobalVar(p_name);
            else{
                if (!IsLocalVar(p_name))
                {
                    int top_index = env.Count - 1;
                    var this_env = env[top_index];
                    this_env.Add(p_name);
                    return new Variable(p_name, this_env.IndexOf(p_name), 0, ValueType.Local);
                }
            }
            return null;
        }

        private Nullable<Variable> SetGlobalVar(string p_name)
        {
            if (!globals.Contains(p_name)){
                globals.Add(p_name);
                return new Variable(p_name, globals.IndexOf(p_name), 0, ValueType.Global);
            }

            return null;
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        private int AddConstant(string p_string)
        {
            if (constants.Contains(p_string))
                return constants.IndexOf(p_string);
            else{
                constants.Add(p_string);
                chunk.AddConstant(new Unit(p_string));
                return constants.Count - 1;
            }
        }

        private int AddConstant(char p_char)
        {
            if (constants.Contains(p_char))
                return constants.IndexOf(p_char);
            else{
                constants.Add(p_char);
                chunk.AddConstant(new Unit(p_char));
                return constants.Count - 1;
            }
        }

        private int AddConstant(Float p_number)
        {
            if (constants.Contains(p_number))
                return constants.IndexOf(p_number);
            else{
                constants.Add(p_number);
                chunk.AddConstant(new Unit(p_number));
                return constants.Count - 1;
            }
        }

        private int AddConstant(Integer p_number)
        {
            if (constants.Contains(p_number))
                return constants.IndexOf(p_number);
            else{
                constants.Add(p_number);
                chunk.AddConstant(new Unit(p_number));
                return constants.Count - 1;
            }
        }

        private int AddConstant(FunctionUnit p_function)
        {
            constants.Add(p_function);
            chunk.AddConstant(new Unit(p_function));
            return constants.Count - 1;
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Add(OpCode p_opcode, int p_line)
        {
            chunk.WriteInstruction(p_opcode, 0, 0, 0, (uint)p_line);
            instructionCounter++;
        }

        private void Add(OpCode p_opcode, Operand p_opA, int p_line)
        {
            chunk.WriteInstruction(p_opcode, p_opA, 0, 0, (uint)p_line);
            instructionCounter++;
        }

        private void Add(OpCode p_opcode, Operand p_opA, Operand p_opB, int p_line)
        {
            chunk.WriteInstruction(p_opcode, p_opA, p_opB, 0, (uint)p_line);
            instructionCounter++;
        }

        private void Add(OpCode p_opcode, Operand p_opA, Operand p_opB, Operand p_opC, int p_line)
        {
            chunk.WriteInstruction(p_opcode, p_opA, p_opB, p_opC, (uint)p_line);
            instructionCounter++;
        }

        private void Error(string p_msg, int p_line)
        {
            Errors.Add(p_msg + " on module: " + moduleName + " on line: " + p_line);
        }
    }
}
