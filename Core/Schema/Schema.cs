using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sdf.Core.Building;
using sdf.Core.Matching;

namespace sdf.Core.Schema {
	public static class SchemaNodeExtension {
		public static int WordCount(this string str, char c) {
			int counter = 0;
			for (int i = 0; i<str.Length; i++) {
				if (str[i] == c)
					counter++;
			}
			return counter;
		}

		public static string GetStringAttribute(this Node n, string name, bool required = true) {
			if (!n.Attributes.ContainsKey(name)) {
				if (required)
					throw new InvalidDataException("Attribute '"+name+"' expected, but not found.");
				return null;
			}

			var v = n.Attributes[name];
			var s = v as StringLiteral;
			if (s == null)
				throw new InvalidDataException("Attribute '"+name+"' expected to be a string.");

			return s.Value;
		}

		public static bool? GetBooleanAttribute(this Node n, string name, bool required = true) {
			if (!n.Attributes.ContainsKey(name)) {
				if (required)
					throw new InvalidDataException("Attribute '"+name+"' expected, but not found.");
				return null;
			}

			var v = n.Attributes[name];
			var b = v as BooleanLiteral;
			if (b == null)
				throw new InvalidDataException("Attribute '"+name+"' expected to be a boolean value.");

			return b.Value;
		}

		public static long? GetLongIntegerAttribute(this Node nd, string name, bool required = true) {
			if (!nd.Attributes.ContainsKey(name)) {
				if (required)
					throw new InvalidDataException("Attribute '"+name+"' expected, but not found.");
				return null;
			}

			var v = nd.Attributes[name];
			var n = v as NumberLiteral;
			if (n == null)
				throw new InvalidDataException("Attribute '"+name+"' expected to be a number.");

			if (n.Fraction != 0)
				throw new InvalidDataException("Attribute '"+name+"' expected to be an integer.");

			return n.Integer;
		}
	}

	class SchemaElement {
	}

	class SchemaNodeElement: SchemaElement {
		public string Name { get; private set; }
		public string TypeName { get; private set; }

		public SchemaNodeElement(Node n) {
			Name = n.GetStringAttribute("name");
			var t = n.GetStringAttribute("type", false);
            TypeName = t == null ?  "node" : "ud:" + t;
		}
	}

	class SchemaLiteralElement: SchemaElement {
		public string TypeName { get; private set; }
		
		public SchemaLiteralElement(Node n, Dictionary<string, SchemaBuiltinType> builtinTypes) {
			var t = n.GetStringAttribute("type");
			if (builtinTypes.ContainsKey(t))
				TypeName = t;
			else
				TypeName = "ud:" + t;
		}
	}

	class SchemaSequenceElement: SchemaElement {
		public List<SchemaElement> Sequence { get; private set; }

		public SchemaSequenceElement(List<SchemaElement> elements) {
			Sequence = elements;
		}
	}

	class SchemaOneOfElement: SchemaElement {
		public List<SchemaElement> Options { get; private set; }

		public SchemaOneOfElement(List<SchemaElement> elements) {
			Options = elements;
		}
	}

	class SchemaListElement: SchemaElement {
		public SchemaElement Element { get; private set; }
		public long Min { get; private set; }
		public long Max { get; private set; }
		public bool Limited { get; private set; }

		public SchemaListElement(Node n, SchemaElement e) {
			Element = e;
			Min = 0;
			Max = 0;
			Limited = false;

			var x = n.GetLongIntegerAttribute("min", false);
			if (x != null) Min = (long) x;

			x = n.GetLongIntegerAttribute("max", false);
			if (x != null) {
				Max = (long) x;
				Limited = true;
			}
		}
	}

	class SchemaType {
	}

	class SchemaBuiltinType: SchemaType {
	}

	class SchemaStringType: SchemaBuiltinType {
	}

	class SchemaNumberType: SchemaBuiltinType {
	}

	class SchemaBooleanType: SchemaBuiltinType {
	}

	class SchemaNullType: SchemaBuiltinType {
	}

	class SchemaSimpleNodeType: SchemaBuiltinType {
	}

