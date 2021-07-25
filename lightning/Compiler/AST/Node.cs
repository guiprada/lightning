using System;
using System.Collections.Generic;

#if DOUBLE
    using Float = System.Double;
    using Integer = System.Int64;
#else
    using Float = System.Single;
    using Integer = System.Int32;
#endif

namespace lightning
{
    public enum NodeType
    {
        BINARY,
        UNARY,
        LOGICAL,

        LITERAL,
        TABLE,
        VARIABLE,
        GROUPING,

        PRINT,
        STMT_EXPR,
        IF,
        WHILE,
        FOR,

        VAR_DECLARATION,
        ASSIGMENT,
        ASSIGMENT_OP,

        BLOCK,
        FUNCTION_CALL,
        ANONYMOUS_FUNCTION_CALL,
        FUNCTION_DECLARATION,
        FUNCTION_EXPRESSION,

        RETURN,
        PROGRAM,
    }

    public enum OperatorType
    {
        ADDITION,
        INCREMENT,
        SUBTRACTION,
        DECREMENT,
        MULTIPLICATION,
        DIVISION,

        APPEND,

        NOT,
        AND,
        OR,
        XOR,
        NAND,
        NOR,
        XNOR,

        NOT_EQUAL,
        EQUAL,
        GREATER,
        GREATER_EQUAL,
        LESS,
        LESS_EQUAL,
        VOID
    }

    public enum AssignmentOperatorType
    {
        ASSIGN,
        ADDITION_ASSIGN,
        SUBTRACTION_ASSIGN,
        MULTIPLICATION_ASSIGN,
        DIVISION_ASSIGN,
    }

    public enum VarAccessType
    {
        DOTTED,
        METHOD,
        PLAIN,
    }

    public abstract class Node
    {
        public NodeType Type { get; private set; }
        public int Line { get; private set; }

        public Node(NodeType p_Type, int p_Line)
        {
            Type = p_Type;
            Line = p_Line;
        }
    }

    public class ProgramNode : Node
    {
        public List<Node> Statements { get; private set; }

        public ProgramNode(List<Node> p_Statements, int p_Line)
            : base(NodeType.PROGRAM, p_Line)
        {
            Statements = p_Statements;
        }
    }

    public class BinaryNode : Node
    {
        public Node Left { get; private set; }
        public Node Right { get; private set; }
        public OperatorType Op { get; private set; }

        public BinaryNode(Node p_Left, OperatorType p_Op, Node p_Right, int p_Line)
            : base(NodeType.BINARY, p_Line)
        {
            Left = p_Left;
            Op = p_Op;
            Right = p_Right;
        }
    }

    public class UnaryNode : Node
    {
        public Node Right { get; private set; }
        public OperatorType Op { get; private set; }
        public UnaryNode(OperatorType p_Op, Node p_Right, int p_Line)
            : base(NodeType.UNARY, p_Line)
        {
            Op = p_Op;
            Right = p_Right;
        }
    }

    public class LiteralNode : Node
    {
        public object Value { get; private set; }
        public Type ValueType { get; private set; }
        public LiteralNode(Float p_Value, int p_Line)
            : base(NodeType.LITERAL, p_Line)
        {
            Value = p_Value;
            ValueType = p_Value.GetType();
        }
        public LiteralNode(Integer p_Value, int p_Line)
            : base(NodeType.LITERAL, p_Line)
        {
            Value = p_Value;
            ValueType = p_Value.GetType();
        }

        public LiteralNode(string p_Value, int p_Line)
            : base(NodeType.LITERAL, p_Line)
        {
            Value = p_Value;
            ValueType = p_Value.GetType();
        }

        public LiteralNode(char p_Value, int p_Line)
            : base(NodeType.LITERAL, p_Line)
        {
            Value = p_Value;
            ValueType = p_Value.GetType();
        }

        public LiteralNode(bool p_Value, int p_Line)
            : base(NodeType.LITERAL, p_Line)
        {
            Value = p_Value;
            ValueType = p_Value.GetType();
        }

        public LiteralNode(int p_line)
            : base(NodeType.LITERAL, p_line)
        {
            Value = null;
            ValueType = null;
        }

        public void SetNegative(){
            if(ValueType == typeof(Float))
                Value = -(Float)Value;
            else if(ValueType == typeof(Integer))
                Value = -(Integer)Value;
        }
    }

    public class GroupingNode : Node
    {
        public Node Expr { get; private set; }

        public GroupingNode(Node p_Expr, int p_Line)
            : base(NodeType.GROUPING, p_Line)
        {
            Expr = p_Expr;
        }
    }

    public class StmtExprNode : Node
    {
        public Node Expr { get; private set; }
        public StmtExprNode(Node p_Expr, int p_Line)
            : base(NodeType.STMT_EXPR, p_Line)
        {
            Expr = p_Expr;
        }
    }

    public class IfNode : Node
    {
        public Node Condition { get; private set; }
        public Node ThenBranch { get; private set; }
        public Node ElseBranch { get; private set; }


        public IfNode(Node p_Condition, Node p_ThenBranch, Node p_ElseBranch, int p_Line)
            : base(NodeType.IF, p_Line)
        {
            Condition = p_Condition;
            ThenBranch = p_ThenBranch;
            ElseBranch = p_ElseBranch;
        }
    }

    public class ForNode : Node
    {
        public Node Initializer { get; private set; }
        public Node Condition { get; private set; }
        public Node Finalizer { get; private set; }
        public Node Body { get; private set; }

