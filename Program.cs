using System.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

#region NodesForAST
namespace EasyScriptInterpreter
{
	public interface Node { }

	public interface ExpressionNode : Node {
		T accept<T> (ExpressionVisitor<T> node);
	}

	public interface StatementNode : Node {
		void accept (StatementVisitor node);
	}

	public interface ExpressionVisitor<T>{
		T visit_number (NumberExpression node);
		T visit_variable (VariableExpression node);
		T visit_func_call (FuncCallNode node);
		T visit_parenthesis (ParenthesisNode node);
		T visit_binary_op (BinaryOpNode node);
		T visit_expression (ExpressionNode node);
		T visit_unary_minus (UnaryMinusNode node);
	}

	public interface StatementVisitor {
		void visit_assignment (AssignmentNode node);
		void visit_if (IfNode node);
		void visit_while (WhileNode node);
		void visit_program (ProgramNode node);
		void visit_expression_statement (ExpressionStatement node);
	}

	public class IfNode : StatementNode {
		public ExpressionNode condition;
		public List<StatementNode> body = new List<StatementNode> ();
		public void accept (StatementVisitor v) {
			v.visit_if (this);
		}
	}

	public class WhileNode : StatementNode {
		public ExpressionNode condition;
		public List<StatementNode> body = new List<StatementNode> ();
		public void accept (StatementVisitor v) {
			v.visit_while (this);
		}
	}

	public class UnaryMinusNode : ExpressionNode {
		public ExpressionNode body;
		public T accept<T> (ExpressionVisitor<T> v) {
			return v.visit_unary_minus (this);
		}
	}

	public class FuncCallNode : ExpressionNode {
		public ExpressionNode identifier;
		public List<ExpressionNode> args;
		public T accept<T> (ExpressionVisitor<T> v) {
			return v.visit_func_call (this);
		}
	}

	public class AssignmentNode : StatementNode {
		public ExpressionNode left_hand_side;
		public ExpressionNode right_hand_side;
		public void accept(StatementVisitor v) {
			v.visit_assignment (this);
		}
	}

	public class NumberExpression : ExpressionNode	{
		public Value val;
		public T accept<T>(ExpressionVisitor<T> v) {
			return v.visit_number (this);
		}
	}

	public class VariableExpression : ExpressionNode {
		public string name;
		public T accept<T>(ExpressionVisitor<T> v) {
			return v.visit_variable (this);
		}
	}

	public class BinaryOpNode : ExpressionNode {
		public ExpressionNode left_hand_side, right_hand_side;
		public string operator_str;
		public T accept<T>(ExpressionVisitor<T> v) {
			return v.visit_binary_op (this);
		}
	}

	public class ParenthesisNode : ExpressionNode {
		public ExpressionNode child;
		public T accept<T>(ExpressionVisitor<T> v) {
			return v.visit_parenthesis (this);
		}
	}

	public class ProgramNode : StatementNode {
		public List<StatementNode> Children = new List<StatementNode> ();
		public void accept (StatementVisitor v) {
			v.visit_program (this);
		}
	}

	public class ExpressionStatement : StatementNode {
		public ExpressionNode expression;
		public void accept (StatementVisitor v) {
			v.visit_expression_statement (this);
		}
	}

	public interface Value	{ }

	public class NumberValue : Value {
		public double val;
		public override string ToString () {
			return string.Format ("{0}", val);
		} 
		public static bool operator==(NumberValue a, NumberValue b) {
			return a.val == b.val;
		}
		public static bool operator!=(NumberValue a, NumberValue b) {
			return a.val != b.val;
		}
	}

	public class BoolValue : Value	{
		public bool val;
		public override string ToString () {
			return string.Format ("{0}", val);
		}
		public static bool operator==(BoolValue a, BoolValue b) {
			return a.val == b.val;
		}
		public static bool operator!=(BoolValue a, BoolValue b) {
			return a.val != b.val;
		}
	}

	public class NullValue : Value {
		public override string ToString () {
			return "nil";
		}
	}

	interface FunctionValue : Value {
		Value Exec (List<Value> args);
	}

	public class PrintFunction : FunctionValue {
		public Value Exec (List<Value> args) {
			Console.WriteLine ("PRINT:");
			foreach (Value item in args) {
				Console.WriteLine (item.ToString());
			}
			return null;
		}
		public override string ToString ()
		{
			return string.Format ("print");
		}
	}

//	public interface ExpressionVisitor<T>{
//		T visit_number (NumberExpression node);
//		T visit_variable (VariableExpression node);
//		T visit_func_call (FuncCallNode node);
//		T visit_parenthesis (ParenthesisNode node);
//		T visit_binary_op (BinaryOpNode node);
//		T visit_expression (ExpressionVisitor<T> node);
//		T visit_unary_minus (UnaryMinusNode node);
//	}
//
//	public interface StatementVisitor {
//		void visit_assignment (AssignmentNode node);
//		void visit_if (IfNode node);
//		void visit_while (WhileNode node);
//		void visit_program (ProgramNode node);
//		void visit_expression (ExpressionStatement node);
//	}

	public class Evaluator : ExpressionVisitor<Value>, StatementVisitor {
		Dictionary<string, Value> environment_variables = new Dictionary<string, Value>() {
			{"true", new BoolValue() {
					val = true
				}},
			{"false", new BoolValue() {
					val = false
				}},
			{"print", new PrintFunction()}
		};
		ProgramNode root;

		public Evaluator(ProgramNode _root) {
			root = _root;
		}

		public Value visit_number (NumberExpression node) {
			return new NumberValue () {
				val = (node.val as NumberValue).val
			};
		}

