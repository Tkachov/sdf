using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sdf.Core.Building;
using sdf.Core.Parsing;
using Expression = System.Linq.Expressions.Expression;

namespace sdf.Core.Matching {
	/*
	/html/body 				Абсолютный путь от корня
	body/p					Относительный путь
	/html 					Узел html.
	/html/ 					Все потомки узла html (фактически, после / идет пустой список ограничений на искомые узлы).
	node/@attr/attr-node 	Обращение к иерархии внутри атрибута attr узла node.

	body/* /b
	body/+/b				Путь с переменной вложенностью(аналогично PCRE, * это 0 и более, + это 1 и более).
							Вложенность может быть в том числе через узлы-атрибуты(body/@attr/b).

	node/#0 				Обращение к узлу по порядковому номеру.
	node/inner@0 			Обращение к указанному по счету узлу с указанным именем(возможно, стоит расширить на произвольные условия).
							Пояснение: h1#1 дает элемент h1, который является вторым в списке потомков, h1@1 дает второй элемент h1 (даже если в списке потомков он находится, например, на десятом месте).

	node/^n					Обращение к узлам с указанным типом(n — number, s — string, b — boolean, ? — node, ? — null).
	node/[>2]
							Условие на значение узла.В зависимости от типа узла применимы разные условия:

Для чисел: >, <, >=, <=, =, !=;
	Для строк: =, !=, ~=, ^=, $= (имеет подстроку, начинается с подстроки, заканчивается подстрокой);
    Для строк, вероятно, стоит также добавить версии-отрицания !~=, !^=, !$=;
    Для булевых значений и null: =, !=;
    Для узлов: условия на значения атрибутов(см.ниже), предикаты вида has_child(childname), has_attr(attrname).

node[@attr>2] Условие на значение атрибута узла.
node/@attr[>2] Условие на значение узла, являющегося значением атрибута.Выглядит похоже на предыдущее, однако происходит выбор не узла node, а узла-значения attr, в данном случае — числовых значений, больших 2.

	*/

	internal class Condition {
		public virtual bool Matches(SDF sdf, SDF parent, string attrbuteName) {
			throw new NotImplementedException();
		}
	}

	internal class NodeNameCondition: Condition {
		public readonly string NodeName;
		public NodeNameCondition(string name) {
			NodeName = name; 
		}

		public override bool Matches(SDF sdf, SDF parent, string attrbuteName) {
			var n = sdf as Node;
			return (n != null && n.Name == NodeName);
		}
	}

	internal class AttributeNameCondition: Condition {
		public readonly string AttributeName;
		public AttributeNameCondition(string name) {
			AttributeName = name;
		}
		public override bool Matches(SDF sdf, SDF parent, string attrbuteName) {
			return attrbuteName == AttributeName;
		}
	}

	internal class ArbitraryNodeHierarchyCondition: Condition {
		public readonly bool AtLeastOne;
		public ArbitraryNodeHierarchyCondition(bool one) {
			AtLeastOne = one;
		}
		public override bool Matches(SDF sdf, SDF parent, string attrbuteName) {
			return true; // handled in Matcher
		}
	}

	internal class NodeIndexCondition: Condition {
		public readonly int Index;
		public NodeIndexCondition(int i) {
			Index = i;
		}
		public override bool Matches(SDF sdf, SDF parent, string attrbuteName) {
			if (attrbuteName != null)
				return false;
						
			var parentNode = parent as Node;
			if (parentNode == null)
				return (Index == 0);

			if (parentNode.Children.Count <= Index)
				return false;

			return (parentNode.Children[Index] == sdf);
		}
	}

	internal class NodeNumberCondition: Condition {
		public readonly int Number;
		public NodeNumberCondition(int i) {
			Number = i;
		}
		public override bool Matches(SDF sdf, SDF parent, string attrbuteName) { // TODO: may be implement it so it counts all elements which match other conditions, not name specifically
			if (attrbuteName != null)
				return false;

			var parentNode = parent as Node;
			if (parentNode == null)
				return (Number == 0);

			var name = (sdf as Node)?.Name;
			if (name == null)
				return false;

			var number = 0;
			foreach (var child in parentNode.Children) {
				if ((child as Node)?.Name == name) {
					if (child == sdf && number == Number)
						return true;
					++number;
				}
			}

			return false;
		}
	}

	internal enum SDFType {
		Node,
		Null,
		Number,
		String,
		Boolean
	}

	internal class TypeCondition: Condition {
		public readonly SDFType Type;

