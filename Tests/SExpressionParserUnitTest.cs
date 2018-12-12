using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using sdf.Parsing.SExpression;

namespace Tests {
	[TestClass]
	public class SExpressionParserUnitTest {
		[TestMethod]
		public void TestLists() {
			// round

			var emptyRoundList = Parser.ParseString("()");
			Assert.IsInstanceOfType(emptyRoundList, typeof(ListExpression));

			var asList = emptyRoundList as ListExpression;
			Assert.AreEqual(asList.Type, ListBracketsType.Round);
			Assert.AreEqual(asList.Contents.Count, 0);

			// square

			var emptySquareList = Parser.ParseString("[]");
			Assert.IsInstanceOfType(emptyRoundList, typeof(ListExpression));

			asList = emptySquareList as ListExpression;
			Assert.AreEqual(asList.Type, ListBracketsType.Square);
			Assert.AreEqual(asList.Contents.Count, 0);

			// curly

			var emptyCurlyList = Parser.ParseString("{}");
			Assert.IsInstanceOfType(emptyRoundList, typeof(ListExpression));

			asList = emptyCurlyList as ListExpression;
			Assert.AreEqual(asList.Type, ListBracketsType.Curly);
			Assert.AreEqual(asList.Contents.Count, 0);
		}

		[TestMethod]
		public void TestLiterals() {
			// keyword

			var literal = Parser.ParseString("literal");
			Assert.IsInstanceOfType(literal, typeof(LiteralExpression));

			var asLiteral = literal as LiteralExpression;
			Assert.AreEqual(asLiteral.Type, LiteralType.Keyword);
			Assert.AreEqual(asLiteral.Value, "literal");

			// string

			var s = Parser.ParseString("\"String with escape sequences and white spaces \\n \\\" \"");
			Assert.IsInstanceOfType(literal, typeof(LiteralExpression));

			asLiteral = s as LiteralExpression;
			Assert.AreEqual(asLiteral.Type, LiteralType.String);
			Assert.AreEqual(asLiteral.Value, "String with escape sequences and white spaces \n \" ");
		}

		private static void AssertList(ListExpression l, ListBracketsType type, int contentsCount) {
			Assert.AreNotEqual(l, null);
			Assert.AreEqual(l.Type, type);
			Assert.AreEqual(l.Contents.Count, contentsCount);
		}

		private static void AssertLiteral(LiteralExpression l, LiteralType type, string value) {
			Assert.AreNotEqual(l, null);
			Assert.AreEqual(l.Type, type);
			Assert.AreEqual(l.Value, value);
		}

		[TestMethod]
		public void TestCombination() {
			var x = Parser.ParseString("(node {key (value \"1\")} [(child) (child 2)])");
			var l = x as ListExpression;
			AssertList(l, ListBracketsType.Round, 3);

			var name = l.Contents[0];
			AssertLiteral(name as LiteralExpression, LiteralType.Keyword, "node");

			var attrs = l.Contents[1];
			var attrsList = attrs as ListExpression;
			AssertList(attrsList, ListBracketsType.Curly, 2);

			var children = l.Contents[2];
			var childrenList = children as ListExpression;
			AssertList(childrenList, ListBracketsType.Square, 2);

			var attrKey = attrsList.Contents[0];
			AssertLiteral(attrKey as LiteralExpression, LiteralType.Keyword, "key");

			var attrValue = attrsList.Contents[1];
			var attrValueList = attrValue as ListExpression;
			AssertList(attrValueList, ListBracketsType.Round, 2);
			AssertLiteral(attrValueList.Contents[0] as LiteralExpression, LiteralType.Keyword, "value");
			AssertLiteral(attrValueList.Contents[1] as LiteralExpression, LiteralType.String, "1");

			var child1 = childrenList.Contents[0];
			var child1List = child1 as ListExpression;
			AssertList(child1List, ListBracketsType.Round, 1);
			AssertLiteral(child1List.Contents[0] as LiteralExpression, LiteralType.Keyword, "child");

			var child2 = childrenList.Contents[1];
			var child2List = child2 as ListExpression;
			AssertList(child2List, ListBracketsType.Round, 2);
			AssertLiteral(child2List.Contents[0] as LiteralExpression, LiteralType.Keyword, "child");
			AssertLiteral(child2List.Contents[1] as LiteralExpression, LiteralType.Keyword, "2");
		}

		[TestMethod]
		public void TestEscapeSequences() {
			var x = Parser.ParseString("\"\\a \\b \\f \\r \\t \\v \"");
			AssertLiteral(x as LiteralExpression, LiteralType.String, "\a \b \f \r \t \v ");
		}

		[TestMethod]
		public void TestExceptions() {
			try {
				Parser.ParseString("(x y z");
			} catch (Exception e) {
				Assert.AreEqual(e.GetType(), typeof(InvalidDataException));
				Assert.AreEqual(e.Message, "Unexpected EOF while parsing list expression.");
			}

			try {
				Parser.ParseString("\"x y z");
			} catch (Exception e) {
				Assert.AreEqual(e.GetType(), typeof(InvalidDataException));
				Assert.AreEqual(e.Message, "Unexpected EOF while parsing string expression.");
			}

			try {
				Parser.ParseString("\" \\x \"");
			} catch (Exception e) {
				Assert.AreEqual(e.GetType(), typeof(InvalidDataException));
				Assert.AreEqual(e.Message, "Unknown escape sequence within string: \\x");
			}
		}
	}
}
