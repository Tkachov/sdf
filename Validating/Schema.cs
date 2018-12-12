using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using sdf.Matching;
using sdf.Parsing;

namespace sdf.Validating {
	/// <summary>
	///     Representation of SDF Schema.
	///     Could be built from SDF representation and then used to validate other SDF representations.
	/// </summary>
	public class Schema {
		private readonly Dictionary<string, SchemaBuiltinType> _builtinTypes = new Dictionary<string, SchemaBuiltinType>();
		private readonly SchemaElement _topElement;
		private readonly Dictionary<string, SchemaType> _types = new Dictionary<string, SchemaType>();

		/// <summary>
		///     Last error message set while validating SDF document.
		/// </summary>
		public string ErrorMessage { get; private set; }

		/// <summary>
		///     Create new SDF Schema read from a file with a given name.
		/// </summary>
		/// <param name="filename">Name of a file to read SDF Schema from.</param>
		public Schema([NotNull] string filename) : this(SimpleParser.Parse(filename)) { }

		/// <summary>
		///     Create new SDF Schema from given SDF representation.
		/// </summary>
		/// <param name="schema">SDF representation of a schema.</param>
		public Schema(SDF schema) {
			var n = schema as Node;
			if (n == null || !n.Name.Equals("schema")) {
				throw new InvalidDataException("Schema must be a (schema) node.");
			}

			// built-ins
			_builtinTypes["node"] = new SchemaSimpleNodeType();
			_builtinTypes["string"] = new SchemaStringType();
			_builtinTypes["boolean"] = _builtinTypes["bool"] = new SchemaBooleanType();
			_builtinTypes["number"] = new SchemaNumberType();
			_builtinTypes["null"] = new SchemaNullType();

			_topElement = MakeElement(n.Attributes["top-element"]);

			// read user-defined types
			foreach (var type in n.Children) {
				var t = MakeType(type);
				var nt = t as SchemaNodeType;
				if (nt != null) {
					_types[nt.Name] = nt;
				} else {
					var lt = t as SchemaLiteralType;
					if (lt != null) {
						_types[lt.Name] = lt;
					} else {
						throw new InvalidDataException("User-defined type must be either node-type or literal-type.");
					}
				}
			}

			// add built-ins
			foreach (var schemaBuiltinType in _builtinTypes) {
				_types[schemaBuiltinType.Key] = schemaBuiltinType.Value;
			}

			// verify all types have a description
			VerifyElement(_topElement);
			foreach (var schemaType in _types) {
				if (schemaType.Value is SchemaBuiltinType) {
					continue;
				}

				if (schemaType.Value is SchemaLiteralType) {
					continue; // can only reference a built-in
				}

				var t = schemaType.Value as SchemaNodeType;
				VerifyElement(t.Children);
				foreach (var attribute in t.Attributes) {
					VerifyElement(attribute.Element);
				}
			}

			ErrorMessage = null;
		}

		/// <summary>
		///     Validate given SDF representation with current schema.
		///     If SDF does not match schema, ErrorMessage would be set to notify of a reason. Only the first error is returned.
		/// </summary>
		/// <param name="input">SDF to validate.</param>
		/// <returns>Whether given SDF matches schema.</returns>
		public bool Validate(SDF input) {
			ErrorMessage = null;

			var e = _topElement;
			return ValidateMatches(e, Match.MakeRootMatch(input));
		}

		/// <summary>
		///     Partially validate given SDF representation with current schema.
		///     Returns whether given SDF could match schema (for example, a node lacks a required attribute, but otherwise it's
		///     completely correct).
		///     If SDF does not match schema (no possible addition will fix it), ErrorMessage would be set to notify of a reason.
		///     Only the first error is returned.
		/// </summary>
		/// <param name="input">SDF to validate.</param>
		/// <returns>Whether given SDF could match schema.</returns>
		public bool ValidatePartial(SDF input) {
			ErrorMessage = null;

			var e = _topElement;
			return ValidateMatchesPartial(e, Match.MakeRootMatch(input));
		}

