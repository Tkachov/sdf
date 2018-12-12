using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using sdf;
using sdf.Parsing;
using sdf.Parsing.SExpression;

namespace Tests {
	[TestClass]
	public class MatchingOperationsUnitTest {
		[TestMethod]
		public void TestAbsoluteAndRelativePaths() {
			var s = TestHelper.ParseString("(node {attr (node 1)} [3.7 (subnode 4) (node 2)])");
			var n = s as Node;
			var matches = s.Find("/node");
			Assert.AreEqual(1, matches.Count);
			Assert.AreEqual(s, matches[0].Value);
			TestHelper.AssertNodeAndFirstNumberLiteralChild(n, "node", 1, 3, 3, 7);

			matches = s.Find("node");
			Assert.AreEqual(3, matches.Count);
			Assert.AreEqual(s, matches[0].Value);
			Assert.AreEqual(n.Children[2] as Node, matches[1].Value);
			Assert.AreEqual(n.Attributes["attr"] as Node, matches[2].Value);
			Assert.AreEqual("/node/node#2", matches[1].Path);
			Assert.AreEqual(matches[0], matches[1].Parent);
			TestHelper.AssertNodeAndFirstNumberLiteralChild(matches[1].Value as Node, "node", 0, 1, 2, 0);
			Assert.AreEqual("/node/@attr", matches[2].Path);
			Assert.AreEqual(matches[0], matches[2].Parent);
			TestHelper.AssertNodeAndFirstNumberLiteralChild(matches[2].Value as Node, "node", 0, 1, 1, 0);

			matches = s.Find("/node/@attr");
			Assert.AreEqual(1, matches.Count);
			Assert.AreEqual(n.Attributes["attr"] as Node, matches[0].Value);

			matches = s.Find("node/^number");
			Assert.AreEqual(3, matches.Count);
			Assert.AreEqual("/node/#0", matches[0].Path);
			TestHelper.AssertNumberLiteral(matches[0].Value as NumberLiteral, 3, 7);
			Assert.AreEqual("/node/node#2/#0", matches[1].Path);
			TestHelper.AssertNumberLiteral(matches[1].Value as NumberLiteral, 2, 0);
			Assert.AreEqual("/node/@attr/#0", matches[2].Path);
			TestHelper.AssertNumberLiteral(matches[2].Value as NumberLiteral, 1, 0);

			matches = s.Find("node/#1");
			Assert.AreEqual(1, matches.Count);
			Assert.AreEqual("/node/subnode#1", matches[0].Path);
			TestHelper.AssertNodeAndFirstNumberLiteralChild(matches[0].Value as Node, "subnode", 0, 1, 4, 0);

			matches = s.Find("^node");
			Assert.AreEqual(4, matches.Count);

			matches = s.Find("[has_child(subnode)]");
			Assert.AreEqual(1, matches.Count);
			Assert.AreEqual(s, matches[0].Value);

			matches = s.Find("[has_attr(attr)]");
			Assert.AreEqual(1, matches.Count);
			Assert.AreEqual(s, matches[0].Value);
		}
		
