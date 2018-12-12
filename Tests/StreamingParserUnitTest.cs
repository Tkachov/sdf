using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using sdf;
using sdf.Parsing;
using sdf.Parsing.SExpression;

namespace Tests {
	[TestClass]
	public class StreamingParserUnitTest {
		[TestMethod]
		public void TestBuilding() {
			var x = TestHelper.MakeTempFile("(node {attr (node 3.14) attr2 (node [\"6\" \"7\"])} [true false null])");
			var s = StreamingParser.Parse(x);
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

			TestHelper.DeleteTempFile(x);

			// a few other cases

			x = TestHelper.MakeTempFile("(node (node {a 1 b 2}))");
			s = StreamingParser.Parse(x);
			n = s as Node;
			AssertNode(n, "node", 0, 1);

			n = n.Children[0] as Node;
			AssertNode(n, "node", 2, 0);
			AssertNumberLiteral(n.Attributes["a"] as NumberLiteral, 1, 0);

			TestHelper.DeleteTempFile(x);
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
		public void TestLiterals() {
			// keyword

			var x = TestHelper.MakeTempFile("null");
			var s = StreamingParser.Parse(x);
			TestHelper.DeleteTempFile(x);
			Assert.IsInstanceOfType(s, typeof(NullLiteral));

			// string
			
			x = TestHelper.MakeTempFile("\"String with escape sequences and white spaces \\n \\\" \"");
			s = StreamingParser.Parse(x);
			TestHelper.DeleteTempFile(x);
			AssertStringLiteral(s as StringLiteral, "String with escape sequences and white spaces \n \" ");
		}		

		[TestMethod]
		public void TestEscapeSequences() {
			var x = TestHelper.MakeTempFile("\"\\a \\b \\f \\r \\t \\v \"");
			var s = StreamingParser.Parse(x);
			TestHelper.DeleteTempFile(x);
			AssertStringLiteral(s as StringLiteral, "\a \b \f \r \t \v ");
		}
		
		[TestMethod]
		public void TestExceptions() {			
			const string PREFIX = "Error while stream parsing the file:\n\t";
			const string UNEXPECTED_EOF = PREFIX + "Unexpected EOF.";
			const string NEITHER_NODE_NOR_LITERAL = PREFIX + "Invalid SDF: neither node nor any of supported literals found.";
			const string NOT_HERE = "Should've thrown an Exception instead of ending up here.";
            string x = null;

			try {
				x = TestHelper.MakeTempFile("(x ");
				StreamingParser.Parse(x);
				Assert.Fail(NOT_HERE);
			} catch (Exception e) {
				Assert.AreEqual(e.GetType(), typeof(InvalidDataException));
				Assert.AreEqual(e.Message, UNEXPECTED_EOF);
				TestHelper.DeleteTempFile(x);
			}

			try {
				x = TestHelper.MakeTempFile("\"x y z");
				StreamingParser.Parse(x);
				Assert.Fail(NOT_HERE);
			} catch (Exception e) {
				Assert.AreEqual(e.GetType(), typeof(InvalidDataException));
				Assert.AreEqual("Unexpected EOF while parsing string expression.", e.Message);
				TestHelper.DeleteTempFile(x);
			}

			try {
				x = TestHelper.MakeTempFile("\" \\x \"");
				StreamingParser.Parse(x);
				Assert.Fail(NOT_HERE);
			} catch (Exception e) {
				Assert.AreEqual(e.GetType(), typeof(InvalidDataException));
				Assert.AreEqual("Unknown escape sequence within string: \\x", e.Message);
				TestHelper.DeleteTempFile(x);
			}

			try {
				x = TestHelper.MakeTempFile("{not-node}");
				StreamingParser.Parse(x);
				Assert.Fail(NOT_HERE);
			} catch (Exception e) {
				Assert.AreEqual(typeof(InvalidDataException), e.GetType());
				Assert.AreEqual(NEITHER_NODE_NOR_LITERAL, e.Message);
				TestHelper.DeleteTempFile(x);
			}
			
			try {
				x = TestHelper.MakeTempFile("(not a node because has lots of expressions)");
				StreamingParser.Parse(x);
				Assert.Fail(NOT_HERE);
			} catch (Exception e) {
				Assert.AreEqual(typeof(InvalidDataException), e.GetType());
				Assert.AreEqual(PREFIX + "Invalid SDF: neither node nor any of supported literals found.", e.Message);
				TestHelper.DeleteTempFile(x);
			}
			
			try {
				x = TestHelper.MakeTempFile("()"); // not a node because not enough expressions within
				StreamingParser.Parse(x);
				Assert.Fail(NOT_HERE);
			} catch (Exception e) {
				Assert.AreEqual(typeof(InvalidDataException), e.GetType());
				Assert.AreEqual(PREFIX + "Invalid SDF: node must have a name.", e.Message);
				TestHelper.DeleteTempFile(x);
			}

			try {
				x = TestHelper.MakeTempFile("((name))");
				StreamingParser.Parse(x);
				Assert.Fail(NOT_HERE);
			} catch (Exception e) {
				Assert.AreEqual(typeof(InvalidDataException), e.GetType());
				Assert.AreEqual(PREFIX + "Invalid SDF: node must have a name.", e.Message);
				TestHelper.DeleteTempFile(x);
			}
			
			try {
				x = TestHelper.MakeTempFile("(n [] {})");
				StreamingParser.Parse(x);
				Assert.Fail(NOT_HERE);
			} catch (Exception e) {
				Assert.AreEqual(typeof(InvalidDataException), e.GetType());
				Assert.AreEqual(PREFIX + "Invalid SDF (expected node to end).", e.Message);
				TestHelper.DeleteTempFile(x);
			}

			try {
				x = TestHelper.MakeTempFile("(n {(n)})");
				StreamingParser.Parse(x);
				Assert.Fail(NOT_HERE);
			} catch (Exception e) {
				Assert.AreEqual(typeof(InvalidDataException), e.GetType());
				Assert.AreEqual(NEITHER_NODE_NOR_LITERAL, e.Message);
				TestHelper.DeleteTempFile(x);
			}

			try {
				x = TestHelper.MakeTempFile("(n {} {})");
				StreamingParser.Parse(x);
				Assert.Fail(NOT_HERE);
			} catch (Exception e) {
				Assert.AreEqual(typeof(InvalidDataException), e.GetType());
				Assert.AreEqual(PREFIX + "Invalid SDF: node cannot have two attribute lists.", e.Message);
				TestHelper.DeleteTempFile(x);
			}
		}
		
		[TestMethod]
		public void TestStreamingParsing() {
			var x = TestHelper.MakeTempFile("(node {attr 5} [true \"he he\" null])");
			var p = new StreamingParser(x);

			AssertParserToken(p, TokenType.DocumentStart);
			AssertParserToken(p, TokenType.NodeStart);
			Assert.AreEqual(p.NodeName, "node");
			AssertParserToken(p, TokenType.NodeAttributeListStart);
			AssertParserToken(p, TokenType.NodeAttributeStart);
			Assert.AreEqual(p.AttributeName, "attr");
			AssertParserToken(p, TokenType.Literal);
			AssertParserToken(p, TokenType.NodeAttributeEnd);
			AssertParserToken(p, TokenType.NodeAttributeListEnd);
			AssertParserToken(p, TokenType.NodeAfterAttributes);
			AssertParserToken(p, TokenType.NodeChildrenListStart);
			AssertParserToken(p, TokenType.Literal);
			AssertParserToken(p, TokenType.NodeChildrenListAfterChild);
			AssertParserToken(p, TokenType.Literal);
			AssertParserToken(p, TokenType.NodeChildrenListAfterChild);
			AssertParserToken(p, TokenType.Literal);
			AssertParserToken(p, TokenType.NodeChildrenListEnd);
			AssertParserToken(p, TokenType.NodeEnd);
			AssertParserToken(p, TokenType.DocumentEnd);
			Assert.IsTrue(p.Ended);
			Assert.IsTrue(!p.HasError);
			TestHelper.AssertNode(p.Document as Node, "node", 1, 3);
		}

		private static void AssertParserToken(StreamingParser p, TokenType type) {
			Assert.IsTrue(!p.Ended);
			Assert.IsTrue(!p.HasError);
			Assert.AreEqual(type, p.ReadNext());
		}
	}
}
