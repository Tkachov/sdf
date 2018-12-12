using System.IO;

namespace sdf.Validating {
	internal static class SchemaNodeExtension {
		internal static string GetStringAttribute(this Node n, string name, bool required = true) {
			if (!n.Attributes.ContainsKey(name)) {
				if (required) {
					throw new InvalidDataException("Attribute '" + name + "' expected, but not found.");
				}

				return null;
			}

			var v = n.Attributes[name];
			var s = v as StringLiteral;
			if (s == null) {
				throw new InvalidDataException("Attribute '" + name + "' expected to be a string.");
			}

			return s.Value;
		}

		internal static bool? GetBooleanAttribute(this Node n, string name, bool required = true) {
			if (!n.Attributes.ContainsKey(name)) {
				if (required) {
					throw new InvalidDataException("Attribute '" + name + "' expected, but not found.");
				}

				return null;
			}

			var v = n.Attributes[name];
			var b = v as BooleanLiteral;
			if (b == null) {
				throw new InvalidDataException("Attribute '" + name + "' expected to be a boolean value.");
			}

			return b.Value;
		}

		internal static long? GetLongIntegerAttribute(this Node nd, string name, bool required = true) {
			if (!nd.Attributes.ContainsKey(name)) {
				if (required) {
					throw new InvalidDataException("Attribute '" + name + "' expected, but not found.");
				}

				return null;
			}

			var v = nd.Attributes[name];
			var n = v as NumberLiteral;
			if (n == null) {
				throw new InvalidDataException("Attribute '" + name + "' expected to be a number.");
			}

			if (n.Fraction != 0) {
				throw new InvalidDataException("Attribute '" + name + "' expected to be an integer.");
			}

			return n.Integer;
		}
	}
}