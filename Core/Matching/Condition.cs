using System;
using System.Collections.Generic;
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
	}

	internal class NodeNumberCondition: Condition {
		public readonly int Number;
		public NodeNumberCondition(int i) {
			Number = i;
		}
	}

	// TODO: node type condition (^n)
	// TODO: node value condition ([>2])
	// TODO: attribute value condition ([@attr>2])
}
