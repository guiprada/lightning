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
    public class Parser
    {
        private List<Token> tokens;
        private bool hasParsed;
        public bool HasParsed { get { return hasParsed;} }
        private Node ast;
        private int current;
        public List<string> Errors { get; private set; }
        public List<string> Warnings { get; private set; }
        private int anonymousCounter;

        public Node ParsedTree
        {
            get
            {
                if (hasParsed == false)
                {
                    try
                    {
                        Parse();
                        if(Errors.Count == 0)
                            hasParsed = true;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }
                return ast;
            }
        }

        public Parser(List<Token> p_tokens)
        {
            tokens = p_tokens;
            hasParsed = false;
            Errors = new List<string>();
            Warnings = new List<string>();
            current = 0;
            anonymousCounter = 0;
        }

        void Parse()
        {
            ast = Program();
        }

        //////////////////////////////////////////////////////////////////////

        Node Program()
        {
            var statements = new List<Node>();

            while (!IsAtEnd())
            {
                statements.Add(Declaration());
            }

            return new ProgramNode(statements, tokens[current].Line);
        }

        Node Declaration()
        {
            if (Match(TokenType.VAR))
            {
                return VarDeclaration();
            }
            else if(Match(TokenType.FUN))
            {
                return FunctionDeclaration();
            }
            else
            {
                return Statement();
            }
        }

        Node CompoundVar()
        {
            Consume(TokenType.IDENTIFIER, "Expected identifier.", true);
            TokenString last_token = Previous() as TokenString;
            string name = last_token.value;
            int line = last_token.Line;

            return IndexedAccess(name, line);
        }

        Node IndexedAccess(string name, int line) {
            List<Node> indexes = new List<Node>();
            while (Check(TokenType.LEFT_BRACKET) || Check(TokenType.DOT) || Check(TokenType.COLON))
            {
                if (Match(TokenType.LEFT_BRACKET))
                {
                    indexes.Add(Expression());
                    Consume(TokenType.RIGHT_BRACKET, "Expected ']' after 'compoundVar identifier'", true);
                }
                else if (Match(TokenType.DOT))
                {
                    Match(TokenType.IDENTIFIER);
                    string this_name = (Previous() as TokenString).value;
                    VariableNode index = new VariableNode(this_name, new List<Node>(), VarAccessType.DOTTED, Previous().Line);
                    indexes.Add(index);
                }
                else if (Match(TokenType.COLON))
                {// Method Call
                    Match(TokenType.IDENTIFIER);
                    string this_name = (Previous() as TokenString).value;
                    VariableNode index = new VariableNode(this_name, new List<Node>(), VarAccessType.METHOD, Previous().Line);
                    indexes.Add(index);
                }
            }
            return new VariableNode(name, indexes, VarAccessType.PLAIN, line);
        }

        Node VarDeclaration()
        {
            TokenString name = Consume(TokenType.IDENTIFIER, "Expected 'variable identifier' after 'var'.", true) as TokenString;
            Node initializer;
            if (Match(TokenType.EQUAL))
                initializer = Expression();
            else
                initializer = null;

            Elide(TokenType.SEMICOLON);
            return new VarDeclarationNode(name.value, initializer, name.Line);
        }

        List<string> Parameters(){
            List<string> parameters = new List<string>();
            bool has_parameter = Check(TokenType.IDENTIFIER);
            while (has_parameter)
            {
                TokenString new_parameter = Consume(TokenType.IDENTIFIER, "Expected 'identifier' as 'function parameter'.", true) as TokenString;
                parameters.Add(new_parameter.value);
                if (Check(TokenType.COMMA))
                {
                    Consume(TokenType.COMMA, "Expected ',' separating parameter list", true);
                    has_parameter = Check(TokenType.IDENTIFIER);
                }
                else
                {
                    has_parameter = false;
                }
            }
            Consume(TokenType.RIGHT_PAREN, "Expected ')' after 'function expression'.", true);

            return parameters;
        }

        Node FunExpr()
        {
            Token fun = Consume(TokenType.FUN, "Expected 'function' to start 'function expression'.", true);
            Consume(TokenType.LEFT_PAREN, "Expected '(' after 'function expression'.", true);

            List<string> parameters = Parameters();

            Node body = Statement();
            List<Node> statements;
            if (body.Type == NodeType.BLOCK)
            {
                statements = (body as BlockNode).Statements;
            }
            else
            {
                statements = new List<Node>();
                statements.Add(body);
            }

            if (statements.Count == 0 || statements[^1].GetType() != typeof(ReturnNode))
            {
                statements.Add(new ReturnNode(null, fun.Line));
            }

            if (parameters == null) Error("parameters null");
            if (statements == null) Error("statements null");
            if (fun == null) Error("fun null");
            return new FunctionExpressionNode(parameters, statements, fun.Line);
        }

        Node FunctionDeclaration()
        {
            TokenString name = Consume(TokenType.IDENTIFIER, "Expected 'function identifier'.", true) as TokenString;
            Consume(TokenType.LEFT_PAREN, "Expected '(' after 'function declaration'.", true);

            List<string> parameters = Parameters();

            Node body = Statement();
            List<Node> statements;
            if(body.Type == NodeType.BLOCK)
            {
                statements = (body as BlockNode).Statements;
            }
            else
            {
                statements = new List<Node>();
                statements.Add(body);
            }

            if (statements.Count == 0 || statements[^1].GetType() != typeof(ReturnNode))
            {
                statements.Add(new ReturnNode(null, name.Line));
            }


            return new FunctionDeclarationNode(name.value, parameters, statements, name.Line);
        }

        Node Statement()
        {
            if (Match(TokenType.FOR))
                return For();
            else if (Match(TokenType.RETURN))
                return Return();
            else if (Match(TokenType.IF))
                return If();
            else if (Match(TokenType.WHILE))
                return While();
            else if (Match(TokenType.LEFT_BRACE))
                return Block();
            else
                return StmtExpr();
        }

        Node Return()
        {
            int line = Previous().Line;
            Node expr = Expression();
            Elide(TokenType.SEMICOLON);
            return new ReturnNode(expr, line);
        }

        Node For()
        {
            int line = Previous().Line;
            Consume(TokenType.LEFT_PAREN, "Expected '(' after 'for'.", true);

            Node initializer;
            if (Match(TokenType.SEMICOLON))
            {
                initializer = null;
            }
            else if (Match(TokenType.VAR))
            {
                initializer = VarDeclaration();
            }
            else
            {
                initializer = StmtExpr();
            }

            Node condition;
            if (!Check(TokenType.SEMICOLON))
            {
                condition = Expression();
            }
            else
            {
                condition = null;
            }
            if (condition == null) Error("'For' 'condition' can not be null.");

            Node finalizer = null;
            if (Match(TokenType.SEMICOLON))
            {
                if (!Check(TokenType.RIGHT_PAREN))
                {
                    finalizer = Expression();
                }
            }
            Consume(TokenType.RIGHT_PAREN, "Expected ')' after 'for' finalizer.", true);

            Node body = Statement();

            return new ForNode(initializer, condition, finalizer, body, line);
        }

        Node While()
        {
            int line = Previous().Line;
            Consume(TokenType.LEFT_PAREN, "Expected '(' after 'while'.", true);
            Node condition = Expression();
            if (condition == null) Error("'while' 'condition' can not be null.");
            Consume(TokenType.RIGHT_PAREN, "Expected ')' after 'while' condition.", true);

            Node body = Statement();

            return new WhileNode(condition, body, line);
        }

        Node If()
        {
            int line = Previous().Line;
            Token start = Consume(TokenType.LEFT_PAREN, "Expected '(' after 'if'.", true);
            Node condition = Expression();
            if (condition == null) Error("'if' 'condition' can not be null.");
            Consume(TokenType.RIGHT_PAREN, "Expected ')' after 'if' condition.", true);

            Node then_branch = Statement();

            Node else_branch = null;
            if (Match(TokenType.ELSE))
            {
                else_branch = Statement();
            }

            return new IfNode(condition, then_branch, else_branch, line);
        }

        Node Block()
        {
            int line = Previous().Line;
            List<Node> statements = new List<Node>();

            while(!Check(TokenType.RIGHT_BRACE) && !IsAtEnd())
            {
                statements.Add(Declaration());
            }
            Consume(TokenType.RIGHT_BRACE, "Expected '}' to terminate 'block'.", true);
            return new BlockNode(statements, line);
        }

        Node StmtExpr()
        {
            Node expr = Expression();

            Elide(TokenType.SEMICOLON);
            return new StmtExprNode(expr, expr.Line);
        }

        Node Expression()
        {
            return Assignment();
        }

        Node Assignment()
        {
            Node assigned = LogicalOr();

            if (Match(TokenType.PLUS_EQUAL))
            {
                Node value = Assignment();

                if (assigned.Type == NodeType.VARIABLE)
                    return new AssignmentOpNode((VariableNode)assigned, value, AssignmentOperatorType.ADDITION_ASSIGN, assigned.Line);
                else
                    Error("Invalid assignment.");
            }
            else if (Match(TokenType.PLUS_PLUS))
            {
                Node value = new LiteralNode(1, assigned.Line);

                if (assigned.Type == NodeType.VARIABLE)
                    return new AssignmentOpNode((VariableNode)assigned, value, AssignmentOperatorType.ADDITION_ASSIGN, assigned.Line);
                else
                    Error("Invalid assignment.");
            }
            else if (Match(TokenType.MINUS_EQUAL))
            {
                Node value = Assignment();

                if (assigned.Type == NodeType.VARIABLE)
                    return new AssignmentOpNode((VariableNode)assigned, value, AssignmentOperatorType.SUBTRACTION_ASSIGN, assigned.Line);
                else
                    Error("Invalid assignment.");
            }
            else if (Match(TokenType.MINUS_MINUS))
            {
                Node value = new LiteralNode(1, assigned.Line);

                if (assigned.Type == NodeType.VARIABLE)
                    return new AssignmentOpNode((VariableNode)assigned, value, AssignmentOperatorType.SUBTRACTION_ASSIGN, assigned.Line);
                else
                    Error("Invalid assignment.");
            }
            else if (Match(TokenType.STAR_EQUAL))
            {
                Node value = Assignment();

                if (assigned.Type == NodeType.VARIABLE)
                    return new AssignmentOpNode((VariableNode)assigned, value, AssignmentOperatorType.MULTIPLICATION_ASSIGN, assigned.Line);
                else
                    Error("Invalid assignment.");
            }
            if (Match(TokenType.SLASH_EQUAL))
            {
                Node value = Assignment();

                if (assigned.Type == NodeType.VARIABLE)
                    return new AssignmentOpNode((VariableNode)assigned, value, AssignmentOperatorType.DIVISION_ASSIGN, assigned.Line);
                else
                    Error("Invalid assignment.");
            }
            else if (Match(TokenType.EQUAL))
            {
                Node value = Assignment();

                if (assigned.Type == NodeType.VARIABLE)
                    return new AssignmentNode((VariableNode)assigned, value, assigned.Line);
                else
                    Error("Invalid assignment.");
            }

            return assigned;
        }

        Node LogicalOr()
        {
            Node left = LogicalAnd();

            while (Match(TokenType.OR))
            {
                Node right = LogicalAnd();

                left = new LogicalNode(left, OperatorType.OR, right, left.Line);
            }

            return left;
        }

        Node LogicalAnd()
        {
            Node left = LogicalXor();

            while (Match(TokenType.AND))
            {
                Node right = LogicalXor();

                left = new LogicalNode(left, OperatorType.AND, right, left.Line);
            }

            return left;
        }

        Node LogicalXor()
        {
            Node left = LogicalNand();

            while (Match(TokenType.XOR))
            {
                Node right = LogicalNand();

                left = new LogicalNode(left, OperatorType.XOR, right, left.Line);
            }

            return left;
        }

        Node LogicalNand()
        {
            Node left = LogicalNor();

            while (Match(TokenType.NAND))
            {
                Node right = LogicalNor();

                left = new LogicalNode(left, OperatorType.NAND, right, left.Line);
            }

            return left;
        }

        Node LogicalNor()
        {
            Node left = LogicalXnor();

            while (Match(TokenType.NOR))
            {
                Node right = LogicalXnor();

                left = new LogicalNode(left, OperatorType.NOR, right, left.Line);
            }

            return left;
        }

        Node LogicalXnor()
        {
            Node left = Equality();

            while (Match(TokenType.XNOR))
            {
                Node right = Equality();

                left = new LogicalNode(left, OperatorType.XNOR, right, left.Line);
            }

            return left;
        }

        Node Equality()
        {
            Node left = Comparison();

            while(Match(TokenType.BANG_EQUAL, TokenType.EQUAL_EQUAL))
            {
                Token op = Previous();
                OperatorType this_op;

                if (op.Type == TokenType.BANG_EQUAL) this_op = OperatorType.NOT_EQUAL;
                else this_op = OperatorType.EQUAL;

                Node right = Comparison();
                left = new BinaryNode(left, this_op, right, op.Line);
            }

            return left;
        }

        Node Comparison()
        {
            Node left = Addition();

            while (Match(TokenType.GREATER, TokenType.GREATER_EQUAL, TokenType.LESS, TokenType.LESS_EQUAL))
            {
                Token op = Previous();
                OperatorType this_op;

                if (op.Type == TokenType.GREATER) this_op = OperatorType.GREATER;
                else if( op.Type == TokenType.GREATER_EQUAL) this_op = OperatorType.GREATER_EQUAL;
                else if (op.Type == TokenType.LESS) this_op = OperatorType.LESS;
                else this_op = OperatorType.LESS_EQUAL;

                Node right = Addition();
                left = new BinaryNode(left, this_op, right, op.Line);
            }

            return left;
        }

        Node Addition()
        {
            Node left = Multiplication();

            while (Match(TokenType.MINUS, TokenType.PLUS, TokenType.APPEND))
            {
                Token op = Previous();
                OperatorType this_op;

                if (op.Type == TokenType.MINUS) this_op = OperatorType.SUBTRACTION;
                else if (op.Type == TokenType.PLUS) this_op = OperatorType.ADDITION;
                else this_op = OperatorType.APPEND;

                Node right = Multiplication();
                left = new BinaryNode(left, this_op, right, op.Line);
            }

            return left;
        }

        Node Multiplication()
        {
            Node left = Unary();

            while (Match(TokenType.SLASH, TokenType.STAR))
            {
                Token op = Previous();
                OperatorType this_op;

                if (op.Type == TokenType.SLASH) this_op = OperatorType.DIVISION;
                else this_op = OperatorType.MULTIPLICATION;

                Node right = Unary();
                left = new BinaryNode(left, this_op, right, op.Line);
            }

            return left;
        }

        Node Unary()
        {
            if (Match(TokenType.BANG, TokenType.MINUS, TokenType.MINUS_MINUS, TokenType.PLUS_PLUS))
            {
                Token op = Previous();
                OperatorType this_op;

                if (op.Type == TokenType.BANG)
                    this_op = OperatorType.NOT;
                else if (op.Type == TokenType.PLUS_PLUS)
                    this_op = OperatorType.INCREMENT;
                else if (op.Type == TokenType.MINUS_MINUS)
                    this_op = OperatorType.DECREMENT;
                else if (op.Type == TokenType.MINUS)
                    this_op = OperatorType.SUBTRACTION;
                else
                {
                    Error("Unkown unary operator!");
                    this_op = OperatorType.VOID;
                }

                Node right = Unary();
                return new UnaryNode(this_op, right, op.Line);
            }

            return FunctionCall();
        }

        Node FunctionCall()
        {
            Node maybe_func = Primary();

            if (Check(TokenType.LEFT_PAREN))
            {
                List<List<Node>> calls = new List<List<Node>>();
                List<VariableNode> indexed_access = new List<VariableNode>();
                FunctionCallNode function_call_node = new FunctionCallNode(
                    (maybe_func as VariableNode),
                    calls,
                    indexed_access,
                    maybe_func.Line);
                function_call_node = FinishFunctionCall(function_call_node);
                return function_call_node;
            }
            if(Check(TokenType.COLON)){
                return AnonymousFunctionCall((LiteralNode)maybe_func);
            }
            return maybe_func;
        }

        Node AnonymousFunctionCall(LiteralNode node){
            if(Check(TokenType.COLON))
            {

                string name = "*(" + anonymousCounter + "_" + node.Value.ToString();
                anonymousCounter++;

                VarDeclarationNode declaration_node = new VarDeclarationNode(name, node, node.Line);

                VariableNode variableNode = (VariableNode)IndexedAccess(name, node.Line);

                List<List<Node>> calls = new List<List<Node>>();
                List<VariableNode> indexed_access = new List<VariableNode>();
                FunctionCallNode function_call_node = new FunctionCallNode(

                    variableNode,
                    calls,
                    indexed_access,
                    node.Line);

                function_call_node = FinishFunctionCall(function_call_node);
                return new AnonymousFunctionCallNode(function_call_node, declaration_node, variableNode, node.Line);
            }
            Error("Expected ':' after anonymous function call.");
            return node;
        }

        FunctionCallNode FinishFunctionCall(FunctionCallNode function_call_node)
        {
            bool go_on = true;
            while(Check(TokenType.LEFT_PAREN) && go_on)
            {
                function_call_node.Calls.Add(Arguments());

                if (Check(TokenType.DOT) || Check(TokenType.LEFT_BRACKET))
                    function_call_node.IndexedAccess.Add(IndexedAccess(function_call_node.Variable.Name, function_call_node.Line) as VariableNode);
                else{
                    function_call_node.IndexedAccess.Add(null);
                    if (!Match(TokenType.PIPE))
                        go_on = false;
                }
            }

            return function_call_node;
        }

        List<Node> Arguments(){
            Consume(TokenType.LEFT_PAREN, "Expected '(' before 'function call' arguments.", true);
            List<Node> arguments = new List<Node>();
            if (!Check(TokenType.RIGHT_PAREN))
            {
                do{
                    arguments.Add(Expression());
                }while (Match(TokenType.COMMA));
            }
            Consume(TokenType.RIGHT_PAREN, "Expected ')' after 'function call' arguments.", true);

            return arguments;
        }

        TableNode TableEntry(TableNode table_node)
        {
            List<Node> elements = table_node.Elements;
            Dictionary<Node, Node> table = table_node.Table;
            if (Match(TokenType.IDENTIFIER))
            {
                Node item = new LiteralNode((Previous() as TokenString).value, Previous().Line);

                Consume(TokenType.COLON, "Expected ':' separating key:values in table constructor", true);

                Node value = Primary();

                table.Add(item, value);
            }
            else if(!Check(TokenType.RIGHT_BRACKET))
            {
                Node item = Primary();
                if(Check(TokenType.COLON)){
                    Consume(TokenType.COLON, "Expected ':' separating key:values in table constructor", true);
                    if(((LiteralNode)item).ValueType == typeof(Float)){
                        LiteralNode number_value = (LiteralNode)item;
                        if((Float)(number_value.Value) == elements.Count)
                            elements.Add(Primary());
                        else
                            table.Add(number_value, Primary());
                    }else if(((LiteralNode)item).ValueType == typeof(Integer)){
                        LiteralNode number_value = (LiteralNode)item;
                        if((Integer)(number_value.Value) == elements.Count)
                            elements.Add(Primary());
                        else
                            table.Add(number_value, Primary());
                    }else if(((LiteralNode)item).ValueType == typeof(string)){
                        LiteralNode string_value = (LiteralNode)item;
                        table.Add(string_value, Primary());
                    }
                }
                else
                {
                    elements.Add(item);
                }
            }

            return table_node;
        }
        Node Table()
        {
            int line = Previous().Line;

            if (!Match(TokenType.RIGHT_BRACKET))
            {
                List<Node> elements = new List<Node>();
                Dictionary<Node, Node> table = new Dictionary<Node, Node>();
                TableNode table_node = new TableNode(elements, table, line);

                do{
                    table_node = TableEntry(table_node);
                }while(Match(TokenType.COMMA));

                Consume(TokenType.RIGHT_BRACKET, "Expected ']' to close 'table'", true);

                return table_node;
            }
            else
            {
                return new TableNode(null, null, line);
            }

        }

        Node Primary()
        {
            if (Match(TokenType.LEFT_BRACKET))
                return Table();
            else if (Match(TokenType.FALSE))
                return new LiteralNode(false, Previous().Line);
            else if (Match(TokenType.TRUE))
                return new LiteralNode(true, Previous().Line);
            else if (Match(TokenType.NIL))
                return new LiteralNode(Previous().Line);
            else if (Match(TokenType.NUMBER)){
                Type this_type = ((TokenNumber)Previous()).type;
                if (this_type == typeof(Integer))
                    return new LiteralNode(((TokenNumber)Previous()).integerValue, Previous().Line);
                else
                    return new LiteralNode(((TokenNumber)Previous()).floatValue, Previous().Line);
            }else if (Match(TokenType.STRING))
                return new LiteralNode(((TokenString)Previous()).value, Previous().Line);
            else if (Match(TokenType.CHAR))
                return new LiteralNode(((TokenChar)Previous()).value , Previous().Line);
            else if (Match(TokenType.LEFT_PAREN)){
                Node expr = Expression();
                Consume(TokenType.RIGHT_PAREN, "Expected ')' after grouping'", true);
                return new GroupingNode(expr, expr.Line);
            }else if (Check(TokenType.FUN))
                return FunExpr();
            else if (Check(TokenType.IDENTIFIER))
                return CompoundVar();
            else if (Match(TokenType.MINUS)){
                if (Match(TokenType.NUMBER)){
                    Type this_type = ((TokenNumber)Previous()).type;
                    if (this_type == typeof(Integer))
                        return new LiteralNode(-((TokenNumber)Previous()).integerValue, Previous().Line);
                    else
                        return new LiteralNode(-((TokenNumber)Previous()).floatValue, Previous().Line);
                }else{
                    Error("Number expected after (-)! " + Previous().Line);
                    return new LiteralNode(Previous().Line);
                }
            }else{
                Error("No match found! " + Peek().Line + " " + Peek());
                return null;
            }
        }

        //////////////////////////////////////////////////////////////////////
        Token Advance()
        {
            if (!IsAtEnd()) current++;

            return Previous();
        }

        Token Previous()
        {
            return tokens[current - 1];
        }

        bool IsAtEnd()
        {
            Token nextToken = Peek();
            return nextToken.Type == TokenType.EOF;
        }

        Token Peek()
        {
            return tokens[current];
        }

        bool Check(TokenType type)
        {
            if (IsAtEnd()) return false;
            else return Peek().Type == type;
        }

        bool Match(TokenType type)
        {
            if (Check(type))
            {
                Advance();
                return true;
            }
            return false;
        }

        bool Match(TokenType type1, TokenType type2)
        {
            if (Check(type1) || Check(type2))
            {
                Advance();
                return true;
            }
            return false;
        }

        bool Match(TokenType type1, TokenType type2, TokenType type3)
        {
            if (Check(type1) || Check(type2) || Check(type3))
            {
                Advance();
                return true;
            }
            return false;
        }

        bool Match(TokenType type1, TokenType type2, TokenType type3, TokenType type4)
        {
            if (Check(type1) || Check(type2) || Check(type3) || Check(type4))
            {
                Advance();
                return true;
            }
            return false;
        }

        Token Consume(TokenType type, string msg, bool error)
        {
            if (Check(type)) return Advance();
            else
            {
                if(error == true)
                    Error(Peek().ToString() +
                    " on line: " + Peek().Line +
                    ", " + msg);
                else
                    Warning(Peek().ToString() +
                    " on line: " + Peek().Line +
                    ", " + msg);
                return null;
            }

        }

        Token Elide(TokenType type)
        {
            if (Check(type)) return Advance();
            return null;
        }

        void Error(string msg)
        {
            Errors.Add(msg);
        }

        void Warning(string msg)
        {
            Warnings.Add(msg);
        }

    }
}
