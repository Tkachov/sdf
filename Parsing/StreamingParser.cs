using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using JetBrains.Annotations;
using sdf.Validating;

namespace sdf.Parsing {
	/// <summary>
	///     Enumeration of token types / states of <c>StreamingParser</c>.
	///     Those are used within and also returned to user while parsing.
	/// </summary>
	public enum TokenType {
		/// <summary>	Returned once on document parsing start. </summary>
		DocumentStart,

		/// <summary>	Returned on node parsing start. <c>NodeName</c> is available and guaranteed to return a name of the node that started. </summary>
		NodeStart,

		/// <summary>	Returned when node's attribute list parsing starts, i.e. <c>{</c> found. </summary>
		NodeAttributeListStart,

		/// <summary>	Returned when node's attribute list parsing ends, i.e. <c>}</c> found. </summary>
		NodeAttributeListEnd,

		/// <summary>	Returned on node attribute parsing start. <c>AttributeName</c> is available and guaranteed to return a name of the started attribute. </summary>
		NodeAttributeStart,

		/// <summary>	Returned on node attribute parsing end. </summary>
		NodeAttributeEnd,

		/// <summary>	Returned when node's children list parsing starts. </summary>
		NodeChildrenListStart,

		/// <summary>	Returned when node's children list parsing ends. </summary>
		NodeChildrenListEnd,

		/// <summary>	Returned on node parsing end. </summary>
		NodeEnd,

		/// <summary>	Returned if literal was parsed. </summary>
		Literal,

		/// <summary>
		///     Returned once on document parsing end. Calling <c>ReadNext()</c> on a parser that returned <c>DocumentEnd</c>
		///     is invalid action and may lead to exception to be thrown.
		/// </summary>
		DocumentEnd,

		/// <summary>	Returned after node attributes parsed and before node children list is parsed. </summary>
		NodeAfterAttributes,

		/// <summary>	Returned between node children parsing (is not returned after the last child). </summary>
		NodeChildrenListAfterChild,

		/// <summary>	Returned between node attributes parsing (is not returned after the last attribute). </summary>
		NodeAttributeListAfterOne,

		/// <summary>	Returned after only child of a node is parsed within []. </summary>
		NodeChildrenListAfterOnlyChild
	}

	/// <summary>
	///     Streaming SDF parser.
	///     Parses SDF from a file token by token and is not recursive.
	///     Returns information about currently parsed document while working.
	///     Could validate document with given SDF Schema while parsing and stops parsing if document does not match the
	///     schema.
	///     Returns only the first SDF document found in a file.
	/// </summary>
	public sealed class StreamingParser {
		private readonly Stack<string> _attributeNames = new Stack<string>();
		private readonly Stack<SDF> _attributeValues = new Stack<SDF>();

		private readonly Stack<SDF> _sdfs = new Stack<SDF>();
		private readonly TextReader _stream;
		private readonly Stack<TokenType> _tokens = new Stack<TokenType>();

		private char? _curChar;

		private char? CurrentCharacter {
			get {
				if (_curChar == null) {
					ReadNextCharacter();
				}

				return _curChar;
			}

			set { _curChar = value; }
		}

		private SDF _currentSDF => _sdfs.Peek();

		/// <summary>
		///     The latest error message, or <c>null</c>.
		/// </summary>
		public string Error { get; private set; }

		/// <summary>
		///     Returns whether document is completely parsed.
		/// </summary>
		public bool Ended { get; private set; }

		/// <summary>
		///     Returns whether an error occured.
		/// </summary>
		public bool HasError => Error != null;

		/// <summary>
		///     Returns current attribute name (mostly makes sense at <c>NodeAttributeStart</c> state).
		/// </summary>
		public string AttributeName => _attributeNames.Peek();

		/// <summary>
		///     Returns current node name (mostly makes sense at <c>NodeStart</c> state).
		/// </summary>
		public string NodeName => (_currentSDF as Node).Name;

		/// <summary>
		///     Returns current document.
		///     If not <c>Ended</c>, returns currently parsed part of the document.
		/// </summary>
		public SDF Document { get; private set; }

		/// <summary>
		///     Create parser to read SDF from a file with given name.
		/// </summary>
		/// <param name="filename">Name of a file to read SDF from.</param>
		public StreamingParser([NotNull] string filename) : this(new StreamReader(filename, Encoding.UTF8)) { }

		/// <summary>
		///     Create parser to read SDF from a given stream.
		/// </summary>
		/// <param name="streamReader">Stream to read SDF from.</param>
		public StreamingParser([NotNull] TextReader streamReader) {
			_stream = streamReader;
			Error = null;
			Ended = false;
			_tokens.Push(TokenType.DocumentEnd);
			_tokens.Push(TokenType.DocumentStart);
		}

