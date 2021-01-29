using System;
using System.Collections.Generic;
using System.Text;

using Operand = System.UInt16;

#if DOUBLE
    using Number = System.Double;
#else
    using Number = System.Single;
#endif

namespace lightning
{
    public class Chunker
    {
        enum ValType
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
            public ValType type;
            public Variable(string p_name, int p_address, int p_envIndex, ValType p_type)
            {
                name = p_name;
                address = p_address;
                envIndex = p_envIndex;
                type = p_type;
            }
        }

        Node ast;
        Chunk code;
        string module_name;
        public Chunk Code
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
                    catch(Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }                    
                }
                return code;
            }
        }

        public bool HasChunked { get; private set; }
        int instructionCounter;
        List<dynamic> constants;
        List<List<string>> env;
        List<string> globals;
        Stack<List<Variable>> upvalueStack;
        Stack<int> funStartEnv;
        int lambdaCounter;

        public List<string> Errors { get; private set; }

        public Chunker(Node p_ast, string p_module_name, Library prelude)
        {
            code = new Chunk(prelude);
            module_name = p_module_name;
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
            foreach (ValIntrinsic v in prelude.intrinsics)
            {
                SetGlobalVar(v.name);
            }

            // load prelude tables
            foreach (KeyValuePair<string, ValTable> entry in prelude.tables)
            {
                SetGlobalVar(entry.Key);

            }
        }

        void ChunkIt(Node p_node)
        {
            NodeType this_type = p_node.Type;
            //Console.WriteLine(this_type.ToString());
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
                    ChunkAssignment(p_node as AssignmentNode);
                    break;
                case NodeType.ASSIGMENTOP:
                    ChunkAssignmentOp(p_node as AssignmentOpNode);
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
                case NodeType.FUNCTION_EXPRESSION:
                    ChunkFunctionExpression(p_node as FunctionExpressionNode);
                    break;
                case NodeType.FOR:
                    ChunkFor(p_node as ForNode);
                    break;
                case NodeType.FOREACH:
                    ChunkForEach(p_node as ForEachNode);
                    break;
                case NodeType.RANGE:
                    ChunkRange(p_node as RangeNode);
                    break;
                case NodeType.TABLE:
                    ChunkTable(p_node as TableNode);
                    break;
                default:
                    Error("Received unkown node." + this_type.ToString(), p_node.Line);
                    break;
            }
        }

        void ChunkProgram(ProgramNode p_node)
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

        void ChunkBinary(BinaryNode p_node)
        {
            ChunkIt(p_node.Left);
            ChunkIt(p_node.Right);

            OpCode this_opcode;
            switch (p_node.Op)
            {
                case OperatorType.MINUS:
                    this_opcode = OpCode.SUB;
                    break;
                case OperatorType.PLUS:
                    this_opcode = OpCode.ADD;
                    break;
                case OperatorType.DIVISION:
                    this_opcode = OpCode.DIV;
                    break;
                case OperatorType.MULTIPLICATION:
                    this_opcode = OpCode.MUL;
                    break;
                case OperatorType.EQUAL:
                    this_opcode = OpCode.EQ;
                    break;
                case OperatorType.NOT_EQUAL:
                    this_opcode = OpCode.NEQ;
                    break;
                case OperatorType.GREATER_EQUAL:
                    this_opcode = OpCode.GTQ;
                    break;
                case OperatorType.LESS_EQUAL:
                    this_opcode = OpCode.LTQ;
                    break;
                case OperatorType.GREATER:
                    this_opcode = OpCode.GT;
                    break;
                case OperatorType.LESS:
                    this_opcode = OpCode.LT;
                    break;
                case OperatorType.APPEND:
                    this_opcode = OpCode.APP;
                    break;
                default:
                    Error("Unkown Binary operator " + p_node.Op.ToString(), p_node.Line);
                    this_opcode = OpCode.EXIT;
                    break;
            }

            Add(this_opcode, p_node.Line);
        }

        void ChunkUnary(UnaryNode p_node)
        {
            ChunkIt(p_node.Right);

            OpCode this_opcode;
            switch (p_node.Op)
            {
                case OperatorType.NOT:
                    this_opcode = OpCode.NOT;
                    Add(this_opcode, p_node.Line);
                    break;
                case OperatorType.MINUS:
                    this_opcode = OpCode.NEG;
                    Add(this_opcode, p_node.Line);
                    break;
                case OperatorType.PLUS_PLUS:
                    this_opcode = OpCode.INC;
                    Add(this_opcode, p_node.Line);
                    break;
                case OperatorType.MINUS_MINUS:
                    this_opcode = OpCode.DEC;
                    Add(this_opcode, p_node.Line);
                    break;
                default:
                    Error("Unkown Unary operator " + p_node.Op.ToString(), p_node.Line);                    
                    break;
            }
        }

        void ChunkTable(TableNode p_node)
        {
            int n_elements = 0;
            if (p_node.elements != null)
            {
                p_node.elements.Reverse();
                foreach (Node n in p_node.elements)
                {
                    ChunkIt(n);
                    n_elements++;
                }
            }

            int n_table = 0;
            if (p_node.table != null)
            {
                foreach (KeyValuePair<Node, Node> entry in p_node.table)
                {
                    ChunkIt(entry.Key);
                    ChunkIt(entry.Value);
                    n_table++;
                }
            }
            Add(OpCode.NTABLE, (Operand)n_elements, (Operand)n_table, p_node.Line);
        }

        void ChunkLiteral(LiteralNode p_node)
        {
            if (p_node.ValueType == typeof(bool))
            {
                if ((bool)p_node.Value == true)
                {
                    Add(OpCode.LOADTRUE, p_node.Line);
                }
                else if ((bool)p_node.Value == false)
                {
                    Add(OpCode.LOADFALSE, p_node.Line);
                }
            }
            else if (p_node.ValueType == typeof(Number))
            {
                int address = AddConstant((Number)p_node.Value);
                Add(OpCode.LOADC, (Operand)address, p_node.Line);
            }
            else if (p_node.ValueType == typeof(string))
            {
                if ((string)p_node.Value == "Nil")
                {
                    Add(OpCode.LOADNIL, p_node.Line);
                }
                else
                {
                    int address = AddConstant((string)p_node.Value);
                    Add(OpCode.LOADC, (Operand)address, p_node.Line);
                }
            }
        }

        void ChunkGrouping(GroupingNode p_node)
        {
            ChunkIt(p_node.Expr);
        }

        void ChunkStmtExpr(StmtExprNode p_node)
        {
            ChunkIt(p_node.Expr);
            Add(OpCode.POP, p_node.Line);
        }

        void ChunkIf(IfNode p_node)
        {
            ChunkIt(p_node.Condition);
            int then_address = instructionCounter;
            Add(OpCode.JNT, 0, p_node.Line);
            ChunkIt(p_node.ThenBranch);
            int else_address = instructionCounter;
            Add(OpCode.JMP, 0, p_node.Line);
            code.FixInstruction(then_address, null, (Operand)(instructionCounter - then_address), null, null);
            if (p_node.ElseBranch != null)
            {
                ChunkIt(p_node.ElseBranch);
            }
            code.FixInstruction(else_address, null, (Operand)(instructionCounter - else_address), null, null);
        }

        void ChunkFor(ForNode p_node)
        {
            Add(OpCode.NENV, p_node.Line);
            env.Add(new List<string>());

            ChunkIt(p_node.Initializer);

            int condition_address = instructionCounter;
            ChunkIt(p_node.Condition);

            int start_address = instructionCounter;

            Add(OpCode.JNT, 0, p_node.Line);
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
            Add(OpCode.JMPB, 0, p_node.Line);

            int exit_adress = instructionCounter;
            Add(OpCode.CENV, p_node.Line);
            env.RemoveAt(env.Count - 1);

            code.FixInstruction(start_address, null, (Operand)(exit_adress - start_address), null, null);
            code.FixInstruction(go_back_address, null, (Operand)(go_back_address - condition_address), null, null);

        }

        void ChunkForEach(ForEachNode p_node)
        {
            ChunkIt(p_node.List);
            ChunkIt(p_node.Function);
            Add(OpCode.FOREACH, p_node.Line);
        }

        void ChunkRange(RangeNode p_node)
        {
            ChunkIt(p_node.Tasks);
            ChunkIt(p_node.List);
            ChunkIt(p_node.Function);
            Add(OpCode.RANGE, p_node.Line);
        }

        void ChunkWhile(WhileNode p_node)
        {
            int condition_address = instructionCounter;
            ChunkIt(p_node.Condition);
            int body_address = instructionCounter;
            Add(OpCode.JNT, 0, p_node.Line);

            ChunkIt(p_node.Body);

            int go_back_address = instructionCounter;
            Add(OpCode.JMPB, 0, p_node.Line);
            //int body_end = instructionCounter;

            code.FixInstruction(body_address, null, (Operand)(instructionCounter - body_address), null, null);
            code.FixInstruction(go_back_address, null, (Operand)(go_back_address - condition_address), null, null);
        }

        void ChunkVariable(VariableNode p_node)
        {
            Nullable<Variable> maybe_var = GetVar(p_node.Name);
            if (maybe_var.HasValue)
            {
                Variable this_var = maybe_var.Value;
                switch (this_var.type)
                {
                    case ValType.Local:
                        Add(OpCode.LOADV, (Operand)this_var.address, (Operand)this_var.envIndex, p_node.Line);
                        break;
                    case ValType.Global:
                        Add(OpCode.LOADG, (Operand)this_var.address, p_node.Line);
                        break;
                    case ValType.UpValue:
                        int this_index = upvalueStack.Peek().IndexOf(this_var);
                        Add(OpCode.LOADUPVAL, (Operand)this_index, p_node.Line);
                        break;
                }
                if (p_node.Indexes.Count != 0)
                {// it is a compoundVar
                    foreach (Node n in p_node.Indexes)
                    {
                        if (n.GetType() != typeof(VariableNode) || (n as VariableNode).AccessType == VarAccessType.PLAIN)
                            ChunkIt(n);
                        else
                        {
                            Operand string_address = (Operand)AddConstant((n as VariableNode).Name);
                            Add(OpCode.LOADC, string_address, p_node.Line);
                        }
                    }

                    Add(OpCode.TABLEGET, (Operand)p_node.Indexes.Count, p_node.Line);
                }
            }
            else
            {
                Error("Variable " + p_node.Name + " not found!", p_node.Line);
            }

        }

        void ChunkVarDeclaration(VarDeclarationNode p_node)
        {
            if (p_node.Initializer == null)
            {
                //int address = AddConstant("Nil");
                //Add(OpCode.LOADC, (Operand)address, p_node.Line);
                Add(OpCode.LOADNIL, p_node.Line);
            }
            else
                ChunkIt(p_node.Initializer);

            Nullable<Variable> maybe_var = SetVar(p_node.Name);
            if (maybe_var.HasValue)
            {
                Variable this_var = maybe_var.Value;
                if (this_var.type == ValType.Global)
                    Add(OpCode.GLOBALDCL, p_node.Line);
                else
                    Add(OpCode.VARDCL, p_node.Line);
            }
            else
            {
                Error("Variable Name has already been used!", p_node.Line);
            }
        }

        void ChunkAssignment(AssignmentNode p_node)
        {
            ChunkIt(p_node.Value);
            Nullable<Variable> maybe_var = GetVar(p_node.Assigned.Name);

            if (maybe_var.HasValue)
            {
                Variable this_var = maybe_var.Value;
                if (p_node.Assigned.Indexes.Count == 0)
                {
                    switch (this_var.type)
                    {
                        case ValType.Local:
                            Add(OpCode.ASSIGN, (Operand)this_var.address, (Operand)this_var.envIndex, 0,p_node.Line);
                            break;
                        case ValType.Global:
                            Add(OpCode.ASSIGNG, (Operand)this_var.address, 0, p_node.Line);
                            break;
                        case ValType.UpValue:
                            int this_index = upvalueStack.Peek().IndexOf(this_var);
                            Add(OpCode.ASSIGNUPVAL, (Operand)this_index, 0, p_node.Line);
                            break;

                    }
                }
                else//  it is a compoundVar
                {
                    switch (this_var.type)
                    {
                        case ValType.Local:
                            Add(OpCode.LOADV, (Operand)this_var.address, (Operand)this_var.envIndex, p_node.Line);
                            break;
                        case ValType.Global:
                            Add(OpCode.LOADG, (Operand)this_var.address, p_node.Line);
                            break;
                        case ValType.UpValue:
                            int this_index = upvalueStack.Peek().IndexOf(this_var);
                            Add(OpCode.LOADUPVAL, (Operand)this_index, p_node.Line);
                            break;
                    }

                    foreach (Node n in p_node.Assigned.Indexes)
                    {
                        if (n.GetType() != typeof(VariableNode) || (n as VariableNode).AccessType == VarAccessType.PLAIN)
                            ChunkIt(n);
                        else
                        {
                            Operand string_address = (Operand)AddConstant((n as VariableNode).Name);
                            Add(OpCode.LOADC, string_address, p_node.Line);
                        }
                    }

                    Add(OpCode.TABLESET, (Operand)p_node.Assigned.Indexes.Count, 0, p_node.Line);
                }
            }
            else
            {
                Error("Assignment to non existing variable!", p_node.Line);
            }
        }

        void ChunkAssignmentOp(AssignmentOpNode p_node)
        {
            ChunkIt(p_node.Value);
            Nullable<Variable> maybe_var = GetVar(p_node.Assigned.Name);

            Operand op = 0;
            switch (p_node.Op)
            {
                case OperatorType.PLUS:
                    {
                        op = 1;
                    }
                    break;
                case OperatorType.MINUS:
                    {
                        op = 2;
                    }
                    break;
                case OperatorType.MULTIPLICATION:
                    {
                        op = 3;
                    }
                    break;
                case OperatorType.DIVISION:
                    {
                        op = 4;
                    }
                    break;
            }

            if (maybe_var.HasValue)
            {
                Variable this_var = maybe_var.Value;
                if (p_node.Assigned.Indexes.Count == 0)
                {
                    switch (this_var.type)
                    {
                        case ValType.Local:
                            Add(OpCode.ASSIGN, (Operand)this_var.address, (Operand)this_var.envIndex, op, p_node.Line);
                            break;
                        case ValType.Global:
                            Add(OpCode.ASSIGNG, (Operand)this_var.address, op, p_node.Line);
                            break;
                        case ValType.UpValue:
                            int this_index = upvalueStack.Peek().IndexOf(this_var);
                            Add(OpCode.ASSIGNUPVAL, (Operand)this_index, op, p_node.Line);
                            break;

                    }
                }
                else//  it is a compoundVar
                {
                    switch (this_var.type)
                    {
                        case ValType.Local:
                            Add(OpCode.LOADV, (Operand)this_var.address, (Operand)this_var.envIndex, p_node.Line);
                            break;
                        case ValType.Global:
                            Add(OpCode.LOADG, (Operand)this_var.address, p_node.Line);
                            break;
                        case ValType.UpValue:
                            int this_index = upvalueStack.Peek().IndexOf(this_var);
                            Add(OpCode.LOADUPVAL, (Operand)this_index, p_node.Line);
                            break;
                    }

                    foreach (Node n in p_node.Assigned.Indexes)
                    {
                        if (n.GetType() != typeof(VariableNode) || (n as VariableNode).AccessType == VarAccessType.PLAIN)
                            ChunkIt(n);
                        else
                        {
                            Operand string_address = (Operand)AddConstant((n as VariableNode).Name);
                            Add(OpCode.LOADC, string_address, p_node.Line);
                        }
                    }

                    Add(OpCode.TABLESET, (Operand)p_node.Assigned.Indexes.Count, op, p_node.Line);
                }
            }
            else
            {
                Error("Assignment to non existing variable!", p_node.Line);
            }
        }

        void ChunkLogical(LogicalNode p_node)
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
                    Error("Unkown Logical operator " + p_node.Op.ToString(), p_node.Line);
                    this_opcode = OpCode.EXIT;
                    break;
            }

            Add(this_opcode, p_node.Line);
        }

        void ChunkBlock(BlockNode p_node)
        {
            Add(OpCode.NENV, p_node.Line);
            env.Add(new List<string>());
            foreach (Node n in p_node.Statements)
                ChunkIt(n);
            env.RemoveAt(env.Count - 1);
            Add(OpCode.CENV, p_node.Line);
        }

        void ChunkFunctionCall(FunctionCallNode p_node)
        {
            //push parameters on stack
            p_node.Calls[0].Reverse();
            foreach (Node n in p_node.Calls[0])
            {
                ChunkIt(n);
            }
            // the first call, we need to decode the function name
            Nullable<Variable> maybe_func = GetVar(p_node.Name.Name);

            if (maybe_func.HasValue)
            {
                Variable this_func = maybe_func.Value;

                if (p_node.Name.Indexes.Count == 0)
                {
                    switch (this_func.type)
                    {
                        case ValType.Local:
                            Add(OpCode.LOADV, (Operand)this_func.address, (Operand)this_func.envIndex, p_node.Line);
                            break;
                        case ValType.Global:
                            Add(OpCode.LOADG, (Operand)this_func.address, p_node.Line);
                            break;
                        case ValType.UpValue:
                            int this_index = upvalueStack.Peek().IndexOf(this_func);
                            Add(OpCode.LOADUPVAL, (Operand)this_index, p_node.Line);
                            break;
                    }
                }
                else
                {// it is a compoundCall/metho/Getvars

                    // is it a method?
                    if ((p_node.Name.Indexes[p_node.Name.Indexes.Count - 1] as VariableNode).AccessType == VarAccessType.METHOD)
                    {// it is a method so we push the table again, to be used as parameter
                        switch (this_func.type)
                        {
                            case ValType.Local:
                                Add(OpCode.LOADV, (Operand)this_func.address, (Operand)this_func.envIndex, p_node.Line);
                                break;
                            case ValType.Global:
                                Add(OpCode.LOADG, (Operand)this_func.address, p_node.Line);
                                break;
                            case ValType.UpValue:
                                int this_index = upvalueStack.Peek().IndexOf(this_func);
                                Add(OpCode.LOADUPVAL, (Operand)this_index, p_node.Line);
                                break;
                        }

                        for (int i = 0; i < p_node.Name.Indexes.Count - 1; i++)
                        {
                            Node n = p_node.Name.Indexes[i];
                            if (n.GetType() != typeof(VariableNode) || (n as VariableNode).AccessType == VarAccessType.PLAIN)
                                ChunkIt(n);
                            else
                            {
                                Operand string_address = (Operand)AddConstant((n as VariableNode).Name);
                                Add(OpCode.LOADC, string_address, p_node.Line);
                            }

                        }

                        Add(OpCode.TABLEGET, (Operand)(p_node.Name.Indexes.Count - 1), p_node.Line);
                    }

                    switch (this_func.type)
                    {
                        case ValType.Local:
                            Add(OpCode.LOADV, (Operand)this_func.address, (Operand)this_func.envIndex, p_node.Line);
                            break;
                        case ValType.Global:
                            Add(OpCode.LOADG, (Operand)this_func.address, p_node.Line);
                            break;
                        case ValType.UpValue:
                            int this_index = upvalueStack.Peek().IndexOf(this_func);
                            Add(OpCode.LOADUPVAL, (Operand)this_index, p_node.Line);
                            break;
                    }

                    foreach (Node n in p_node.Name.Indexes)
                    {
                        if (n.GetType() != typeof(VariableNode) || (n as VariableNode).AccessType == VarAccessType.PLAIN)
                            ChunkIt(n);
                        else
                        {
                            Operand string_address = (Operand)AddConstant((n as VariableNode).Name);
                            Add(OpCode.LOADC, string_address, p_node.Line);
                        }

                    }
                    Add(OpCode.TABLEGET, (Operand)p_node.Name.Indexes.Count, p_node.Line);
                }

                // Call
                Add(OpCode.CALL, p_node.Line);
                // Does it have GetVars?
                if (p_node.GetVars[0] != null)
                {
                    foreach (Node n in p_node.GetVars[0].Indexes)
                    {
                        if (n.GetType() != typeof(VariableNode) || (n as VariableNode).AccessType == VarAccessType.PLAIN)
                            ChunkIt(n);
                        else
                        {
                            Operand string_address = (Operand)AddConstant((n as VariableNode).Name);
                            Add(OpCode.LOADC, string_address, p_node.Line);
                        }
                    }
                    Add(OpCode.TABLEGET, (Operand)p_node.GetVars[0].Indexes.Count, p_node.Line);
                }

                // Is it a compound call?
                for (int i = 1; i < p_node.Calls.Count; i++)
                {
                    Add(OpCode.STASHTOP, p_node.Line);

                    p_node.Calls[i].Reverse();
                    foreach (Node n in p_node.Calls[i])
                    {
                        ChunkIt(n);
                    }

                    Add(OpCode.POPSTASH, p_node.Line);
                    Add(OpCode.CALL, p_node.Line);

                    if (p_node.GetVars[i] != null)
                    {
                        foreach (Node n in p_node.GetVars[i].Indexes)
                        {
                            if (n.GetType() != typeof(VariableNode) || (n as VariableNode).AccessType == VarAccessType.PLAIN)
                                ChunkIt(n);
                            else
                            {
                                Operand string_address = (Operand)AddConstant((n as VariableNode).Name);
                                Add(OpCode.LOADC, string_address, p_node.Line);
                            }
                        }
                        Add(OpCode.TABLEGET, (Operand)p_node.GetVars[i].Indexes.Count, p_node.Line);
                    }
                }
            }
            else
            {
                Error("Tried to call unkown function!", p_node.Line);
            }
        }

        void ChunkReturn(ReturnNode p_node)
        {
            if (p_node.Expr == null)
            {
                Add(OpCode.LOADNIL, p_node.Line);
            }
            else
            {
                ChunkIt(p_node.Expr);
            }
            Add(OpCode.RET, p_node.Line);
        }

        void ChunkFunctionExpression(FunctionExpressionNode p_node)
        {
            //set the address

            CompileFunction("lambda" + lambdaCounter, p_node.Line, p_node, false);
            lambdaCounter++;
        }

        void ChunkFunctionDeclaration(FunctionDeclarationNode p_node)
        {
            //register name
            if (globals.Contains(p_node.Name))
                Error("Local functions cant have the same name as global ones, yet :)", p_node.Line);
            Nullable<Variable> maybe_name = SetVar(p_node.Name);
            //Console.WriteLine("fun dcl " + p_node.Name + " " + maybe_name.Value.address + " " + maybe_name.Value.envIndex + " " + maybe_name.Value.type + " we are in:" + (env.Count - 1));

            if (maybe_name.HasValue)
            {
                Variable this_function = maybe_name.Value;

                if (this_function.type == ValType.Global)
                {
                    CompileFunction(this_function.name, p_node.Line, p_node, true);
                }
                else
                {
                    CompileFunction(this_function.name, p_node.Line, p_node, false);
                }

            }
            else
            {
                Error("Function name has already been used!", p_node.Line);
            }
        }

        void CompileFunction(string name, int line, FunctionExpressionNode p_node, bool isGlobal)
        {

            ValFunction new_function = new ValFunction(name, module_name);
            Operand this_address = (Operand)AddConstant(new_function);

            if (p_node.GetType() == typeof(FunctionExpressionNode))
            {
                Add(OpCode.FUNDCL, 0, 1, this_address, line);
            }
            else
            {
                if (isGlobal)
                {
                    Add(OpCode.FUNDCL, 0, 0, this_address, p_node.Line);// zero for gloabal
                }
                else
                {
                    Add(OpCode.FUNDCL, 1, 0, this_address, p_node.Line);// one for current env
                }
            }


            // body
            int function_start = instructionCounter;

            // env
            env.Add(new List<string>());
            Add(OpCode.NENV, line);
            //Add funStartEnv 
            funStartEnv.Push(env.Count - 1);

            int exit_instruction_address = instructionCounter;
            Add(OpCode.RETSREL, 0, line);

            foreach (string p in p_node.Parameters)
            {
                SetVar(p);// it is always local
                Add(OpCode.VARDCL, line);
                //Add(OpCode.POP, line);
                new_function.arity++;
            }

            upvalueStack.Push(new List<Variable>());
            foreach (Node n in p_node.Body)
                ChunkIt(n);

            bool is_closure = false;
            if (upvalueStack.Peek().Count != 0)
            {
                is_closure = true;
                List<Variable> this_variables = upvalueStack.Pop();
                List<ValUpValue> new_upvalues = new List<ValUpValue>();
                foreach (Variable v in this_variables)
                {
                    new_upvalues.Add(new ValUpValue((Operand)v.address, (Operand)v.envIndex));
                }
                ValClosure new_closure = new ValClosure(new_function, new_upvalues);
                code.SwapConstant(this_address, new_closure);
            }
            else
            {
                upvalueStack.Pop();
            }

            // fix the exit address
            code.FixInstruction(exit_instruction_address, null, (Operand)(instructionCounter - exit_instruction_address), null, null);

            if (is_closure == true)
                Add(OpCode.CLOSURECLOSE, line);
            else
                Add(OpCode.FUNCLOSE, line);

            new_function.body = code.Slice(function_start, instructionCounter);
            new_function.originalPosition = (Operand)function_start;
            instructionCounter = function_start;

            //Console.WriteLine("Removed env");
            env.RemoveAt(env.Count - 1);

            //pop fun start env
            funStartEnv.Pop();
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        Nullable<Variable> GetVar(string name)
        {
            int top_index = env.Count - 1;
            for (int i = top_index; i >= 0; i--)
            {
                var this_env = env[i];
                if (this_env.Contains(name))
                {
                    bool is_in_function = upvalueStack.Count != 0;// if we are compiling a function this stack will not be empty
                    bool isGlobal = (i == 0);
                    //Console.WriteLine("found var " + name + " " + isGlobal.ToString());
                    if (!isGlobal)
                    {
                        if (is_in_function && i < funStartEnv.Peek())// not global
                        {
                            List<Variable> this_upvalues = upvalueStack.Peek();
                            Variable this_upvalue = GetOrSetInUpValue(name, this_env.IndexOf(name), top_index - i, this_upvalues);
                            return this_upvalue;
                        }
                        // local var                        
                        return new Variable(name, this_env.IndexOf(name), top_index - i, ValType.Local);
                    }
                    if (isGlobal && globals.Contains(name))// Sanity check
                    {
                        return new Variable(name, this_env.IndexOf(name), 0, ValType.Global);
                    }
                    Console.WriteLine("Var finding gone mad!");
                    return null;
                }
            }
            return null;
        }

        Variable GetOrSetInUpValue(string name, int adress, int env, List<Variable> list)
        {
            foreach (Variable v in list)
            {
                if (name == v.name) return v;
            }
            Variable this_upvalue = new Variable(name, adress, env, ValType.UpValue);
            list.Add(this_upvalue);
            return this_upvalue;
        }

        bool IsLocalVar(string name)
        {
            int top_index = env.Count - 1;
            var this_env = env[top_index];

            if (this_env.Contains(name))
                return true;
            else
                return false;
        }

        Nullable<Variable> SetVar(string name)
        {
            if (env.Count == 1)// we are in global scope
            {
                return SetGlobalVar(name);
            }
            else
            {
                if (!IsLocalVar(name))
                {
                    //Console.WriteLine("added local var " + name);
                    int top_index = env.Count - 1;
                    var this_env = env[top_index];
                    this_env.Add(name);
                    return new Variable(name, this_env.IndexOf(name), 0, ValType.Local);
                }
            }
            return null;
        }

        Nullable<Variable> SetGlobalVar(string name)
        {
            if (!globals.Contains(name))
            {
                //Console.WriteLine("added global " + name);
                globals.Add(name);
                return new Variable(name, globals.IndexOf(name), 0, ValType.Global);
            }

            return null;
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        int AddConstant(string p_string)
        {
            if (constants.Contains(p_string))
            {
                return constants.IndexOf(p_string);
            }
            else
            {
                constants.Add(p_string);
                if (p_string == "Nil")
                    code.AddConstant(Value.Nil);
                else
                    code.AddConstant(new ValString(p_string));

                return constants.Count - 1;
            }
        }

        int AddConstant(Number p_number)
        {
            if (constants.Contains(p_number))
            {
                return constants.IndexOf(p_number);
            }
            else
            {
                constants.Add(p_number);
                code.AddConstant(new ValNumber(p_number));
                return constants.Count - 1;
            }
        }

        //int AddConstant(bool p_bool)
        //{
        //    if (constants.Contains(p_bool))
        //    {
        //        return constants.IndexOf(p_bool);
        //    }
        //    else
        //    {
        //        constants.Add(p_bool);
        //        code.AddConstant(new ValBool(p_bool));
        //        return constants.Count - 1;
        //    }
        //}

        int AddConstant(ValFunction new_function)
        {
            constants.Add(new_function);
            code.AddConstant(new_function);
            return constants.Count - 1;
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        void Add(OpCode p_opcode, int p_line)
        {
            code.WriteInstruction(p_opcode, 0, 0, 0, (uint)p_line);
            instructionCounter++;
        }

        void Add(OpCode p_opcode, Operand p_opA, int p_line)
        {
            code.WriteInstruction(p_opcode, p_opA, 0, 0, (uint)p_line);
            instructionCounter++;
        }

        void Add(OpCode p_opcode, Operand p_opA, Operand p_opB, int p_line)
        {
            code.WriteInstruction(p_opcode, p_opA, p_opB, 0, (uint)p_line);
            instructionCounter++;
        }

        void Add(OpCode p_opcode, Operand p_opA, Operand p_opB, Operand p_opC, int p_line)
        {
            code.WriteInstruction(p_opcode, p_opA, p_opB, p_opC, (uint)p_line);
            instructionCounter++;
        }

        void Error(string p_msg, int p_line)
        {
            Errors.Add(p_msg + " on line: " + p_line);
        }
    }
}
