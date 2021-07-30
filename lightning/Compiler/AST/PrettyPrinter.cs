using System;
using System.Collections.Generic;
using System.IO;

namespace lightning
{
    public class PrettyPrinter
    {
        int identLevel;
        string identString;

        public PrettyPrinter()
        {
            identLevel = 0;
            identString = "";
        }
        public void PrintToFile(Node p_node, string p_path, bool p_append = false){
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(p_path, p_append)){
                Console.SetOut(file);
                Print(p_node);
                var standardOutput = new StreamWriter(Console.OpenStandardOutput());
                standardOutput.AutoFlush = true;
                Console.SetOut(standardOutput);
            }
        }
        public void Print(Node p_node)
        {
            NodeType this_type = p_node.Type;
            switch (this_type){
                case NodeType.PROGRAM:
                    PrintProgram(p_node as ProgramNode);
                    break;
                case NodeType.BINARY:
                    PrintBinary(p_node as BinaryNode);
                    break;
                case NodeType.UNARY:
                    PrintUnary(p_node as UnaryNode);
                    break;
                case NodeType.LITERAL:
                    PrintLiteral(p_node as LiteralNode);
                    break;
                case NodeType.GROUPING:
                    PrintGrouping(p_node as GroupingNode);
                    break;
                case NodeType.STMT_EXPR:
                    PrintStmtExpr(p_node as StmtExprNode);
                    break;
                case NodeType.IF:
                    PrintIf(p_node as IfNode);
                    break;
                case NodeType.WHILE:
                    PrintWhile(p_node as WhileNode);
                    break;
                case NodeType.VARIABLE:
                    PrintVariable(p_node as VariableNode);
                    break;
                case NodeType.INDEX:
                    PrintIndex(p_node as IndexNode);
                    break;
                case NodeType.VAR_DECLARATION:
                    PrintVarDeclaration(p_node as VarDeclarationNode);
                    break;
                case NodeType.ASSIGMENT_OP:
                    PrintassignmentOp(p_node as AssignmentNode);
                    break;
                case NodeType.LOGICAL:
                    PrintLogical(p_node as LogicalNode);
                    break;
                case NodeType.BLOCK:
                    PrintBlock(p_node as BlockNode);
                    break;
                case NodeType.FUNCTION_CALL:
                    PrintFunctionCall(p_node as FunctionCallNode);
                    break;
                case NodeType.RETURN:
                    PrintReturn(p_node as ReturnNode);
                    break;
                case NodeType.FUNCTION_DECLARATION:
                    PrintFunctionDeclaration(p_node as FunctionDeclarationNode);
                    break;
                case NodeType.FUNCTION_EXPRESSION:
                    PrintFunctionExpression(p_node as FunctionExpressionNode);
                    break;
                case NodeType.FOR:
                    PrintFor(p_node as ForNode);
                    break;
                case NodeType.TABLE:
                    PrintTable(p_node as TableNode);
                    break;
                default:
                    Error("Pretty Printer Received unkown node." + this_type.ToString(), p_node.Line);
                    break;
            }
        }

        private void PrintProgram(ProgramNode p_node)
        {
            Console.WriteLine("[PROGRAM");
            if (p_node.Statements != null){
                IdentPlus();
                foreach (Node n in p_node.Statements)
                    Print(n);
                IdentMinus();
            }else{
                Error("Program does not contain any statements!", p_node.Line);
            }
            Console.WriteLine("]");
        }

        private void PrintBinary(BinaryNode p_node)
        {
            Console.Write("[BINARY ");
            Print(p_node.Left);
            if (p_node.Op == OperatorType.ADDITION) Console.Write(" + ");
            else if (p_node.Op == OperatorType.SUBTRACTION) Console.Write(" - ");
            else if (p_node.Op == OperatorType.MULTIPLICATION) Console.Write(" * ");
            else if (p_node.Op == OperatorType.DIVISION) Console.Write(" / ");
            else if (p_node.Op == OperatorType.APPEND) Console.Write(" .. ");

            else if (p_node.Op == OperatorType.NOT_EQUAL) Console.Write(" != ");
            else if (p_node.Op == OperatorType.EQUAL) Console.Write(" == ");
            else if (p_node.Op == OperatorType.GREATER) Console.Write(" > ");
            else if (p_node.Op == OperatorType.GREATER_EQUAL) Console.Write(" >= ");
            else if (p_node.Op == OperatorType.LESS) Console.Write(" < ");
            else if (p_node.Op == OperatorType.LESS_EQUAL) Console.Write(" <= ");
            else
                Error("Invalid binary operator.", p_node.Line);
            Print(p_node.Right);
            Console.Write("]");
        }

        private void PrintUnary(UnaryNode p_node)
        {
            Console.Write("[UNARY ");
            if (p_node.Op == OperatorType.SUBTRACTION) Console.Write(" - ");
            else if (p_node.Op == OperatorType.NOT) Console.Write(" ! ");
            else if (p_node.Op == OperatorType.INCREMENT) Console.Write(" ++ ");
            else if (p_node.Op == OperatorType.DECREMENT) Console.Write(" -- ");
            else
                Error("Invalid unary operator.", p_node.Line);
            Print(p_node.Right);
            Console.Write("]");
        }

