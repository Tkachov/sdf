using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sdf.Core.Matching {
	enum TokenType {
		NodeName,
		AttributeName,
		NodeIndex,
		NodeNumber,
		Type,
		ValueCondition
	}

	class ConditionParser {
		private string _full;
		private int _position;
		private string _buffer;
		private TokenType _currentlyParsing = TokenType.NodeName;
		private List<Condition> conditions;

		public ConditionParser(string s) {
			_full = s;
			_position = 0;
			_buffer = "";
			conditions = new List<Condition>();
		}

		public List<Condition> Parse() {
			while (_position < _full.Length)
				ParseChar();
			HandleBuffer();
			return conditions;
		}

		private void ParseChar() {
			char[] separators = {'[', '#', '^', '*', '+', '@'};
			char c = _full[_position];
			++_position;

			if (_currentlyParsing == TokenType.ValueCondition) {
				if (c == ']') {
					HandleBuffer();
					return; // don't put into buffer
				}
			} else {
				if (separators.Contains(c)) {
					var bufferWasEmpty = (_buffer == "");

					// handle buffer
					HandleBuffer();

					// start parsing something new
					if (c == '@') {
						if (bufferWasEmpty) { // attribute name
							_currentlyParsing = TokenType.AttributeName;
						} else { // node@111
							_currentlyParsing = TokenType.NodeNumber;
						}
					} else if (c == '[') {
						_currentlyParsing = TokenType.ValueCondition;
					} else if (c == '#') {
						_currentlyParsing = TokenType.NodeIndex;
					} else if (c == '^') {
						_currentlyParsing = TokenType.Type;
					} else if (c == '*' || c == '+') {
						conditions.Add(new ArbitraryNodeHierarchyCondition(c == '+'));
					}

					return; // don't put into buffer
				}
			}

			_buffer += c;
		}

		private void HandleBuffer() {
			Condition c = GetConditionFromBuffer();
			if (c != null)
				conditions.Add(c);

			// reset state
			_currentlyParsing = TokenType.NodeName;
			_buffer = "";
		}

		private Condition GetConditionFromBuffer() {
			if (_buffer == "") return null;

			switch (_currentlyParsing) {
				case TokenType.NodeName:
					return new NodeNameCondition(_buffer);

				case TokenType.AttributeName:
					return new AttributeNameCondition(_buffer);

				case TokenType.NodeIndex:
					return new NodeIndexCondition(int.Parse(_buffer));

				case TokenType.NodeNumber:
					return new NodeNumberCondition(int.Parse(_buffer));

				case TokenType.Type:
					return new TypeCondition(_buffer);

				case TokenType.ValueCondition:
					return ValueCondition.Parse(_buffer);

				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}
