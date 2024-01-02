using System;
using System.Collections.Generic;

using lightningTools;
using lightningChunk;
using lightningUnit;
using lightningPrelude;
using lightningAST;
using lightningExceptions;

namespace lightningCompiler
{
    public class Compiler
    {
        enum ValueType
        {
            UpValue,
            Global,
            Local,
            Expression,
        }

        struct Variable
        {
            public readonly string name;
            public readonly int address;
            public readonly int envIndex;
            public readonly Node expression;
            public readonly ValueType type;
            public readonly bool isConst;
            public Variable(string p_name, int p_address, int p_envIndex, ValueType p_type, bool p_is_const)
            {
                name = p_name;
                address = p_address;
                envIndex = p_envIndex;
                expression = null;
                type = p_type;
                isConst = p_is_const;
            }

            public Variable(Node p_expression, bool p_is_const)
            {
                name = null;
                address = -1;
                envIndex = -1;
                expression = p_expression;
                type = ValueType.Expression;
                isConst = p_is_const;
            }
        }

        private Node ast;
        private Chunk chunk;
        private string moduleName;

        public bool HasChunked { get; private set; }
        private int instructionCounter;
        private List<object> dataLiterals;
        private List<List<string>> env;
        private List<string> globals;
        private List<List<string>> const_env;
        private List<string> const_globals;
        private Stack<List<Variable>> upvalueStack;
        private Stack<int> funStartEnv;
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
                        LogErrors();
                        if (Errors.Count > 0)
                        {
                            return null;
                        }
                        else
                        {
                            HasChunked = true;
                            chunk.PrintToFile(lightningPath.ToPath(moduleName) + ".chunk");
                        }
                    }
                    catch (Exception e)
                    {
                        if (e == Exceptions.compiler_error)
                            Logger.LogLine("Compiling had errors on module: " + moduleName, Defaults.Config.CompilerLogFile);
                        else
                            Logger.LogLine("Compiling broke the runtime! on module: " + moduleName + "\nException: " + e.ToString(), Defaults.Config.CompilerLogFile);
                        LogErrors();
                        return null;
                    }
                }
                return chunk;
            }
        }

        public Compiler(Node p_ast, string p_moduleName, Library p_prelude)
        {
            chunk = new Chunk(p_moduleName, p_prelude);
            moduleName = p_moduleName;
            ast = p_ast;
            instructionCounter = 0;
            HasChunked = false;
            Errors = new List<string>();
            globals = new List<string>();
            const_env = new List<List<string>>();
            const_globals = new List<string>();
            dataLiterals = new List<dynamic>();
            upvalueStack = new Stack<List<Variable>>();
            env = new List<List<string>>();
            env.Add(globals);// set env[0] to globals
            const_env.Add(const_globals);
            funStartEnv = new Stack<int>();

            // place prelude functions on data
            foreach (IntrinsicUnit v in p_prelude.intrinsics)
            {
                SetGlobalVar(v.Name, true);
            }

            // load prelude tables
            foreach (KeyValuePair<string, TableUnit> entry in p_prelude.tables)
            {
                SetGlobalVar(entry.Key, true);

            }
        }

        private void LogErrors()
        {
            if (Errors.Count > 0)
                foreach (string error in Errors)
                    Logger.LogLine("\t-" + error, Defaults.Config.CompilerLogFile);
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
                case NodeType.CONST_DECLARATION:
                    ChunkConstDeclaration(p_node as ConstDeclarationNode);
                    break;
                case NodeType.ASSIGMENT_OP:
                    ChunkAssignmentOp(p_node as AssignmentNode);
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
                case NodeType.RETURN:
                    ChunkReturn(p_node as ReturnNode);
                    break;
                case NodeType.FUNCTION_DECLARATION:
                    ChunkFunctionDeclaration(p_node as FunctionDeclarationNode);
                    break;
                case NodeType.MEMBER_FUNCTION_DECLARATION:
                    ChunkMemberFunctionDeclaration(p_node as MemberFunctionDeclarationNode);
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
                case NodeType.LIST:
                    ChunkList(p_node as ListNode);
                    break;
                default:
                    Error("Received unkown node." + this_type.ToString(), p_node.PositionData);
                    break;
            }
        }

        private void ChunkProgram(ProgramNode p_node)
        {
            PositionData position_data = p_node.PositionData;
            if (p_node.Statements != null)
            {
                foreach (Node n in p_node.Statements)
                {
                    ChunkIt(n);
                }

                if (p_node.Statements.Count > 1)
                    position_data = p_node.Statements[p_node.Statements.Count - 1].PositionData;
            }
            Add(OpCode.EXIT, position_data);
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
                    Error("Unkown Binary operator " + p_node.Op.ToString(), p_node.PositionData);
                    this_opcode = OpCode.EXIT;
                    break;
            }

            Add(this_opcode, p_node.PositionData);
        }

        private void ChunkUnary(UnaryNode p_node)
        {
            ChunkIt(p_node.Right);

            OpCode this_opcode;
            switch (p_node.Op)
            {
                case OperatorType.NOT:
                    this_opcode = OpCode.NOT;
                    Add(this_opcode, p_node.PositionData);
                    break;
                case OperatorType.SUBTRACTION:
                    this_opcode = OpCode.NEGATE;
                    Add(this_opcode, p_node.PositionData);
                    break;
                case OperatorType.INCREMENT:
                    this_opcode = OpCode.INCREMENT;
                    Add(this_opcode, p_node.PositionData);
                    break;
                case OperatorType.DECREMENT:
                    this_opcode = OpCode.DECREMENT;
                    Add(this_opcode, p_node.PositionData);
                    break;
                default:
                    Error("Unkown Unary operator " + p_node.Op.ToString(), p_node.PositionData);
                    break;
            }
        }

        private void ChunkTable(TableNode p_node)
        {
            int n_table = 0;
            if (p_node.Map != null)
            {
                n_table = p_node.Map.Count;
                foreach (KeyValuePair<Node, Node> entry in p_node.Map)
                {
                    ChunkIt(entry.Key);
                    ChunkIt(entry.Value);
                }
            }

            Add(OpCode.NEW_TABLE, (Operand)n_table, p_node.PositionData);
        }

        private void ChunkList(ListNode p_node)
        {
            int n_elements = 0;
            if (p_node.Elements != null)
            {
                n_elements = p_node.Elements.Count;
                for (int i = p_node.Elements.Count - 1; i >= 0; i--)
                    ChunkIt(p_node.Elements[i]);
            }
            Add(OpCode.NEW_LIST, (Operand)n_elements, p_node.PositionData);
        }

        private void ChunkLiteral(LiteralNode p_node)
        {
            if (p_node.ValueType == typeof(bool))
            {
                if ((bool)p_node.Value == true)
                {
                    Add(OpCode.LOAD_TRUE, p_node.PositionData);
                }
                else if ((bool)p_node.Value == false)
                {
                    Add(OpCode.LOAD_FALSE, p_node.PositionData);
                }
            }
            else if (p_node.ValueType == typeof(Float))
            {
                int address = AddData((Float)p_node.Value);
                Add(OpCode.LOAD_DATA, (Operand)address, p_node.PositionData);
            }
            else if (p_node.ValueType == typeof(Integer))
            {
                int address = AddData((Integer)p_node.Value);
                Add(OpCode.LOAD_DATA, (Operand)address, p_node.PositionData);
            }
            else if (p_node.ValueType == typeof(string))
            {
                int address = AddData((string)p_node.Value);
                Add(OpCode.LOAD_DATA, (Operand)address, p_node.PositionData);
            }
            else if (p_node.ValueType == typeof(char))
            {
                int address = AddData((char)p_node.Value);
                Add(OpCode.LOAD_DATA, (Operand)address, p_node.PositionData);
            }
        }

        private void ChunkGrouping(GroupingNode p_node)
        {
            ChunkIt(p_node.Expr);
        }

        private void ChunkStmtExpr(StmtExprNode p_node)
        {
            ChunkIt(p_node.Expr);
            Add(OpCode.POP, p_node.PositionData);
        }

        private void ChunkIf(IfNode p_node)
        {
            ChunkIt(p_node.Condition);
            int then_address = instructionCounter;
            Add(OpCode.JUMP_IF_NOT_TRUE, 0, p_node.PositionData);
            ChunkIt(p_node.ThenBranch);
            int else_address = instructionCounter;
            Add(OpCode.JUMP, 0, p_node.PositionData);
            chunk.FixInstruction(then_address, null, (Operand)(instructionCounter - then_address), null, null);
            if (p_node.ElseBranch != null)
            {
                ChunkIt(p_node.ElseBranch);
            }
            chunk.FixInstruction(else_address, null, (Operand)(instructionCounter - else_address), null, null);
        }

        private void ChunkFor(ForNode p_node)
        {
            Add(OpCode.OPEN_ENV, p_node.PositionData);
            env.Add(new List<string>());
            const_env.Add(new List<string>());

            ChunkIt(p_node.Initializer);

            int condition_address = instructionCounter;
            ChunkIt(p_node.Condition);

            int start_address = instructionCounter;

            Add(OpCode.JUMP_IF_NOT_TRUE, 0, p_node.PositionData);
            if (p_node.Body != null)
            {
                ChunkIt(p_node.Body);
            }
            if (p_node.Finalizer != null)
            {
                ChunkIt(p_node.Finalizer);
                Add(OpCode.POP, p_node.PositionData);
            }

            int go_back_address = instructionCounter;
            Add(OpCode.JUMP_BACK, 0, p_node.PositionData);

            int exit_address = instructionCounter;
            Add(OpCode.CLOSE_ENV, p_node.PositionData);
            env.RemoveAt(env.Count - 1);
            const_env.RemoveAt(const_env.Count - 1);

            chunk.FixInstruction(start_address, null, (Operand)(exit_address - start_address), null, null);
            chunk.FixInstruction(go_back_address, null, (Operand)(go_back_address - condition_address), null, null);

        }

        private void ChunkWhile(WhileNode p_node)
        {
            int condition_address = instructionCounter;
            ChunkIt(p_node.Condition);
            int body_address = instructionCounter;
            Add(OpCode.JUMP_IF_NOT_TRUE, 0, p_node.PositionData);

            ChunkIt(p_node.Body);

            int go_back_address = instructionCounter;
            Add(OpCode.JUMP_BACK, 0, p_node.PositionData);

            chunk.FixInstruction(body_address, null, (Operand)(instructionCounter - body_address), null, null);
            chunk.FixInstruction(go_back_address, null, (Operand)(go_back_address - condition_address), null, null);
        }

        private void LoadVariable(Variable p_variable, PositionData p_positionData)
        {
            switch (p_variable.type)
            {
                case ValueType.Local:
                    Add(OpCode.LOAD_VARIABLE, (Operand)p_variable.address, (Operand)p_variable.envIndex, p_positionData);
                    break;
                case ValueType.Global:
                    Add(OpCode.LOAD_GLOBAL, (Operand)p_variable.address, p_positionData);
                    break;
                case ValueType.UpValue:
                    int this_index = upvalueStack.Peek().IndexOf(p_variable);
                    Add(OpCode.LOAD_UPVALUE, (Operand)this_index, p_positionData);
                    break;
                case ValueType.Expression:
                    ChunkIt(p_variable.expression);
                    break;
            }
        }
        private void LoadIndex(IndexNode p_node)
        {
            if (p_node.IsAnonymous)
                ChunkIt(p_node.Expression);
            else
            {
                Operand string_address = (Operand)AddData(p_node.Name);
                Add(OpCode.LOAD_DATA, string_address, p_node.PositionData);
            }
        }
        private void LoadIndexes(List<IndexNode> p_node)
        {
            foreach (IndexNode n in p_node)
            {
                LoadIndex(n);
            }
        }

        private void ChunkVariable(VariableNode p_node)
        {
            Nullable<Variable> maybe_var = GetVar(p_node.Name, p_node.PositionData);
            if (maybe_var.HasValue)
            {
                Variable this_var = maybe_var.Value;
                LoadVariable(this_var, p_node.PositionData);

                if (p_node.Indexes.Count > 0)
                {// it is a compoundVar
                    LoadIndexes(p_node.Indexes);
                    Add(OpCode.GET, (Operand)p_node.Indexes.Count, p_node.PositionData);
                }
            }
            else
            {
                Error("Variable: " + p_node.Name + " not found!", p_node.PositionData);
            }
        }

        private void ChunkVarDeclaration(VarDeclarationNode p_node)
        {
            if (p_node.Initializer == null)
            {
                // This should Error while parsing.
                Error("Uninitialized Variable: " + p_node.Name, p_node.PositionData);
            }
            else
                ChunkIt(p_node.Initializer);

            Nullable<Variable> maybe_var = SetVar(p_node.Name, false);
            if (maybe_var.HasValue)
            {
                Variable this_var = maybe_var.Value;
                if (this_var.type == ValueType.Global)
                    Add(OpCode.DECLARE_GLOBAL, p_node.PositionData);
                else
                    Add(OpCode.DECLARE_VARIABLE, p_node.PositionData);
            }
            else
                Error("Variable Name has already been used!", p_node.PositionData);
        }

        private void ChunkConstDeclaration(ConstDeclarationNode p_node)
        {
            if (p_node.Initializer == null)
            {
                // This should Error while parsing.
                Error("Uninitialized Variable: " + p_node.Name, p_node.PositionData);
            }
            else
                ChunkIt(p_node.Initializer);

            Nullable<Variable> maybe_var = SetVar(p_node.Name, true);
            if (maybe_var.HasValue)
            {
                Variable this_var = maybe_var.Value;
                if (this_var.type == ValueType.Global)
                    Add(OpCode.DECLARE_GLOBAL, p_node.PositionData);
                else
                    Add(OpCode.DECLARE_VARIABLE, p_node.PositionData);
            }
            else
                Error("Variable Name has already been used!", p_node.PositionData);
        }

        private void ChunkAssignmentOp(AssignmentNode p_node)
        {
            ChunkIt(p_node.Value);
            Variable? maybe_var = GetVar(p_node.Assigned.Name, p_node.PositionData);

            Operand op = (Operand)p_node.Op;

            if (maybe_var.HasValue)
            {
                Variable this_var = maybe_var.Value;

                if (this_var.isConst)
                    Error("Trying to modify const variable: " + this_var.name, p_node.PositionData);

                if (p_node.Assigned.Indexes.Count == 0)
                {
                    switch (this_var.type)
                    {
                        case ValueType.Local:
                            Add(OpCode.ASSIGN_VARIABLE, (Operand)this_var.address, (Operand)this_var.envIndex, op, p_node.PositionData);
                            break;
                        case ValueType.Global:
                            Add(OpCode.ASSIGN_GLOBAL, (Operand)this_var.address, op, p_node.PositionData);
                            break;
                        case ValueType.UpValue:
                            int this_index = upvalueStack.Peek().IndexOf(this_var);
                            Add(OpCode.ASSIGN_UPVALUE, (Operand)this_index, op, p_node.PositionData);
                            break;
                    }
                }
                else
                {//  it is a compoundVar
                    LoadVariable(this_var, p_node.PositionData);
                    LoadIndexes(p_node.Assigned.Indexes);
                    Add(OpCode.SET, (Operand)p_node.Assigned.Indexes.Count, op, p_node.PositionData);
                }
            }
            else
            {
                Error("assignment to non existing variable!", p_node.PositionData);
            }
        }

        private void ChunkLogical(LogicalNode p_node)
        {
            ChunkIt(p_node.Left);
            ChunkIt(p_node.Right);

            OpCode this_opcode;
            switch (p_node.Op)
            {
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
                    Error("Unkown Logical operator " + p_node.Op.ToString(), p_node.PositionData);
                    this_opcode = OpCode.EXIT;
                    break;
            }

            Add(this_opcode, p_node.PositionData);
        }

        private void ChunkBlock(BlockNode p_node)
        {
            Add(OpCode.OPEN_ENV, p_node.PositionData);
            env.Add(new List<string>());
            const_env.Add(new List<string>());

            foreach (Node n in p_node.Statements)
                ChunkIt(n);

            env.RemoveAt(env.Count - 1);
            const_env.RemoveAt(const_env.Count - 1);

            Add(OpCode.CLOSE_ENV, p_node.PositionData);
        }

        private void ChunkFunctionCall(FunctionCallNode p_node)
        {
            //push parameters on stack
            for (int i = p_node.Calls[0].Arguments.Count - 1; i >= 0; i--)
                ChunkIt(p_node.Calls[0].Arguments[i]);

            // the first call, we need to decode the function name, or object
            Variable this_called;
            if (p_node.Variable.IsAnonymous)
                this_called = new Variable(p_node.Variable.Expression, true);
            else
            {
                Nullable<Variable> maybe_call = GetVar(p_node.Variable.Name, p_node.PositionData);
                if (maybe_call.HasValue)
                    this_called = maybe_call.Value;
                else
                    return;
            }

            if (p_node.Variable.Indexes.Count == 0)
                LoadVariable(this_called, p_node.PositionData);
            else
            {// it is a compoundCall/method/IndexedAccess!
                if (p_node.Variable.Indexes[p_node.Variable.Indexes.Count - 1].AccessType == VarAccessType.COLON)
                {// is it a method!
                    LoadVariable(this_called, p_node.PositionData);
                    Add(OpCode.DUP, p_node.PositionData);// it is a method so we push the table again, to be used as parameter!
                    Add(OpCode.PUSH_STASH, p_node.PositionData);

                    for (int i = 0; i < p_node.Variable.Indexes.Count - 1; i++)
                    {
                        LoadIndex(p_node.Variable.Indexes[i]);
                    }

                    Add(OpCode.GET, (Operand)(p_node.Variable.Indexes.Count - 1), p_node.PositionData);
                    Add(OpCode.POP_STASH, p_node.PositionData);
                }
                else
                {
                    LoadVariable(this_called, p_node.PositionData);
                }

                LoadIndexes(p_node.Variable.Indexes);
                Add(OpCode.GET, (Operand)p_node.Variable.Indexes.Count, p_node.PositionData);
            }

            // Call
            Add(OpCode.CALL, p_node.PositionData);
            // Does it have IndexedAccess?
            if (p_node.Calls[0].Indexes != null)
            {
                LoadIndexes(p_node.Calls[0].Indexes);
                Add(OpCode.GET, (Operand)p_node.Calls[0].Indexes.Count, p_node.PositionData);
            }

            // Is it a compound call?
            for (int i = 1; i < p_node.Calls.Count; i++)
            {
                Add(OpCode.PUSH_STASH, p_node.PositionData);

                for (int j = p_node.Calls[i].Arguments.Count - 1; j >= 0; j--)
                    ChunkIt(p_node.Calls[i].Arguments[j]);

                Add(OpCode.POP_STASH, p_node.PositionData);
                Add(OpCode.CALL, p_node.PositionData);

                if (p_node.Calls[i].Indexes != null)
                {
                    LoadIndexes(p_node.Calls[i].Indexes);
                    Add(OpCode.GET, (Operand)p_node.Calls[i].Indexes.Count, p_node.PositionData);
                }
            }
        }

        private void ChunkReturn(ReturnNode p_node)
        {
            if (p_node.Expr == null)
            {
                Add(OpCode.LOAD_VOID, p_node.PositionData);
            }
            else
            {
                ChunkIt(p_node.Expr);
            }
            Add(OpCode.RETURN, p_node.PositionData);
        }
        private void ChunkFunctionExpression(FunctionExpressionNode p_node)
        {
            CompileFunction(GetLambdaName(p_node), p_node, false);
        }

        private void ChunkMemberFunctionDeclaration(MemberFunctionDeclarationNode p_node)
        {
            CompileFunction(p_node.Name, p_node, false);
        }

        private void ChunkFunctionDeclaration(FunctionDeclarationNode p_node)
        {
            //register name
            if (p_node.Variable.Indexes.Count == 0)
            {// it is a regular function
                if (globals.Contains(p_node.Variable.Name))
                    Error("Local functions can not override global ones.", p_node.PositionData);

                Nullable<Variable> maybe_name = SetVar(p_node.Variable.Name, true);

                if (maybe_name.HasValue)
                {
                    Variable this_function = maybe_name.Value;
                    if (this_function.type == ValueType.Global)
                        CompileFunction(this_function.name, p_node, true);
                    else
                        CompileFunction(this_function.name, p_node, false);
                }
                else
                    Error("Function name has already been used!", p_node.PositionData);
            }
            else
            {// it is a member function
             // assemble a name for it
                string name = p_node.Variable.Name;
                for (int i = 0; i < p_node.Variable.Indexes.Count; i++)
                {
                    if (p_node.Variable.Indexes[i].Type == NodeType.VARIABLE)
                    {
                        IndexNode this_index = p_node.Variable.Indexes[i];
                        if (this_index.AccessType == VarAccessType.COLON)
                            Error("Method Declaration is not supported yet!", p_node.PositionData);
                        if (this_index.IsAnonymous)
                        {
                            name += GetLambdaName(p_node);
                        }
                        else
                            name += "." + this_index.Name;
                    }
                }
                // Break it down
                MemberFunctionDeclarationNode extracted_function = new MemberFunctionDeclarationNode(name, p_node.Parameters, p_node.Body, p_node.PositionData);
                AssignmentNode new_assigment = new AssignmentNode(p_node.Variable, extracted_function, AssignmentOperatorType.ASSIGN, p_node.PositionData);
                StmtExprNode new_stmt_expr = new StmtExprNode(new_assigment, p_node.PositionData);
                ChunkIt(new_stmt_expr);
            }
        }

        private void CompileFunction(string p_name, FunctionExpressionNode p_node, bool p_isGlobal)
        {

            FunctionUnit new_function = new FunctionUnit(p_name, moduleName);
            Operand this_address = (Operand)AddData(new_function);

            if (p_node.Type != NodeType.FUNCTION_DECLARATION)
                Add(OpCode.DECLARE_FUNCTION, 0, 1, this_address, p_node.PositionData);
            else
            {
                if (p_isGlobal)
                    Add(OpCode.DECLARE_FUNCTION, 0, 0, this_address, p_node.PositionData);// zero for gloabal
                else
                    Add(OpCode.DECLARE_FUNCTION, 1, 0, this_address, p_node.PositionData);// one for current env
            }

            // body
            int function_start = instructionCounter;

            // env
            env.Add(new List<string>());
            const_env.Add(new List<string>());

            Add(OpCode.OPEN_ENV, p_node.PositionData);
            //Add funStartEnv
            funStartEnv.Push(env.Count - 1);

            int exit_instruction_address = instructionCounter;
            Add(OpCode.RETURN_SET, 0, p_node.PositionData);

            Operand arity = 0;
            if (p_node.Parameters != null)
                foreach (ParameterInfo p in p_node.Parameters)
                {
                    SetVar(p.identifier, !p.isMut);// it is always local
                    Add(OpCode.DECLARE_VARIABLE, p_node.PositionData);
                    arity++;
                }

            upvalueStack.Push(new List<Variable>());
            foreach (Node n in p_node.Body)
                ChunkIt(n);

            bool is_closure = false;
            if (upvalueStack.Peek().Count != 0)
            {
                is_closure = true;
                List<Variable> this_variables = upvalueStack.Pop();
                List<UpValueUnit> new_upvalues = new List<UpValueUnit>();
                foreach (Variable v in this_variables)
                {
                    new_upvalues.Add(new UpValueUnit((Operand)v.address, (Operand)v.envIndex));
                }
                ClosureUnit new_closure = new ClosureUnit(new_function, new_upvalues);
                chunk.SwapDataLiteral(this_address, new Unit(new_closure));
            }
            else
                upvalueStack.Pop();

            // fix the exit address
            chunk.FixInstruction(exit_instruction_address, null, (Operand)(instructionCounter - exit_instruction_address), null, null);

            if (is_closure == true)
                Add(OpCode.CLOSE_CLOSURE, p_node.PositionData);
            else
                Add(OpCode.CLOSE_FUNCTION, p_node.PositionData);

            new_function.Set(
                arity,
                chunk.Slice(function_start, instructionCounter),
                chunk.ChunkPosition.Slice(function_start, instructionCounter),
                (Operand)function_start);
            instructionCounter = function_start;

            //Console.WriteLine("Removed env");
            env.RemoveAt(env.Count - 1);
            const_env.RemoveAt(const_env.Count - 1);

            //pop fun start env
            funStartEnv.Pop();
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        private Nullable<Variable> GetVar(string p_name, PositionData p_positionData)
        {
            int top_index = env.Count - 1;
            for (int i = top_index; i >= 0; i--)
            {
                var this_env = env[i];
                var this_const_env = const_env[i];
                if (this_env.Contains(p_name))
                {
                    bool is_in_function = upvalueStack.Count != 0;// if we are compiling a function this stack will not be empty
                    bool isGlobal = i == 0;
                    if (!isGlobal)
                    {
                        if (is_in_function && i < funStartEnv.Peek())
                        {// not global
                            List<Variable> this_upvalues = upvalueStack.Peek();
                            Variable this_upvalue = GetOrSetUpValue(p_name, this_env.IndexOf(p_name), i, this_upvalues);
                            return this_upvalue;
                        }

                        // local var
                        return new Variable(p_name, this_env.IndexOf(p_name), top_index - i, ValueType.Local, this_const_env.Contains(p_name));
                    }
                    if (isGlobal && globals.Contains(p_name))
                    {
                        return new Variable(p_name, this_env.IndexOf(p_name), 0, ValueType.Global, const_globals.Contains(p_name));
                    }
                    else
                    {
                        Error("Var finding gone mad, global Variable: " + p_name + " not found!", p_positionData);
                    }
                    return null;
                }
            }
            return null;
        }

        private Variable GetOrSetUpValue(string p_name, int p_address, int p_env, List<Variable> p_list)
        {
            foreach (Variable v in p_list)
                if (p_name == v.name) return v;

            Variable this_upvalue = new Variable(p_name, p_address, p_env, ValueType.UpValue, const_env[p_env].Contains(p_name));
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

        private Nullable<Variable> SetVar(string p_name, bool p_is_const)
        {
            if (env.Count == 1)// global scope
                return SetGlobalVar(p_name, p_is_const);
            else
            {
                if (!IsLocalVar(p_name))
                {
                    int top_index = env.Count - 1;
                    var this_env = env[top_index];
                    this_env.Add(p_name);

                    if (p_is_const)
                        const_env[top_index].Add(p_name);

                    return new Variable(p_name, this_env.IndexOf(p_name), 0, ValueType.Local, p_is_const);
                }
            }
            return null;
        }

        private Variable? SetGlobalVar(string p_name, bool p_is_const)
        {
            if (!globals.Contains(p_name))
            {
                globals.Add(p_name);

                if (p_is_const)
                    const_globals.Add(p_name);

                chunk.AddGlobalVariableAddress(p_name, (Operand)globals.IndexOf(p_name));
                return new Variable(p_name, globals.IndexOf(p_name), 0, ValueType.Global, p_is_const);
            }

            return null;
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        private int AddData(string p_string)
        {
            if (dataLiterals.Contains(p_string))
                return dataLiterals.IndexOf(p_string);
            else
            {
                dataLiterals.Add(p_string);
                chunk.AddData(new Unit(p_string));
                return dataLiterals.Count - 1;
            }
        }

        private int AddData(char p_char)
        {
            if (dataLiterals.Contains(p_char))
                return dataLiterals.IndexOf(p_char);
            else
            {
                dataLiterals.Add(p_char);
                chunk.AddData(new Unit(p_char));
                return dataLiterals.Count - 1;
            }
        }

        private int AddData(Float p_number)
        {
            if (dataLiterals.Contains(p_number))
                return dataLiterals.IndexOf(p_number);
            else
            {
                dataLiterals.Add(p_number);
                chunk.AddData(new Unit(p_number));
                return dataLiterals.Count - 1;
            }
        }

        private int AddData(Integer p_number)
        {
            if (dataLiterals.Contains(p_number))
                return dataLiterals.IndexOf(p_number);
            else
            {
                dataLiterals.Add(p_number);
                chunk.AddData(new Unit(p_number));
                return dataLiterals.Count - 1;
            }
        }

        private int AddData(FunctionUnit p_function)
        {
            dataLiterals.Add(p_function);
            chunk.AddData(new Unit(p_function));
            return dataLiterals.Count - 1;
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Add(OpCode p_opcode, PositionData p_positionData)
        {
            chunk.WriteInstruction(p_opcode, 0, 0, 0, p_positionData);
            instructionCounter++;
        }

        private void Add(OpCode p_opcode, Operand p_opA, PositionData p_positionData)
        {
            chunk.WriteInstruction(p_opcode, p_opA, 0, 0, p_positionData);
            instructionCounter++;
        }

        private void Add(OpCode p_opcode, Operand p_opA, Operand p_opB, PositionData p_positionData)
        {
            chunk.WriteInstruction(p_opcode, p_opA, p_opB, 0, p_positionData);
            instructionCounter++;
        }

        private void Add(OpCode p_opcode, Operand p_opA, Operand p_opB, Operand p_opC, PositionData p_positionData)
        {
            chunk.WriteInstruction(p_opcode, p_opA, p_opB, p_opC, p_positionData);
            instructionCounter++;
        }

        private string GetLambdaName(Node p_node)
        {
            return "lambda@" + lightningPath.ToLambdaName(moduleName) + p_node.PositionData;
        }

        private void Error(string p_msg, PositionData p_positionData)
        {
            Errors.Add(p_msg + " on module: " + moduleName + " on position: " + p_positionData);
            throw Exceptions.compiler_error;
        }
    }
}