	class SchemaNodeType: SchemaType {
		public readonly string Name;
		public SchemaElement Children { get; private set; }
		private SchemaTypeCondition conditions;
		public List<SchemaNodeTypeAttribute> Attributes { get; private set; }

		public SchemaNodeType(Node n, SchemaElement children, SchemaTypeCondition conditions, List<SchemaNodeTypeAttribute> attributes) {
			Name = "ud:" + n.GetStringAttribute("name");
			this.Children=children;
			this.conditions=conditions;
			this.Attributes=attributes;
		}
	}

	class SchemaLiteralType: SchemaType {
		public readonly string Name;
		public SchemaBuiltinType BaseType { get; private set; }
		private SchemaTypeCondition conditions;

		public SchemaLiteralType(Node n, Dictionary<string, SchemaBuiltinType> builtinTypes, SchemaTypeCondition conditions) {
			Name = "ud:" + n.GetStringAttribute("name");
			var bt = n.GetStringAttribute("base-type");
			if (!builtinTypes.ContainsKey(bt))
				throw new InvalidDataException("Unknown built-in type '"+bt+"' used in literal-type description.");
			BaseType = builtinTypes[bt];
			this.conditions=conditions;
		}
	}

	class SchemaTypeCondition {
	}

	class SchemaTypeSingleCondition: SchemaTypeCondition {
		private string condition;

		public SchemaTypeSingleCondition(StringLiteral s) {
			condition = s.Value;
		}
	}

	class SchemaTypeOneOfConditions: SchemaTypeCondition {
		private List<SchemaTypeCondition> conditions;

		public SchemaTypeOneOfConditions(List<SchemaTypeCondition> conditions) {
			this.conditions=conditions;
		}
	}

	class SchemaTypeAllOfConditions: SchemaTypeCondition {
		private List<SchemaTypeCondition> conditions;

		public SchemaTypeAllOfConditions(List<SchemaTypeCondition> conditions) {
			this.conditions=conditions;
		}
	}

	class SchemaNodeTypeAttribute {
		public string Name { get; private set; }
		public bool Required { get; private set; }
		public SchemaElement Element { get; private set; }

		public SchemaNodeTypeAttribute(Node n, SchemaElement schemaElement) {
			Name = n.GetStringAttribute("name");
			Required = (bool) n.GetBooleanAttribute("required");
			Element = schemaElement;
		}
	}

	class Schema {
		private SchemaElement topElement;
		private readonly Dictionary<string, SchemaBuiltinType> builtinTypes = new Dictionary<string, SchemaBuiltinType>();
		private Dictionary<string, SchemaType> types = new Dictionary<string, SchemaType>();

		public Schema(SDF schema) {
			var n = schema as Node;
			if (n == null || !n.Name.Equals("schema")) throw new InvalidDataException("Schema must be a (schema) node.");

			// built-ins
			builtinTypes["node"] = new SchemaSimpleNodeType();
			builtinTypes["string"] = new SchemaStringType();
			builtinTypes["boolean"] = builtinTypes["bool"] = new SchemaBooleanType();
			builtinTypes["number"] = new SchemaNumberType();
			builtinTypes["null"] = new SchemaNullType();

			topElement = MakeElement(n.Attributes["top-element"]);

			// read user-defined types
			foreach (var type in n.Children) {
				var t = MakeType(type);
				var nt = t as SchemaNodeType;
				if (nt != null)
					types[nt.Name] = nt;
				else {
					var lt = t as SchemaLiteralType;
					if (lt != null)
						types[lt.Name] = lt;
					else {
						throw new InvalidDataException("User-defined type must be either node-type or literal-type.");
					}
				}
			}

			// add built-ins
			foreach (var schemaBuiltinType in builtinTypes) {
				types[schemaBuiltinType.Key] = schemaBuiltinType.Value;
			}

			// verify all types have a description
			VerifyElement(topElement);
			foreach (var schemaType in types) {
				if (schemaType.Value is SchemaBuiltinType) continue;
				if (schemaType.Value is SchemaLiteralType) continue; // can only reference a built-in
				var t = schemaType.Value as SchemaNodeType;
				VerifyElement(t.Children);
				foreach (var attribute in t.Attributes)
					VerifyElement(attribute.Element);
			}

			ErrorMessage = null;
		}

