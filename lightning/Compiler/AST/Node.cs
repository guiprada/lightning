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
        INDEX,
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
        FUNCTION_DECLARATION,
        MEMBER_FUNCTION_DECLARATION,
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
        DOT,
        COLON,
        BRACKET,
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

        public LiteralNode(int p_Line)
            : base(NodeType.LITERAL, p_Line)
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

    public class IndexNode : Node{
        public string Name { get; private set; }
        public Node Expression { get; private set; }
        public bool IsAnonymous { get; private set;}
        public VarAccessType AccessType { get; private set;}
        public IndexNode(string p_Name, VarAccessType p_AccessType, int p_Line)
            : base(NodeType.INDEX, p_Line)
        {
            Name = p_Name;
            AccessType = p_AccessType;
            IsAnonymous = false;
        }
        public IndexNode(Node p_Expression, VarAccessType p_AccessType, int p_Line)
            : base(NodeType.INDEX, p_Line)
        {
            Name = null;
            Expression = p_Expression;
            AccessType = p_AccessType;
            IsAnonymous = true;
        }
    }

    public class VariableNode : Node
    {
        public string Name { get; private set; }
        public List<IndexNode> Indexes { get; private set; }
        public Node Expression { get; private set; }
        public bool IsAnonymous { get; private set;}

        public VariableNode(string p_Name, List<IndexNode> p_Indexes, int p_Line)
            : base(NodeType.VARIABLE, p_Line)
        {
            Name = p_Name;
            Indexes = p_Indexes;
            IsAnonymous = false;
        }
        public VariableNode(Node p_Expression, List<IndexNode> p_Indexes, int p_Line)
            : base(NodeType.VARIABLE, p_Line)
        {
            Name = null;
            Expression = p_Expression;
            Indexes = p_Indexes;
            IsAnonymous = true;
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
        public AssignmentOperatorType Op { get; private set; }

        public AssignmentNode(VariableNode p_Assigned, Node p_Value, AssignmentOperatorType p_Op, int p_Line)
        : base(NodeType.ASSIGMENT_OP, p_Line)
        {
            Assigned = p_Assigned;
            Value = p_Value;
            Op = p_Op;
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

    public class FunctionCallNode : Node
    {
        public VariableNode Variable { get; private set; }
        public List<List<Node>> Calls { get; private set; }
        public List<List<IndexNode>> IndexedAccess { get; private set; }

        public FunctionCallNode(VariableNode p_Name, List<List<Node>> p_Calls, List<List<IndexNode>> p_IndexedAccess, int p_Line)
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

        public TableNode(List<Node> p_Elements, Dictionary<Node, Node> p_Table, int p_Line)
            : base(NodeType.TABLE, p_Line)
        {
            Elements = p_Elements;
            Table = p_Table;
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

    public class MemberFunctionDeclarationNode: FunctionExpressionNode
    {
        public string Name { get; private set;}

        public MemberFunctionDeclarationNode(String p_Name, List<string> p_Parameters, List<Node> p_Body, int p_Line)
            : base(p_Parameters, p_Body, p_Line, NodeType.MEMBER_FUNCTION_DECLARATION)
        {
            Name = p_Name;
        }
    }

    public class FunctionDeclarationNode : FunctionExpressionNode
    {
        public VariableNode Variable { get; private set;}

        public FunctionDeclarationNode(VariableNode p_Variable, List<string> p_Parameters, List<Node> p_Body, int p_Line)
            : base(p_Parameters, p_Body, p_Line, NodeType.FUNCTION_DECLARATION)
        {
            Variable = p_Variable;
        }
    }
}
