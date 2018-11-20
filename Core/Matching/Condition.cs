using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sdf.Core.Building;

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
	
	// TODO: node value condition ([>2])
	// TODO: attribute value condition ([@attr>2])
}