		public string ErrorMessage { get; private set; }

		private void VerifyElement(SchemaElement schemaElement) {
			if (schemaElement == null)
				return;

			var l = schemaElement as SchemaListElement;
			if (l != null) {
				VerifyElement(l.Element);
				return;
			}

			var o = schemaElement as SchemaOneOfElement;
			if (o != null) {
				foreach (var c in o.Options) {
					VerifyElement(c);
				}
				return;
			}

			var s = schemaElement as SchemaSequenceElement;
			if (s != null) {
				foreach (var c in s.Sequence) {
					VerifyElement(c);
				}
				return;
			}

			var lt = schemaElement as SchemaLiteralElement;
			if (lt != null) {
				if (!types.ContainsKey(lt.TypeName))
					throw new InvalidDataException("Literal element references an underclared type '"+lt.TypeName+"'");

				var t = types[lt.TypeName];
				if ((!(t is SchemaBuiltinType) || t is SchemaSimpleNodeType) && !(t is SchemaLiteralType)) {
					throw new InvalidDataException("Literal element references non-literal type.");
				}
				return;
			}

			var e = schemaElement as SchemaNodeElement;
			if (e == null) {
				throw new InvalidDataException("Unknown element type found in VerifyElement.");
			}

			if (!types.ContainsKey(e.TypeName))
				throw new InvalidDataException("Node element references an underclared type '"+e.TypeName+"'");

			var et = types[e.TypeName];
			if (!(et is SchemaSimpleNodeType) && !(et is SchemaNodeType)) {
				throw new InvalidDataException("Node element references non-node type.");
			}
		}

		private SchemaElement MakeElement(SDF sdf) {
			var n = sdf as Node;
			if (n == null) throw new InvalidDataException("Schema element description must be a node.");

			switch (n.Name) {
				case "node-element":
					return new SchemaNodeElement(n);
				case "literal-element":
					return new SchemaLiteralElement(n, builtinTypes);

				case "sequence":
				case "one-of":
					var elements = new List<SchemaElement>();
					foreach (var child in n.Children) {
						elements.Add(MakeElement(child));
					}
					if (n.Name == "sequence")
						return new SchemaSequenceElement(elements);
					return new SchemaOneOfElement(elements);

				case "list":
					var children = n.Children;
					if (children.Count != 1)
						throw new InvalidDataException("Schema list description must have exactly one element description.");
					return new SchemaListElement(n, MakeElement(children[0]));
				default:
					throw new InvalidDataException("Invalid schema element description.");
			}
		}

		private SchemaType MakeType(SDF sdf) {
			var n = sdf as Node;
			if (n == null) throw new InvalidDataException("Schema type description must be a node.");

			var conditions = MakeConditionsForNode(n);
			switch (n.Name) {
				case "node-type":
					SchemaElement children = null;
					if (n.Attributes.ContainsKey("children"))
						children = MakeElement(n.Attributes["children"]);
					var attributes = MakeAttributes(n.Children);
					return new SchemaNodeType(n, children, conditions, attributes);

				case "literal-type":
					return new SchemaLiteralType(n, builtinTypes, conditions);

				default:
					throw new InvalidDataException("Invalid schema type description.");
			}
		}

		private SchemaTypeCondition MakeConditionsForNode(Node node) {
			if (!node.Attributes.ContainsKey("conditions"))
				return null;

			var c = node.Attributes["conditions"] as Node;
			if (c == null)
				throw new InvalidDataException("Schema condition description must be a node.");
			return MakeCondition(c);
		}

		private SchemaTypeCondition MakeCondition(Node n) {
			var c = n.Children;

			switch (n.Name) {
				case "condition":					
					if (c.Count != 1)
						throw new InvalidDataException("Schema condition description must have exactly one value.");
					var v = n.Children[0];
					var s = v as StringLiteral;
					if (s == null)
						throw new InvalidDataException("Schema condition description must be a string.");
					return new SchemaTypeSingleCondition(s);

				case "one-of-conditions":
				case "all-of-conditions":
					if (c.Count < 1)
						throw new InvalidDataException("Schema "+n.Name+" description must have at least one value.");

					var conditions = new List<SchemaTypeCondition>();
					foreach (var sdf in c) {
						var nd = sdf as Node;
						if (nd == null)
							throw new InvalidDataException("All of schema "+n.Name+" description values must be nodes.");
						conditions.Add(MakeCondition(nd));
					}

					if (n.Name == "one-of-conditions")
						return new SchemaTypeOneOfConditions(conditions);
					return new SchemaTypeAllOfConditions(conditions);

				default:
					throw new InvalidDataException("Invalid schema condition description.");
			}
		}

