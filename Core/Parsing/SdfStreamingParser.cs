using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using JetBrains.Annotations;
using sdf.Core.Building;

namespace sdf.Core.Parsing {
	public enum TokenType {
		DocumentStart,
		NodeStart,
		NodeAttributeListStart,
		NodeAttributeListEnd,
		NodeAttributeStart,
		NodeAttributeEnd,
		NodeChildrenListStart,
		NodeChildrenListEnd,
		NodeEnd,
		Literal,
		DocumentEnd,
		NodeAfterAttributes,
		NodeChildrenListAfterChild,
		NodeAttributeListAfterOne
	}

	public sealed class SdfStreamingParser {
		private TextReader stream;

		//private char CurrentCharacter => _contents[_position];
		//private bool EndOfFile => _position >= _contents.Length;

		public string Error { get; private set;  }

		public bool Ended { get; private set; }
		public bool HasError => Error != null;

		private SDF document = null;
		private Stack<SDF> sdfs = new Stack<SDF>();
		private SDF currentSDF => sdfs.Peek();

		private Stack<string> attributeNames = new Stack<string>();
		private Stack<SDF> attributeValues = new Stack<SDF>();

		public string AttributeName => attributeNames.Peek();
		public string NodeName => (currentSDF as Node).Name;


		private char? _curChar = null;
		private char? CurrentCharacter {
			get {
				if (_curChar == null) ReadNextCharacter();
				return _curChar;
			}

			set { _curChar = value; }
		}

		private bool ReadNextCharacter() {
			var c = stream.Read();
			if (c == -1) {
				Ended = true;
				return false;
			}

			CurrentCharacter = (char)c;
			return true;
		}

		private TokenType currentToken => tokens.Peek();
		private Stack<TokenType> tokens = new Stack<TokenType>();

		public SDF Document => document;

		public SdfStreamingParser([NotNull] TextReader streamReader) {
			stream = streamReader;
			Error = null;
			Ended = false;
			tokens.Push(TokenType.DocumentEnd);
			tokens.Push(TokenType.DocumentStart);			
		} // new StreamReader(filename, Encoding.UTF8)) {



		// helpers

			/*
		private void MoveToNextCharacter() {
			++_position;
		}
		*/

		private void SkipWhitespace() {
			while (CurrentCharacter == null || char.IsWhiteSpace((char) CurrentCharacter)) {
				if (!ReadNextCharacter())
					break;
			}
		}

		/*
		private string ReadUntilWhitespaceOrOneOfCharacters(string forbiddenCharset) {
			var result = "";
			while (!EndOfFile && !char.IsWhiteSpace(CurrentCharacter) && forbiddenCharset.IndexOf(CurrentCharacter) == -1) {
				result += CurrentCharacter;
				MoveToNextCharacter();
			}
			return result;
		}
		*/

		// main methods

		private string ReadUntilWhitespaceOrOneOfCharacters(string forbiddenCharset) {
			var result = "";
			while (CurrentCharacter != null && !char.IsWhiteSpace((char) CurrentCharacter) && forbiddenCharset.IndexOf((char) CurrentCharacter) == -1) {
				result += CurrentCharacter;
				if (!ReadNextCharacter()) {
					break;
				}
			}
			return result;
		}

		private string ReadUntilWhitespaceOnlyAllowedCharacters(string allowedCharset) {
			var result = "";
			while (CurrentCharacter != null && !char.IsWhiteSpace((char)CurrentCharacter)) {
				if (allowedCharset.IndexOf((char) CurrentCharacter) == -1) {
					//throw new InvalidDataException("Invalid character (only one of '"+allowedCharset+"' expected before next whitespace).");
					break;
				}
                result += CurrentCharacter;
				if (!ReadNextCharacter()) {
					break;
				}
			}
			return result;
		}

		private string ReadStringLiteral() {
			var result = "";
			var backslash = false;
			var first = true;
			while (backslash || CurrentCharacter != '"') {
				if (CurrentCharacter == null) break;

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

					//ReadNextCharacter();

					if (!ReadNextCharacter()) {
						throw new InvalidDataException("Unexpected EOF while parsing string expression.");
					}
					continue;
				}

				if (CurrentCharacter == '\\') {
					backslash = true;

					if (!ReadNextCharacter()) {
						throw new InvalidDataException("Unexpected EOF while parsing string expression.");
					}
					continue;
				}

				result += CurrentCharacter;

				if (!ReadNextCharacter()) {
					throw new InvalidDataException("Unexpected EOF while parsing string expression.");
				}
			}

			ReadNextCharacter(); // skip last "
			return result;
		}

		private SDF readLiteral() {
			if (CurrentCharacter == null)
				return null;

			switch (char.ToLower(CurrentCharacter.Value)) {
				case 'n':
				case 't':
				case 'f':
					// null, true, false
					var bufferN = ReadUntilWhitespaceOnlyAllowedCharacters("nultrefas"); //ReadUntilWhitespaceOrOneOfCharacters("}])");
					Console.WriteLine("\tkeyword: " + bufferN);
					if (bufferN.ToLower() == "null") {
						return new NullLiteral();
					}
					if (bufferN.ToLower() == "true") {
						return new BooleanLiteral(true);
					}
					if (bufferN.ToLower() == "false") {
						return new BooleanLiteral(false);
					}
				break;
				
				case '"':
					// string
					ReadNextCharacter();
					var s = ReadStringLiteral();
					Console.WriteLine("\tstring: " + s);
					return new StringLiteral(s);
				break;
			}

			if (CurrentCharacter == '-' || char.IsDigit((char)CurrentCharacter)) {
				// number
				var buffer = ReadUntilWhitespaceOnlyAllowedCharacters("-1234567890.");//ReadUntilWhitespaceOrOneOfCharacters("}])");
				Console.WriteLine("\tnumber: " + buffer);
				var index = buffer.IndexOf('.');
				if (index == -1)
					return new NumberLiteral(long.Parse(buffer), 0);

				return new NumberLiteral(long.Parse(buffer.Substring(0, index)), long.Parse(buffer.Substring(index+1)));
			}

			return null;
		}