		// making Schema elements, types and verifying those are OK

		private void VerifyElement(SchemaElement schemaElement) {
			if (schemaElement == null) {
				return;
			}

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
				if (!_types.ContainsKey(lt.TypeName)) {
					throw new InvalidDataException("Literal element references an undeclared type '" + lt.TypeName + "'");
				}

				var t = _types[lt.TypeName];
				if ((!(t is SchemaBuiltinType) || t is SchemaSimpleNodeType) && !(t is SchemaLiteralType)) {
					throw new InvalidDataException("Literal element references non-literal type.");
				}

				return;
			}

			var e = schemaElement as SchemaNodeElement;
			if (e == null) {
				throw new InvalidDataException("Unknown element type found in VerifyElement.");
			}

			if (!_types.ContainsKey(e.TypeName)) {
				throw new InvalidDataException("Node element references an undeclared type '" + e.TypeName + "'");
			}

			var et = _types[e.TypeName];
			if (!(et is SchemaSimpleNodeType) && !(et is SchemaNodeType)) {
				throw new InvalidDataException("Node element references non-node type.");
			}
		}

		private SchemaElement MakeElement(SDF sdf) {
			var n = sdf as Node;
			if (n == null) {
				throw new InvalidDataException("Schema element description must be a node.");
			}

			switch (n.Name) {
				case "node-element":
					return new SchemaNodeElement(n);
				case "literal-element":
					return new SchemaLiteralElement(n, _builtinTypes);

				case "sequence":
				case "one-of":
					var elements = new List<SchemaElement>();
					foreach (var child in n.Children) {
						elements.Add(MakeElement(child));
					}

					if (n.Name == "sequence") {
						return new SchemaSequenceElement(elements);
					}

					return new SchemaOneOfElement(elements);

				case "list":
					var children = n.Children;
					if (children.Count != 1) {
						throw new InvalidDataException("Schema list description must have exactly one element description.");
					}

					return new SchemaListElement(n, MakeElement(children[0]));
				default:
					throw new InvalidDataException("Invalid schema element description.");
			}
		}

		private SchemaType MakeType(SDF sdf) {
			var n = sdf as Node;
			if (n == null) {
				throw new InvalidDataException("Schema type description must be a node.");
			}

			var conditions = MakeConditionsForNode(n);
			switch (n.Name) {
				case "node-type":
					SchemaElement children = null;
					if (n.Attributes.ContainsKey("children")) {
						children = MakeElement(n.Attributes["children"]);
					}

					var attributes = MakeAttributes(n.Children);
					return new SchemaNodeType(n, children, conditions, attributes);

				case "literal-type":
					return new SchemaLiteralType(n, _builtinTypes, conditions);

				default:
					throw new InvalidDataException("Invalid schema type description.");
			}
		}

		private static SchemaTypeCondition MakeConditionsForNode(Node node) {
			if (!node.Attributes.ContainsKey("conditions")) {
				return null;
			}

			var c = node.Attributes["conditions"] as Node;
			if (c == null) {
				throw new InvalidDataException("Schema condition description must be a node.");
			}

			return MakeCondition(c);
		}

		private static SchemaTypeCondition MakeCondition(Node n) {
			var c = n.Children;

			switch (n.Name) {
				case "condition":
					if (c.Count != 1) {
						throw new InvalidDataException("Schema condition description must have exactly one value.");
					}

					var v = n.Children[0];
					var s = v as StringLiteral;
					if (s == null) {
						throw new InvalidDataException("Schema condition description must be a string.");
					}

					return new SchemaTypeSingleCondition(s);

				case "one-of-conditions":
				case "all-of-conditions":
					if (c.Count < 1) {
						throw new InvalidDataException("Schema " + n.Name + " description must have at least one value.");
					}

					var conditions = new List<SchemaTypeCondition>();
					foreach (var sdf in c) {
						var nd = sdf as Node;
						if (nd == null) {
							throw new InvalidDataException("All of schema " + n.Name + " description values must be nodes.");
						}

						conditions.Add(MakeCondition(nd));
					}

					if (n.Name == "one-of-conditions") {
						return new SchemaTypeOneOfConditions(conditions);
					}

					return new SchemaTypeAllOfConditions(conditions);

				default:
					throw new InvalidDataException("Invalid schema condition description.");
			}
		}