        private void PrintLiteral(LiteralNode p_node)
        {
            Console.Write("[LITERAL ");
            if (p_node.Value == null)
                Console.Write("\"" + ("Null") + "\"");
            else if (p_node.ValueType == typeof(string))
                Console.Write("\"" + (p_node.Value) + "\"");
            else
                Console.Write(p_node.Value.ToString());
            Console.Write("]");
        }

        private void PrintTable(TableNode p_node)
        {
            Console.Write("[TABLE ");
            if (p_node.Elements != null){
                bool first = true;
                foreach(Node n in p_node.Elements){
                    if (first){
                        Print(n);
                        first = false;
                    }else{
                        Console.Write(", ");
                        Print(n);
                    }
                }
                foreach(KeyValuePair<Node,Node> entry in p_node.Table){
                    if (first){
                        Print(entry.Key);
                        Console.Write(" : ");
                        Print(entry.Value);
                        first = false;
                    }else{
                        Console.Write(", ");
                        Print(entry.Key);
                        Console.Write(" : ");
                        Print(entry.Value);
                    }
                }
            }
        }

        private void PrintGrouping(GroupingNode p_node)
        {
            Console.Write("[GROUPING ");
            Print(p_node.Expr);
            Console.Write("]");
        }

        private void PrintStmtExpr(StmtExprNode p_node)
        {
            Console.Write(identString + "[EXPR_STMT ");
            Print(p_node.Expr);
            Console.WriteLine("]");
        }

        private void PrintIf(IfNode p_node)
        {
            Console.Write(identString + "[IF ");
            Print(p_node.Condition);
            Console.WriteLine();
            IdentPlus();
            Print(p_node.ThenBranch);
            if (p_node.ElseBranch != null){
                IdentMinus();
                Console.WriteLine(identString + "ELSE ");
                IdentPlus();
                Print(p_node.ElseBranch);
                IdentMinus();
                IdentPlus();
            }
            IdentMinus();
            Console.WriteLine(identString + "]");
        }

        private void PrintFor(ForNode p_node)
        {
            Console.WriteLine(identString + "[FOR ");
            if (p_node.Initializer != null){
                IdentPlus();
                Console.WriteLine(identString + "[INITIALIZER");
                IdentPlus();
                Print(p_node.Initializer);
                IdentMinus();
                Console.WriteLine(identString + "]");
                IdentMinus();
            }
            if (p_node.Condition != null){
                IdentPlus();
                Console.Write(identString + "[CONDITION ");
                Print(p_node.Condition);
                Console.WriteLine("]");
                IdentMinus();
            }
            if (p_node.Finalizer != null){
                IdentPlus();
                Console.Write(identString + "[FINALIZER ");
                Print(p_node.Finalizer);
                Console.WriteLine("]");
                IdentMinus();
            }
            if (p_node.Body != null){
                IdentPlus();
                Console.WriteLine(identString + "[BODY");
                IdentPlus();
                Print(p_node.Body);
                IdentMinus();
                Console.WriteLine(identString + "]");
                IdentMinus();
            }
            Console.WriteLine(identString + "]");
        }

        private void PrintWhile(WhileNode p_node)
        {
            Console.Write(identString + "[WHILE ");
            if (p_node.Condition != null)
                Print(p_node.Condition);
            Console.WriteLine();

            if (p_node.Body != null){
                IdentPlus();
                Print(p_node.Body);
                IdentMinus();
            }

            Console.WriteLine("\n" + identString + "]");
        }

        private void PrintVariable(VariableNode p_node)
        {
            if (p_node.IsAnonymous){
                Console.Write("[VAR_EXPRESSION ");
                Print(p_node.Expression);
            }else{
                Console.Write("[VARIABLE " + p_node.Name);
            }

            for (int i = 0; i < p_node.Indexes.Count; i++) {
                Print(p_node.Indexes[i]);
            }
            Console.Write("]");
        }

        private void PrintIndex(IndexNode p_node)
        {
            if (p_node.IsAnonymous){
                if ((p_node.GetType() != typeof(VariableNode)) || p_node.AccessType == VarAccessType.BRACKET){
                    Console.Write("[");
                    Print(p_node.Expression);
                    Console.Write("]");
                }else if (p_node.AccessType == VarAccessType.COLON){
                    Console.Write(":");
                    Print(p_node.Expression);
                }else if (p_node.AccessType == VarAccessType.DOT){
                    Console.Write(".");
                    Print(p_node.Expression);
                }
            }else{
                if ((p_node.GetType() != typeof(VariableNode)) || p_node.AccessType == VarAccessType.BRACKET){
                    Console.Write("[" +  p_node.Name + "]");
                }else if (p_node.AccessType == VarAccessType.COLON){
                    Console.Write(":" +  p_node.Name);
                }else if (p_node.AccessType == VarAccessType.DOT){
                    Console.Write("." +  p_node.Name);
                }
            }

        }

