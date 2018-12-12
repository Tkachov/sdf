using System.Collections.Generic;
using System.IO;
using System.Text;
using JetBrains.Annotations;

namespace sdf.Parsing.SExpression {
	/// <summary>
	///     Simple S-Expression parser.
	///     Parses given string or reads whole file into string to parse.
	///     Returns only first S-Expression found.
	/// </summary>
	public sealed class Parser {
		private readonly string _contents;
		private int _position;

		private char CurrentCharacter => _contents[_position];
		private bool EndOfFile => _position >= _contents.Length;

		private Parser([NotNull] string contents) {
			_contents = contents;
			_position = 0;
		}

		private Parser([NotNull] TextReader streamReader) : this(streamReader.ReadToEnd()) { }

		/// <summary>
		///     Parse S-Expression written in a file <c>filename</c>.
		/// </summary>
		/// <param name="filename">Name of a file to read S-Expression from.</param>
		/// <returns>Parsed S-Expression.</returns>
		public static Expression Parse([NotNull] string filename) {
			using (var streamReader = new StreamReader(filename, Encoding.UTF8)) {
				var parser = new Parser(streamReader);
				return parser.Parse();
			}
		}

		/// <summary>
		///     Parse S-Expression written in a string <c>s</c>.
		/// </summary>
		/// <param name="s">String to read S-Expression from.</param>
		/// <returns>Parsed S-Expression.</returns>
		public static Expression ParseString([NotNull] string s) {
			var parser = new Parser(s);
			return parser.Parse();
		}

		private Expression Parse() {
			return ParseExpression();
		}

		// helpers

		private void MoveToNextCharacter() {
			++_position;
		}

		private void SkipWhitespace() {
			while (!EndOfFile && char.IsWhiteSpace(CurrentCharacter)) {
				MoveToNextCharacter();
			}
		}

		private string ReadUntilWhitespaceOrOneOfCharacters(string forbiddenCharset) {
			var result = "";
			while (!EndOfFile && !char.IsWhiteSpace(CurrentCharacter) && forbiddenCharset.IndexOf(CurrentCharacter) == -1) {
				result += CurrentCharacter;
				MoveToNextCharacter();
			}

			return result;
		}

		// main methods

		private Expression ParseExpression() {
			SkipWhitespace();
			var listTypes = new ParserListType[3] {
				new ParserListType('(', ')', ListBracketsType.Round),
				new ParserListType('[', ']', ListBracketsType.Square),
				new ParserListType('{', '}', ListBracketsType.Curly)
			};
			foreach (var listType in listTypes) {
				if (CurrentCharacter == listType.OpenBrace) {
					MoveToNextCharacter();
					return ParseListExpression(listType.ClosedBrace, listType.Type);
				}
			}

			return ParseLiteralExpression();
		}

		private ListExpression ParseListExpression(char listEndingCharacter, ListBracketsType listType) {
			var contents = new List<Expression>();
			while (true) {
				SkipWhitespace();
				if (EndOfFile) {
					throw new InvalidDataException("Unexpected EOF while parsing list expression.");
				}

				if (CurrentCharacter == listEndingCharacter) {
					MoveToNextCharacter();
					break;
				}

				contents.Add(ParseExpression());
			}

			return new ListExpression(listType, contents);
		}

		private Expression ParseLiteralExpression() {
			if (CurrentCharacter == '"') {
				MoveToNextCharacter();
				return ParseStringLiteralExpression();
			}

			var literal = ReadUntilWhitespaceOrOneOfCharacters("()[]{}");
			return new LiteralExpression(LiteralType.Keyword, literal);
		}

		private LiteralExpression ParseStringLiteralExpression() {
			var result = "";
			var backslash = false;
			for (; !EndOfFile && (backslash || CurrentCharacter != '"'); MoveToNextCharacter()) {
				// supported: \a, \b, \f, \n, \r, \t, \v, \\, \', \", \?
				if (backslash) {
					backslash = false;
					switch (CurrentCharacter) {
						case '\\':
						case '\'':
						case '"':
						case '?':
							result += CurrentCharacter;
							break;
						case 'a':
							result += '\a';
							break;
						case 'b':
							result += '\b';
							break;
						case 'f':
							result += '\f';
							break;
						case 'n':
							result += '\n';
							break;
						case 'r':
							result += '\r';
							break;
						case 't':
							result += '\t';
							break;
						case 'v':
							result += '\v';
							break;
						default:
							throw new InvalidDataException("Unknown escape sequence within string: \\" + CurrentCharacter);
					}

					continue;
				}

				if (CurrentCharacter == '\\') {
					backslash = true;
					continue;
				}

				result += CurrentCharacter;
			}

			if (EndOfFile) {
				throw new InvalidDataException("Unexpected EOF while parsing string expression.");
			}

			MoveToNextCharacter();
			return new LiteralExpression(LiteralType.String, result);
		}

		private struct ParserListType {
			public readonly char OpenBrace, ClosedBrace;
			public readonly ListBracketsType Type;

			public ParserListType(char o, char c, ListBracketsType t) {
				OpenBrace = o;
				ClosedBrace = c;
				Type = t;
			}
		}
	}
}