		[TestMethod]
		public void TestReplace() {
			var s = TestHelper.ParseString("(node {attr (node) attr2 -2} [(node) -1 (node)])");
			var n = s as Node;
			var matches = s.Find("/+/node");
			Assert.AreEqual(3, matches.Count);

			var replaceWith = new NumberLiteral(0, 0);
			var s2 = s.Replace("/+/node", replaceWith);
			Assert.AreEqual(s, s2);
			TestHelper.AssertNumberLiteral(n.Attributes["attr"] as NumberLiteral, 0, 0);
			Assert.AreNotEqual(replaceWith, n.Attributes["attr"]);
			TestHelper.AssertNumberLiteral(n.Attributes["attr2"] as NumberLiteral, -2, 0);
			TestHelper.AssertNumberLiteral(n.Children[0] as NumberLiteral, 0, 0);
			Assert.AreNotEqual(replaceWith, n.Children[0]);
			TestHelper.AssertNumberLiteral(n.Children[1] as NumberLiteral, -1, 0);
			TestHelper.AssertNumberLiteral(n.Children[2] as NumberLiteral, 0, 0);
			Assert.AreNotEqual(replaceWith, n.Children[2]);

			var s3 = s2.Replace("^number[<=0]", new NullLiteral());
			Assert.AreEqual(s, s3);
			Assert.AreEqual(typeof(NullLiteral), n.Attributes["attr"].GetType());
			Assert.AreEqual(typeof(NullLiteral), n.Attributes["attr2"].GetType());
			Assert.AreEqual(typeof(NullLiteral), n.Children[0].GetType());
			Assert.AreEqual(typeof(NullLiteral), n.Children[1].GetType());
			Assert.AreEqual(typeof(NullLiteral), n.Children[2].GetType());

			replaceWith = new NumberLiteral(1337, 0);
			var s4 = s3.Replace("*", replaceWith);
			Assert.AreEqual(replaceWith, s4);
		}

		[TestMethod]
		public void TestAdding() {
			var n = new Node("node", new Dictionary<string, SDF>(), new List<SDF>());
			var matches = n.Find("/[has_child(subnode)]");
			Assert.AreEqual(0, matches.Count);

			n.AddChild("/", new Node("subnode", new Dictionary<string, SDF>(), new List<SDF>()));
			matches = n.Find("/[has_child(subnode)]");
			Assert.AreEqual(1, matches.Count);
			Assert.AreEqual(n, matches[0].Value);

			matches = n.Find("/[has_attr(attr)]");
			Assert.AreEqual(0, matches.Count);

			n.AddAttribute("/", "attr", new BooleanLiteral(true));
			matches = n.Find("/[has_attr(attr)]");
			Assert.AreEqual(1, matches.Count);
			Assert.AreEqual(n, matches[0].Value);

			matches = n.Find("[@attr=true]");
			Assert.AreEqual(1, matches.Count);
			Assert.AreEqual(n, matches[0].Value);

			matches = n.Find("[@attr!=false]");
			Assert.AreEqual(1, matches.Count);
			Assert.AreEqual(n, matches[0].Value);
		}

		[TestMethod]
		public void TestInsertsAndRemoves() {
			var s = TestHelper.ParseString("(node [1 50 2 60 3 70])");
			var n = s as Node;
			Assert.AreEqual(6, n.Children.Count);

			s.InsertAfter("[>=10]", new StringLiteral("lemons"));
			Assert.AreEqual(9, n.Children.Count);

			s.InsertBefore("[<10]", new StringLiteral("stage"));
			Assert.AreEqual(12, n.Children.Count);

			s.InsertAt("/", 4, new StringLiteral(","));
			s.InsertAt("/", 9, new StringLiteral(","));
			Assert.AreEqual(14, n.Children.Count);

			var s2 = s.Remove("[~=\"e\"]");
			Assert.AreEqual(s, s2);
			Assert.AreEqual(8, n.Children.Count);

			s2 = s.Remove("^string");
			Assert.AreEqual(s, s2);
			Assert.AreEqual(6, n.Children.Count);

			s2 = s.Remove("*");
			Assert.AreEqual(null, s2);
		}

		[TestMethod]
		public void TestAttributeRemoves() {
			var s = TestHelper.ParseString("(n {a (av {ava 2} [(avc 3)])})");
			var n = s as Node;
			TestHelper.AssertNode(n, "n", 1, 0);
			TestHelper.AssertNode(n.Attributes["a"] as Node, "av", 1, 1);

			var matches = s.Find("[attr_has_child(@a, avc)]");
			Assert.AreEqual(1, matches.Count);

			s.Remove("avc@0");
			matches = s.Find("[attr_has_child(@a, avc)]");
			Assert.AreEqual(0, matches.Count);

			matches = s.Find("[attr_has_attr(@a, ava)]");
			Assert.AreEqual(1, matches.Count);

			s.Remove("[=2]");
			matches = s.Find("[attr_has_attr(@a, ava)]");
			Assert.AreEqual(0, matches.Count);
		}

