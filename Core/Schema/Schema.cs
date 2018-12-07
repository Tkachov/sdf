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
		public SchemaTypeCondition Conditions { get; private set; }
		public List<SchemaNodeTypeAttribute> Attributes { get; private set; }

		public SchemaNodeType(Node n, SchemaElement children, SchemaTypeCondition conditions, List<SchemaNodeTypeAttribute> attributes) {
			Name = "ud:" + n.GetStringAttribute("name");
			this.Children=children;
			this.Conditions=conditions;
			this.Attributes=attributes;
		}
	}

	class SchemaLiteralType: SchemaType {
		public readonly string Name;
		public SchemaBuiltinType BaseType { get; private set; }
		public SchemaTypeCondition Conditions { get; private set; }

		public SchemaLiteralType(Node n, Dictionary<string, SchemaBuiltinType> builtinTypes, SchemaTypeCondition conditions) {
			Name = "ud:" + n.GetStringAttribute("name");
			var bt = n.GetStringAttribute("base-type");
			if (!builtinTypes.ContainsKey(bt))
				throw new InvalidDataException("Unknown built-in type '"+bt+"' used in literal-type description.");
			BaseType = builtinTypes[bt];
			this.Conditions=conditions;
		}
	}

	class SchemaTypeCondition {
	}

	class SchemaTypeSingleCondition: SchemaTypeCondition {
		public string RawCondition { get; private set; }
		public ValueCondition Condition { get; private set; }

		public SchemaTypeSingleCondition(StringLiteral s) {
			RawCondition = s.Value;

			var cp = new ConditionParser("[" + RawCondition + "]");
			var cnd = cp.Parse();
			if (cnd.Count != 1) {
				throw new InvalidDataException("Invalid condition '"+RawCondition+"'.");
			}

			var c0 = cnd[0] as ValueCondition;
			if (c0 == null) {
				throw new InvalidDataException("Invalid condition '"+RawCondition+"'.");
			}

			Condition = c0;
		}
	}

	class SchemaTypeOneOfConditions: SchemaTypeCondition {
		public List<SchemaTypeCondition> Conditions { get; private set; }

		public SchemaTypeOneOfConditions(List<SchemaTypeCondition> conditions) {
			Conditions=conditions;
		}
	}

	class SchemaTypeAllOfConditions: SchemaTypeCondition {
		public List<SchemaTypeCondition> Conditions { get; private set; }

		public SchemaTypeAllOfConditions(List<SchemaTypeCondition> conditions) {
			Conditions=conditions;
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

	public class Schema {
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
			return ValidateMatches(e, Match.MakeRootMatch(input));
		}

		private bool ValidateMatches(SchemaElement schemaElement, Match input, string attributeName=null) {
			var n = schemaElement as SchemaNodeElement;
			if (n != null) return ValidateMatchesNodeElement(n, input, attributeName);

			var l = schemaElement as SchemaLiteralElement;
			if (l != null) return ValidateMatchesLiteralElement(l, input, attributeName);

			var ls = schemaElement as SchemaListElement;
			if (ls != null) return ValidateMatchesListElement(ls, input, attributeName);

			var s = schemaElement as SchemaSequenceElement;
			if (s != null) return ValidateMatchesSequenceElement(s, input, attributeName);

			var o = schemaElement as SchemaOneOfElement;
			if (o != null) return ValidateMatchesOneOfElement(o, input, attributeName);

			ErrorMessage = "Unknown element type in ValidateMatches.";
			return false;
		}

		private bool ValidateMatches(SchemaElement schemaElement, List<Match> input) {
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

		private bool ValidateMatchesNodeElement(SchemaNodeElement schemaNodeElement, Match input, string attributeName=null) {
			var n = input.Value as Node;
			if (n == null || n.Name != schemaNodeElement.Name) {
				ErrorMessage = "Element '"+input.Path+"' must be a ("+schemaNodeElement.Name+") node.";
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

			var mn = input as MatchNode;
			if (mn == null) {
				ErrorMessage = "Element '"+input.Path+"' is a node and it's Match is not?";
				return false;
			}

			if (nt.Children != null) {
				if (!ValidateMatches(nt.Children, mn.Children))
					return false;
			}

			if (!ValidateConditions(nt.Conditions, input, attributeName))
				return false;

			// attributes
			foreach (var attribute in nt.Attributes) {
				if (!n.Attributes.ContainsKey(attribute.Name)) {
					if (!attribute.Required) continue;
					ErrorMessage = "Required attribute '" + attribute.Name + "' is missing on element '" + input.Path + "'.";
					return false;
				}

				var a = mn.Attributes[attribute.Name];
				if (!ValidateMatches(attribute.Element, a, attribute.Name))
					return false;
			}
			return true;
		}

		private bool ValidateMatchesLiteralElement(SchemaLiteralElement schemaLiteralElement, Match input, string attributeName = null) {
			if (input.Value is Node) {
				ErrorMessage = "Element '"+input.Path+"' must be literal.";
				return false;
			}

			var t = types[schemaLiteralElement.TypeName];
			if (t is SchemaBuiltinType) {
				if (!ValidateMatchesType(t as SchemaBuiltinType, input, attributeName)) return false;
			} else if (t is SchemaLiteralType) {
				var lt = t as SchemaLiteralType;
				if (!ValidateMatchesType(lt.BaseType, input, attributeName))
					return false;

				if (!ValidateConditions(lt.Conditions, input, attributeName))
					return false;
			} else {
				ErrorMessage = "Bad type in schema.";
				return false;
			}

			return true;
		}

		private bool ValidateConditions(SchemaTypeCondition conditions, Match input, string attributeName = null) {
			if (conditions == null)
				return true;

			var allOf = conditions as SchemaTypeAllOfConditions;
			if (allOf != null) {
				foreach (var condition in allOf.Conditions) {
					if (!ValidateConditions(condition, input, attributeName)) {
						ErrorMessage = "One of the conditions is not met:\n\t" + ErrorMessage.Replace("\n", "\n\t");
						return false;
					}
				}

				return true;
			}

			var oneOf = conditions as SchemaTypeOneOfConditions;
			if (oneOf != null) {
				var found = false;
				var fullMessage = "";
				foreach (var condition in oneOf.Conditions) {
					if (ValidateConditions(condition, input, attributeName)) {
						found = true;
						break;
					}
					fullMessage += "\t" + ErrorMessage.Replace("\n", "\n\t") + "\n";
				}

				if (!found) {
					ErrorMessage = "None of the following multiple conditions is not met:\n" + fullMessage;
					return false;
				}

				return true;
			}

			// ok, it's simple condition
			var c = conditions as SchemaTypeSingleCondition;
			if (c == null) {
				ErrorMessage = "Unknown type of condition in ValidateConditions";
				return false;
			}

			if (!c.Condition.Matches(input.Value, input.Parent?.Value, attributeName)) {
				ErrorMessage = "Element '"+input.Path+"' does not match '"+c.RawCondition+"' condition.";
				return false;
			}
			

			return true;
		}

		private bool ValidateMatchesType(SchemaBuiltinType schemaType, Match match, string attributeName = null) {
			var input = match.Value;
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

		private bool ValidateMatchesListElement(SchemaListElement ls, Match input, string attributeName = null) { // no lists or sequences
			var l = new List<Match> {input};
			return ValidateMatchesListElement(ls, l);
		}

		private bool ValidateMatchesListElement(SchemaListElement ls, List<Match> input) {
			var l = input.Count;
			if (ls.Min > l) {
				ErrorMessage = "Less than minimal ("+ls.Min+") amount of elements in a list.";
				return false;
			}

			if (ls.Limited && ls.Max < l) {
				ErrorMessage = "More than maximum ("+ls.Max+") amount of elements in a list.";
				return false;
			}

			foreach (var sdf in input) {
				if (!ValidateMatches(ls.Element, sdf))
					return false;
			}

			return true;
		}

		private bool ValidateMatchesSequenceElement(SchemaSequenceElement schemaSequenceElement, Match input, string attributeName = null) { // no sequences
			if (schemaSequenceElement.Sequence.Count > 1) {
				ErrorMessage = "A sequence of " + schemaSequenceElement.Sequence.Count + " elements expected, one element found.";
				return false;
			}

			return ValidateMatches(schemaSequenceElement.Sequence[0], input, attributeName);
		}

		private bool ValidateMatchesSequenceElement(SchemaSequenceElement schemaSequenceElement, List<Match> input) {
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

		private bool ValidateMatchesOneOfElement(SchemaOneOfElement schemaOneOfElement, Match input, string attributeName = null) { // only nodes and literals
			var fullMessage = "";
			foreach (var option in schemaOneOfElement.Options) {
				if (ValidateMatches(option, input, attributeName))
					return true;

				fullMessage += "\t" + ErrorMessage.Replace("\n", "\n\t") + "\n";
			}

			ErrorMessage = "Element '"+input.Path+"' does not match neither of allowed options:\n" + fullMessage;
			return false;
		}

		private bool ValidateMatchesOneOfElement(SchemaOneOfElement schemaOneOfElement, List<Match> input) {
			foreach (var option in schemaOneOfElement.Options) {
				if (ValidateMatches(option, input))
					return true;
			}

			ErrorMessage = "Element does not match neither of allowed options.";
			return false;
		}

		/////////////

		public bool ValidatePartial(SDF input) { // sets ErrorMessage if returns false
			var e = topElement;
			return ValidateMatchesPartial(e, Match.MakeRootMatch(input));
		}

		private bool ValidateMatchesPartial(SchemaElement schemaElement, Match input, string attributeName = null) {
			var n = schemaElement as SchemaNodeElement;
			if (n != null)
				return ValidateMatchesNodeElementPartial(n, input, attributeName);

			var l = schemaElement as SchemaLiteralElement;
			if (l != null)
				return ValidateMatchesLiteralElement(l, input, attributeName); // cannot match literal partially, you either do or you don't

			var ls = schemaElement as SchemaListElement;
			if (ls != null)
				return ValidateMatchesListElementPartial(ls, input, attributeName);

			var s = schemaElement as SchemaSequenceElement;
			if (s != null)
				return ValidateMatchesSequenceElementPartial(s, input, attributeName);
			
			var o = schemaElement as SchemaOneOfElement;
			if (o != null)
				return ValidateMatchesOneOfElementPartial(o, input, attributeName);

			ErrorMessage = "Unknown element type in ValidateMatches.";
			return false;
		}

		private bool ValidateMatchesPartial(SchemaElement schemaElement, List<Match> input) {
			var n = schemaElement as SchemaNodeElement;
			if (n != null) {
				if (input.Count > 1) {
					ErrorMessage = "One node expected, multiple found.";
					return false;
				}
				if (input.Count == 0) {
					return true; // might be incomplete list yet
				}
				return ValidateMatchesNodeElementPartial(n, input[0]);
			}

			var l = schemaElement as SchemaLiteralElement;
			if (l != null) {
				if (input.Count > 1) {
					ErrorMessage = "One literal expected, multiple found.";
					return false;
				}
				if (input.Count == 0) {
					return true; // might be incomplete list yet
				}
				return ValidateMatchesLiteralElement(l, input[0]); // cannot match literal partially
			}

			var ls = schemaElement as SchemaListElement;
			if (ls != null)
				return ValidateMatchesListElementPartial(ls, input);

			var s = schemaElement as SchemaSequenceElement;
			if (s != null)
				return ValidateMatchesSequenceElementPartial(s, input);
			
			var o = schemaElement as SchemaOneOfElement;
			if (o != null)
				return ValidateMatchesOneOfElementPartial(o, input);

			ErrorMessage = "Unknown element type in ValidateMatches.";
			return false;
		}

		private bool ValidateMatchesNodeElementPartial(SchemaNodeElement schemaNodeElement, Match input, string attributeName = null) {
			var n = input.Value as Node;
			if (n == null || n.Name != schemaNodeElement.Name) {
				ErrorMessage = "Element '"+input.Path+"' must be a ("+schemaNodeElement.Name+") node.";
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

			var mn = input as MatchNode;
			if (mn == null) {
				ErrorMessage = "Element '"+input.Path+"' is a node and it's Match is not?";
				return false;
			}

			if (nt.Children != null) {
				if (!ValidateMatchesPartial(nt.Children, mn.Children))
					return false;
			}
			
			if (!ValidateConditionsPartial(nt.Conditions, input, attributeName))
				return false;

			// attributes
			foreach (var attribute in nt.Attributes) {
				if (!n.Attributes.ContainsKey(attribute.Name)) {
					continue;
				}

				var a = mn.Attributes[attribute.Name];
				if (!ValidateMatchesPartial(attribute.Element, a, attribute.Name))
					return false;
			}
			return true;
		}

		private bool ValidateConditionsPartial(SchemaTypeCondition conditions, Match input, string attributeName = null) {
			// need deeper analysis of conditions to determine whether conditions couldn't be met anymore
			// for example, if we know that condition is ^string or ~="smth" (must be of type string) and SDF is a node, it cannot match anyhow
			// but if condition is has_attr() and node is not completely parsed, we might meet the condition later
			/*
			if (conditions == null)
				return true;

			var allOf = conditions as SchemaTypeAllOfConditions;
			if (allOf != null) {
				foreach (var condition in allOf.Conditions) {
					if (!ValidateConditionsPartial(condition, input, attributeName)) {
						ErrorMessage = "One of the conditions is not met:\n\t" + ErrorMessage.Replace("\n", "\n\t");
						return false;
					}
				}

				return true;
			}

			var oneOf = conditions as SchemaTypeOneOfConditions;
			if (oneOf != null) {
				var found = false;
				var fullMessage = "";
				foreach (var condition in oneOf.Conditions) {
					if (ValidateConditionsPartial(condition, input, attributeName)) {
						found = true;
						break;
					}
					fullMessage += "\t" + ErrorMessage.Replace("\n", "\n\t") + "\n";
				}

				if (!found) {
					ErrorMessage = "None of the following multiple conditions is not met:\n" + fullMessage;
					return false;
				}

				return true;
			}

			// ok, it's simple condition
			var c = conditions as SchemaTypeSingleCondition;
			if (c == null) {
				ErrorMessage = "Unknown type of condition in ValidateConditions";
				return false;
			}

			if (!c.Condition.Matches(input.Value, input.Parent?.Value, attributeName)) {
				ErrorMessage = "Element '"+input.Path+"' does not match '"+c.RawCondition+"' condition.";
				return false;
			}
			*/
			return true;
		}

		private bool ValidateMatchesListElementPartial(SchemaListElement ls, Match input, string attributeName = null) { // no lists or sequences
			var l = new List<Match> { input };
			return ValidateMatchesListElementPartial(ls, l);
		}

		private bool ValidateMatchesListElementPartial(SchemaListElement ls, List<Match> input) {
			var l = input.Count;
			if (ls.Min > l) {
				// less than minimal - OK, might be incomplete list
			}

			if (ls.Limited && ls.Max < l) {
				ErrorMessage = "More than maximum ("+ls.Max+") amount of elements in a list.";
				return false;
			}

			foreach (var sdf in input) {
				if (!ValidateMatchesPartial(ls.Element, sdf))
					return false;
			}

			return true;
		}

		private bool ValidateMatchesSequenceElementPartial(SchemaSequenceElement schemaSequenceElement, Match input, string attributeName = null) { // no sequences
			// only one element found, but it could be the first in sequence,
			// so check that it partially matches schema of first element in the sequence
			return ValidateMatchesPartial(schemaSequenceElement.Sequence[0], input, attributeName);
		}

		private bool ValidateMatchesSequenceElementPartial(SchemaSequenceElement schemaSequenceElement, List<Match> input) {
			int l = schemaSequenceElement.Sequence.Count;
			if (input.Count > l) {
				ErrorMessage = "A sequence of " + l + " elements expected, more (" + input.Count + ") elements found.";
				return false;
			}

			for (int i = 0; i < input.Count; ++i) {
				if (!ValidateMatchesPartial(schemaSequenceElement.Sequence[i], input[i]))
					return false;
			}

			return true;
		}

		private bool ValidateMatchesOneOfElementPartial(SchemaOneOfElement schemaOneOfElement, Match input, string attributeName = null) { // only nodes and literals
			var fullMessage = "";
			foreach (var option in schemaOneOfElement.Options) {
				if (ValidateMatchesPartial(option, input, attributeName))
					return true;

				fullMessage += "\t" + ErrorMessage.Replace("\n", "\n\t") + "\n";
			}

			ErrorMessage = "Element '"+input.Path+"' does not match neither of allowed options even partially:\n" + fullMessage;
			return false;
		}

		private bool ValidateMatchesOneOfElementPartial(SchemaOneOfElement schemaOneOfElement, List<Match> input) {
			foreach (var option in schemaOneOfElement.Options) {
				if (ValidateMatchesPartial(option, input))
					return true;
			}

			ErrorMessage = "Element does not match neither of allowed options even partially.";
			return false;
		}
	}
}

/*
# schema of valid schema

(schema {top-element (node-element {name "schema" type "schema-type"})} [	
	(node-type {name "schema-type" children (list (one-of [
		(node-element {name "node-type" type "node-type-type"})
		(node-element {name "literal-type" type "literal-type-type"})
	]))} [
		(attribute {name "top-element" required true} (one-of [
			(node-element {name "node-element" type "node-element-type"})
			(node-element {name "literal-element" type "literal-element-type"})
			(node-element {name "one-of" type "one-of-type"})
		]))
	])

	(node-type {name "node-type-type" children (list (node-element {name "attribute" type="attribute-type"}))} [
		(attribute {name "name" required true} (literal-element {type "string"}))
		(attribute {name "children" required false} (one-of [
			(node-element {name "node-element" type "node-element-type"})
			(node-element {name "literal-element" type "literal-element-type"})
			(node-element {name "sequence" type "sequence-type"})
			(node-element {name "one-of" type "one-of-type"})
			(node-element {name "list" type "list-type"})
		]))
		(attribute {name "conditions" required false} (one-of [
			(node-element {name "condition" type "condition-type"})
			(node-element {name "one-of-conditions" type "list-of-conditions-type"})
			(node-element {name "all-of-conditions" type "list-of-conditions-type"})
		]))
	])
	(node-type {name "literal-type-type"} [
		(attribute {name "name" required true} (literal-element {type "string"}))
		(attribute {name "base-type" required true} (literal-element {type "builtin-literal-type-name"}))
		(attribute {name "conditions" required false} (one-of [
			(node-element {name "condition" type "condition-type"})
			(node-element {name "one-of-conditions" type "list-of-conditions-type"})
			(node-element {name "all-of-conditions" type "list-of-conditions-type"})
		]))
	])

	(literal-type {name "builtin-literal-type-name" base-type "string" conditions (one-of-conditions [
		(condition "=\"null\"")
		(condition "=\"bool\"")
		(condition "=\"boolean\"")
		(condition "=\"number\"")
		(condition "=\"string\"")
	])})

	(node-type {name "node-element-type"} [
		(attribute {name "name" required true} (literal-element {type "string"}))
		(attribute {name "type" required false} (literal-element {type "string"}))
	])
	(node-type {name "literal-element-type"} [
		(attribute {name "type" required true} (literal-element {type "string"}))
	])
	(node-type {name "sequence-type" children (list (one-of [
		(node-element {name "node-element" type "node-element-type"})
		(node-element {name "literal-element" type "literal-element-type"})
		(node-element {name "one-of" type "one-of-type"})
	]))})
	(node-type {name "one-of-type" children (list (one-of [
		(node-element {name "node-element" type "node-element-type"})
		(node-element {name "literal-element" type "literal-element-type"})
	]))})
	(node-type {name "list-type" children (one-of [
		(node-element {name "node-element" type "node-element-type"})
		(node-element {name "literal-element" type "literal-element-type"})
		(node-element {name "one-of" type "one-of-type"})
	])} [
		(attribute {name "min" required false} (literal-element {type "positive-number"}))
		(attribute {name "max" required false} (literal-element {type "positive-number"}))
	])

	(literal-type {name "positive-number" base-type "number" conditions (condition ">=0")})

	(node-type {name "condition-type" children (literal-element {type "string"})})
	(node-type {name "list-of-conditions-type" children (list {min 1} (one-of [
		(node-element {name "condition" type "condition-type"})
		(node-element {name "one-of-conditions" type "list-of-conditions-type"})
		(node-element {name "all-of-conditions" type "list-of-conditions-type"})
	]))})

	(node-type {name "attribute-type" children (one-of [
		(node-element {name "node-element" type "node-element-type"})
		(node-element {name "literal-element" type "literal-element-type"})
		(node-element {name "one-of" type "one-of-type"})
	])} [
		(attribute {name "name" required true} (literal-element {type "string"}))
		(attribute {name "required" required true} (literal-element {type "bool"}))
	])
])
*/