		public Value visit_variable (VariableExpression node) {
			Value variable;
			if (!environment_variables.TryGetValue(node.name, out variable)) {
				throw new Exception ("No such variable");
			}
			return variable;
		}

		public Value visit_func_call(FuncCallNode node) {
			Value func;
			if (!environment_variables.TryGetValue(node.identifier.accept(this).ToString(), out func)) {
				throw new Exception ("Call on unknown function");
			}

			List<Value> args = new List<Value> ();
			foreach(ExpressionNode item in node.args) {
				args.Add (item.accept (this));
			}
			return (func as FunctionValue).Exec (args);
		}

		public Value visit_parenthesis(ParenthesisNode node) {
			return node.child.accept (this);
		}

		public Value visit_binary_op(BinaryOpNode node) {
			if (node.right_hand_side == null) {
				return new NullValue ();
			}
			var a = node.left_hand_side.accept (this);
			var b = node.right_hand_side.accept (this);
			return new Calculator (a, b, node.operator_str).exec_calculation ();
		}

		public Value visit_expression(ExpressionNode node) {
			return node.accept (this);
		}

		public Value visit_unary_minus(UnaryMinusNode node) {
			var value = node.body.accept (this);
			if (!(value is NumberValue)) {
				throw new Exception ("Unary minus is only for numbers");
			}
			return new NumberValue() {
				val = -(value as NumberValue).val
			};
		}

		public void visit_assignment(AssignmentNode node) {
			if (node.left_hand_side is FuncCallNode) {
				node.left_hand_side.accept (this);
				return;
			}
			environment_variables [(node.left_hand_side as VariableExpression).name] = node.right_hand_side.accept (this);
		}

		public void visit_if(IfNode node) {
			var cond = node.condition.accept (this);
			if (!(cond is BoolValue))
				throw new Exception ("Expected boolean condition");
			if ((cond as BoolValue).val) {
				foreach (StatementNode item in node.body) {
					item.accept (this);
				}
			}
		}

		public void visit_while(WhileNode node) {
			var cond = node.condition.accept (this);
			if (!(cond is BoolValue))
				throw new Exception ("Expected boolean condition");
			while ((node.condition.accept(this) as BoolValue).val) {
				foreach (StatementNode item in node.body) {
					item.accept (this);
				}
			}
		}

		public void visit_program(ProgramNode node) {
			foreach(StatementNode item in node.Children) {
				item.accept (this);
			}
		}

		public void visit_expression_statement(ExpressionStatement node) {
			node.expression.accept (this);
		}

		public void Run() {
			visit_program (root);
		}
	}

	public class Calculator {
		Value a, b;
		string op;
		public Calculator (Value _a, Value _b, string _op){
			a = _a; b = _b; op = _op;
		}
		public Value exec_calculation() {
			switch (op) {
			case "+":
				assert_types (true, typeof(NumberValue));
				return new NumberValue {
					val = (a as NumberValue).val + (b as NumberValue).val
				};
			case "-":
				assert_types (true, typeof(NumberValue));
				return new NumberValue {
					val = (a as NumberValue).val - (b as NumberValue).val
				};
			case "*":
				assert_types (true, typeof(NumberValue));
				return new NumberValue {
					val = (a as NumberValue).val * (b as NumberValue).val
				};
			case "/":
				assert_types (true, typeof(NumberValue));
				return new NumberValue {
					val = (a as NumberValue).val / (b as NumberValue).val
				};
			case "<":
				assert_types (true, typeof(NumberValue));
				return new BoolValue {
					val = (a as NumberValue).val < (b as NumberValue).val
				};
			case ">" :
				assert_types (true, typeof(NumberValue));
				return new BoolValue {
					val = (a as NumberValue).val > (b as NumberValue).val
				};
			case "<=":
				assert_types (true, typeof(NumberValue));
				return new BoolValue {
					val = (a as NumberValue).val < (b as NumberValue).val
				};
			case ">=":
				assert_types (true, typeof(NumberValue));
				return new BoolValue {
					val = (a as NumberValue).val < (b as NumberValue).val
				};
			case "==":
				assert_types (true, typeof(NumberValue), typeof(BoolValue));
				if (a is NumberValue) {
					return new BoolValue {
						val = (a as NumberValue) == (b as NumberValue)
					};
				}
				else {
					return new BoolValue {
						val = (a as BoolValue) == (b as BoolValue)
					};
				}
			case "!=":
				assert_types (true, typeof(NumberValue), typeof(BoolValue));
				if (a is NumberValue) {
					return new BoolValue {
						val = (a as NumberValue) != (b as NumberValue)
					};
				}
				else {
					return new BoolValue {
						val = (a as BoolValue) != (b as BoolValue)
					};
				}
			default:
				throw new Exception ("Unknown operator");
			}
		}
		public void assert_types(bool require_equal_types, params Type[] types) {
			if (require_equal_types && a.GetType() != b.GetType()) {
				throw new Exception ("Incompatible types");
			}
			bool asserted_any_a, asserted_any_b;
			asserted_any_a = asserted_any_b = false;
			foreach (Type t in types) {
				if (a.GetType () == t)
					asserted_any_a = true;
				if (b.GetType () == t)
					asserted_any_b = true;
			}
			if (!asserted_any_a || !asserted_any_b) {
				throw new Exception ("Incompatible types");
			}
		}
	}
	
	#endregion NodesForAST

	class Program
	{
		static void Main (string[] args)
		{
			Lexer l = new Lexer(
				@"a = -1 + 2;
				b = a + 1;
c = a == b;
c = 1;
c = true;
if (c == true){
	print(a);
}
while (a < b + 10){
	print(a);
	a = a + 1;
}");

			Parser p = new Parser (l);
			ProgramNode pn = p.parse_program ();
			new Evaluator (pn).Run ();
		}
	}
}