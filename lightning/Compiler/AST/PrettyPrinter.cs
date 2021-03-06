﻿using System;
using System.Collections.Generic;
using System.Text;

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
        public void Print(Node p_node)
        {
            NodeType this_type = p_node.Type;
            //Console.WriteLine(this_type.ToString());
            switch (this_type)
            {
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
                case NodeType.VAR_DECLARATION:
                    PrintVarDeclaration(p_node as VarDeclarationNode);
                    break;
                case NodeType.ASSIGMENT:
                    PrintAssignment(p_node as AssignmentNode);
                    break;
                case NodeType.ASSIGMENTOP:
                    PrintAssignmentOp(p_node as AssignmentOpNode);
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
                case NodeType.RANGE:
                    PrintRange(p_node as RangeNode);
                    break;
                case NodeType.FOREACH:
                    PrintForEach(p_node as ForEachNode);
                    break;
                case NodeType.TABLE:
                    PrintTable(p_node as TableNode);
                    break;
                default:
                    Error("Pretty Printer Received unkown node." + this_type.ToString(), p_node.Line);
                    break;
            }
        }

        public void PrintProgram(ProgramNode p_node)
        {
            Console.WriteLine("[PROGRAM");
            if (p_node.Statements != null)
            {
                IdentPlus();
                foreach (Node n in p_node.Statements)
                {
                    Print(n);
                }
                IdentMinus();
            }
            else
            {
                Error("Program does not contain any statements!", p_node.Line);
            }
            Console.WriteLine("]");
        }

        public void PrintBinary(BinaryNode p_node)
        {
            Console.Write("[BINARY ");
            Print(p_node.Left);
            if (p_node.Op == OperatorType.PLUS) Console.Write(" + ");
            else if (p_node.Op == OperatorType.MINUS) Console.Write(" - ");
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

        public void PrintUnary(UnaryNode p_node)
        {
            Console.Write("[UNARY ");
            if (p_node.Op == OperatorType.MINUS) Console.Write(" - ");
            else if (p_node.Op == OperatorType.NOT) Console.Write(" ! ");
            else if (p_node.Op == OperatorType.PLUS_PLUS) Console.Write(" ++ ");
            else if (p_node.Op == OperatorType.MINUS_MINUS) Console.Write(" -- ");
            else 
                Error("Invalid unary operator.", p_node.Line);
            Print(p_node.Right);
            Console.Write("]");
        }

        public void PrintLiteral(LiteralNode p_node)
        {
            Console.Write("[LITERAL ");
            if (p_node.ValueType == typeof(string))
            {
                Console.Write("\"" + (p_node.Value) + "\"");
            }
            else
            {
                Console.Write(p_node.Value.ToString());
            }
            Console.Write("]");
        }

        public void PrintTable(TableNode p_node)
        {
            Console.Write("[LIST ");
            if (p_node.elements != null)
            {
                bool first = true;
                foreach(Node n in p_node.elements)
                {
                    if (first)
                    {
                        Print(n);
                        first = false;
                    }
                    else
                    {
                        Console.Write(", ");
                        Print(n);
                    }
                }                
                foreach(KeyValuePair<Node,Node> entry in p_node.table)
                {
                    if (first)
                    {
                        Print(entry.Key);
                        Console.Write(" : ");
                        Print(entry.Value);
                        first = false;
                    }
                    else
                    {
                        Console.Write(", ");
                        Print(entry.Key);
                        Console.Write(" : ");
                        Print(entry.Value);
                    }
                }
            }
        }

        public void PrintGrouping(GroupingNode p_node)
        {
            Console.Write("[GROUPING ");
            Print(p_node.Expr);
            Console.Write("]");
        }

        public void PrintStmtExpr(StmtExprNode p_node)
        {
            Console.Write(identString + "[EXPR_STMT ");
            Print(p_node.Expr);
            Console.WriteLine("]");
        }

        public void PrintIf(IfNode p_node)
        {
            Console.Write(identString + "[IF ");
            Print(p_node.Condition);
            Console.WriteLine();
            IdentPlus();
            Print(p_node.ThenBranch);
            if (p_node.ElseBranch != null)
            {
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

        public void PrintFor(ForNode p_node)
        {
            Console.WriteLine(identString + "[FOR ");            
            if (p_node.Initializer != null)
            {
                IdentPlus();
                Console.WriteLine(identString + "[INITIALIZER");
                IdentPlus();
                Print(p_node.Initializer);
                IdentMinus();
                Console.WriteLine(identString + "]");
                IdentMinus();
            }
            if (p_node.Condition != null)
            {
                IdentPlus();
                Console.Write(identString + "[CONDITION ");
                Print(p_node.Condition);
                Console.WriteLine("]");
                IdentMinus();
            }
            if (p_node.Finalizer != null)
            {
                IdentPlus();
                Console.Write(identString + "[FINALIZER ");                
                Print(p_node.Finalizer);
                Console.WriteLine("]");
                IdentMinus();
            }
            if (p_node.Body != null)
            {
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

        public void PrintForEach(ForEachNode p_node)
        {
            Console.Write(identString + "[FOREACH ");
            Print(p_node.List);
            Print(p_node.Function);
            Console.WriteLine("]");
        }
        public void PrintRange(RangeNode p_node)
        {
            Console.Write(identString + "[RANGE ");
            Print(p_node.Tasks);
            Print(p_node.List);
            Print(p_node.Function);
            Console.WriteLine("]");
        }

        public void PrintWhile(WhileNode p_node)
        {
            Console.Write(identString + "[WHILE ");
            if (p_node.Condition != null)
                Print(p_node.Condition);
            Console.WriteLine();
            
            if (p_node.Body != null)
            {
                IdentPlus();
                Print(p_node.Body);
                IdentMinus();
            }

            Console.WriteLine("\n" + identString + "]");
        }

        public void PrintVariable(VariableNode p_node)
        {

            if (p_node.AccessType == VarAccessType.METHOD)
                Console.Write("[METHOD " + p_node.Name);
            else if (p_node.AccessType == VarAccessType.DOTTED)
                Console.Write("[IDENTIFIER " + p_node.Name);
            else
                Console.Write("[VARIABLE " + p_node.Name);

            for (int i = 0; i < p_node.Indexes.Count; i++) {
                if ((p_node.Indexes[i].GetType() != typeof(VariableNode)) || (p_node.Indexes[i]as VariableNode).AccessType == VarAccessType.PLAIN)
                {
                    Console.Write("[");
                    Print(p_node.Indexes[i]);
                    Console.Write("]");
                }
                else if ((p_node.Indexes[i] as VariableNode).AccessType == VarAccessType.METHOD)
                {
                    Console.Write(":");
                    Print(p_node.Indexes[i]);
                }
                else
                {
                    Console.Write(".");
                    Print(p_node.Indexes[i]);
                }
            }
            Console.Write("]");
        }

        public void PrintVarDeclaration(VarDeclarationNode p_node)
        {
            Console.Write(identString + "[VARIABLE DECLARATION " + p_node.Name);
            if (p_node.Initializer != null)
            {
                Console.Write(" = ");
                Print(p_node.Initializer);
            }
            Console.WriteLine("]");
        }

        public void PrintAssignment(AssignmentNode p_node)
        {
            Console.Write("[ASSIGMENT " + p_node.Assigned.Name + " = ");
            Print(p_node.Value);
            Console.Write("]");
        }

        public void PrintAssignmentOp(AssignmentOpNode p_node)
        {
            Console.Write("[ASSIGMENTOP " + p_node.Assigned.Name);
            string op;
            switch (p_node.Op) {
                case OperatorType.PLUS:
                    op = " += ";
                    break;
                case OperatorType.MINUS:
                    op = " -= ";
                    break;
                case OperatorType.MULTIPLICATION:
                    op = " *= ";
                    break;
                case OperatorType.DIVISION:
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

        public void PrintLogical(LogicalNode p_node)
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

        public void PrintBlock(BlockNode p_node)
        {
            Console.WriteLine(identString + "[BLOCK");
            IdentPlus();
            foreach (Node n in p_node.Statements)
            {
                Print(n);
            }
            IdentMinus();
            Console.WriteLine(identString + "]");
        }

        public void PrintFunctionCall(FunctionCallNode p_node)
        {
            Console.Write("[FUNCTION CALL ");
            Print(p_node.Name);

            int counter = 0;
            foreach (List<Node> arguments in p_node.Calls)
            {
                Console.Write("(");
                bool is_first = true;
                foreach (Node n in arguments)
                {
                    if (is_first)
                    {
                        Print(n);
                        is_first = false;
                    }
                    else
                    {
                        Print(n);
                        Console.Write(", ");
                    }
                }
                Console.Write(")");
                if(p_node.GetVars[counter] != null)
                {
                    Console.Write(".getVar(");
                    foreach(Node n in p_node.GetVars[counter].Indexes)
                        Print(n);
                    Console.Write(")");
                }
                counter++;
            }
            Console.Write("]");
        }

        public void PrintReturn(ReturnNode p_node)
        {
            Console.Write(identString + "[RETURN");
            if (p_node.Expr != null)
            {
                Console.Write(" ");
                Print(p_node.Expr);
            }
            Console.WriteLine("]");
        }

        public void PrintFunctionExpression(FunctionExpressionNode p_node)
        {
            IdentPlus();
            Console.Write("\n" + identString + "[FUNCTION EXPRESSION (");
            bool is_first = true;
            foreach (string n in p_node.Parameters)
            {
                if (is_first)
                {
                    Console.Write(n);
                    is_first = false;
                }
                else
                {
                    Console.Write(", " + n);
                }
            }
            Console.WriteLine(")");
            IdentPlus();
            foreach (Node n in p_node.Body)
            {
                Print(n);
            }
            IdentMinus();
            Console.Write(identString + "]");
            IdentMinus();
        }

        public void PrintFunctionDeclaration(FunctionDeclarationNode p_node)
        {
            Console.Write(identString + "[FUNCTION DECLARATION " + p_node.Name + " (");
            bool is_first = true;
            foreach (string n in p_node.Parameters)
            {
                if (is_first)
                {
                    Console.Write(n);
                    is_first = false;
                }
                else
                {
                    Console.Write(", " + n);
                }
            }
            Console.WriteLine(")");
            IdentPlus();
            foreach (Node n in p_node.Body)
            {
                Print(n);
            }
            IdentMinus();
            Console.WriteLine(identString + "]");
        }

        string GenIdentString()
        {
            string identString = "";
            for (int i = 0; i < identLevel; i++)
            {
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
