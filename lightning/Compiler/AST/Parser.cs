using System;
using System.Collections.Generic;
using System.IO;

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
			public FunctionStruct(
				List<string> p_parameters,
				List<Node> p_statements
			)
			{
				parameters = p_parameters;
				statements = p_statements;
			}
		}

		private List<Token> tokens;
		private string moduleName;
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
						LogWarnings();
						LogErrors();
						if(Errors.Count > 0)
							return null;
						else{
							hasParsed = true;
							PrettyPrinter astPrinter = new PrettyPrinter();
							astPrinter.PrintToFile(
								ParsedTree,
								Path.ToPath(moduleName) + ".ast"
							);
						}
					}catch (Exception e){
						Console.WriteLine(
							"Parsing broke the runtime, check out " +
							System.IO.Path.DirectorySeparatorChar +
							Path.ToPath(moduleName) +
							"_parser.log!"
						);
						Logger.LogLine(
							e.ToString(),
							Path.ToPath(moduleName) + "_parser.log"
						);
						LogWarnings();
						LogErrors();
						return null;
					}
				}
				return ast;
			}
		}

		public Parser(List<Token> p_tokens, string p_moduleName)
		{
			tokens = p_tokens;
			moduleName = p_moduleName;
			hasParsed = false;
			Errors = new List<string>();
			Warnings = new List<string>();
			current = 0;
		}

		private void Parse()
		{
			ast = Program();
		}

		private void LogWarnings(){
			if (Warnings.Count > 0){
				Logger.LogLine("Parsing had warnings:", "vm.log");
				foreach (string warning in Warnings)
					Logger.LogLine("\t-" + warning, "vm.log");
			}
		}

		private void LogErrors(){
			if (Errors.Count > 0){
				Logger.LogLine("Parsing had Errors:", "vm.log");
				foreach (string error in Errors)
					Logger.LogLine("\t-" + error, "vm.log");
			}
		}