		private List<SchemaNodeTypeAttribute> MakeAttributes(List<SDF> sdfList) {
			var res = new List<SchemaNodeTypeAttribute>();
			foreach (var sdf in sdfList) {
				res.Add(MakeAttribute(sdf));
			}
			return res;
		}

		private SchemaNodeTypeAttribute MakeAttribute(SDF sdf) {
			var n = sdf as Node;
			if (n == null || n.Name != "attribute") throw new InvalidDataException("Schema attribute description must be an (attribute) node.");

			var children = n.Children;
			if (children.Count != 1)
				throw new InvalidDataException("Schema attribute description must have exactly one element description.");
			return new SchemaNodeTypeAttribute(n, MakeElement(children[0]));
		}

		public bool Validate(SDF input) { // sets ErrorMessage if returns false
			var e = topElement;
			return ValidateMatches(e, input);
		}

		private bool ValidateMatches(SchemaElement schemaElement, SDF input) {
			var n = schemaElement as SchemaNodeElement;
			if (n != null) return ValidateMatchesNodeElement(n, input);

			var l = schemaElement as SchemaLiteralElement;
			if (l != null) return ValidateMatchesLiteralElement(l, input);

			var ls = schemaElement as SchemaListElement;
			if (ls != null) return ValidateMatchesListElement(ls, input);

			var s = schemaElement as SchemaSequenceElement;
			if (s != null) return ValidateMatchesSequenceElement(s, input);

			var o = schemaElement as SchemaOneOfElement;
			if (o != null) return ValidateMatchesOneOfElement(o, input);

			ErrorMessage = "Unknown element type in ValidateMatches.";
			return false;
		}

		private bool ValidateMatches(SchemaElement schemaElement, List<SDF> input) {
			var n = schemaElement as SchemaNodeElement;
			if (n != null) {
				if (input.Count != 1) {
					ErrorMessage = "One node expected, multiple (or none) found.";
					return false;
				}
				return ValidateMatchesNodeElement(n, input[0]);
			}

			var l = schemaElement as SchemaLiteralElement;
			if (l != null) {
				if (input.Count != 1) {
					ErrorMessage = "One literal expected, multiple (or none) found.";
					return false;
				}
				return ValidateMatchesLiteralElement(l, input[0]);
			}

			var ls = schemaElement as SchemaListElement;
			if (ls != null)
				return ValidateMatchesListElement(ls, input);

			var s = schemaElement as SchemaSequenceElement;
			if (s != null)
				return ValidateMatchesSequenceElement(s, input);

			var o = schemaElement as SchemaOneOfElement;
			if (o != null)
				return ValidateMatchesOneOfElement(o, input);

			ErrorMessage = "Unknown element type in ValidateMatches.";
			return false;
		}

		private bool ValidateMatchesNodeElement(SchemaNodeElement schemaNodeElement, SDF input) {
			var n = input as Node;
			if (n == null || n.Name != schemaNodeElement.Name) {
				ErrorMessage = "Element '"+Match.MakeRootMatch(input).Path+"' must be a ("+schemaNodeElement.Name+") node.";
				return false;
			}

			var t = types[schemaNodeElement.TypeName];
			if (t is SchemaSimpleNodeType)
				return true;

			var nt = t as SchemaNodeType;
			if (nt == null) {
				ErrorMessage = "Bad type in schema.";
				return false;
			}

			if (nt.Children != null) {
				if (!ValidateMatches(nt.Children, n.Children))
					return false;
			}

			// TODO: conditions (/*cnd*/)

			// attributes
			foreach (var attribute in nt.Attributes) {
				if (!n.Attributes.ContainsKey(attribute.Name)) {
					if (!attribute.Required) continue;
					ErrorMessage = "Required attribute '" + attribute.Name + "' is missing on element '" + Match.MakeRootMatch(input).Path + "'.";
					return false;
				}

				var a = n.Attributes[attribute.Name];
				if (!ValidateMatches(attribute.Element, a))
					return false;
			}
			return true;
		}