        public ForNode(Node p_Initializer, Node p_Condition, Node p_Finalizer, Node p_Body, int p_Line)
            : base(NodeType.FOR, p_Line)
        {
            Initializer = p_Initializer;
            Condition = p_Condition;
            Finalizer = p_Finalizer;
            Body = p_Body;
        }
    }

    public class WhileNode : Node
    {
        public Node Condition { get; private set; }
        public Node Body { get; private set; }

        public WhileNode(Node p_Condition, Node p_Body, int p_Line)
            : base(NodeType.WHILE, p_Line)
        {
            Condition = p_Condition;
            Body = p_Body;
        }
    }

    public class VariableNode : Node
    {
        public string Name { get; private set; }
        public List<Node> Indexes { get; private set; }

        public VarAccessType AccessType { get; set;}

        public VariableNode(string p_Name, List<Node> p_Indexes, VarAccessType p_AccessType, int p_Line)
            : base(NodeType.VARIABLE, p_Line)
        {
            Name = p_Name;
            Indexes = p_Indexes;
            AccessType = p_AccessType;
        }
    }

    public class VarDeclarationNode : Node
    {
        public string Name { get; private set; }
        public Node Initializer { get; private set; }
        public VarDeclarationNode(string p_Name, Node p_Initializer, int p_Line)
            : base(NodeType.VAR_DECLARATION, p_Line)
        {
            Name = p_Name;
            Initializer = p_Initializer;
        }
    }

    public class AssignmentNode : Node
    {
        public VariableNode Assigned { get; private set; }
        public Node Value { get; private set; }

        public AssignmentNode(VariableNode p_assigned, Node p_Value, int p_Line)
        : base(NodeType.ASSIGMENT, p_Line)
        {
            Assigned = p_assigned;
            Value = p_Value;
        }
    }

    public class AssignmentOpNode : Node
    {
        public VariableNode Assigned { get; private set; }
        public Node Value { get; private set; }

        public AssignmentOperatorType Op { get; private set; }

        public AssignmentOpNode(VariableNode p_assigned, Node p_Value, AssignmentOperatorType p_op, int p_Line)
        : base(NodeType.ASSIGMENT_OP, p_Line)
        {
            Assigned = p_assigned;
            Value = p_Value;
            Op = p_op;
        }
    }

    public class LogicalNode : Node
    {
        public Node Left { get; private set; }
        public Node Right { get; private set; }
        public OperatorType Op { get; private set; }

        public LogicalNode(Node p_Left, OperatorType p_Op, Node p_Right, int p_Line)
            : base(NodeType.LOGICAL, p_Line)
        {
            Left = p_Left;
            Right = p_Right;
            Op = p_Op;
        }
    }

    public class BlockNode : Node
    {
        public List<Node> Statements { get; private set; }

        public BlockNode(List<Node> p_Statements, int p_Line)
            : base(NodeType.BLOCK, p_Line)
        {
            Statements = p_Statements;
        }
    }

    public class AnonymousFunctionCallNode : Node{
        public Node FunctionCall { get; private set; }
        public VarDeclarationNode Declaration { get; private set; }
        public VariableNode Variable { get; private set; }

        public AnonymousFunctionCallNode(Node p_functionCall, VarDeclarationNode p_declaration, VariableNode p_variable, int p_Line)
            : base (NodeType.ANONYMOUS_FUNCTION_CALL, p_Line)
        {
            FunctionCall = p_functionCall;
            Declaration = p_declaration;
            Variable = p_variable;
        }
    }

    public class FunctionCallNode : Node
    {
        public VariableNode Variable { get; private set; }
        public List<List<Node>> Calls { get; private set; }
        public List<VariableNode> IndexedAccess { get; private set; }

        public FunctionCallNode(VariableNode p_Name, List<List<Node>> p_Calls, List<VariableNode> p_IndexedAccess, int p_Line)
            : base(NodeType.FUNCTION_CALL, p_Line)
        {
            Variable = p_Name;
            Calls = p_Calls;
            IndexedAccess = p_IndexedAccess;
        }
    }

    public class ReturnNode : Node
    {
        public Node Expr { get; private set; }
        public ReturnNode(Node p_Expr, int p_Line)
            : base(NodeType.RETURN, p_Line)
        {
            Expr = p_Expr;
        }
    }

    public class TableNode : Node
    {
        public List<Node> Elements { get; private set; }
        public Dictionary<Node, Node> Table { get; private set; }

        public TableNode(List<Node> p_elements, Dictionary<Node, Node> p_table, int p_Line)
            : base(NodeType.TABLE, p_Line)
        {
            Elements = p_elements;
            Table = p_table;
        }
    }

    public class FunctionExpressionNode : Node
    {
        public List<string> Parameters { get; private set; }
        public List<Node> Body { get; private set; }

        public FunctionExpressionNode(List<string> p_Parameters, List<Node> p_Body, int p_Line, NodeType p_Type = NodeType.FUNCTION_EXPRESSION)
            : base(p_Type, p_Line)
        {
            Parameters = p_Parameters;
            Body = p_Body;
        }
    }

    public class FunctionDeclarationNode : FunctionExpressionNode
    {
        public string Name { get; private set; }

        public FunctionDeclarationNode(string p_Name, List<string> p_Parameters, List<Node> p_Body, int p_Line)
            : base(p_Parameters, p_Body, p_Line, NodeType.FUNCTION_DECLARATION)
        {
            Name = p_Name;
        }
    }
}
