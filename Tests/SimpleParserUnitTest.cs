using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using sdf;
using sdf.Parsing;
using sdf.Parsing.SExpression;

namespace Tests {
	[TestClass]
	public class SimpleParserUnitTest {
		[TestMethod]
		public void TestBuilding() {
			var x = Parser.ParseString("(node {attr (node 3.14) attr2 (node [\"6\" \"7\"])} [true false null])");
			var s = SimpleParser.Build(x);
			var n = s as Node;
			AssertNode(n, "node", 2, 3);

			var attr = n.Attributes["attr"];
			var attrNode = attr as Node;
			AssertNode(attrNode, "node", 0, 1);
			AssertNumberLiteral(attrNode.Children[0] as NumberLiteral, 3, 14);

			var attr2 = n.Attributes["attr2"];
			var attr2Node = attr2 as Node;
			AssertNode(attr2Node, "node", 0, 2);
			AssertStringLiteral(attr2Node.Children[0] as StringLiteral, "6");
			AssertStringLiteral(attr2Node.Children[1] as StringLiteral, "7");

			AssertBooleanLiteral(n.Children[0] as BooleanLiteral, true);
			AssertBooleanLiteral(n.Children[1] as BooleanLiteral, false);
			Assert.AreEqual(n.Children[2].GetType(), typeof(NullLiteral));

			// a few other cases

			var fn = TestHelper.MakeTempFile("(node (node {a 1 b 2}))");
			s = SimpleParser.Parse(fn);
			TestHelper.DeleteTempFile(fn);
			n = s as Node;
			AssertNode(n, "node", 0, 1);

			n = n.Children[0] as Node;
			AssertNode(n, "node", 2, 0);
			AssertNumberLiteral(n.Attributes["a"] as NumberLiteral, 1, 0);
		}

		private static void AssertNode(Node n, string name, int attributesCount, int childrenCount) {
			Assert.AreNotEqual(n, null);
			Assert.AreEqual(n.Name, name);
			Assert.AreEqual(n.Attributes.Count, attributesCount);
			Assert.AreEqual(n.Children.Count, childrenCount);
		}

		private static void AssertNumberLiteral(NumberLiteral l, long integer, long fraction) {
			Assert.AreNotEqual(l, null);
			Assert.AreEqual(l.Integer, integer);
			Assert.AreEqual(l.Fraction, fraction);
		}

		private static void AssertStringLiteral(StringLiteral l, string value) {
			Assert.AreNotEqual(l, null);
			Assert.AreEqual(l.Value, value);
		}

		private static void AssertBooleanLiteral(BooleanLiteral l, bool value) {
			Assert.AreNotEqual(l, null);
			Assert.AreEqual(l.Value, value);
		}

		[TestMethod]
		public void TestExceptions() {
			const string NOT_HERE = "Should've thrown an Exception instead of ending up here.";

			try {
				SimpleParser.Build(Parser.ParseString("{not-node}"));
				Assert.Fail(NOT_HERE);
			} catch (Exception e) {
				Assert.AreEqual(e.GetType(), typeof(InvalidDataException));
				Assert.AreEqual(e.Message, "Syntax error: () list expected while building a Node.");
			}

			try {
				SimpleParser.Build(Parser.ParseString("(not a node because has lots of expressions)"));
				Assert.Fail(NOT_HERE);
			} catch (Exception e) {
				Assert.AreEqual(e.GetType(), typeof(InvalidDataException));
				Assert.AreEqual(e.Message, "Syntax error: Node's () list must contain from 1 to 3 element within.");
			}

			try {
				SimpleParser.Build(Parser.ParseString("()")); // not a node because not enough expressions within
				Assert.Fail(NOT_HERE);
			} catch (Exception e) {
				Assert.AreEqual(e.GetType(), typeof(InvalidDataException));
				Assert.AreEqual(e.Message, "Syntax error: Node's () list must contain from 1 to 3 element within.");
			}

			try {
				SimpleParser.Build(Parser.ParseString("((name))"));
				Assert.Fail(NOT_HERE);
			} catch (Exception e) {
				Assert.AreEqual(e.GetType(), typeof(InvalidDataException));
				Assert.AreEqual(e.Message, "Syntax error: Node's name must be a keyword.");
			}

			try {
				SimpleParser.Build(Parser.ParseString("(n [] {})"));
				Assert.Fail(NOT_HERE);
			} catch (Exception e) {
				Assert.AreEqual(e.GetType(), typeof(InvalidDataException));
				Assert.AreEqual(e.Message, "Syntax error: {} list excepted while building Node's attributes.");
			}

			try {
				SimpleParser.Build(Parser.ParseString("(n {(n)})"));
				Assert.Fail(NOT_HERE);
			} catch (Exception e) {
				Assert.AreEqual(e.GetType(), typeof(InvalidDataException));
				Assert.AreEqual(e.Message, "Syntax error: attribute name must be a keyword.");
			}

			try {
				SimpleParser.Build(Parser.ParseString("(n {} {})"));
				Assert.Fail(NOT_HERE);
			} catch (Exception e) {
				Assert.AreEqual(e.GetType(), typeof(InvalidDataException));
				Assert.AreEqual(e.Message, "Syntax error: Node's child or children cannot be represented as {} list.");
			}
		}
	}
}