		private bool ValidateMatchesLiteralElement(SchemaLiteralElement schemaLiteralElement, SDF input) {
			if (input is Node) {
				ErrorMessage = "Element '"+Match.MakeRootMatch(input).Path+"' must be literal.";
				return false;
			}

			var t = types[schemaLiteralElement.TypeName];
			if (t is SchemaBuiltinType) {
				if (!ValidateMatchesType(t as SchemaBuiltinType, input)) return false;
			} else if (t is SchemaLiteralType) {
				var lt = t as SchemaLiteralType;
				if (!ValidateMatchesType(lt.BaseType, input))
					return false;

				// TODO: conditions (/*cnd*/)
			} else {
				ErrorMessage = "Bad type in schema.";
				return false;
			}

			return true;
		}

		private bool ValidateMatchesType(SchemaBuiltinType schemaType, SDF input) {
			if (schemaType is SchemaStringType) {
				if (!(input is StringLiteral)) {
					ErrorMessage = "String expected.";
					return false;
				}
			} else if (schemaType is SchemaNumberType) {
				if (!(input is NumberLiteral)) {
					ErrorMessage = "Number expected.";
					return false;
				}
			} else if (schemaType is SchemaBooleanType) {
				if (!(input is BooleanLiteral)) {
					ErrorMessage = "Boolean value expected.";
					return false;
				}
			} else if (schemaType is SchemaNullType) {
				if (!(input is NullLiteral)) {
					ErrorMessage = "Null expected.";
					return false;
				}
			}

			return true;
		}

		private bool ValidateMatchesListElement(SchemaListElement ls, SDF input) { // no lists or sequences
			var l = new List<SDF> {input};
			return ValidateMatchesListElement(ls, l);
		}

		private bool ValidateMatchesListElement(SchemaListElement ls, List<SDF> input) {
			var l = input.Count;
			if (ls.Min > l) {
				ErrorMessage = "";
				return false;
			}

			if (ls.Limited && ls.Max < 1) {
				ErrorMessage = "";
				return false;
			}

			foreach (var sdf in input) {
				if (!ValidateMatches(ls.Element, sdf))
					return false;
			}

			return true;
		}

		private bool ValidateMatchesSequenceElement(SchemaSequenceElement schemaSequenceElement, SDF input) { // no sequences
			if (schemaSequenceElement.Sequence.Count > 1) {
				ErrorMessage = "A sequence of " + schemaSequenceElement.Sequence.Count + " elements expected, one element found.";
				return false;
			}

			return ValidateMatches(schemaSequenceElement.Sequence[0], input);
		}

		private bool ValidateMatchesSequenceElement(SchemaSequenceElement schemaSequenceElement, List<SDF> input) {
			int l = schemaSequenceElement.Sequence.Count;
			if (input.Count != l) {
				ErrorMessage = "A sequence of " + l + " elements expected, " + input.Count + " element(s) found.";
				return false;
			}

			for (int i = 0; i < l; ++i) {
				if (!ValidateMatches(schemaSequenceElement.Sequence[i], input[i]))
					return false;
			}

			return true;
		}

		private bool ValidateMatchesOneOfElement(SchemaOneOfElement schemaOneOfElement, SDF input) { // only nodes and literals
			foreach (var option in schemaOneOfElement.Options) {
				if (ValidateMatches(option, input))
					return true;
			}

			ErrorMessage = "Element does not match neither of allowed options.";
			return false;
		}

		private bool ValidateMatchesOneOfElement(SchemaOneOfElement schemaOneOfElement, List<SDF> input) {
			foreach (var option in schemaOneOfElement.Options) {
				if (ValidateMatches(option, input))
					return true;
			}

			ErrorMessage = "Element does not match neither of allowed options.";
			return false;
		}
	}
}