///////////////////////////////////////////////////////////////////////////////

		Node Program()
		{
			ElideMany(TokenType.NEW_LINE);
			var statements = new List<Node>();

			while (!IsAtEnd())
			{
				statements.Add(Declaration());
			}

			return new ProgramNode(statements, tokens[current].PositionData);
		}

		Node Declaration()
		{
			if (Match(TokenType.VAR))
			{
				Node varDecl = VarDecl();
				ElideMany(TokenType.NEW_LINE);
				return varDecl;
			}
			else if(Match(TokenType.FUN))
			{
				Node functionDecl = FunctionDecl();
				ElideMany(TokenType.NEW_LINE);
				return functionDecl;
			}
			else
			{
				Node statement = Statement();
				ElideMany(TokenType.NEW_LINE);
				return statement;
			}
		}

		Node CompoundVar()
		{
			Consume(TokenType.IDENTIFIER, "Expected identifier.", true);
			TokenString last_token = Previous() as TokenString;
			string name = last_token.value;
			PositionData position_data = last_token.PositionData;

			List<IndexNode> indexes = IndexedAccess();

			VariableNode indexed_variable = new VariableNode(name, indexes, position_data);
			Node method_call = MethodAccess(indexed_variable);

			return method_call;
		}

		List<IndexNode> IndexedAccess() {
			List<IndexNode> indexes = new List<IndexNode>();
			while (Check(TokenType.LEFT_BRACKET) || Check(TokenType.DOT))
			{
				if (Match(TokenType.LEFT_BRACKET))
				{
					IndexNode index = new IndexNode(
						Expression(),
						VarAccessType.BRACKET,
						Previous().PositionData);
					indexes.Add(index);

					Consume(
						TokenType.RIGHT_BRACKET,
						"Expected ']' after 'compoundVar identifier'",
						true
					);
				}
				else if (Match(TokenType.DOT))
				{
					Match(TokenType.IDENTIFIER);
					string this_name = (Previous() as TokenString).value;
					IndexNode index = new IndexNode(
						this_name,
						VarAccessType.DOT,
						Previous().PositionData);
					indexes.Add(index);
				}
			}
			return indexes;
		}

		Node VarDecl()
		{
			TokenString name = (TokenString)Consume(
				TokenType.IDENTIFIER,
				"Expected 'variable identifier' after 'var'.",
				true
			);
			Node initializer;
			if (Match(TokenType.EQUAL)) {
				ElideMany(TokenType.NEW_LINE);
				initializer = Expression();
			} else {
				initializer = null;
			}

			ElideMany(TokenType.NEW_LINE);
			Elide(TokenType.SEMICOLON);
			ElideMany(TokenType.NEW_LINE);

			return new VarDeclarationNode(name.value, initializer, name.PositionData);
		}

		List<string> Parameters(){
			List<string> parameters = new List<string>();

			ElideMany(TokenType.NEW_LINE);
			bool has_parameter = Check(TokenType.IDENTIFIER);
			while (has_parameter)
			{
				TokenString new_parameter = (TokenString)Consume(
					TokenType.IDENTIFIER,
					"Expected 'identifier' as 'function parameter'.",
					true
				);
				parameters.Add(new_parameter.value);
				ElideMany(TokenType.NEW_LINE);
				if (Check(TokenType.COMMA))
				{
					Consume(
						TokenType.COMMA,
						"Expected ',' separating parameter list",
						true
					);
					ElideMany(TokenType.NEW_LINE);
					has_parameter = Check(TokenType.IDENTIFIER);
				}
				else
				{
					has_parameter = false;
				}
			}

			return parameters;
		}

		FunctionStruct Function(){
			List<string> parameters = null;
			if(Match(TokenType.LEFT_PAREN)){
				parameters = Parameters();

				ElideMany(TokenType.NEW_LINE);
				Consume(
					TokenType.RIGHT_PAREN,
					"Expected ')' after 'function expression'.",
					true
				);
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

			if ((statements.Count == 0) ||
				(statements[^1].GetType() != typeof(ReturnNode)))
			{
				statements.Add(new ReturnNode(null, Previous().PositionData));
			}

			return new FunctionStruct(parameters, statements);
		}
		Node FunctionExpr()
		{
			Token function_token = Consume(
				TokenType.FUN,
				"Expected 'function' or '\' to start 'function expression'.",
				true
			);

			FunctionStruct this_function = Function();

			return new FunctionExpressionNode(
				this_function.parameters,
				this_function.statements,
				function_token.PositionData
			);
		}

		Node FunctionDecl(){
			VariableNode name = (VariableNode)CompoundVar();
			FunctionStruct this_function = Function();

			return new FunctionDeclarationNode(
				name,
				this_function.parameters,
				this_function.statements,
				name.PositionData
			);
		}

		Node Statement()
		{
			ElideMany(TokenType.NEW_LINE);

			if (Match(TokenType.FOR)) {
				Node statement = For();
				ElideMany(TokenType.NEW_LINE);
				return statement;
			} else if (Match(TokenType.RETURN)) {
				Node statement = Return();
				ElideMany(TokenType.NEW_LINE);
				return statement;
			} else if (Match(TokenType.IF)) {
				Node statement = If();
				ElideMany(TokenType.NEW_LINE);
				return statement;
			} else if (Match(TokenType.WHILE)) {
				Node statement = While();
				ElideMany(TokenType.NEW_LINE);
				return statement;
			} else if (Match(TokenType.LEFT_BRACE)) {
				Node statement = Block();
				ElideMany(TokenType.NEW_LINE);
				return statement;
			} else {
				Node statement = ExprStmt();
				ElideMany(TokenType.NEW_LINE);
				return statement;
			}
		}

		Node Return()
		{
			PositionData position_data = Previous().PositionData;
			Node expr = Expression();
			Elide(TokenType.SEMICOLON);
			return new ReturnNode(expr, position_data);
		}

		Node For()
		{
			PositionData position_data = Previous().PositionData;
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
			Consume(
				TokenType.RIGHT_PAREN,
				"Expected ')' after 'for' finalizer.",
				 true
			);

			Node body = Statement();

			return new ForNode(initializer, condition, finalizer, body, position_data);
		}

		Node While()
		{
			PositionData position_data = Previous().PositionData;
			Consume(TokenType.LEFT_PAREN, "Expected '(' after 'while'.", true);
			Node condition = Expression();
			if (condition == null)
				Error("'while' 'condition' can not be null.");
			Consume(
				TokenType.RIGHT_PAREN,
				"Expected ')' after 'while' condition.",
				 true
			);

			Node body = Statement();

			return new WhileNode(condition, body, position_data);
		}

		Node If()
		{
			PositionData position_data = Previous().PositionData;
			Token start = Consume(
				TokenType.LEFT_PAREN,
				"Expected '(' after 'if'.",
				true
			);
			Node condition = Expression();
			if (condition == null) Error("'if' 'condition' can not be null.");
			Consume(
				TokenType.RIGHT_PAREN,
				"Expected ')' after 'if' condition.",
				true
			);

			Node then_branch = Statement();

			Node else_branch = null;
			if (Match(TokenType.ELSE))
			{
				else_branch = Statement();
			}

			return new IfNode(condition, then_branch, else_branch, position_data);
		}

		Node Block()
		{
			ElideMany(TokenType.NEW_LINE);
			PositionData position_data = Previous().PositionData;
			List<Node> statements = new List<Node>();

			while(!Check(TokenType.RIGHT_BRACE) && !IsAtEnd()) {
				statements.Add(Declaration());
			}

			Consume(
				TokenType.RIGHT_BRACE,
				"Expected '}' to terminate 'block'.",
				true
			);
			return new BlockNode(statements, position_data);
		}

		Node ExprStmt()
		{
			Node expr = Expression();

			Elide(TokenType.SEMICOLON);
			return new StmtExprNode(expr, expr.PositionData);
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
				ElideMany(TokenType.NEW_LINE);
				Node value = Assignment();

				if (assigned.Type == NodeType.VARIABLE)
					return new AssignmentNode(
						(VariableNode)assigned,
						value,
						AssignmentOperatorType.ADDITION_ASSIGN,
						assigned.PositionData
					);
				else
					Error("Invalid assignment.");
			}
			else if (Match(TokenType.PLUS_PLUS))
			{
				ElideMany(TokenType.NEW_LINE);
				Node value = new LiteralNode(1, assigned.PositionData);

				if (assigned.Type == NodeType.VARIABLE)
					return new AssignmentNode(
						(VariableNode)assigned,
						value,
						AssignmentOperatorType.ADDITION_ASSIGN,
						assigned.PositionData
					);
				else
					Error("Invalid assignment.");
			}
			else if (Match(TokenType.MINUS_EQUAL))
			{
				ElideMany(TokenType.NEW_LINE);
				Node value = Assignment();

				if (assigned.Type == NodeType.VARIABLE)
					return new AssignmentNode(
						(VariableNode)assigned,
						value,
						AssignmentOperatorType.SUBTRACTION_ASSIGN,
						assigned.PositionData
					);
				else
					Error("Invalid assignment.");
			}
			else if (Match(TokenType.MINUS_MINUS))
			{
				ElideMany(TokenType.NEW_LINE);
				Node value = new LiteralNode(1, assigned.PositionData);

				if (assigned.Type == NodeType.VARIABLE)
					return new AssignmentNode(
						(VariableNode)assigned,
						value, AssignmentOperatorType.SUBTRACTION_ASSIGN,
						assigned.PositionData
					);
				else
					Error("Invalid assignment.");
			}
			else if (Match(TokenType.STAR_EQUAL))
			{
				ElideMany(TokenType.NEW_LINE);
				Node value = Assignment();

				if (assigned.Type == NodeType.VARIABLE)
					return new AssignmentNode(
						(VariableNode)assigned,
						value,
						AssignmentOperatorType.MULTIPLICATION_ASSIGN,
						assigned.PositionData
					);
				else
					Error("Invalid assignment.");
			}
			if (Match(TokenType.SLASH_EQUAL))
			{
				ElideMany(TokenType.NEW_LINE);
				Node value = Assignment();

				if (assigned.Type == NodeType.VARIABLE)
					return new AssignmentNode(
						(VariableNode)assigned,
						value,
						AssignmentOperatorType.DIVISION_ASSIGN,
						assigned.PositionData
					);
				else
					Error("Invalid assignment.");
			}
			else if (Match(TokenType.EQUAL))
			{
				ElideMany(TokenType.NEW_LINE);
				Node value = Assignment();

				if (assigned.Type == NodeType.VARIABLE)
					return new AssignmentNode(
						(VariableNode)assigned,
						value,
						AssignmentOperatorType.ASSIGN,
						assigned.PositionData
					);
				else
					Error("Invalid assignment.");
			}

			ElideMany(TokenType.NEW_LINE);
			return assigned;
		}

		Node LogicalOr()
		{
			Node left = LogicalAnd();

			while (Match(TokenType.OR))
			{
				Node right = LogicalAnd();

				left = new LogicalNode(
					left,
					OperatorType.OR,
					right,
					left.PositionData
				);
			}

			return left;
		}

		Node LogicalAnd()
		{
			Node left = LogicalXor();

			while (Match(TokenType.AND))
			{
				Node right = LogicalXor();

				left = new LogicalNode(
					left,
					OperatorType.AND,
					right,
					left.PositionData
				);
			}

			return left;
		}

		Node LogicalXor()
		{
			Node left = LogicalNand();

			while (Match(TokenType.XOR))
			{
				Node right = LogicalNand();

				left = new LogicalNode(
					left,
					OperatorType.XOR,
					right,
					left.PositionData
				);
			}

			return left;
		}

		Node LogicalNand()
		{
			Node left = LogicalNor();

			while (Match(TokenType.NAND))
			{
				Node right = LogicalNor();

				left = new LogicalNode(
					left,
					OperatorType.NAND,
					right,
					left.PositionData
				);
			}

			return left;
		}

		Node LogicalNor()
		{
			Node left = LogicalXnor();

			while (Match(TokenType.NOR))
			{
				Node right = LogicalXnor();

				left = new LogicalNode(
					left,
					OperatorType.NOR,
					right,
					left.PositionData
				);
			}

			return left;
		}

		Node LogicalXnor()
		{
			Node left = Equality();

			while (Match(TokenType.XNOR))
			{
				Node right = Equality();

				left = new LogicalNode(
					left,
					OperatorType.XNOR,
					right,
					left.PositionData
				);
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

				if (op.Type == TokenType.BANG_EQUAL)
					this_op = OperatorType.NOT_EQUAL;
				else
					this_op = OperatorType.EQUAL;

				Node right = Comparison();
				left = new BinaryNode(left, this_op, right, op.PositionData);
			}

			return left;
		}

		Node Comparison()
		{
			Node left = Addition();

			while (Match(TokenType.GREATER,
				TokenType.GREATER_EQUAL,
				TokenType.LESS,
				TokenType.LESS_EQUAL
			))
			{
				Token op = Previous();
				OperatorType this_op;

				if (op.Type == TokenType.GREATER)
					this_op = OperatorType.GREATER;
				else if( op.Type == TokenType.GREATER_EQUAL)
					this_op = OperatorType.GREATER_EQUAL;
				else if (op.Type == TokenType.LESS)
					this_op = OperatorType.LESS;
				else this_op = OperatorType.LESS_EQUAL;

				Node right = Addition();
				left = new BinaryNode(left, this_op, right, op.PositionData);
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

				if (op.Type == TokenType.MINUS)
					this_op = OperatorType.SUBTRACTION;
				else if (op.Type == TokenType.PLUS)
					this_op = OperatorType.ADDITION;
				else
					this_op = OperatorType.APPEND;

				Node right = Multiplication();
				left = new BinaryNode(left, this_op, right, op.PositionData);
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

				if (op.Type == TokenType.SLASH)
					this_op = OperatorType.DIVISION;
				else
					this_op = OperatorType.MULTIPLICATION;

				Node right = Unary();
				left = new BinaryNode(left, this_op, right, op.PositionData);
			}

			return left;
		}

		Node Unary()
		{
			if (Match(
				TokenType.BANG,
				TokenType.MINUS,
				TokenType.MINUS_MINUS,
				TokenType.PLUS_PLUS
			))
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
				return new UnaryNode(this_op, right, op.PositionData);
			}

			return Call();
		}

		Node Call()
		{
			Node maybe_funcall = Primary();

			if (Check(TokenType.LEFT_PAREN))
			{
				Node function_call_node = new FunctionCallNode(
					(maybe_funcall as VariableNode),
					maybe_funcall.PositionData);
				maybe_funcall = CallTail(function_call_node);
			} else if(Check(TokenType.COLON)){
				maybe_funcall = AnonymousCall(maybe_funcall);
			}

			return maybe_funcall;
		}

		Node MethodAccess(Node p_node){
			if (Match(TokenType.COLON))
			{// Method Call
				if(Match(TokenType.IDENTIFIER)){
					string this_name = (Previous() as TokenString).value;
					IndexNode index = new IndexNode(
						this_name,
						VarAccessType.COLON,
						Previous().PositionData
					);
					(p_node as VariableNode).Indexes.Add(index);
				}else if(Match(TokenType.STRING)){
					// Convert String to identifier
					string this_name = (Previous() as TokenString).value;
					IndexNode index = new IndexNode(
						this_name,
						VarAccessType.COLON,
						Previous().PositionData
					);
					(p_node as VariableNode).Indexes.Add(index);
				}else if(Match(TokenType.MINUS)){
					// Convert Negative Number to expression
					if(Match(TokenType.NUMBER)){
						Type this_type = ((TokenNumber)Previous()).type;
						Node this_value;
						if (this_type == typeof(Integer))
							this_value = new LiteralNode(
								-((TokenNumber)Previous()).integerValue,
								Previous().PositionData
							);
						else
							this_value = new LiteralNode(
								-((TokenNumber)Previous()).floatValue,
								Previous().PositionData
							);
						IndexNode index = new IndexNode(
							this_value,
							VarAccessType.COLON,
							Previous().PositionData
						);
						(p_node as VariableNode).Indexes.Add(index);
					}else{
						Error("Expected Number after '-' in 'Method Access'");
					}
				}else if(Match(TokenType.NUMBER)){
					// Convert Number to expression
					Type this_type = ((TokenNumber)Previous()).type;
					Node this_value;
					if (this_type == typeof(Integer))
						this_value = new LiteralNode(
							((TokenNumber)Previous()).integerValue,
							Previous().PositionData
						);
					else
						this_value = new LiteralNode(
							((TokenNumber)Previous()).floatValue,
							Previous().PositionData
						);
					IndexNode index = new IndexNode(
						this_value,
						VarAccessType.COLON,
						Previous().PositionData
					);
					(p_node as VariableNode).Indexes.Add(index);
				}else if (Match(TokenType.LEFT_BRACKET)) {
					IndexNode index = new IndexNode(
						Expression(),
						VarAccessType.COLON,
						Previous().PositionData
					);
					(p_node as VariableNode).Indexes.Add(index);
					Consume(
						TokenType.RIGHT_BRACKET,
						"Expected ']' after 'Method Access'",
						true
					);
				}
			}
			return p_node;
		}

		Node AnonymousCall(Node p_node){
			if(Check(TokenType.COLON))
			{
				VariableNode variable_node = new VariableNode(
					p_node,
					new List<IndexNode>(),
					p_node.PositionData
				);
				variable_node = (VariableNode)MethodAccess(variable_node);

				Node function_call_node = new FunctionCallNode(
					variable_node,
					p_node.PositionData);

				function_call_node = CallTail(function_call_node);
				return function_call_node;
			}
			Error("Expected ':' after anonymous function call.");
			return p_node;
		}

		Node CallTail(Node p_functionCallNode)
		{
			while(Check(TokenType.LEFT_PAREN))
			{
				FunctionCallNode this_function_call_node =
					(FunctionCallNode)p_functionCallNode;
				List<Node> arguments = Arguments();
				List<IndexNode> indexes;
				if(Check(TokenType.DOT) || Check(TokenType.LEFT_BRACKET)){
					indexes = IndexedAccess();
				}else{
					indexes = null;
				}
				this_function_call_node.Calls.Add(new CallInfo(arguments, indexes));
			}
			if(Check(TokenType.COLON))
				p_functionCallNode = AnonymousCall(p_functionCallNode);

			return p_functionCallNode;
		}

		List<Node> Arguments(){
			Consume(
				TokenType.LEFT_PAREN,
				"Expected '(' before 'function call' arguments.",
				true
			);

			List<Node> arguments = new List<Node>();
			ElideMany(TokenType.NEW_LINE);

			if (!Check(TokenType.RIGHT_PAREN))
			{
				do {
					ElideMany(TokenType.NEW_LINE);
					arguments.Add(Expression());
					ElideMany(TokenType.NEW_LINE);
				} while (Match(TokenType.COMMA));
			}

			ElideMany(TokenType.NEW_LINE);
			Consume(
				TokenType.RIGHT_PAREN,
				"Expected ')' after 'function call' arguments.",
				true
			);

			return arguments;
		}

		ListNode ListEntry(ListNode p_listNode)
		{
			List<Node> elements = p_listNode.Elements;

			bool is_negative = false;
			if(Match(TokenType.MINUS))
				is_negative = true;

			Node item = Primary();
			bool minus_error = false;
			if(is_negative){
				if(item.Type == NodeType.LITERAL){
					LiteralNode literal_node = (LiteralNode)item;
					if(literal_node.ValueType == typeof(Integer) ||
						literal_node.ValueType == typeof(Float))
						((LiteralNode)item).SetNegative();
					else
						minus_error = true;
				}else
					minus_error = true;
			}
			if(minus_error == true)
				Error("Minus Sign should only be used by Numeric" +
					"Types in List Declaration.");
			elements.Add(item);

			return p_listNode;
		}

		TableNode TableEntry(TableNode p_tableNode)
		{
			Dictionary<Node, Node> table = p_tableNode.Map;

			bool is_negative = false;
			if(Match(TokenType.MINUS))
				is_negative = true;

			if(!Check(TokenType.RIGHT_BRACKET)){
				if (Match(TokenType.IDENTIFIER))
				{
					Node item = new LiteralNode(
						(Previous() as TokenString).value,
						Previous().PositionData
					);
					Match(TokenType.COLON);
					Node value = Primary();
					table.Add(item, value);
				}
				else
				{
					Node item = Primary();
					Consume(
						TokenType.COLON,
						"Expected ':' separating key:values" +
						"in table constructor",
						true
					);
					if(((LiteralNode)item).ValueType == typeof(Float)){
						LiteralNode number_value = (LiteralNode)item;
						if(!is_negative){
							table.Add(number_value, Primary());
						}else{
							number_value.SetNegative();
							table.Add(number_value, Primary());
						}
					}else if(((LiteralNode)item).ValueType
						== typeof(Integer)){
						LiteralNode number_value = (LiteralNode)item;
						if(!is_negative){
							table.Add(number_value, Primary());
						}else{
							number_value.SetNegative();
							table.Add(number_value, Primary());
						}
					}else if(((LiteralNode)item).ValueType == typeof(string)){
						if(is_negative)
							Error("Minus Sign should only be used" +
								"by Numeric Types in List Declaration.");
						LiteralNode string_value = (LiteralNode)item;
						table.Add(string_value, Primary());
					}
				}
			}

			return p_tableNode;
		}

		Node Table()
		{
			ElideMany(TokenType.NEW_LINE);
			PositionData position_data = Previous().PositionData;

			if (Match(TokenType.COLON)){// empty table
				if (Match(TokenType.RIGHT_BRACKET))
					return new TableNode(null, position_data);
				else
					Error("Invalid Table declaration");
					return null;
			}else if (!Match(TokenType.RIGHT_BRACKET)){
				Node table_or_list;
				if((Peek2() != null) && (Peek2().Type == TokenType.COLON)){// new table
					Dictionary<Node, Node> map = new Dictionary<Node, Node>();
					TableNode table_node = new TableNode(map, position_data);

					do{
						ElideMany(TokenType.NEW_LINE);
						table_node = TableEntry(table_node);
						ElideMany(TokenType.NEW_LINE);
					}while(Match(TokenType.COMMA));

					table_or_list = table_node;
				}else{// new list
					List<Node> elements = new List<Node>();
					ListNode list_node = new ListNode(elements, position_data);

					do{
						ElideMany(TokenType.NEW_LINE);
						list_node = ListEntry(list_node);
						ElideMany(TokenType.NEW_LINE);
					}while(Match(TokenType.COMMA));

					table_or_list = list_node;
				}

				ElideMany(TokenType.NEW_LINE);
				Consume(
					TokenType.RIGHT_BRACKET,
					"Expected ']' to close 'table'",
					true
				);
				ElideMany(TokenType.NEW_LINE);

				return table_or_list;

			}else{  // empty list
				return new ListNode(null, position_data);
			}
		}

		Node Primary()
		{
			if (Match(TokenType.LEFT_BRACKET))
				return Table();
			else if (Match(TokenType.FALSE))
				return new LiteralNode(false, Previous().PositionData);
			else if (Match(TokenType.TRUE))
				return new LiteralNode(true, Previous().PositionData);
			else if (Match(TokenType.NIL))
				return new LiteralNode(Previous().PositionData);
			else if (Match(TokenType.NUMBER)){
				Type this_type = ((TokenNumber)Previous()).type;
				if (this_type == typeof(Integer))
					return new LiteralNode(
						((TokenNumber)Previous()).integerValue,
						Previous().PositionData
					);
				else
					return new LiteralNode(
						((TokenNumber)Previous()).floatValue,
						Previous().PositionData
					);
			} else if (Match(TokenType.STRING))
				return new LiteralNode(
					((TokenString)Previous()).value,
					Previous().PositionData
				);
			else if (Match(TokenType.CHAR))
				return new LiteralNode(
					((TokenChar)Previous()).value,
					Previous().PositionData
				);
			else if (Match(TokenType.LEFT_PAREN)){
				Node expr = Expression();
				Consume(
					TokenType.RIGHT_PAREN,
					"Expected ')' after grouping'",
					true
				);
				return new GroupingNode(expr, expr.PositionData);
			} else if (Check(TokenType.FUN))
				return FunctionExpr();
			else if (Check(TokenType.IDENTIFIER))
				return CompoundVar();
			else if (Match(TokenType.MINUS)){
				if (Match(TokenType.NUMBER)){
					Type this_type = ((TokenNumber)Previous()).type;
					if (this_type == typeof(Integer))
						return new LiteralNode(
							-((TokenNumber)Previous()).integerValue,
							Previous().PositionData
						);
					else
						return new LiteralNode(
							-((TokenNumber)Previous()).floatValue,
							Previous().PositionData
						);
				}else{
					Error("Number expected after (-)! " + Previous().PositionData);
					return new LiteralNode(Previous().PositionData);
				}
			} else {
				Error("Primary Terminal expected - Primary node can not begin with: " + Peek());
				return null;
			}
		}

///////////////////////////////////////////////////////////////////////////////

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

		bool Match(
			TokenType p_type1,
			TokenType p_type2,
			TokenType p_type3,
			TokenType p_type4
		)
		{
			if (Check(p_type1) ||
				Check(p_type2) ||
				Check(p_type3) ||
				Check(p_type4))
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
					" on position: " + Peek().PositionData +
					", " + p_msg);
				else
					Warning(Peek().ToString() +
					" on position: " + Peek().PositionData +
					", " + p_msg);
				return null;
			}

		}

		void Elide(TokenType p_type)
		{
			if (Check(p_type)) Advance();
		}

		void ElideMany(TokenType p_type)
		{
			while (Peek().Type == p_type)
				Elide(p_type);
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
