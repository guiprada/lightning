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
        struct FunctionStruct
		{
			public List<string> parameters;
            public List<Node> statements;
            public FunctionStruct(List<string> p_parameters, List<Node> p_statements){
                parameters = p_parameters;
                statements = p_statements;
            }
		}

        private List<Token> tokens;
        private bool hasParsed;
        public bool HasParsed { get { return hasParsed;} }
        private Node ast;
        private int current;
        public List<string> Errors { get; private set; }
        public List<string> Warnings { get; private set; }

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
                return VarDecl();
            }
            else if(Match(TokenType.FUN))
            {
                return FunctionDecl();
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

            Node indexed_access = IndexedAccess(name, line);

            Node method_call = MethodAccess(indexed_access);

            return method_call;
        }

        Node IndexedAccess(string p_name, int p_line) {
            List<Node> indexes = new List<Node>();
            while (Check(TokenType.LEFT_BRACKET) || Check(TokenType.DOT))
            {
                if (Match(TokenType.LEFT_BRACKET))
                {
                    List<Node> expression_var = new List<Node>();
                    expression_var.Add(Expression());
                    VariableNode index = new VariableNode("_expression_as_index", expression_var, VarAccessType.PLAIN, Previous().Line);
                    indexes.Add(index);

                    Consume(TokenType.RIGHT_BRACKET, "Expected ']' after 'compoundVar identifier'", true);
                }
                else if (Match(TokenType.DOT))
                {
                    Match(TokenType.IDENTIFIER);
                    string this_name = (Previous() as TokenString).value;
                    VariableNode index = new VariableNode(this_name, new List<Node>(), VarAccessType.DOTTED, Previous().Line);
                    indexes.Add(index);
                }
            }
            return new VariableNode(p_name, indexes, VarAccessType.PLAIN, p_line);
        }

        Node VarDecl()
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

        FunctionStruct Function(){
            List<string> parameters = null;
            if(Match(TokenType.LEFT_PAREN)){
                parameters = Parameters();
            }

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
                statements.Add(new ReturnNode(null, Previous().Line));
            }

            return new FunctionStruct(parameters, statements);
        }
		Node FunctionExpr()
        {
            Token function_token = Consume(TokenType.FUN, "Expected 'function' or '\' to start 'function expression'.", true);

            FunctionStruct this_function = Function();

            return new FunctionExpressionNode(this_function.parameters, this_function.statements, function_token.Line);
        }

        Node FunctionDecl()
        {
            TokenString name = Consume(TokenType.IDENTIFIER, "Expected 'function identifier'.", true) as TokenString;

            FunctionStruct this_function = Function();

            return new FunctionDeclarationNode(name.value, this_function.parameters, this_function.statements, name.Line);
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
                return ExprStmt();
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
                initializer = VarDecl();
            }
            else
            {
                initializer = ExprStmt();
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

        Node ExprStmt()
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

            return Call();
        }

        Node Call()
        {
            Node maybe_func = Primary();

            if (Check(TokenType.LEFT_PAREN))
            {
                List<List<Node>> calls = new List<List<Node>>();
                List<VariableNode> indexed_access = new List<VariableNode>();
                Node function_call_node = new FunctionCallNode(
                    (maybe_func as VariableNode),
                    calls,
                    indexed_access,
                    maybe_func.Line);
                function_call_node = CallTail(function_call_node);
                return function_call_node;
            }
            if(Check(TokenType.COLON)){
                return AnonymousCall(maybe_func);
            }
            return maybe_func;
        }

        Node MethodAccess(Node p_node){
            if (Match(TokenType.COLON))
            {// Method Call
                if(Match(TokenType.IDENTIFIER)){
                    string this_name = (Previous() as TokenString).value;
                    VariableNode index = new VariableNode(this_name, new List<Node>(), VarAccessType.METHOD, Previous().Line);
                    (p_node as VariableNode).Indexes.Add(index);
                }else if(Match(TokenType.STRING)){
                    string this_name = (Previous() as TokenString).value;
                    VariableNode index = new VariableNode(this_name, new List<Node>(), VarAccessType.METHOD, Previous().Line);
                    (p_node as VariableNode).Indexes.Add(index);
                }else if (Match(TokenType.LEFT_BRACKET)) {
                    List<Node> expression_var = new List<Node>();
                    expression_var.Add(Expression());
                    VariableNode index = new VariableNode("_expression_as_index", expression_var, VarAccessType.METHOD, Previous().Line);
                    (p_node as VariableNode).Indexes.Add(index);
                    Consume(TokenType.RIGHT_BRACKET, "Expected ']' after 'Method Access'", true);
                }
            }
            return p_node;
        }

        Node AnonymousCall(Node p_node){
            if(Check(TokenType.COLON))
            {
                string name = string.Format(@"*_{0}.txt", Guid.NewGuid());

                VarDeclarationNode declaration_node = new VarDeclarationNode(name, p_node, p_node.Line);
                VariableNode variableNode = new VariableNode(name, new List<Node>(), VarAccessType.PLAIN, p_node.Line);
                variableNode = (VariableNode)MethodAccess(variableNode);

                List<List<Node>> calls = new List<List<Node>>();
                List<VariableNode> indexed_access = new List<VariableNode>();
                Node function_call_node = new FunctionCallNode(

                    variableNode,
                    calls,
                    indexed_access,
                    p_node.Line);

                function_call_node = CallTail(function_call_node);
                return new AnonymousFunctionCallNode(function_call_node, declaration_node, variableNode, p_node.Line);
            }
            Error("Expected ':' after anonymous function call.");
            return p_node;
        }

        Node CallTail(Node p_functionCallNode)
        {
            bool go_on = true;
            while(Check(TokenType.LEFT_PAREN) && go_on)
            {
                FunctionCallNode this_function_call_node = (FunctionCallNode)p_functionCallNode;
                this_function_call_node.Calls.Add(Arguments());

                if(Check(TokenType.DOT)){
                    if(Check2(TokenType.LEFT_PAREN)){
                        Match(TokenType.DOT);
                        this_function_call_node.IndexedAccess.Add(null);
                    }else{
                        if(Check2(TokenType.LEFT_BRACKET)){
                            Match(TokenType.DOT);
                        }
                        this_function_call_node.IndexedAccess.Add(IndexedAccess(this_function_call_node.Variable.Name, this_function_call_node.Line) as VariableNode);
                    }
                }else{
                    this_function_call_node.IndexedAccess.Add(null);
                    go_on = false;
                }
            }
            if(Check(TokenType.COLON))
                p_functionCallNode = AnonymousCall(p_functionCallNode);

            return p_functionCallNode;
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

        TableNode TableEntry(TableNode p_tableNode)
        {
            List<Node> elements = p_tableNode.Elements;
            Dictionary<Node, Node> table = p_tableNode.Table;

            bool is_negative = false;
            if(Match(TokenType.MINUS))
                is_negative = true;

            if(!Check(TokenType.RIGHT_BRACKET)){
                if((Peek2() != null) && (Peek2().Type == TokenType.COLON)){
                    if (Match(TokenType.IDENTIFIER))
                    {
                        Node item = new LiteralNode((Previous() as TokenString).value, Previous().Line);

                        if(Match(TokenType.COLON)){
                            Node value = Primary();
                            table.Add(item, value);
                        }else
                            elements.Add(item);
                    }
                    else
                    {
                        Node item = Primary();
                        Consume(TokenType.COLON, "Expected ':' separating key:values in table constructor", true);
                        if(((LiteralNode)item).ValueType == typeof(Float)){
                            LiteralNode number_value = (LiteralNode)item;
                            if(!is_negative){
                                if((Float)(number_value.Value) == elements.Count)
                                    elements.Add(Primary());
                                else
                                    table.Add(number_value, Primary());
                            }else{
                                number_value.SetNegative();
                                table.Add(number_value, Primary());
                            }
                        }else if(((LiteralNode)item).ValueType == typeof(Integer)){
                            LiteralNode number_value = (LiteralNode)item;
                            if(!is_negative){
                                if((Integer)(number_value.Value) == elements.Count)
                                    elements.Add(Primary());
                                else
                                    table.Add(number_value, Primary());
                            }else{
                                number_value.SetNegative();
                                table.Add(number_value, Primary());
                            }
                        }else if(((LiteralNode)item).ValueType == typeof(string)){
                            if(is_negative)
                                Error("Minus Sign should only be used by Numeric Types in List Declaration.");
                            LiteralNode string_value = (LiteralNode)item;
                            table.Add(string_value, Primary());
                        }
                    }
                }
                else
                {
                    Node item = Primary();
                    bool minus_error = false;
                    if(is_negative){
                        if(item.Type == NodeType.LITERAL){
                            LiteralNode literal_node = (LiteralNode)item;
                            if(literal_node.ValueType == typeof(Integer) || literal_node.ValueType == typeof(Float))
                                ((LiteralNode)item).SetNegative();
                            else
                                minus_error = true;
                        }else
                            minus_error = true;
                    }
                    if(minus_error == true)
                        Error("Minus Sign should only be used by Numeric Types in List Declaration.");
                    elements.Add(item);
                }
            }

            return p_tableNode;
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
                return FunctionExpr();
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

        Token Peek2()
        {
            if((current + 1) < (tokens.Count -1))
                return tokens[current + 1];
            else
                return null;
        }

        bool Check(TokenType p_type)
        {
            if (IsAtEnd()) return false;
            else return Peek().Type == p_type;
        }

        bool Check2(TokenType p_type)
        {
            if (IsAtEnd()) return false;
            else return Peek2().Type == p_type;
        }

        bool Match(TokenType p_type)
        {
            if (Check(p_type))
            {
                Advance();
                return true;
            }
            return false;
        }

        bool Match(TokenType p_type1, TokenType p_type2)
        {
            if (Check(p_type1) || Check(p_type2))
            {
                Advance();
                return true;
            }
            return false;
        }

        bool Match(TokenType p_type1, TokenType p_type2, TokenType p_type3)
        {
            if (Check(p_type1) || Check(p_type2) || Check(p_type3))
            {
                Advance();
                return true;
            }
            return false;
        }

        bool Match(TokenType p_type1, TokenType p_type2, TokenType p_type3, TokenType p_type4)
        {
            if (Check(p_type1) || Check(p_type2) || Check(p_type3) || Check(p_type4))
            {
                Advance();
                return true;
            }
            return false;
        }

        Token Consume(TokenType p_type, string p_msg, bool p_error)
        {
            if (Check(p_type)) return Advance();
            else
            {
                if(p_error == true)
                    Error(Peek().ToString() +
                    " on line: " + Peek().Line +
                    ", " + p_msg);
                else
                    Warning(Peek().ToString() +
                    " on line: " + Peek().Line +
                    ", " + p_msg);
                return null;
            }

        }

        Token Elide(TokenType p_type)
        {
            if (Check(p_type)) return Advance();
            return null;
        }

        void Error(string p_msg)
        {
            Errors.Add(p_msg);
        }

        void Warning(string p_msg)
        {
            Warnings.Add(p_msg);
        }

    }
}