		private List<SchemaNodeTypeAttribute> MakeAttributes(List<SDF> sdfList) {
			return sdfList.Select(MakeAttribute).ToList();
		}

		private SchemaNodeTypeAttribute MakeAttribute(SDF sdf) {
			var n = sdf as Node;
			if (n == null || n.Name != "attribute") {
				throw new InvalidDataException("Schema attribute description must be an (attribute) node.");
			}

			var children = n.Children;
			if (children.Count != 1) {
				throw new InvalidDataException("Schema attribute description must have exactly one element description.");
			}

			return new SchemaNodeTypeAttribute(n, MakeElement(children[0]));
		}

		// VALIDATION //

		private bool ValidateMatches(SchemaElement schemaElement, Match input, string attributeName = null) {
			var n = schemaElement as SchemaNodeElement;
			if (n != null) {
				return ValidateMatchesNodeElement(n, input, attributeName);
			}

			var l = schemaElement as SchemaLiteralElement;
			if (l != null) {
				return ValidateMatchesLiteralElement(l, input, attributeName);
			}

			var ls = schemaElement as SchemaListElement;
			if (ls != null) {
				return ValidateMatchesListElement(ls, input, attributeName);
			}

			var s = schemaElement as SchemaSequenceElement;
			if (s != null) {
				return ValidateMatchesSequenceElement(s, input, attributeName);
			}

			var o = schemaElement as SchemaOneOfElement;
			if (o != null) {
				return ValidateMatchesOneOfElement(o, input, attributeName);
			}

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
			if (ls != null) {
				return ValidateMatchesListElement(ls, input);
			}

			var s = schemaElement as SchemaSequenceElement;
			if (s != null) {
				return ValidateMatchesSequenceElement(s, input);
			}

			var o = schemaElement as SchemaOneOfElement;
			if (o != null) {
				return ValidateMatchesOneOfElement(o, input);
			}

			ErrorMessage = "Unknown element type in ValidateMatches.";
			return false;
		}

		private bool ValidateMatchesNodeElement(SchemaNodeElement schemaNodeElement, Match input, string attributeName = null) {
			var n = input.Value as Node;
			if (n == null || n.Name != schemaNodeElement.Name) {
				ErrorMessage = "Element '" + input.Path + "' must be a (" + schemaNodeElement.Name + ") node.";
				return false;
			}

			var t = _types[schemaNodeElement.TypeName];
			if (t is SchemaSimpleNodeType) {
				return true;
			}

			var nt = t as SchemaNodeType;
			if (nt == null) {
				ErrorMessage = "Bad type in schema.";
				return false;
			}

			var mn = input as MatchNode;
			if (mn == null) {
				ErrorMessage = "Element '" + input.Path + "' is a node and it's Match is not?";
				return false;
			}

			if (nt.Children != null) {
				if (!ValidateMatches(nt.Children, mn.Children)) {
					return false;
				}
			}

			if (!ValidateConditions(nt.Conditions, input, attributeName)) {
				return false;
			}

			// attributes
			foreach (var attribute in nt.Attributes) {
				if (!n.Attributes.ContainsKey(attribute.Name)) {
					if (!attribute.Required) {
						continue;
					}

					ErrorMessage = "Required attribute '" + attribute.Name + "' is missing on element '" + input.Path + "'.";
					return false;
				}

				var a = mn.Attributes[attribute.Name];
				if (!ValidateMatches(attribute.Element, a, attribute.Name)) {
					return false;
				}
			}

			return true;
		}

