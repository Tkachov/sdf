using System.Collections.Generic;
using System.IO;
using sdf.Matching;

namespace sdf.Validating {
	internal class SchemaType { }

	// built-ins
	internal class SchemaBuiltinType : SchemaType { }
	internal sealed class SchemaStringType : SchemaBuiltinType { }
	internal sealed class SchemaNumberType : SchemaBuiltinType { }
	internal sealed class SchemaBooleanType : SchemaBuiltinType { }
	internal sealed class SchemaNullType : SchemaBuiltinType { }
	internal sealed class SchemaSimpleNodeType : SchemaBuiltinType { }

	internal sealed class SchemaNodeType : SchemaType {
		internal readonly string Name;
		internal SchemaElement Children { get; }
		internal SchemaTypeCondition Conditions { get; }
		internal List<SchemaNodeTypeAttribute> Attributes { get; }

		internal SchemaNodeType(Node n, SchemaElement children, SchemaTypeCondition conditions, List<SchemaNodeTypeAttribute> attributes) {
			Name = "ud:" + n.GetStringAttribute("name");
			Children = children;
			Conditions = conditions;
			Attributes = attributes;
		}
	}

	internal sealed class SchemaLiteralType : SchemaType {
		internal readonly string Name;
		internal SchemaBuiltinType BaseType { get; }
		internal SchemaTypeCondition Conditions { get; }

		internal SchemaLiteralType(Node n, Dictionary<string, SchemaBuiltinType> builtinTypes, SchemaTypeCondition conditions) {
			Name = "ud:" + n.GetStringAttribute("name");
			var bt = n.GetStringAttribute("base-type");
			if (!builtinTypes.ContainsKey(bt)) {
				throw new InvalidDataException("Unknown built-in type '" + bt + "' used in literal-type description.");
			}

			BaseType = builtinTypes[bt];
			Conditions = conditions;
		}
	}

	// conditions used in types

	internal class SchemaTypeCondition { }

	internal sealed class SchemaTypeSingleCondition : SchemaTypeCondition {
		internal string RawCondition { get; }
		internal ValueCondition Condition { get; }

		internal SchemaTypeSingleCondition(StringLiteral s) {
			RawCondition = s.Value;

			var cp = new ConditionParser("[" + RawCondition + "]");
			var cnd = cp.Parse();
			if (cnd.Count != 1) {
				throw new InvalidDataException("Invalid condition '" + RawCondition + "'.");
			}

			var c0 = cnd[0] as ValueCondition;
			if (c0 == null) {
				throw new InvalidDataException("Invalid condition '" + RawCondition + "'.");
			}

			Condition = c0;
		}
	}

	internal sealed class SchemaTypeOneOfConditions : SchemaTypeCondition {
		internal List<SchemaTypeCondition> Conditions { get; }

		internal SchemaTypeOneOfConditions(List<SchemaTypeCondition> conditions) {
			Conditions = conditions;
		}
	}

	internal sealed class SchemaTypeAllOfConditions : SchemaTypeCondition {
		internal List<SchemaTypeCondition> Conditions { get; }

		internal SchemaTypeAllOfConditions(List<SchemaTypeCondition> conditions) {
			Conditions = conditions;
		}
	}

	// attributes used in types

	internal sealed class SchemaNodeTypeAttribute {
		internal string Name { get; }
		internal bool Required { get; }
		internal SchemaElement Element { get; }

		internal SchemaNodeTypeAttribute(Node n, SchemaElement schemaElement) {
			Name = n.GetStringAttribute("name");
			Required = (bool) n.GetBooleanAttribute("required");
			Element = schemaElement;
		}
	}
}