		private void readEitherNodeOrLiteral() {
			// either Node or Literal
			if (CurrentCharacter == '(') {
				ReadNextCharacter();
				SkipWhitespace();
				tokens.Push(TokenType.NodeEnd);
				tokens.Push(TokenType.NodeStart);
				var nodename = "";
				while (CurrentCharacter != null && !char.IsWhiteSpace((char) CurrentCharacter)) {
					nodename += CurrentCharacter;
					if (!ReadNextCharacter())
						break; // TODO: unexpected eof
				}
				sdfs.Push(new Node(nodename, new Dictionary<string, SDF>(), new List<SDF>()));
				return;
			} else {
				tokens.Push(TokenType.Literal);
				var l = readLiteral();
				if (l != null) {
					sdfs.Push(l);
					return;
				}
			}

			Error = "Invalid SDF: neither node nor any of supported literals found.";
		}

		public TokenType readNext() {
			var result = tokens.Pop();
			SkipWhitespace();
			switch (result) {
				case TokenType.DocumentStart:
					readEitherNodeOrLiteral();
					document = currentSDF;
				break;

				case TokenType.NodeStart:
					if (CurrentCharacter == ')') {
						// skip, next one is NodeEnd
					} else if (CurrentCharacter == '{') {
						ReadNextCharacter();
						tokens.Push(TokenType.NodeAfterAttributes);
						tokens.Push(TokenType.NodeAttributeListEnd);
						tokens.Push(TokenType.NodeAttributeListStart);
					} else {
						tokens.Push(TokenType.NodeChildrenListEnd);
						tokens.Push(TokenType.NodeChildrenListStart);						
					}
				break;

				case TokenType.NodeAfterAttributes:
					if (CurrentCharacter == ')') {
						// skip, next one is NodeEnd
					} else if (CurrentCharacter == '{') {
						Error = "Invalid SDF: node cannot have two attribute lists.";
					} else {
						tokens.Push(TokenType.NodeChildrenListEnd);
						tokens.Push(TokenType.NodeChildrenListStart);
					}
				break;

				case TokenType.NodeAttributeListStart:
					if (CurrentCharacter == '}') {
						// skip, next one is ListEnd
					} else {
						tokens.Push(TokenType.NodeAttributeListAfterOne);
						tokens.Push(TokenType.NodeAttributeEnd);
						tokens.Push(TokenType.NodeAttributeStart);
						var buffer = ReadUntilWhitespaceOrOneOfCharacters("");
						attributeNames.Push(buffer); // TODO
					}
				break;

				case TokenType.NodeAttributeStart:
					readEitherNodeOrLiteral();
					attributeValues.Push(currentSDF);
				break;

				case TokenType.NodeAttributeEnd:
					var name = attributeNames.Pop();
					var value = attributeValues.Pop(); sdfs.Pop();
					var node = currentSDF as Node;
					node.Attributes.Add(name, value);
				break;

				case TokenType.NodeAttributeListAfterOne:
					if (CurrentCharacter == '}') {
						ReadNextCharacter();
						return tokens.Pop(); // move to ListEnd + AfterOne won't appear for users
					}

					tokens.Push(TokenType.NodeAttributeListAfterOne);
					tokens.Push(TokenType.NodeAttributeEnd);
					tokens.Push(TokenType.NodeAttributeStart);
					var bufferx = ReadUntilWhitespaceOrOneOfCharacters("");
					attributeNames.Push(bufferx); // TODO
					break;

				case TokenType.NodeAttributeListEnd:
					if (CurrentCharacter != '}') {
						Error = "Invalid SDF (expected attribute list to end).";
					}
					ReadNextCharacter();
				break;

				case TokenType.NodeChildrenListStart:
					bool multiple = false;
					if (CurrentCharacter == '[') {
						ReadNextCharacter();
						SkipWhitespace();
						multiple = true;
					}
					if (CurrentCharacter == ']') {
						// skip, next one is ListEnd
					} else {
						if (multiple)
							tokens.Push(TokenType.NodeChildrenListAfterChild);
						readEitherNodeOrLiteral();
					}
				break;

				case TokenType.NodeChildrenListAfterChild:
					var c = sdfs.Pop();
					var node2 = currentSDF as Node;
					node2.Children.Add(c);

					if (CurrentCharacter == ']') {
						ReadNextCharacter();
						return tokens.Pop(); // so ListEnd wouldn't break stack + AfterChild won't appear for users
					} else {
						tokens.Push(TokenType.NodeChildrenListAfterChild);
						readEitherNodeOrLiteral();
					}
				break;

				case TokenType.NodeChildrenListEnd:
					var c2 = sdfs.Pop();
					var node3 = currentSDF as Node;
					node3.Children.Add(c2);
					if (CurrentCharacter == ']') {
						ReadNextCharacter();
					}
				break;

				case TokenType.NodeEnd:
					if (CurrentCharacter != ')') {
						Error = "Invalid SDF (expected node to end).";
					}
					ReadNextCharacter();
				break;

				case TokenType.Literal:
					// just nothing
				break;

				case TokenType.DocumentEnd:
					// just nothing
				break;

				default:
					throw new ArgumentOutOfRangeException();
			}
			return result;
		}
	}
}