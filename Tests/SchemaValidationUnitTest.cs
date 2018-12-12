using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using sdf;
using sdf.Validating;

namespace Tests {
	[TestClass]
	public class SchemaValidationUnitTest {
		private static readonly string TEXT_OF_SCHEMA_OF_ALL_SCHEMAS = @"
			(schema {top-element (node-element {name ""schema"" type ""schema-type""})} [	
				(node-type {name ""schema-type"" children (list (one-of [
					(node-element {name ""node-type"" type ""node-type-type""})
					(node-element {name ""literal-type"" type ""literal-type-type""})
				]))} [
					(attribute {name ""top-element"" required true} (one-of [
						(node-element {name ""node-element"" type ""node-element-type""})
						(node-element {name ""literal-element"" type ""literal-element-type""})
						(node-element {name ""one-of"" type ""one-of-type""})
					]))
				])

				(node-type {name ""node-type-type"" children (list (node-element {name ""attribute"" type ""attribute-type""}))} [
					(attribute {name ""name"" required true} (literal-element {type ""string""}))
					(attribute {name ""children"" required false} (one-of [
						(node-element {name ""node-element"" type ""node-element-type""})
						(node-element {name ""literal-element"" type ""literal-element-type""})
						(node-element {name ""sequence"" type ""sequence-type""})
						(node-element {name ""one-of"" type ""one-of-type""})
						(node-element {name ""list"" type ""list-type""})
					]))
					(attribute {name ""conditions"" required false} (one-of [
						(node-element {name ""condition"" type ""condition-type""})
						(node-element {name ""one-of-conditions"" type ""list-of-conditions-type""})
						(node-element {name ""all-of-conditions"" type ""list-of-conditions-type""})
					]))
				])
				(node-type {name ""literal-type-type""} [
					(attribute {name ""name"" required true} (literal-element {type ""string""}))
					(attribute {name ""base-type"" required true} (literal-element {type ""builtin-literal-type-name""}))
					(attribute {name ""conditions"" required false} (one-of [
						(node-element {name ""condition"" type ""condition-type""})
						(node-element {name ""one-of-conditions"" type ""list-of-conditions-type""})
						(node-element {name ""all-of-conditions"" type ""list-of-conditions-type""})
					]))
				])

				(literal-type {name ""builtin-literal-type-name"" base-type ""string"" conditions (one-of-conditions [
					(condition ""=\""null\"""")
					(condition ""=\""bool\"""")
					(condition ""=\""boolean\"""")
					(condition ""=\""number\"""")
					(condition ""=\""string\"""")
				])})

				(node-type {name ""node-element-type""} [
					(attribute {name ""name"" required true} (literal-element {type ""string""}))
					(attribute {name ""type"" required false} (literal-element {type ""string""}))
				])
				(node-type {name ""literal-element-type""} [
					(attribute {name ""type"" required true} (literal-element {type ""string""}))
				])
				(node-type {name ""sequence-type"" children (list (one-of [
					(node-element {name ""node-element"" type ""node-element-type""})
					(node-element {name ""literal-element"" type ""literal-element-type""})
					(node-element {name ""one-of"" type ""one-of-type""})
				]))})
				(node-type {name ""one-of-type"" children (list (one-of [
					(node-element {name ""node-element"" type ""node-element-type""})
					(node-element {name ""literal-element"" type ""literal-element-type""})
				]))})
				(node-type {name ""list-type"" children (one-of [
					(node-element {name ""node-element"" type ""node-element-type""})
					(node-element {name ""literal-element"" type ""literal-element-type""})
					(node-element {name ""one-of"" type ""one-of-type""})
				])} [
					(attribute {name ""min"" required false} (literal-element {type ""positive-number""}))
					(attribute {name ""max"" required false} (literal-element {type ""positive-number""}))
				])

				(literal-type {name ""positive-number"" base-type ""number"" conditions (condition "">=0"")})

				(node-type {name ""condition-type"" children (literal-element {type ""string""})})
				(node-type {name ""list-of-conditions-type"" children (list {min 1} (one-of [
					(node-element {name ""condition"" type ""condition-type""})
					(node-element {name ""one-of-conditions"" type ""list-of-conditions-type""})
					(node-element {name ""all-of-conditions"" type ""list-of-conditions-type""})
				]))})

				(node-type {name ""attribute-type"" children (one-of [
					(node-element {name ""node-element"" type ""node-element-type""})
					(node-element {name ""literal-element"" type ""literal-element-type""})
					(node-element {name ""one-of"" type ""one-of-type""})
				])} [
					(attribute {name ""name"" required true} (literal-element {type ""string""}))
					(attribute {name ""required"" required true} (literal-element {type ""bool""}))
				])
			])
		";

		private static readonly SDF SDF_OF_SCHEMA_OF_ALL_SCHEMAS = TestHelper.ParseString(TEXT_OF_SCHEMA_OF_ALL_SCHEMAS);
		private static readonly Schema SCHEMA_OF_ALL_SCHEMAS = new Schema(SDF_OF_SCHEMA_OF_ALL_SCHEMAS);

		[TestMethod]
		public void SanityCheck() {
			Assert.IsTrue(SCHEMA_OF_ALL_SCHEMAS.Validate(SDF_OF_SCHEMA_OF_ALL_SCHEMAS));
		}

		[TestMethod]
		public void TestStreamParsingValidation() {
			var TEXT_OF_NODE_SCHEMA = @"
				(schema {top-element (node-element {name ""node"" type ""node-type""})} [	
					(node-type {name ""node-type"" children (list {min 1 max 5} (one-of [
						(node-element {name ""node"" type ""node-type""})
						(node-element {name ""subnode"" type ""subnode-type""})
						(literal-element {type ""literal-subnode-type""})
					]))} [
						(attribute {name ""attr"" required true} (one-of [
							(node-element {name ""attr-node""})
							(literal-element {type ""null""})
						]))
					])

					(node-type {name ""subnode-type"" children 
						(literal-element {type ""literal-subnode-type""})
					})

					(literal-type {name ""literal-subnode-type"" base-type ""number"" conditions 
						(one-of-conditions [
							(all-of-conditions [
								(condition "">0"")
								(condition ""<10"")
							])
							(condition ""=1337"")
						])
					})
				])
			";
			var SDF_OF_NODE_SCHEMA = TestHelper.ParseString(TEXT_OF_NODE_SCHEMA);
			Assert.IsTrue(SCHEMA_OF_ALL_SCHEMAS.Validate(SDF_OF_NODE_SCHEMA));

			var NODE_SCHEMA = new Schema(SDF_OF_NODE_SCHEMA);
			var VALID_SDF_TEXT = @"
				(node
					{
						attr
						(attr-node {attr-node-attr 1} (attr-node-children 2))
					} 
					[
						1 (subnode 2) 3 (subnode 3.14)
						(node {attr null} [(subnode 5)])
					])
			";
			var VALID_SDF = TestHelper.ParseString(VALID_SDF_TEXT);
			Assert.IsTrue(NODE_SCHEMA.Validate(VALID_SDF));

			var STREAMING_VALID_SDF = TestHelper.StreamParseString(VALID_SDF_TEXT, NODE_SCHEMA);
			TestHelper.AssertAreDeeplyEqual(VALID_SDF, STREAMING_VALID_SDF);

			const string NOT_HERE = "Should've thrown an Exception instead of ending up here.";

			try {
				TestHelper.StreamParseString(@"
					(node
						{
							attr
							(attr-node {attr-node-attr 1} (attr-node-children 2))
						} 
						[
							1 (subnode 2) 3 (subnode 3.14)
							(node {attr null} [(subnode 5)]) 6
						])
				", NODE_SCHEMA);
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Document already does not match the schema:\nMore than maximum (5) amount of elements in a list.", e.Message);
			}

			try {
				TestHelper.StreamParseString(@"
					(node
						{
							attr
							(attr-node {attr-node-attr 1} (attr-node-children 2))
						} 
						[
							1 (subnode 2) 3 (subnode 3.14)
							(node {attr null})
						])
				", NODE_SCHEMA);
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("File is read completely, but document does not match the schema:\nElement \'/node/node#4\' does not match neither of allowed options:\n\tLess than minimal (1) amount of elements in a list.\n\tElement \'/node/node#4\' must be a (subnode) node.\n\tElement \'/node/node#4\' must be literal.\n", e.Message);
			}
		}

		[TestMethod]
		public void TestHtmlExample() {
			var schema = new Schema(TestHelper.GetSimpleHTMLSchema());

			var document = TestHelper.StreamParseString(@"
				(html [
					(head)
					(body [
						(p ""string"") 
						(img {src ""file.png""}) 
						(img {src ""file2.png"" title ""file 2""}) 
						(p ""other string"") 
						(img {title ""other order"" src ""file3.png""})
					])
				])
			", schema);

			Assert.IsTrue(schema.Validate(document));
		}

		[TestMethod]
		public void TestFilenameConstructor() {
			var fn = TestHelper.MakeTempFile(@"(schema {top-element (literal-element {type ""number""})})");
			var s = new Schema(fn);			
			TestHelper.DeleteTempFile(fn);

			Assert.IsTrue(s.Validate(new NumberLiteral(5, 0)));
			Assert.IsTrue(!s.Validate(new NullLiteral()));
		}

		[TestMethod]
		public void TestExceptions() {
			const string NOT_HERE = "Should've thrown an Exception instead of ending up here.";

			var d = TestHelper.ParseString("(node {a 0} [1 2])");

			try {
				new Schema(d);
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Schema must be a (schema) node.", e.Message);
			}

			try {
				new Schema(TestHelper.ParseString(@"(schema {top-element (literal-element {type ""number""})} (invalid-node-type))"));
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Invalid schema type description.", e.Message);
			}

			try {
				new Schema(TestHelper.ParseString(@"(schema {top-element (literal-element {type ""type""})})"));
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Literal element references an undeclared type 'ud:type'", e.Message);
			}

			try {
				new Schema(TestHelper.ParseString(@"
					(schema 
							{top-element (literal-element {type ""type""})}
							(node-type {name ""type""}))
				"));
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Literal element references non-literal type.", e.Message);
			}

			try {
				new Schema(TestHelper.ParseString(@"
					(schema 
							{top-element (node-element {name ""n"" type ""type""})})
				"));
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Node element references an undeclared type 'ud:type'", e.Message);
			}

			try {
				new Schema(TestHelper.ParseString(@"
					(schema 
							{top-element (node-element {name ""n"" type ""type""})}
							(literal-type {name ""type"" base-type ""number""}))
				"));
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Node element references non-node type.", e.Message);
			}

			try {
				new Schema(TestHelper.ParseString(@"(schema {top-element 1})"));
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Schema element description must be a node.", e.Message);
			}

			try {
				new Schema(TestHelper.ParseString(@"(schema {top-element (list [1 2])})"));
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Schema list description must have exactly one element description.", e.Message);
			}

			try {
				new Schema(TestHelper.ParseString(@"(schema {top-element (unknown)})"));
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Invalid schema element description.", e.Message);
			}

			try {
				new Schema(TestHelper.ParseString(@"(schema {top-element (literal-element {type ""type""})} 1)"));
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Schema type description must be a node.", e.Message);
			}

			try {
				new Schema(TestHelper.ParseString(@"
					(schema 
							{top-element (literal-element {type ""type""})}
							(literal-type {name ""type"" conditions 1}))
				"));
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Schema condition description must be a node.", e.Message);
			}

			try {
				new Schema(TestHelper.ParseString(@"
					(schema 
							{top-element (literal-element {type ""type""})}
							(literal-type {name ""type"" conditions (condition [1 2])}))
				"));
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Schema condition description must have exactly one value.", e.Message);
			}

			try {
				new Schema(TestHelper.ParseString(@"
					(schema 
							{top-element (literal-element {type ""type""})}
							(literal-type {name ""type"" conditions (condition 1)}))
				"));
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Schema condition description must be a string.", e.Message);
			}

			try {
				new Schema(TestHelper.ParseString(@"
					(schema 
							{top-element (literal-element {type ""type""})}
							(literal-type {name ""type"" conditions (one-of-conditions)}))
				"));
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Schema one-of-conditions description must have at least one value.", e.Message);
			}

			try {
				new Schema(TestHelper.ParseString(@"
					(schema 
							{top-element (literal-element {type ""type""})}
							(literal-type {name ""type"" conditions (all-of-conditions 1)}))
				"));
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("All of schema all-of-conditions description values must be nodes.", e.Message);
			}

			try {
				new Schema(TestHelper.ParseString(@"
					(schema 
							{top-element (literal-element {type ""type""})}
							(literal-type {name ""type"" conditions (c)}))
				"));
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Invalid schema condition description.", e.Message);
			}

			try {
				new Schema(TestHelper.ParseString(@"
					(schema 
							{top-element (literal-element {type ""number""})}
							(node-type {name ""type""} [
								(not-attribute)
							]))
				"));
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Schema attribute description must be an (attribute) node.", e.Message);
			}

			try {
				new Schema(TestHelper.ParseString(@"
					(schema 
							{top-element (literal-element {type ""number""})}
							(node-type {name ""type""} [
								(attribute [1 2])
							]))
				"));
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Schema attribute description must have exactly one element description.", e.Message);
			}

			try {
				new Schema(TestHelper.ParseString(@"
					(schema 
							{top-element (literal-element {type ""number""})}
							(node-type {name ""type""} [
								(attribute (literal-element {type ""number""}))
							]))
				"));
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Attribute 'name' expected, but not found.", e.Message);
			}

			try {
				new Schema(TestHelper.ParseString(@"
					(schema 
							{top-element (literal-element {type ""number""})}
							(node-type {name ""type""} [
								(attribute {name true} (literal-element {type ""number""}))
							]))
				"));
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Attribute 'name' expected to be a string.", e.Message);
			}

			try {
				new Schema(TestHelper.ParseString(@"
					(schema 
							{top-element (literal-element {type ""number""})}
							(node-type {name ""type""} [
								(attribute {name ""n""} (literal-element {type ""number""}))
							]))
				"));
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Attribute 'required' expected, but not found.", e.Message);
			}

			try {
				new Schema(TestHelper.ParseString(@"
					(schema 
							{top-element (literal-element {type ""number""})}
							(node-type {name ""type""} [
								(attribute {name ""n"" required 1} (literal-element {type ""number""}))
							]))
				"));
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Attribute 'required' expected to be a boolean value.", e.Message);
			}

			try {
				new Schema(TestHelper.ParseString(@"
					(schema 
							{top-element (list {min true} (literal-element {type ""number""}))})
				"));
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Attribute 'min' expected to be a number.", e.Message);
			}

			try {
				new Schema(TestHelper.ParseString(@"
					(schema 
							{top-element (list {min 1.1} (literal-element {type ""number""}))})
				"));
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Attribute 'min' expected to be an integer.", e.Message);
			}

			// schema type exceptions
			
			try {
				new Schema(TestHelper.ParseString(@"
					(schema 
							{top-element (literal-element {type ""ud""})}
							(literal-type {name ""ud"" base-type ""unknown""}))
				"));
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Unknown built-in type 'unknown' used in literal-type description.", e.Message);
			}

			try {
				new Schema(TestHelper.ParseString(@"
					(schema 
							{top-element (literal-element {type ""ud""})}
							(literal-type {name ""ud"" base-type ""unknown"" conditions 
								(condition ""=5]/[=1"")}))
				"));
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Invalid condition '=5]/[=1'.", e.Message);
			}

			try {
				new Schema(TestHelper.ParseString(@"
					(schema 
							{top-element (literal-element {type ""ud""})}
							(literal-type {name ""ud"" base-type ""unknown"" conditions 
								(condition ""]"")}))
				"));
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Invalid condition ']'.", e.Message);
			}

			// validate partial

			var schema = new Schema(TestHelper.GetSimpleHTMLSchema());

			try {
				TestHelper.StreamParseString(@"
					(html [
						(head)
						(body [
							(p ""string"") 
							(img {src ""file.png""}) 
							(img {src ""file2.png"" title ""file 2""})
							(not-one-of-permitted)
							(p ""other string"") 
							(img {title ""other order"" src ""file3.png""})
						])
					])
				", schema);
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Document already does not match the schema:\nElement \'/html/body#1/not-one-of-permitted#3\' does not match neither of allowed options even partially:\n\tElement \'/html/body#1/not-one-of-permitted#3\' must be a (p) node.\n\tElement \'/html/body#1/not-one-of-permitted#3\' must be a (img) node.\n", e.Message);
			}

			try {
				TestHelper.StreamParseString(@"
					(html [
						(head)
					])
				", schema);
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("File is read completely, but document does not match the schema:\nA sequence of 2 elements expected, 1 element(s) found.", e.Message);
			}

			try {
				TestHelper.StreamParseString(@"
					(html [
						(head)
						(body)
						(fuck)
					])
				", schema);
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Document already does not match the schema:\nA sequence of 2 elements expected, more (3) elements found.", e.Message);
			}

			const string EXAMPLE = "(n {a 5})";
			var SDF_EXAMPLE = TestHelper.ParseString(EXAMPLE);

			try {
				var listSchema = new Schema(TestHelper.StreamParseString(@"
					(schema {top-element (node-element {name ""n"" type ""n-type""})} [
						(node-type {name ""n-type""} [
							(attribute {name ""a"" required true} (list {min 2} (literal-element {type ""number""})))
						])
					])
				"));
				Assert.IsTrue(!listSchema.Validate(SDF_EXAMPLE));

				TestHelper.StreamParseString(EXAMPLE, listSchema);
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("File is read completely, but document does not match the schema:\nLess than minimal (2) amount of elements in a list.", e.Message);
			}

			try {
				var listSchema = new Schema(TestHelper.StreamParseString(@"
					(schema {top-element (node-element {name ""n"" type ""n-type""})} [
						(node-type {name ""n-type""} [
							(attribute {name ""a"" required true} (list {max 0} (literal-element {type ""number""})))
						])
					])
				"));
				Assert.IsTrue(!listSchema.Validate(SDF_EXAMPLE));

				TestHelper.StreamParseString(EXAMPLE, listSchema);
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Document already does not match the schema:\nMore than maximum (0) amount of elements in a list.", e.Message);
			}

			try {
				var listSchema = new Schema(TestHelper.StreamParseString(@"
					(schema {top-element (node-element {name ""n"" type ""n-type""})} [
						(node-type {name ""n-type""} [
							(attribute {name ""a"" required true} (sequence [
								(literal-element {type ""number""})
								(literal-element {type ""string""})
							]))
						])
					])
				"));
				Assert.IsTrue(!listSchema.Validate(SDF_EXAMPLE));

				TestHelper.StreamParseString(EXAMPLE, listSchema);
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("File is read completely, but document does not match the schema:\nA sequence of 2 elements expected, one element found.", e.Message);
			}

			try {
				var listSchema = new Schema(TestHelper.StreamParseString(@"
					(schema {top-element (node-element {name ""n"" type ""n-type""})} [
						(node-type {name ""n-type"" children (one-of [
								(literal-element {type ""null""})
								(literal-element {type ""string""})
							])
						})
					])
				"));
				var ex = "(n [null 5 null])";
				var sdf = TestHelper.ParseString(ex);
				Assert.IsTrue(!listSchema.Validate(sdf));

				TestHelper.StreamParseString(ex, listSchema);
				Assert.Fail(NOT_HERE);
			} catch (InvalidDataException e) {
				Assert.AreEqual("Document already does not match the schema:\nElement does not match neither of allowed options even partially.", e.Message);
			}

			{
				var listSchema = new Schema(TestHelper.StreamParseString(@"
					(schema {top-element (node-element {name ""n"" type ""n-type""})} [
						(node-type {name ""n-type"" children (one-of [
								(literal-element {type ""nll""})
								(literal-element {type ""str""})
								(literal-element {type ""bln""})
							])
						})

						(literal-type {name ""nll"" base-type ""null""})
						(literal-type {name ""str"" base-type ""string""})
						(literal-type {name ""bln"" base-type ""bool"" conditions (condition ""=true"")})
					])
				"));
				var ex = "(n null)";
				var sdf = TestHelper.ParseString(ex);
				Assert.IsTrue(listSchema.Validate(sdf));
				
				TestHelper.AssertAreDeeplyEqual(sdf, TestHelper.StreamParseString(ex, listSchema));

				Assert.IsTrue(!listSchema.Validate(TestHelper.ParseString("(n 5)")));
				Assert.IsTrue(!listSchema.Validate(TestHelper.ParseString("(n false)")));
			}

			try {
				var listSchema = new Schema(TestHelper.StreamParseString(@"
					(schema {top-element (node-element {name ""n"" type ""n-type""})} [
						(node-type {name ""n-type"" children 
							(node-element {name ""nd"" type ""nd""})
						})

						(node-type {name ""nd"" conditions (condition ""=true"")})
					])
				"));
				var ex = "(n (nd))";
				var sdf = TestHelper.ParseString(ex);
				Assert.IsTrue(!listSchema.Validate(sdf));

				TestHelper.AssertAreDeeplyEqual(sdf, TestHelper.StreamParseString(ex, listSchema));
			} catch (InvalidDataException e) {
				Assert.AreEqual("File is read completely, but document does not match the schema:\nElement \'/n/nd\' does not match \'=true\' condition.", e.Message);
			}

			try {
				var listSchema = new Schema(TestHelper.StreamParseString(@"
					(schema {top-element (node-element {name ""n"" type ""n-type""})} [
						(node-type {name ""n-type"" children 
							(node-element {name ""nd"" type ""nd""})
						})

						(node-type {name ""nd"" conditions (condition ""=true"") children (literal-element {type ""number""})})
					])
				"));
				var ex = "(n (nd))";
				var sdf = TestHelper.ParseString(ex);
				Assert.IsTrue(!listSchema.Validate(sdf));

				TestHelper.AssertAreDeeplyEqual(sdf, TestHelper.StreamParseString(ex, listSchema));
			} catch (InvalidDataException e) {
				Assert.AreEqual("File is read completely, but document does not match the schema:\nOne literal expected, multiple (or none) found.", e.Message);
			}

			try {
				var listSchema = new Schema(TestHelper.StreamParseString(@"
					(schema {top-element (node-element {name ""n"" type ""n-type""})} [
						(node-type {name ""n-type"" children 
							(node-element {name ""nd"" type ""nd""})
						})

						(node-type {name ""nd"" conditions (condition ""=true"")})
					])
				"));
				var ex = "(n [5 6])";
				var sdf = TestHelper.ParseString(ex);
				Assert.IsTrue(!listSchema.Validate(sdf));

				TestHelper.AssertAreDeeplyEqual(sdf, TestHelper.StreamParseString(ex, listSchema));
			} catch (InvalidDataException e) {
				Assert.AreEqual("Document already does not match the schema:\nOne node expected, multiple found.", e.Message);
			}

			try {
				var listSchema = new Schema(TestHelper.StreamParseString(@"
					(schema {top-element (node-element {name ""n"" type ""n-type""})} [
						(node-type {name ""n-type"" children 
							(node-element {name ""nd"" type ""nd""})
						})

						(node-type {name ""nd""} [(attribute {name ""n"" required true} (literal-element {type ""number""}))])
					])
				"));
				var ex = "(n (nd))";
				var sdf = TestHelper.ParseString(ex);
				Assert.IsTrue(!listSchema.Validate(sdf));

				TestHelper.AssertAreDeeplyEqual(sdf, TestHelper.StreamParseString(ex, listSchema));
			} catch (InvalidDataException e) {
				Assert.AreEqual("File is read completely, but document does not match the schema:\nRequired attribute \'n\' is missing on element \'/n/nd\'.", e.Message);
			}

			try {
				var listSchema = new Schema(TestHelper.StreamParseString(@"
					(schema {top-element (node-element {name ""n"" type ""n-type""})} [
						(node-type {name ""n-type"" children 
							(node-element {name ""nd"" type ""nd""})
						})

						(node-type {name ""nd"" conditions (all-of-conditions [(condition ""=true"") (condition ""=false"")])})
					])
				"));
				var ex = "(n (nd))";
				var sdf = TestHelper.ParseString(ex);
				Assert.IsTrue(!listSchema.Validate(sdf));

				TestHelper.AssertAreDeeplyEqual(sdf, TestHelper.StreamParseString(ex, listSchema));
			} catch (InvalidDataException e) {
				Assert.AreEqual("File is read completely, but document does not match the schema:\nOne of the conditions is not met:\n\tElement \'/n/nd\' does not match \'=true\' condition.", e.Message);
			}

			try {
				var listSchema = new Schema(TestHelper.StreamParseString(@"
					(schema {top-element (node-element {name ""n"" type ""n-type""})} [
						(node-type {name ""n-type"" children 
							(node-element {name ""nd"" type ""nd""})
						})

						(node-type {name ""nd"" conditions (one-of-conditions [(condition ""=true"") (condition ""=false"")])})
					])
				"));
				var ex = "(n (nd))";
				var sdf = TestHelper.ParseString(ex);
				Assert.IsTrue(!listSchema.Validate(sdf));

				TestHelper.AssertAreDeeplyEqual(sdf, TestHelper.StreamParseString(ex, listSchema));
			} catch (InvalidDataException e) {
				Assert.AreEqual("File is read completely, but document does not match the schema:\nNone of the following multiple conditions is not met:\n\tElement \'/n/nd\' does not match \'=true\' condition.\n\tElement \'/n/nd\' does not match \'=false\' condition.\n", e.Message);
			}

			{
				var listSchema = new Schema(TestHelper.StreamParseString(@"
					(schema {top-element (node-element {name ""n"" type ""n-type""})} [
						(node-type {name ""n-type""} [
							(attribute {name ""a"" required true} (sequence [
								(literal-element {type ""number""})
							]))
						])
					])
				"));
				var ex = "(n {a null})";
				var sdf = TestHelper.ParseString(ex);
				Assert.IsTrue(!listSchema.Validate(sdf));
			}

			{
				var listSchema = new Schema(TestHelper.StreamParseString(@"
					(schema {top-element (node-element {name ""n"" type ""n-type""})} [
						(node-type {name ""n-type"" children (sequence [
								(literal-element {type ""number""})
							])
						})
					])
				"));
				var ex = "(n [null])";
				var sdf = TestHelper.ParseString(ex);
				Assert.IsTrue(!listSchema.Validate(sdf));
			}
		}
	}
}
