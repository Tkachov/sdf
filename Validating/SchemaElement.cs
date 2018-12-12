using System.Collections.Generic;

namespace sdf.Validating {
	internal class SchemaElement { }

	internal sealed class SchemaNodeElement : SchemaElement {
		internal string Name { get; }
		internal string TypeName { get; }

		internal SchemaNodeElement(Node n) {
			Name = n.GetStringAttribute("name");
			var t = n.GetStringAttribute("type", false);
			TypeName = t == null ? "node" : "ud:" + t;
		}
	}

	internal sealed class SchemaLiteralElement : SchemaElement {
		internal string TypeName { get; }

		internal SchemaLiteralElement(Node n, Dictionary<string, SchemaBuiltinType> builtinTypes) {
			var t = n.GetStringAttribute("type");
			if (builtinTypes.ContainsKey(t)) {
				TypeName = t;
			} else {
				TypeName = "ud:" + t;
			}
		}
	}

	internal sealed class SchemaSequenceElement : SchemaElement {
		internal List<SchemaElement> Sequence { get; }

		internal SchemaSequenceElement(List<SchemaElement> elements) {
			Sequence = elements;
		}
	}

	internal sealed class SchemaOneOfElement : SchemaElement {
		internal List<SchemaElement> Options { get; }

		internal SchemaOneOfElement(List<SchemaElement> elements) {
			Options = elements;
		}
	}

	internal sealed class SchemaListElement : SchemaElement {
		internal SchemaElement Element { get; }
		internal long Min { get; }
		internal long Max { get; }
		internal bool Limited { get; }

		internal SchemaListElement(Node n, SchemaElement e) {
			Element = e;
			Min = 0;
			Max = 0;
			Limited = false;

			var x = n.GetLongIntegerAttribute("min", false);
			if (x != null) {
				Min = (long) x;
			}

			x = n.GetLongIntegerAttribute("max", false);
			if (x != null) {
				Max = (long) x;
				Limited = true;
			}
		}
	}
}