		private bool ValidateMatchesLiteralElement(SchemaLiteralElement schemaLiteralElement, Match input, string attributeName = null) {
			if (input.Value is Node) {
				ErrorMessage = "Element '" + input.Path + "' must be literal.";
				return false;
			}

			var t = _types[schemaLiteralElement.TypeName];
			if (t is SchemaBuiltinType) {
				if (!ValidateMatchesType(t as SchemaBuiltinType, input, attributeName)) {
					return false;
				}
			} else if (t is SchemaLiteralType) {
				var lt = t as SchemaLiteralType;
				if (!ValidateMatchesType(lt.BaseType, input, attributeName)) {
					return false;
				}

				if (!ValidateConditions(lt.Conditions, input, attributeName)) {
					return false;
				}
			} else {
				ErrorMessage = "Bad type in schema.";
				return false;
			}

			return true;
		}

		private bool ValidateConditions(SchemaTypeCondition conditions, Match input, string attributeName = null) {
			if (conditions == null) {
				return true;
			}

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
				ErrorMessage = "Element '" + input.Path + "' does not match '" + c.RawCondition + "' condition.";
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
				ErrorMessage = "Less than minimal (" + ls.Min + ") amount of elements in a list.";
				return false;
			}

			if (ls.Limited && ls.Max < l) {
				ErrorMessage = "More than maximum (" + ls.Max + ") amount of elements in a list.";
				return false;
			}

			foreach (var sdf in input) {
				if (!ValidateMatches(ls.Element, sdf)) {
					return false;
				}
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
			var l = schemaSequenceElement.Sequence.Count;
			if (input.Count != l) {
				ErrorMessage = "A sequence of " + l + " elements expected, " + input.Count + " element(s) found.";
				return false;
			}

			for (var i = 0; i < l; ++i) {
				if (!ValidateMatches(schemaSequenceElement.Sequence[i], input[i])) {
					return false;
				}
			}

			return true;
		}

		private bool ValidateMatchesOneOfElement(SchemaOneOfElement schemaOneOfElement, Match input, string attributeName = null) { // only nodes and literals
			var fullMessage = "";
			foreach (var option in schemaOneOfElement.Options) {
				if (ValidateMatches(option, input, attributeName)) {
					return true;
				}

				fullMessage += "\t" + ErrorMessage.Replace("\n", "\n\t") + "\n";
			}

			ErrorMessage = "Element '" + input.Path + "' does not match neither of allowed options:\n" + fullMessage;
			return false;
		}

		private bool ValidateMatchesOneOfElement(SchemaOneOfElement schemaOneOfElement, List<Match> input) {
			foreach (var option in schemaOneOfElement.Options) {
				if (ValidateMatches(option, input)) {
					return true;
				}
			}

			ErrorMessage = "Element does not match neither of allowed options.";
			return false;
		}

		// PARTIAL VALIDATION //

		private bool ValidateMatchesPartial(SchemaElement schemaElement, Match input, string attributeName = null) {
			var n = schemaElement as SchemaNodeElement;
			if (n != null) {
				return ValidateMatchesNodeElementPartial(n, input, attributeName);
			}

			var l = schemaElement as SchemaLiteralElement;
			if (l != null) {
				return ValidateMatchesLiteralElement(l, input, attributeName); // cannot match literal partially, you either do or you don't
			}

			var ls = schemaElement as SchemaListElement;
			if (ls != null) {
				return ValidateMatchesListElementPartial(ls, input, attributeName);
			}

			var s = schemaElement as SchemaSequenceElement;
			if (s != null) {
				return ValidateMatchesSequenceElementPartial(s, input, attributeName);
			}

			var o = schemaElement as SchemaOneOfElement;
			if (o != null) {
				return ValidateMatchesOneOfElementPartial(o, input, attributeName);
			}

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
			if (ls != null) {
				return ValidateMatchesListElementPartial(ls, input);
			}

			var s = schemaElement as SchemaSequenceElement;
			if (s != null) {
				return ValidateMatchesSequenceElementPartial(s, input);
			}

			var o = schemaElement as SchemaOneOfElement;
			if (o != null) {
				return ValidateMatchesOneOfElementPartial(o, input);
			}

			ErrorMessage = "Unknown element type in ValidateMatches.";
			return false;
		}