        private void PrintVarDeclaration(VarDeclarationNode p_node)
        {
            Console.Write(identString + "[VARIABLE DECLARATION " + p_node.Name);
            if (p_node.Initializer != null){
                Console.Write(" = ");
                Print(p_node.Initializer);
            }
            Console.WriteLine("]");
        }

        private void PrintassignmentOp(AssignmentNode p_node)
        {
            Console.Write("[ASSIGMENT_OP " + p_node.Assigned.Name);
            string op;
            switch (p_node.Op){
                case AssignmentOperatorType.ADDITION_ASSIGN:
                    op = " += ";
                    break;
                case AssignmentOperatorType.SUBTRACTION_ASSIGN:
                    op = " -= ";
                    break;
                case AssignmentOperatorType.MULTIPLICATION_ASSIGN:
                    op = " *= ";
                    break;
                case AssignmentOperatorType.DIVISION_ASSIGN:
                    op = " /= ";
                    break;
                default:
                    op = "";
                    break;
            }
            Console.Write(op);
            Print(p_node.Value);
            Console.Write("]");
        }

        private void PrintLogical(LogicalNode p_node)
        {
            Console.Write("[LOGICAL ");
            Print(p_node.Left);
            if (p_node.Op == OperatorType.AND) Console.Write(" and ");
            else if (p_node.Op == OperatorType.OR) Console.Write(" or ");
            else if (p_node.Op == OperatorType.XOR) Console.Write(" xor ");
            else if (p_node.Op == OperatorType.NAND) Console.Write(" nand ");
            else if (p_node.Op == OperatorType.NOR) Console.Write(" nor ");
            else if (p_node.Op == OperatorType.XNOR) Console.Write(" xnor ");
            else
                Error("Invalid logical operation.", p_node.Line);

            Print(p_node.Right);
            Console.Write("]");
        }

        private void PrintBlock(BlockNode p_node)
        {
            Console.WriteLine(identString + "[BLOCK");
            IdentPlus();
            foreach (Node n in p_node.Statements){
                Print(n);
            }
            IdentMinus();
            Console.WriteLine(identString + "]");
        }

        private void PrintFunctionCall(FunctionCallNode p_node)
        {
            Console.Write("[FUNCTION CALL ");
            Print(p_node.Variable);

            int counter = 0;
            foreach (List<Node> arguments in p_node.Calls){
                Console.Write("(");
                bool is_first = true;
                foreach (Node n in arguments){
                    if (is_first){
                        Print(n);
                        is_first = false;
                    }else{
                        Print(n);
                        Console.Write(", ");
                    }
                }
                Console.Write(")");
                if(p_node.IndexedAccess[counter] != null){
                    Console.Write(".indexedAccess(");
                    foreach(Node n in p_node.IndexedAccess[counter].Indexes)
                        Print(n);
                    Console.Write(")");
                }
                counter++;
            }
            Console.Write("]");
        }

        private void PrintReturn(ReturnNode p_node)
        {
            Console.Write(identString + "[RETURN");
            if (p_node.Expr != null){
                Console.Write(" ");
                Print(p_node.Expr);
            }
            Console.WriteLine("]");
        }

        private void PrintFunctionExpression(FunctionExpressionNode p_node)
        {
            IdentPlus();
            Console.Write("\n" + identString + "[FUNCTION EXPRESSION (");
            bool is_first = true;
            if(p_node.Parameters != null)
                foreach (string n in p_node.Parameters){
                    if (is_first){
                        Console.Write(n);
                        is_first = false;
                    }else{
                        Console.Write(", " + n);
                    }
                }
            Console.WriteLine(")");
            IdentPlus();
            foreach (Node n in p_node.Body){
                Print(n);
            }
            IdentMinus();
            Console.Write(identString + "]");
            IdentMinus();
        }

        private void PrintFunctionDeclaration(FunctionDeclarationNode p_node)
        {
            Console.Write(identString + "[FUNCTION DECLARATION ");
            Print(p_node.Variable);
            Console.Write(" (");
            bool is_first = true;

            if(p_node.Parameters != null)
                foreach (string n in p_node.Parameters){
                    if (is_first){
                        Console.Write(n);
                        is_first = false;
                    }else{
                        Console.Write(", " + n);
                    }
                }
            Console.WriteLine(")");
            IdentPlus();
            foreach (Node n in p_node.Body){
                Print(n);
            }
            IdentMinus();
            Console.WriteLine(identString + "]");
        }

        string GenIdentString()
        {
            string identString = "";
            for (int i = 0; i < identLevel; i++){
                identString += "    ";
            }
            return identString;
        }

        void IdentPlus()
        {
            identLevel++;
            identString = GenIdentString();
        }

        void IdentMinus()
        {
            identLevel--;
            identString = GenIdentString();
        }

        void Error(string p_msg, int p_line)
        {
            Console.WriteLine("Error: " + p_msg + " on line: " + p_line);
        }
    }
}
