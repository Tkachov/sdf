using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using sdf;
using sdf.Parsing;
using sdf.Parsing.SExpression;
using sdf.Validating;

namespace Tests {
	internal class TestHelper {
		// parsing

		internal static SDF ParseString(string s) {
			return SimpleParser.Build(Parser.ParseString(s));
		}

		internal static SDF StreamParseString(string s, Schema schema = null) {
			var fn = MakeTempFile(s);

			try {
				return (schema == null ? 
					StreamingParser.Parse(fn) : 
					StreamingParser.ParseAndValidateSchema(fn, schema));
			} finally {
				DeleteTempFile(fn);
			}
		}

		// file-related

		internal static string GetTempFilePathWithExtension(string extension) {
			var path = Path.GetTempPath();
			var fileName = Guid.NewGuid() + extension;
			return Path.Combine(path, fileName);
		}

		internal static string MakeTempFile(string contents) {
			var fn = GetTempFilePathWithExtension(".sdf.tmp");
			using (var sw = new StreamWriter(fn)) {
				sw.WriteLine(contents);
			}
			return fn;
		}

		internal static void DeleteTempFile(string fn) {
			if (fn == null) return;
			File.Delete(fn);
		}

		// node and literal assertions
		
		internal static void AssertNode(Node n, string name, int attributesCount, int childrenCount) {
			Assert.AreNotEqual(n, null);
			Assert.AreEqual(n.Name, name);
			Assert.AreEqual(n.Attributes.Count, attributesCount);
			Assert.AreEqual(n.Children.Count, childrenCount);
		}

		internal static void AssertNumberLiteral(NumberLiteral l, long integer, long fraction) {
			Assert.AreNotEqual(l, null);
			Assert.AreEqual(l.Integer, integer);
			Assert.AreEqual(l.Fraction, fraction);
		}

		internal static void AssertNodeAndFirstNumberLiteralChild(Node n, string name, int attributes, int children, long integer, long fraction) {
			Assert.IsTrue(children > 0);
			AssertNode(n, name, attributes, children);
			AssertNumberLiteral(n.Children[0] as NumberLiteral, integer, fraction);
		}

		internal static void AssertAreDeeplyEqual(SDF expected, SDF actual) {
			var n = expected as Node;
			if (n == null) {
				AssertAreDeeplyEqualLiterals(expected, actual);
			} else {
				AssertAreDeeplyEqualNodes(expected, actual);
			}
		}

		internal static void AssertAreDeeplyEqualLiterals(SDF expected, SDF actual) {
			var n1 = expected as Node;
			var n2 = actual as Node;
			if (n1 != null || n2 != null)
				Assert.Fail("Both expected and actual must be literals, but at least one of them is not:\n\t"+expected+"\n\t"+actual);

			var sl1 = expected as StringLiteral;
			var sl2 = actual as StringLiteral;
			if (sl1 != null) {
				if (sl2 == null)
					Assert.Fail("Expected string literal, but actual is not:\n\t"+expected+"\n\t"+actual);

				Assert.AreEqual(sl1.Value, sl2.Value);
			}

			var nl1 = expected as NumberLiteral;
			var nl2 = actual as NumberLiteral;
			if (nl1 != null) {
				if (nl2 == null)
					Assert.Fail("Expected number literal, but actual is not:\n\t"+expected+"\n\t"+actual);

				Assert.AreEqual(nl1.Integer, nl2.Integer);
				Assert.AreEqual(nl1.Fraction, nl2.Fraction);
			}

			var bl1 = expected as BooleanLiteral;
			var bl2 = actual as BooleanLiteral;
			if (bl1 != null) {
				if (bl2 == null)
					Assert.Fail("Expected boolean literal, but actual is not:\n\t"+expected+"\n\t"+actual);

				Assert.AreEqual(bl1.Value, bl2.Value);
			}

			var nll1 = expected as NullLiteral;
			var nll2 = actual as NullLiteral;
			if (nll1 != null) {
				if (nll2 == null)
					Assert.Fail("Expected null literal, but actual is not:\n\t"+expected+"\n\t"+actual);
			}
		}

		internal static void AssertAreDeeplyEqualNodes(SDF expected, SDF actual) {
			var n1 = expected as Node;
			var n2 = actual as Node;
			if (n1 == null || n2 == null)
				Assert.Fail("Both expected and actual must be nodes, but at least one of them is not:\n\t"+expected+"\n\t"+actual);

			Assert.AreEqual(n1.Name, n2.Name);
			Assert.AreEqual(n1.Attributes.Count, n2.Attributes.Count);
			Assert.AreEqual(n1.Children.Count, n2.Children.Count);

			// compare attributes
			foreach (var attribute in n1.Attributes) {
				Assert.IsTrue(n2.Attributes.ContainsKey(attribute.Key));
				AssertAreDeeplyEqual(attribute.Value, n2.Attributes[attribute.Key]);
			}
			
			// now compare children
			for (var i = 0; i < n1.Children.Count; ++i) {
				AssertAreDeeplyEqual(n1.Children[i], n2.Children[i]);
			}
		}

		// examples

		internal static SDF GetSimpleHTMLSchema() {
			return ParseString(@"
				(schema {top-element (node-element {name ""html"" type ""html-type""})} [
					(node-type {name ""html-type"" children (sequence [
						(node-element {name ""head"" })
						(node-element {name ""body"" type ""body-type""})
					])})

					(node-type {name ""body-type"" children (list (one-of [
						(node-element {name ""p"" type ""p-type""})
						(node-element {name ""img"" type ""img-type""})
					]))})

					(node-type {name ""p-type"" children (literal-element {type ""string""})})

					(node-type {name ""img-type""} [
						(attribute {name ""src"" required true} (literal-element {type ""string""}))
						(attribute {name ""title"" required false} (literal-element {type ""string""}))
					])
				])
			");
		}
	}
}