		/// <summary>
		///     Parse whole SDF document from a file with given name via <c>StreamingParser</c>.
		/// </summary>
		/// <param name="filename">Name of a file to read SDF from.</param>
		/// <returns>Parsed SDF.</returns>
		public static SDF Parse([NotNull] string filename) {
			var p = new StreamingParser(filename);
			while (!p.Ended && !p.HasError) {
				var t = p.ReadNext();
				if (t == TokenType.DocumentEnd) {
					break;
				}
			}

			p.Close();

			if (p.HasError) {
				throw new InvalidDataException("Error while stream parsing the file:\n\t" + p.Error);
			}

			return p.Document;
		}

		/// <summary>
		///     Parse whole SDF document from a file with given name via <c>StreamingParser</c>.
		///     Validates SDF document with given SDF Schema while parsing, and returns the document only if it matches the schema.
		///     Otherwise, exception will be thrown.
		/// </summary>
		/// <param name="filename">Name of a file to read SDF from.</param>
		/// <param name="schema">SDF Schema to validate SDF document with.</param>
		/// <returns>Parsed SDF.</returns>
		public static SDF ParseAndValidateSchema([NotNull] string filename, Schema schema) {
			var p = new StreamingParser(filename);
			while (!p.Ended && !p.HasError) {
				var t = p.ReadNext();
				if (t == TokenType.DocumentStart) {
					continue;
				}

				if (t == TokenType.DocumentEnd) {
					break;
				}

				if (t == TokenType.NodeEnd) {
					if (!schema.ValidatePartial(p.Document)) {
						p.Close();
						throw new InvalidDataException("Document already does not match the schema:\n" + schema.ErrorMessage);
					}
				}
			}

			p.Close();

			if (p.HasError) {
				throw new InvalidDataException("Error while stream parsing the file:\n\t" + p.Error);
			}

			if (!schema.Validate(p.Document)) {
				throw new InvalidDataException("File is read completely, but document does not match the schema:\n" + schema.ErrorMessage);
			}

			return p.Document;
		}

		/// <summary>
		///     Make parser read one token further.
		/// </summary>
		/// <returns>Type of a processed token.</returns>
		public TokenType ReadNext() {
			var result = _tokens.Pop();
			SkipWhitespace();
			switch (result) {
				case TokenType.DocumentStart:
					if (ReadEitherNodeOrLiteral()) {
						Document = _currentSDF;
					}

					break;

				case TokenType.NodeStart:
					if (CurrentCharacter == ')') {
						// skip, next one is NodeEnd
					} else if (CurrentCharacter == '{') {
						ReadNextCharacter();
						_tokens.Push(TokenType.NodeAfterAttributes);
						_tokens.Push(TokenType.NodeAttributeListEnd);
						_tokens.Push(TokenType.NodeAttributeListStart);
					} else {
						_tokens.Push(TokenType.NodeChildrenListEnd);
						_tokens.Push(TokenType.NodeChildrenListStart);
					}

					break;

				case TokenType.NodeAfterAttributes:
					if (CurrentCharacter == ')') {
						// skip, next one is NodeEnd
					} else if (CurrentCharacter == '{') {
						Error = "Invalid SDF: node cannot have two attribute lists.";
					} else {
						_tokens.Push(TokenType.NodeChildrenListEnd);
						_tokens.Push(TokenType.NodeChildrenListStart);
					}

					break;

				case TokenType.NodeAttributeListStart:
					if (CurrentCharacter == '}') {
						// skip, next one is ListEnd
					} else {
						_tokens.Push(TokenType.NodeAttributeListAfterOne);
						_tokens.Push(TokenType.NodeAttributeEnd);
						_tokens.Push(TokenType.NodeAttributeStart);
						var buffer = ReadUntilWhitespaceOrOneOfCharacters("");
						_attributeNames.Push(buffer); // TODO
					}

					break;

				case TokenType.NodeAttributeStart:
					if (ReadEitherNodeOrLiteral()) {
						_attributeValues.Push(_currentSDF);
					}

					break;

				case TokenType.NodeAttributeEnd:
					var name = _attributeNames.Pop();
					var value = _attributeValues.Pop();
					_sdfs.Pop();
					var node = _currentSDF as Node;
					node.Attributes.Add(name, value);
					break;

				case TokenType.NodeAttributeListAfterOne:
					if (CurrentCharacter == '}') {
						ReadNextCharacter();
						return _tokens.Pop(); // move to ListEnd + AfterOne won't appear for users
					}

					_tokens.Push(TokenType.NodeAttributeListAfterOne);
					_tokens.Push(TokenType.NodeAttributeEnd);
					_tokens.Push(TokenType.NodeAttributeStart);
					var bufferx = ReadUntilWhitespaceOrOneOfCharacters("");
					_attributeNames.Push(bufferx); // TODO
					break;

				case TokenType.NodeAttributeListEnd:
					if (CurrentCharacter != '}') {
						Error = "Invalid SDF (expected attribute list to end).";
					}

					ReadNextCharacter();
					break;

				case TokenType.NodeChildrenListStart:
					var multiple = false;
					if (CurrentCharacter == '[') {
						ReadNextCharacter();
						SkipWhitespace();
						multiple = true;
					}

					if (CurrentCharacter == ']') {
						// skip, next one is ListEnd
					} else {
						if (multiple) {
							_tokens.Push(TokenType.NodeChildrenListAfterChild);
						} else {
							_tokens.Push(TokenType.NodeChildrenListAfterOnlyChild);
						}

						ReadEitherNodeOrLiteral();
					}

					break;

				case TokenType.NodeChildrenListAfterChild:
					var c = _sdfs.Pop();
					var node2 = _currentSDF as Node;
					node2.Children.Add(c);

					if (CurrentCharacter == ']') {
						ReadNextCharacter();
						return _tokens.Pop(); // so ListEnd wouldn't break stack + AfterChild won't appear for users
					}

					_tokens.Push(TokenType.NodeChildrenListAfterChild);
					ReadEitherNodeOrLiteral();
					break;

				case TokenType.NodeChildrenListAfterOnlyChild:
					var c2 = _sdfs.Pop();
					var node3 = _currentSDF as Node;
					node3.Children.Add(c2);
					break;

				case TokenType.NodeChildrenListEnd:
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
					Close();
					throw new ArgumentOutOfRangeException();
			}

			return result;
		}

