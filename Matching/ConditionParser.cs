using System;
using System.Collections.Generic;
using System.Linq;

namespace sdf.Matching {
	internal enum ConditionTokenType {
		NodeName,
		AttributeName,
		NodeIndex,
		NodeNumber,
		Type,
		ValueCondition
	}

	internal sealed class ConditionParser {
		private readonly List<Condition> _conditions;
		private readonly string _full;
		private string _buffer;
		private ConditionTokenType _currentlyParsing = ConditionTokenType.NodeName;
		private int _position;

		internal ConditionParser(string s) {
			_full = s;
			_position = 0;
			_buffer = "";
			_conditions = new List<Condition>();
		}

		internal List<Condition> Parse() {
			while (_position < _full.Length) {
				ParseChar();
			}

			HandleBuffer();
			return _conditions;
		}

		private void ParseChar() {
			char[] separators = {'[', '#', '^', '*', '+', '@'};
			var c = _full[_position];
			++_position;

			if (_currentlyParsing == ConditionTokenType.ValueCondition) {
				if (c == ']') {
					HandleBuffer();
					return; // don't put into buffer
				}
			} else {
				if (separators.Contains(c)) {
					var bufferWasEmpty = _buffer == "";

					// handle buffer
					HandleBuffer();

					// start parsing something new
					if (c == '@') {
						if (bufferWasEmpty) { // attribute name
							_currentlyParsing = ConditionTokenType.AttributeName;
						} else { // node@111
							_currentlyParsing = ConditionTokenType.NodeNumber;
						}
					} else if (c == '[') {
						_currentlyParsing = ConditionTokenType.ValueCondition;
					} else if (c == '#') {
						_currentlyParsing = ConditionTokenType.NodeIndex;
					} else if (c == '^') {
						_currentlyParsing = ConditionTokenType.Type;
					} else if (c == '*' || c == '+') {
						_conditions.Add(new ArbitraryNodeHierarchyCondition(c == '+'));
					}

					return; // don't put into buffer
				}
			}

			_buffer += c;
		}

		private void HandleBuffer() {
			var c = GetConditionFromBuffer();
			if (c != null) {
				_conditions.Add(c);
			}

			// reset state
			_currentlyParsing = ConditionTokenType.NodeName;
			_buffer = "";
		}

		private Condition GetConditionFromBuffer() {
			if (_buffer == "") {
				return null;
			}

			switch (_currentlyParsing) {
				case ConditionTokenType.NodeName:
					return new NodeNameCondition(_buffer);

				case ConditionTokenType.AttributeName:
					return new AttributeNameCondition(_buffer);

				case ConditionTokenType.NodeIndex:
					return new NodeIndexCondition(int.Parse(_buffer));

				case ConditionTokenType.NodeNumber:
					return new NodeNumberCondition(int.Parse(_buffer));

				case ConditionTokenType.Type:
					return new TypeCondition(_buffer);

				case ConditionTokenType.ValueCondition:
					return ValueCondition.Parse(_buffer);

				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}