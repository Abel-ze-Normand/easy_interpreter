using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace EasyScriptInterpreter {
	public class Lexer {
		public string s; // входная строк
		int pos; // позиция начала следующей лексeмы
		Regex lexemRx;

		public Lexer(string s) {
			this.s = s;
			string build_regex = String.Format(@"(?<comment>{0})|(?<number>{4})|(?<sp>{2})|(?<punkt>{3})|(?<ident>{1})|(?<str>{5})|(?<for_cycle>{6})",
				comment, ident, space, punktuation, number, str_regex, for_regex);
			lexemRx = new Regex(build_regex,
				RegexOptions.Compiled |
				RegexOptions.CultureInvariant |
				RegexOptions.ExplicitCapture |
				RegexOptions.IgnorePatternWhitespace |
				RegexOptions.Multiline |
				RegexOptions.Singleline);
		}


		string ident = @"\w[\w\d_]*";
		string space = @"[\s]+";
		string punktuation = @"==|!=|<=|>=|<|>|[\.\;\=\{\}\,\+\-\*\/\!\(\)\:\[\]\<\>\|\?\&]";
		string number = @"[0-9]+";
		string comment = @"//[^\n]*\n";
		string str_regex = @"""[^\n]*""|@""[^\n]*""";
		string for_regex = @"for[ ]*\(.*;.*;.*\)";
		public Token GetNextToken() {
			var rest = s.Substring(pos);
			var match = lexemRx.Match(rest);
			if (!match.Success) {
				throw new Exception("Bad string");
			}
			if (match.Index != 0) {
				throw new Exception("Bad string");
			}
			pos += match.Length;
			if (match.Groups ["sp"].Success) {
				return new Token (Token.Type.Space, match.Groups ["sp"].Value);
			}
			if (match.Groups["number"].Success) {
				return new Token(Token.Type.Number, match.Groups["number"].Value);
			}
			if (match.Groups["ident"].Success) {
				return new Token(Token.Type.Identifier, match.Groups["ident"].Value);
			}
			if (match.Groups["punkt"].Success) {
				return new Token(Token.Type.Punktuation, match.Groups["punkt"].Value);
			}
			if (match.Groups["comment"].Success) {
				return new Token(Token.Type.Comment, match.Groups["comment"].Value);
			}
			if (match.Groups["str"].Success) {
				return new Token(Token.Type.String, match.Groups["str"].Value);
			}
			if (match.Groups["for_cycle"].Success) {
				return new Token(Token.Type.For_cycle, match.Groups["for_cycle"].Value);
			}
			return new Token();
		}
		public IEnumerable<Token> GetTokens() {
			pos = 0;
			while (pos < s.Length) {
				yield return GetNextToken();
			}
		}
	}
	public class Token {
		public string value;
		public Type type;
		public enum Type {
			Space,
			Operator,
			Punktuation,
			Identifier,
			Number,
			Comment,
			String,
			For_cycle,
			None
		}
		public override string ToString() {
			return String.Format("<#Token: \"{0}\", type:\"{1}\">", value, type.ToString());
		}
		public Token() {
			type = Type.None;
		}
		public Token(Type tp, string val) {
			type = tp; value = val;
		}
	}
}