		public TypeCondition(string type) {
			switch (type) {
				case "node":
					Type = SDFType.Node;
					break;

				case "null":
					Type = SDFType.Null;
					break;

				case "number":
					Type = SDFType.Number;
					break;

				case "string":
					Type = SDFType.String;
					break;

				case "bool":
				case "boolean":
					Type = SDFType.Boolean;
					break;

				default:
					throw new InvalidDataException();
			}
		}

		public override bool Matches(SDF sdf, SDF parent, string attrbuteName) {
			switch (Type) {
				case SDFType.Node:
					return (sdf is Node);

				case SDFType.Null:
					return (sdf is NullLiteral);

				case SDFType.Number:
					return (sdf is NumberLiteral);

				case SDFType.String:
					return (sdf is StringLiteral);

				case SDFType.Boolean:
					return (sdf is BooleanLiteral);

				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}

	// TODO: node/attribute value predicate conditions

	internal class ValueCondition: Condition {
		internal ValueCondition() {}

		public static ValueCondition Parse(string condition) {
			// condition: attribute-condition | value-condition
			// attribute-condition: attribute-name attribute-value-condition | attribute-node-condition
			// attribute-name: @ name
			// attribute-value-condition: number-condition | string-condition | bool-or-null-condition
			// value-condition: number-condition | string-condition | bool-or-null-condition | node-condition			
			// number-condition: [> < >= <= = !=] number-value
			// string-condition: [= != ~= ^= $= !~= !^= !$=] string-value 
			// bool-or-null-condition: [= !=] bool-or-null-value
			// node-condition: [has_child has_attr] ( name )
			// attribute-node-condition: [attr_has_child attr_has_attr] ( attribute-name name )

			// condition: binary-operator | function-operator
			// binary-operator: [attribute-name] one-of(= != > < >= <= ~= ^= $= !~= !^= !$=) value
			// function-operator: unary-function | binary-function
			// unary-function: unary-function-name ( name )
			// binary-function: binary-function-name ( attribute-name , name )
			// attribute-name: @ name
			// unary-function-name: one-of(has_child has_attr)
			// binary-function-name: one-of(attr_has_child attr_has_attr)

			Console.WriteLine(condition);

			string[] nodeFunctions = {"has_child", "has_attr"};
			string[] attrFunctions = {"attr_has_child", "attr_has_attr"};

			foreach (var f in nodeFunctions)
				if (condition.StartsWith(f))
					return ParseNodeFunction(condition, f);

			foreach (var f in attrFunctions)
				if (condition.StartsWith(f))
					return ParseAttributeFunction(condition, f);

			string[] operators = {"!~=", "!^=", "!$=", "!=", ">=", "<=", "~=", "^=", "$=", "=",  ">", "<"}; // in order of most unique to least unique ('=' is a substring of '!~=')
			foreach (var o in operators) {
				var index = condition.IndexOf(o);
				if (index != -1) {
					return ParseOperator(condition, o, index);
				}
			}

			throw new InvalidDataException();
		}		

		private static NodeValueCondition ParseNodeFunction(string condition, string functionName) {
			Console.WriteLine("parsing node function "+functionName);
			throw new NotImplementedException();
		}

		private static AttributeValueCondition ParseAttributeFunction(string condition, string functionName) {
			Console.WriteLine("parsing attribute function "+functionName);
			throw new NotImplementedException();
		}

		private static ValueCondition ParseOperator(string condition, string operatorName, int index) {
			Console.WriteLine("parsing operator "+operatorName);
			string attributeName = null;
			if (index > 0) {
				attributeName = condition.Substring(0, index);
				if (!attributeName.StartsWith("@"))
					throw new InvalidDataException();

				attributeName = attributeName.Substring(1); // remove @
			}

			Console.WriteLine("attribute name: "+attributeName);

			var value = condition.Substring(index + operatorName.Length);
			var expr = Parser.ParseString(value);
			if (!(expr is LiteralExpression))
				throw new InvalidDataException();
			var sdf = Builder.Build(expr);
			OperatorCondition operatorCondition;

			switch (operatorName) {
				case "=": case "!=":
					// common operators
					operatorCondition = new CommonOperatorCondition(operatorName, sdf);
				break;

				case ">": case "<": case ">=": case "<=":
					// number operators
					if (!(sdf is NumberLiteral))
						throw new InvalidDataException();

					operatorCondition = new NumberOperatorCondition(operatorName, (NumberLiteral) sdf);
				break;

				case "~=": case "^=": case "$=": case "!~=": case "!^=": case "!$=":
					// string operators
					if (!(sdf is StringLiteral))
						throw new InvalidDataException();

					operatorCondition = new StringOperatorCondition(operatorName, (StringLiteral)sdf);
				break;

				default:
					throw new InvalidDataException();
			}

			if (attributeName != null)
				return new AttributeValueCondition(attributeName, operatorCondition);

			return new NodeValueCondition(operatorCondition);
		}
	}

	internal class OperatorCondition {
		public virtual bool MeetsCondition(SDF value) {
			throw new NotImplementedException();
		}
	}

	internal class CommonOperatorCondition: OperatorCondition {
		private readonly string _operatorName;
		private readonly SDF _value;

		public CommonOperatorCondition(string operatorName, SDF value) {
			_operatorName = operatorName;
			_value = value;
		}

		private bool MeetsOperator(SDF value, string operatorName) {
			switch (operatorName) {
				case "=":
					var aString = _value as StringLiteral;
					var bString = value as StringLiteral;
					if (aString != null && bString != null)
						return aString.Value == bString.Value;

					var aNumber = _value as NumberLiteral;
					var bNumber = value as NumberLiteral;
					if (aNumber != null && bNumber != null)
						return aNumber.Integer == bNumber.Integer && aNumber.Fraction == bNumber.Fraction;

					var aBoolean = _value as BooleanLiteral;
					var bBoolean = value as BooleanLiteral;
					if (aBoolean != null && bBoolean != null)
						return aBoolean.Value == bBoolean.Value;

					if (_value is NullLiteral && value is NullLiteral)
						return true;

					return false;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private bool SameType(SDF v) {
			if (_value is StringLiteral && v is StringLiteral)
				return true;

			if (_value is NumberLiteral && v is NumberLiteral)
				return true;

			if (_value is BooleanLiteral && v is BooleanLiteral)
				return true;

			if (_value is NullLiteral && v is NullLiteral)
				return true;

			return false;
		}

		public override bool MeetsCondition(SDF value) {
			switch (_operatorName) {
				case "=":
					return MeetsOperator(value, "=");

				case "!=":
					return !MeetsOperator(value, "=") && SameType(value); // so !=3 wouldn't produce nodes, strings, etc

				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}

	internal class NumberOperatorCondition: OperatorCondition {
		private readonly string _operatorName;
		private readonly NumberLiteral _value;

		public NumberOperatorCondition(string operatorName, NumberLiteral value) {
			_operatorName = operatorName;
			_value = value;
		}

		public override bool MeetsCondition(SDF value) {
			var aNumber = value as NumberLiteral;
			if (aNumber == null)
				return false;

			var a = aNumber.Double;
			var b = _value.Double;

			switch (_operatorName) {
				case "<": return a < b;
				case ">": return a > b;
				case "<=": return a <= b || (_value.Integer == aNumber.Integer && _value.Fraction == aNumber.Fraction); // just in case
				case ">=": return a >= b || (_value.Integer == aNumber.Integer && _value.Fraction == aNumber.Fraction);

				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}

	internal class StringOperatorCondition: OperatorCondition {
		private readonly string _operatorName;
		private readonly StringLiteral _value;

		public StringOperatorCondition(string operatorName, StringLiteral value) {
			_operatorName = operatorName;
			_value = value;
		}

		private bool MeetsOperator(string a, string operatorName) {
			var b = _value.Value;
			switch (operatorName) {
				case "~=": return a.Contains(b);
				case "^=": return a.StartsWith(b);
				case "$=": return a.EndsWith(b);

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public override bool MeetsCondition(SDF value) {
			var a = value as StringLiteral;
			if (a == null)
				return false;

			switch (_operatorName) {
				case "~=": case "^=": case "$=":
					return MeetsOperator(a.Value, _operatorName);

				case "!~=": case "!^=": case "!$=":
					return !MeetsOperator(a.Value, _operatorName.Substring(1));

				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}

	internal class NodeValueCondition: ValueCondition {
		private readonly OperatorCondition _operatorCondition;		

		public NodeValueCondition(OperatorCondition operatorCondition) {
			_operatorCondition = operatorCondition;
		}

		public override bool Matches(SDF sdf, SDF parent, string attrbuteName) {
			return attrbuteName == null && _operatorCondition.MeetsCondition(sdf);
		}
	}

	internal class AttributeValueCondition: ValueCondition {
		private readonly string _attributeName;
		private readonly OperatorCondition _operatorCondition;

		public AttributeValueCondition(string attributeName, OperatorCondition operatorCondition) {
			_attributeName = attributeName;
			_operatorCondition = operatorCondition;
		}

		public override bool Matches(SDF sdf, SDF parent, string attrbuteName) {
			return attrbuteName == _attributeName && _operatorCondition.MeetsCondition(sdf);
		}
	}
}