		/// <summary>
		///     Close inner <c>TextReader</c>, making it impossible to parse document further.
		///     Should be used when document is already parsed or an error occured.
		/// </summary>
		public void Close() {
			_stream.Close();
		}

		// helpers

		private bool ReadNextCharacter() {
			var c = _stream.Read();
			if (c == -1) {
				Ended = true;
				if (_tokens.Count > 0 && _tokens.Peek() != TokenType.DocumentEnd) {
					Close();
					Error = "Unexpected EOF.";
				}

				return false;
			}

			CurrentCharacter = (char) c;
			return true;
		}

		private void SkipWhitespace() {
			while (CurrentCharacter == null || char.IsWhiteSpace((char) CurrentCharacter)) {
				if (!ReadNextCharacter()) {
					break;
				}
			}
		}

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
			while (CurrentCharacter != null && !char.IsWhiteSpace((char) CurrentCharacter)) {
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
			while (backslash || CurrentCharacter != '"') {
				if (CurrentCharacter == null) {
					break;
				}

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
							Close();
							throw new InvalidDataException("Unknown escape sequence within string: \\" + CurrentCharacter);
					}

					//ReadNextCharacter();

					if (!ReadNextCharacter()) {
						Close();
						throw new InvalidDataException("Unexpected EOF while parsing string expression.");
					}

					continue;
				}

				if (CurrentCharacter == '\\') {
					backslash = true;

					if (!ReadNextCharacter()) {
						Close();
						throw new InvalidDataException("Unexpected EOF while parsing string expression.");
					}

					continue;
				}

				result += CurrentCharacter;

				if (!ReadNextCharacter()) {
					Close();
					throw new InvalidDataException("Unexpected EOF while parsing string expression.");
				}
			}

			ReadNextCharacter(); // skip last "
			return result;
		}

		// main methods

		private SDF ReadLiteral() {
			if (CurrentCharacter == null) {
				return null;
			}

			switch (char.ToLower(CurrentCharacter.Value)) {
				case 'n':
				case 't':
				case 'f':
					// null, true, false
					var bufferN = ReadUntilWhitespaceOnlyAllowedCharacters("nultrefas"); //ReadUntilWhitespaceOrOneOfCharacters("}])");
					//Console.WriteLine("\tkeyword: " + bufferN);
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
					//Console.WriteLine("\tstring: " + s);
					return new StringLiteral(s);
			}

			if (CurrentCharacter == '-' || char.IsDigit((char) CurrentCharacter)) {
				// number
				var buffer = ReadUntilWhitespaceOnlyAllowedCharacters("-1234567890."); //ReadUntilWhitespaceOrOneOfCharacters("}])");
				//Console.WriteLine("\tnumber: " + buffer);
				var index = buffer.IndexOf('.');
				if (index == -1) {
					return new NumberLiteral(long.Parse(buffer), 0);
				}

				return new NumberLiteral(long.Parse(buffer.Substring(0, index)), long.Parse(buffer.Substring(index + 1)));
			}

			return null;
		}

		private bool ReadEitherNodeOrLiteral() {
			// either Node or Literal
			if (CurrentCharacter == '(') {
				ReadNextCharacter();
				SkipWhitespace();
				_tokens.Push(TokenType.NodeEnd);
				_tokens.Push(TokenType.NodeStart);
				var nodename = ReadUntilWhitespaceOrOneOfCharacters("(){}[]\"");
				if (nodename == "") {
					Error = "Invalid SDF: node must have a name.";
					return false;
				}

				_sdfs.Push(new Node(nodename, new Dictionary<string, SDF>(), new List<SDF>()));
				return true;
			}

			_tokens.Push(TokenType.Literal);
			var l = ReadLiteral();
			if (l != null) {
				_sdfs.Push(l);
				return true;
			}

			Error = "Invalid SDF: neither node nor any of supported literals found.";
			return false;
		}
	}
}