		[TestMethod]
		public void TestSomeSearches() {
			var s = TestHelper.ParseString("(n [true false null null false null true true null])");
			var n = s as Node;
			TestHelper.AssertNode(n, "n", 0, 9);

			var matches = s.Find("^bool");
			Assert.AreEqual(5, matches.Count);

			matches = s.Find("^boolean");
			Assert.AreEqual(5, matches.Count);

			matches = s.Find("^null");
			Assert.AreEqual(4, matches.Count);

			matches = s.Find("[=null]");
			Assert.AreEqual(4, matches.Count);

			matches = s.Find("[!=null]");
			Assert.AreEqual(0, matches.Count);

			var s2 = TestHelper.ParseString("(node {a 3} [4 4 5 6])");
			s.InsertAt("/", 0, s2);

			matches = s.Find("[>3]");
			Assert.AreEqual(4, matches.Count);

			matches = s.Find("[!=4]");
			Assert.AreEqual(3, matches.Count);

			s = TestHelper.ParseString("(n [\"abba\" \"abab\" \"baba\" \"baab\" \"not a case\"])");

			matches = s.Find("[=\"abba\"]");
			Assert.AreEqual(1, matches.Count);

			matches = s.Find("[!=\"abba\"]");
			Assert.AreEqual(4, matches.Count);

			matches = s.Find("[~=\"ab\"]");
			Assert.AreEqual(4, matches.Count);

			matches = s.Find("[!~=\"ab\"]");
			Assert.AreEqual(1, matches.Count);

			matches = s.Find("[^=\"ab\"]");
			Assert.AreEqual(2, matches.Count);

			matches = s.Find("[!^=\"ab\"]");
			Assert.AreEqual(3, matches.Count);

			matches = s.Find("[$=\"ab\"]");
			Assert.AreEqual(2, matches.Count);

			matches = s.Find("[!$=\"ab\"]");
			Assert.AreEqual(3, matches.Count);

			matches = s.Find("[^=\"not\"]");
			Assert.AreEqual(1, matches.Count);

			matches = s.Find("[!^=\"not\"]");
			Assert.AreEqual(4, matches.Count);

			matches = s.Find("[$=\"case\"]");
			Assert.AreEqual(1, matches.Count);

			matches = s.Find("[!$=\"case\"]");
			Assert.AreEqual(4, matches.Count);

			matches = s.Find("[~=\"a\"]");
			Assert.AreEqual(5, matches.Count);
		}

