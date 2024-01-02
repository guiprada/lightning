using System;
using System.Collections.Generic;

using lightningChunk;
namespace lightningAST
{
    public enum NodeType
    {
        BINARY,
        UNARY,
        LOGICAL,

        LITERAL,
        TABLE,
        LIST,
        VARIABLE,
        INDEX,
        GROUPING,

        PRINT,
        STMT_EXPR,
        IF,
        WHILE,
        FOR,

        VAR_DECLARATION,
        CONST_DECLARATION,
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

    public enum VarAccessType
    {
        DOT,
        COLON,
        BRACKET,
    }

    public struct ParameterInfo
    {
        public readonly string identifier;
        public readonly bool isMut;

        public ParameterInfo(string p_identifier, bool p_isMut)
        {
            identifier = p_identifier;
            isMut = p_isMut;
        }

        public override string ToString()
        {
            if (isMut)
                return "mut " + identifier;
            else
                return identifier;
        }
    }

    public struct CallInfo
    {
        public readonly List<Node> Arguments;
        public readonly List<IndexNode> Indexes;
        public CallInfo(List<Node> p_Arguments, List<IndexNode> p_Indexes)
        {
            Arguments = p_Arguments;
            Indexes = p_Indexes;
        }
    }

    public abstract class Node
    {
        public NodeType Type { get; private set; }
        public PositionData PositionData { get; private set; }

        public Node(NodeType p_Type, PositionData p_PositionData)
        {
            Type = p_Type;
            PositionData = p_PositionData;
        }
    }

    public class ProgramNode : Node
    {
        public List<Node> Statements { get; private set; }

        public ProgramNode(List<Node> p_Statements, PositionData p_PositionData)
            : base(NodeType.PROGRAM, p_PositionData)
        {
            Statements = p_Statements;
        }
    }

    public class BinaryNode : Node
    {
        public Node Left { get; private set; }
        public Node Right { get; private set; }
        public OperatorType Op { get; private set; }

        public BinaryNode(Node p_Left, OperatorType p_Op, Node p_Right, PositionData p_PositionData)
            : base(NodeType.BINARY, p_PositionData)
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
        public UnaryNode(OperatorType p_Op, Node p_Right, PositionData p_PositionData)
            : base(NodeType.UNARY, p_PositionData)
        {
            Op = p_Op;
            Right = p_Right;
        }
    }

    public class LiteralNode : Node
    {
        public object Value { get; private set; }
        public Type ValueType { get; private set; }
        public LiteralNode(Float p_Value, PositionData p_PositionData)
            : base(NodeType.LITERAL, p_PositionData)
        {
            Value = p_Value;
            ValueType = p_Value.GetType();
        }
        public LiteralNode(Integer p_Value, PositionData p_PositionData)
            : base(NodeType.LITERAL, p_PositionData)
        {
            Value = p_Value;
            ValueType = p_Value.GetType();
        }

        public LiteralNode(string p_Value, PositionData p_PositionData)
            : base(NodeType.LITERAL, p_PositionData)
        {
            Value = p_Value;
            ValueType = p_Value.GetType();
        }

        public LiteralNode(char p_Value, PositionData p_PositionData)
            : base(NodeType.LITERAL, p_PositionData)
        {
            Value = p_Value;
            ValueType = p_Value.GetType();
        }

        public LiteralNode(bool p_Value, PositionData p_PositionData)
            : base(NodeType.LITERAL, p_PositionData)
        {
            Value = p_Value;
            ValueType = p_Value.GetType();
        }

        public LiteralNode(PositionData p_PositionData)
            : base(NodeType.LITERAL, p_PositionData)
        {
            Value = null;
            ValueType = null;
        }

        public void SetNegative()
        {
            if (ValueType == typeof(Float))
                Value = -(Float)Value;
            else if (ValueType == typeof(Integer))
                Value = -(Integer)Value;
        }
    }

    public class GroupingNode : Node
    {
        public Node Expr { get; private set; }

        public GroupingNode(Node p_Expr, PositionData p_PositionData)
            : base(NodeType.GROUPING, p_PositionData)
        {
            Expr = p_Expr;
        }
    }

    public class StmtExprNode : Node
    {
        public Node Expr { get; private set; }
        public StmtExprNode(Node p_Expr, PositionData p_PositionData)
            : base(NodeType.STMT_EXPR, p_PositionData)
        {
            Expr = p_Expr;
        }
    }

    public class IfNode : Node
    {
        public Node Condition { get; private set; }
        public Node ThenBranch { get; private set; }
        public Node ElseBranch { get; private set; }


        public IfNode(Node p_Condition, Node p_ThenBranch, Node p_ElseBranch, PositionData p_PositionData)
            : base(NodeType.IF, p_PositionData)
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

        public ForNode(Node p_Initializer, Node p_Condition, Node p_Finalizer, Node p_Body, PositionData p_PositionData)
            : base(NodeType.FOR, p_PositionData)
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

        public WhileNode(Node p_Condition, Node p_Body, PositionData p_PositionData)
            : base(NodeType.WHILE, p_PositionData)
        {
            Condition = p_Condition;
            Body = p_Body;
        }
    }

