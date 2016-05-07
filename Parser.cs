using System;
using System.Collections.Generic;
using System.Linq;

#region grammar
/*
 * Identifier = asdasdas
 * IDentifier();
 * 
Program:
  StatementList
Statement:
  IfClause | WhileClause | Assignment
StatementList:
  Statement*
Assignment:
  Identifier '=' Expression ';'
  ExpressionStatement
ExpressionStatement:
  Expression ';'
IfClause:
  'if''('Expression')''{'StatementList'}'
WhileClause:
  'while''('Expression')''{'StatementList'}'
Identifier:
  \w[\w\d_]*
Number:
  [0-9]+
ARG:
  Identifier | Number
Expression:
  Comparison (('==' | '!=') Comparison)*
Comparison:
  Sum(('<'|'>'|'<='|'>=')Sum)*
Sum:
  Product(('+'|'-')Product)*
Product:
  Primary(('*'|'/')Primary)*
Primary:
  Main FuncCallArguments*
Main:
  '(' Expression ')' 
  Identifier
  Number
FuncCallArguments:
  '(' Expression (',' Expression)* ','')'

example: 
a = 1 + 2; //3
b = a + 1; //4
c = a == b; //false
c = 1;
c = true;
if (c == true){
	print(a);
}
while (a < b + 10){
	print(a);
	a = a + 1;
}

*/
#endregion grammar

namespace EasyScriptInterpreter {
	public class Parser {
		Token[] tokens;
		int position = 0;
		enum PARSE_STATEMENT_LIST_EXIT_CASE {
			CLOSING_CURLY_BRACE,
			END_OF_FILE
		}

		Token current_token {
			get {
				return tokens [position];
			}
		}

		bool end_of_tokens {
			get {
				return position >= tokens.Length;
			}
		}

		public Parser (Lexer l) {
			tokens = filter_whitespaces (l.GetTokens ());
		}

		public Token[] filter_whitespaces(IEnumerable<Token> enumerable) {
			return enumerable.Where (x => x.type != Token.Type.Space).ToArray ();
		}

		bool skip_if_value(string val) {
			if (current_token.value == val && !end_of_tokens) {
				position++;
				return true;
			}
			return false;
		}

		bool skip_if_type(Token.Type type) {
			if (current_token.type == type && !end_of_tokens) {
				position++;
				return true;
			}
			return false;
		}

		bool curr_token_is_type(Token.Type type) {
			return current_token.type == type;
		}

		bool curr_token_is_value(string val) {
			return current_token.value == val;
		}

		void expect_required_token_value(string value) {
			if (current_token.value == value && !end_of_tokens) {
				position++;
			}
			else {
				throw new Exception ("EXPECTED SYMBOL: " + value);
			}
		}

		public ProgramNode parse_program() {
			ProgramNode pn = new ProgramNode ();
			pn.Children = parse_program_statement_list ();
			return pn;
		}

		StatementNode parse_statement() {
			StatementNode result;
			if (end_of_tokens) {
				return null;
			}
			switch(current_token.value) {
			case "if":
				move_next ();
				result = parse_if_clause ();
				break;
			case "while":
				move_next ();
				result = parse_while_clause ();
				break;
			default:
				result = parse_assignment ();
				expect_required_token_value (";");
				break;
			}
			return result;
		}

		StatementNode parse_expression_statement() {
			return new ExpressionStatement() { 
				expression = parse_expression () 
			};
		}

		StatementNode parse_assignment() {
			ExpressionNode expr = parse_expression ();
			if (curr_token_is_value("=")) {
				var left_var = expr as VariableExpression;
				if (left_var == null)
					throw new Exception ("Variable on left side");
				AssignmentNode res = new AssignmentNode ();
				res.left_hand_side = left_var;
				if (skip_if_value("=")) {
					res.right_hand_side = parse_expression ();
				}
				return res;
			}
			else if (curr_token_is_value("(")) {
				return new ExpressionStatement () {
					expression = new FuncCallNode () {
						identifier = expr,
						args = parse_func_arguments (),
					}
				};
			}
			else if (curr_token_is_value(";")) {
				return new ExpressionStatement () {
					expression = expr,
				};
			}
			else {
				throw new Exception ("Unresolved statement");
			}
		}

		IfNode parse_if_clause() {
			expect_required_token_value ("(");
			var condition = parse_expression ();
			expect_required_token_value (")");
			expect_required_token_value ("{");
			return new IfNode () {
				condition = condition,
				body = parse_block_statement_list ()
			};
		}

		WhileNode parse_while_clause() {
			expect_required_token_value ("(");
			var condition = parse_expression ();
			expect_required_token_value (")");
			expect_required_token_value ("{");
			return new WhileNode () {
				condition = condition,
				body = parse_block_statement_list ()
			};
		}