		private bool ValidateMatchesNodeElementPartial(SchemaNodeElement schemaNodeElement, Match input, string attributeName = null) {
			var n = input.Value as Node;
			if (n == null || n.Name != schemaNodeElement.Name) {
				ErrorMessage = "Element '" + input.Path + "' must be a (" + schemaNodeElement.Name + ") node.";
				return false;
			}

			var t = _types[schemaNodeElement.TypeName];
			if (t is SchemaSimpleNodeType) {
				return true;
			}

			var nt = t as SchemaNodeType;
			if (nt == null) {
				ErrorMessage = "Bad type in schema.";
				return false;
			}

			var mn = input as MatchNode;
			if (mn == null) {
				ErrorMessage = "Element '" + input.Path + "' is a node and it's Match is not?";
				return false;
			}

			if (nt.Children != null) {
				if (!ValidateMatchesPartial(nt.Children, mn.Children)) {
					return false;
				}
			}

			if (!ValidateConditionsPartial(nt.Conditions, input, attributeName)) {
				return false;
			}

			// attributes
			foreach (var attribute in nt.Attributes) {
				if (!n.Attributes.ContainsKey(attribute.Name)) {
					continue;
				}

				var a = mn.Attributes[attribute.Name];
				if (!ValidateMatchesPartial(attribute.Element, a, attribute.Name)) {
					return false;
				}
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
			var l = new List<Match> {input};
			return ValidateMatchesListElementPartial(ls, l);
		}

		private bool ValidateMatchesListElementPartial(SchemaListElement ls, List<Match> input) {
			var l = input.Count;
			if (ls.Min > l) {
				// less than minimal - OK, might be incomplete list
			}

			if (ls.Limited && ls.Max < l) {
				ErrorMessage = "More than maximum (" + ls.Max + ") amount of elements in a list.";
				return false;
			}

			foreach (var sdf in input) {
				if (!ValidateMatchesPartial(ls.Element, sdf)) {
					return false;
				}
			}

			return true;
		}

		private bool ValidateMatchesSequenceElementPartial(SchemaSequenceElement schemaSequenceElement, Match input, string attributeName = null) { // no sequences
			// only one element found, but it could be the first in sequence,
			// so check that it partially matches schema of first element in the sequence
			return ValidateMatchesPartial(schemaSequenceElement.Sequence[0], input, attributeName);
		}

		private bool ValidateMatchesSequenceElementPartial(SchemaSequenceElement schemaSequenceElement, List<Match> input) {
			var l = schemaSequenceElement.Sequence.Count;
			if (input.Count > l) {
				ErrorMessage = "A sequence of " + l + " elements expected, more (" + input.Count + ") elements found.";
				return false;
			}

			for (var i = 0; i < input.Count; ++i) {
				if (!ValidateMatchesPartial(schemaSequenceElement.Sequence[i], input[i])) {
					return false;
				}
			}

			return true;
		}

		private bool ValidateMatchesOneOfElementPartial(SchemaOneOfElement schemaOneOfElement, Match input, string attributeName = null) { // only nodes and literals
			var fullMessage = "";
			foreach (var option in schemaOneOfElement.Options) {
				if (ValidateMatchesPartial(option, input, attributeName)) {
					return true;
				}

				fullMessage += "\t" + ErrorMessage.Replace("\n", "\n\t") + "\n";
			}

			ErrorMessage = "Element '" + input.Path + "' does not match neither of allowed options even partially:\n" + fullMessage;
			return false;
		}

		private bool ValidateMatchesOneOfElementPartial(SchemaOneOfElement schemaOneOfElement, List<Match> input) {
			foreach (var option in schemaOneOfElement.Options) {
				if (ValidateMatchesPartial(option, input)) {
					return true;
				}
			}

			ErrorMessage = "Element does not match neither of allowed options even partially.";
			return false;
		}
	}
}