    public class IndexNode : Node
    {
        public string Name { get; private set; }
        public Node Expression { get; private set; }
        public bool IsAnonymous { get; private set; }
        public VarAccessType AccessType { get; private set; }
        public IndexNode(string p_Name, VarAccessType p_AccessType, PositionData p_PositionData)
            : base(NodeType.INDEX, p_PositionData)
        {
            Name = p_Name;
            AccessType = p_AccessType;
            IsAnonymous = false;
        }
        public IndexNode(Node p_Expression, VarAccessType p_AccessType, PositionData p_PositionData)
            : base(NodeType.INDEX, p_PositionData)
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
        public bool IsAnonymous { get; private set; }

        public VariableNode(string p_Name, List<IndexNode> p_Indexes, PositionData p_PositionData)
            : base(NodeType.VARIABLE, p_PositionData)
        {
            Name = p_Name;
            Indexes = p_Indexes;
            IsAnonymous = false;
        }
        public VariableNode(Node p_Expression, List<IndexNode> p_Indexes, PositionData p_PositionData)
            : base(NodeType.VARIABLE, p_PositionData)
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

        public VarDeclarationNode(string p_Name, Node p_Initializer, PositionData p_PositionData)
            : base(NodeType.VAR_DECLARATION, p_PositionData)
        {
            Name = p_Name;
            Initializer = p_Initializer;
        }
    }

    public class ConstDeclarationNode : Node
    {
        public string Name { get; private set; }
        public Node Initializer { get; private set; }

        public ConstDeclarationNode(string p_Name, Node p_Initializer, PositionData p_PositionData)
            : base(NodeType.CONST_DECLARATION, p_PositionData)
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

        public AssignmentNode(VariableNode p_Assigned, Node p_Value, AssignmentOperatorType p_Op, PositionData p_PositionData)
        : base(NodeType.ASSIGMENT_OP, p_PositionData)
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

        public LogicalNode(Node p_Left, OperatorType p_Op, Node p_Right, PositionData p_PositionData)
            : base(NodeType.LOGICAL, p_PositionData)
        {
            Left = p_Left;
            Right = p_Right;
            Op = p_Op;
        }
    }

    public class BlockNode : Node
    {
        public List<Node> Statements { get; private set; }

        public BlockNode(List<Node> p_Statements, PositionData p_PositionData)
            : base(NodeType.BLOCK, p_PositionData)
        {
            Statements = p_Statements;
        }
    }

    public class FunctionCallNode : Node
    {
        public VariableNode Variable { get; private set; }
        public List<CallInfo> Calls { get; private set; }

        public FunctionCallNode(VariableNode p_Name, PositionData p_PositionData)
            : base(NodeType.FUNCTION_CALL, p_PositionData)
        {
            Variable = p_Name;
            Calls = new List<CallInfo>();
        }
    }

    public class ReturnNode : Node
    {
        public Node Expr { get; private set; }

        public ReturnNode(Node p_Expr, PositionData p_PositionData)
            : base(NodeType.RETURN, p_PositionData)
        {
            Expr = p_Expr;
        }
    }

    public class TableNode : Node
    {
        public Dictionary<Node, Node> Map { get; private set; }

        public TableNode(Dictionary<Node, Node> p_Map, PositionData p_PositionData)
            : base(NodeType.TABLE, p_PositionData)
        {
            Map = p_Map;
        }
    }

    public class ListNode : Node
    {
        public List<Node> Elements { get; private set; }

        public ListNode(List<Node> p_Elements, PositionData p_PositionData)
            : base(NodeType.LIST, p_PositionData)
        {
            Elements = p_Elements;
        }
    }

    public class FunctionExpressionNode : Node
    {
        public List<ParameterInfo> Parameters { get; private set; }
        public List<Node> Body { get; private set; }

        public FunctionExpressionNode(List<ParameterInfo> p_Parameters, List<Node> p_Body, PositionData p_PositionData, NodeType p_Type = NodeType.FUNCTION_EXPRESSION)
            : base(p_Type, p_PositionData)
        {
            Parameters = p_Parameters;
            Body = p_Body;
        }
    }

    public class MemberFunctionDeclarationNode : FunctionExpressionNode
    {
        public string Name { get; private set; }

        public MemberFunctionDeclarationNode(String p_Name, List<ParameterInfo> p_Parameters, List<Node> p_Body, PositionData p_PositionData)
            : base(p_Parameters, p_Body, p_PositionData, NodeType.MEMBER_FUNCTION_DECLARATION)
        {
            Name = p_Name;
        }
    }

    public class FunctionDeclarationNode : FunctionExpressionNode
    {
        public VariableNode Variable { get; private set; }

        public FunctionDeclarationNode(VariableNode p_Variable, List<ParameterInfo> p_Parameters, List<Node> p_Body, PositionData p_PositionData)
            : base(p_Parameters, p_Body, p_PositionData, NodeType.FUNCTION_DECLARATION)
        {
            Variable = p_Variable;
        }
    }
}