		VariableExpression parse_variable() {
			return new VariableExpression () {
				name = current_token.value
			}; 
		}

		NumberExpression parse_number() {
			return new NumberExpression() {
				val = new NumberValue() {
					val = double.Parse(current_token.value)
				}
			};
		}

		ExpressionNode parse_expression() {
			return parse_comparison ();
		}

		ExpressionNode parse_comparison() {
			var left = parse_sum ();
			string current_token_value = current_token.value;
			while (true) {
				if (skip_if_value("<")  || 
					skip_if_value(">")  ||
					skip_if_value("==") || 
					skip_if_value("!=") ||
					skip_if_value("<=") || 
					skip_if_value(">=")) {
					var right = parse_sum ();
					left = new BinaryOpNode () {
						left_hand_side = left,
						right_hand_side = right,
						operator_str = current_token_value
					};
				}
				else {
					break;
				}
			}
			return left;
		}

		ExpressionNode parse_sum() {
			var left = parse_product ();
			while (true) {
				string current_token_value = current_token.value;
				if (skip_if_value("+") || skip_if_value("-")) {
					var right = parse_product ();
					left = new BinaryOpNode () {
						left_hand_side = left,
						right_hand_side = right,
						operator_str = current_token_value
					};
				}
				else {
					break;
				}
			}
			return left;
		}

		ExpressionNode parse_product() {
			var left = parse_primary ();
			while (true) {
				string current_token_value = current_token.value;
				if (skip_if_value("*") || skip_if_value("/")) {
					var right = parse_primary();
					left = new BinaryOpNode() {
						left_hand_side = left,
						right_hand_side = right,
						operator_str = current_token_value
					};
				}
				else {
					break;
				}
			}
			return left;
		}

		ExpressionNode parse_primary() {
			ExpressionNode result = parse_main ();
			while (true) {
				if (curr_token_is_value ("(")) {
					result = new FuncCallNode () {
						identifier = result,
						args = parse_func_arguments()
					};
				}
				else {
					break;
				}
			}
			return result;
		}

		ExpressionNode parse_main() {
			ExpressionNode result;
			if (skip_if_value("(")) {
				result = parse_parenthesis ();
				//move_next ();
			}
			else if (curr_token_is_type(Token.Type.Number)) {
				result = parse_number ();
				move_next ();
			}
			else if (curr_token_is_type(Token.Type.Identifier)) {
				result = parse_variable ();
				move_next ();
			}
			else {
				throw new Exception ("Expected primary token");
			}
			return result;
		}

		List<ExpressionNode> parse_func_arguments() {
			List<ExpressionNode> args = null;
			if (skip_if_value("(") && !skip_if_value(")")) {
				args = new List<ExpressionNode> ();
				args.Add (parse_expression ());
				expect_required_token_value (",");
				while(!skip_if_value(")")) {
					args.Add (parse_expression ());
					expect_required_token_value (",");
				}
			}
			return args;
		}

		ParenthesisNode parse_parenthesis() {
			var ex = parse_expression ();
			if (!skip_if_value(")")) {
				throw new Exception ("Expected closing brace");
			}
			return new ParenthesisNode () {
				child = ex
			};
		}

		List<StatementNode> parse_program_statement_list() {
			List<StatementNode> result = new List<StatementNode> ();
			while(true) {
				if (end_of_tokens) {
					return result;
				}
				else {
					result.Add (parse_statement ());
				}
			}
		}

		List<StatementNode> parse_block_statement_list() {
			List<StatementNode> result = new List<StatementNode> ();
			while (true) {
				if (skip_if_value ("}")) {
					return result;
				}
				else if (end_of_tokens) {
					throw new Exception ("EXPECTED END OF BLOCK");
				}
				else {
					result.Add (parse_statement ());
				}
			}
		}

//		List<StatementNode> parse_statement_list(PARSE_STATEMENT_LIST_EXIT_CASE exit_case) {
//			List<StatementNode> result = new List<StatementNode> ();
//			while (true) {
//				if (exit_case == PARSE_STATEMENT_LIST_EXIT_CASE.CLOSING_CURLY_BRACE && skip_if_value ("}")) {
//					return result;
//				}
//				else if (exit_case == PARSE_STATEMENT_LIST_EXIT_CASE.END_OF_FILE && end_of_tokens) {
//					return result;
//				}
//				if (!end_of_tokens) {
//					result.Add (parse_statement ());
//				}
//				else {
//					throw new Exception ("UNEXPECTED END OF STATEMENT LIST, EXPECTED : " + exit_case.ToString());
//				}
//			}
//		}

		void move_next() {
			position++;
		}
	}
}