		[TestMethod]
		public void TestNodeAt() {
			var s = TestHelper.ParseString(@"
				(html [
					(head
						(title ""t"")
					)
					(body [
						(h1 ""header"")
						(p ""paragraph 1"")
						(p ""paragraph 2"")
						(img {src ""image.png""})
						(p ""paragraph 3"")
						(h1 ""next"")
						(p ""paragraph"")
					])
					(h1)
				])
			");

			var matches = s.Find("/+/h1#1");
			Assert.AreEqual(0, matches.Count);

			matches = s.Find("/+/h1@1");
			Assert.AreEqual(1, matches.Count);
			Assert.AreEqual("/html/body#1/h1#5", matches[0].Path);

			matches = s.Find("//+/h1");
			Assert.AreEqual(2, matches.Count);

			matches = s.Find("//*/h1");
			Assert.AreEqual(3, matches.Count);
		}

		[TestMethod]
		public void TestExceptions() {
			const string NOT_HERE = "Should've thrown an Exception instead of ending up here.";

			var d = TestHelper.ParseString("(node {a 0} [1 2])");

			try {
				d.AddAttribute("^number", "a", new NullLiteral());
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Cannot add an attribute to something but a Node.", e.Message);
			}

			try {
				d.AddAttribute("/", "a", new NullLiteral());
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Cannot add an attribute, because attribute with such name already exists.", e.Message);
			}

			try {
				d.AddChild("^number", new NullLiteral());
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Cannot add a child to something but a Node.", e.Message);
			}

			try {
				d.InsertAt("^number", 0, new NullLiteral());
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Cannot insert a child into something but a Node.", e.Message);
			}

			try {
				d.InsertAfter("/", new NullLiteral());
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Cannot add something next to root element.", e.Message);
			}

			try {
				d.InsertBefore("/", new NullLiteral());
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Cannot add something next to root element.", e.Message);
			}

			var n = d as Node;

			try {
				n.InsertAfterChild(new NullLiteral(), new NullLiteral());
				Assert.Fail(NOT_HERE);
			} catch (ArgumentException e) {
				Assert.AreEqual("Argument passed as <child> is not a child of Node, thus <value> cannot be inserted after it.", e.Message);
			}

			try {
				n.InsertBeforeChild(new NullLiteral(), new NullLiteral());
				Assert.Fail(NOT_HERE);
			} catch (ArgumentException e) {
				Assert.AreEqual("Argument passed as <child> is not a child of Node, thus <value> cannot be inserted before it.", e.Message);
			}			

			try {
				var s = TestHelper.ParseString("(n (n (n (n (n (n (n (n (n)))))))))");
				s.Find("////+*/*+/+/*/");
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Cannot have multiple arbitrary node hierarchy conditions (* or +) at the same hierarchy level.", e.Message);
			}

			try {
				d.Find("^unknown");
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Unknown type \"unknown\" passed in type condition.", e.Message);
			}

			try {
				d.Find("[^^5]");
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Invalid value condition: no known operators or predicates found.", e.Message);
			}

			try {
				d.Find("[undefined(5)]");
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Invalid value condition: no known operators or predicates found.", e.Message);
			}

			try {
				d.Find("[has_child]");
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Predicate in value condition does not have opening brace.", e.Message);
			}

			try {
				d.Find("[has_child(c]");
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Predicate in value condition does not have closing brace.", e.Message);
			}

			try {
				d.Find("[has_child(\"c\")]");
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Argument of a predicate in value condition is not a simple word.", e.Message);
			}

			try {
				d.Find("[has_child({})]");
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Argument of a predicate in value condition is not a simple word.", e.Message);
			}

			try {
				d.Find("[attr_has_child]");
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Predicate in value condition does not have opening brace.", e.Message);
			}

			try {
				d.Find("[attr_has_child(c]");
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Predicate in value condition does not have closing brace.", e.Message);
			}

			try {
				d.Find("[attr_has_child(a)]");
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Binary predicate in value condition does not have two arguments.", e.Message);
			}

			try {
				d.Find("[attr_has_child(a,b,c)]");
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Binary predicate in value condition does not have two arguments.", e.Message);
			}

			try {
				d.Find("[attr_has_child(\"c\",b)]");
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Argument of a predicate in value condition is not a simple word.", e.Message);
			}

			try {
				d.Find("[attr_has_child(a,{})]");
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Argument of a predicate in value condition is not a simple word.", e.Message);
			}

			try {
				d.Find("[attr_has_child(a,\"c\")]");
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Argument of a predicate in value condition is not a simple word.", e.Message);
			}

			try {
				d.Find("[attr_has_child({},b)]");
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Argument of a predicate in value condition is not a simple word.", e.Message);
			}

			try {
				d.Find("[attr_has_child(a,b)]");
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Invalid attribute name given (does not start with an @).", e.Message);
			}

			try {
				d.Find("[attr=5]");
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Invalid attribute name given (does not start with an @).", e.Message);
			}

			try {
				d.Find("[=(node)]");
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Argument of an operator in value condition is not a literal.", e.Message);
			}

			try {
				d.Find("[>=null]");
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Cannot apply a number operator in value condition to something but a number literal.", e.Message);
			}

			try {
				d.Find("[^=5]");
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Cannot apply a string operator in value condition to something but a string literal.", e.Message);
			}
		}
	}
}
