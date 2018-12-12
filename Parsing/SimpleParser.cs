using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using sdf.Parsing.SExpression;

namespace sdf.Parsing {
	/// <summary>
	///     Simple SDF parser.
	///     Uses <c>SExpression.Parser</c> to parse data into intermediate S-Expression and then builds <c>SDF</c>
	///     representation from it.
	/// </summary>
	public sealed class SimpleParser {
		/// <summary>
		///     Parse SDF from a file with given name.
		/// </summary>
		/// <param name="filename">Name of a file to read SDF from.</param>
		/// <returns>Parsed SDF.</returns>
		public static SDF Parse([NotNull] string filename) {
			var root = Parser.Parse(filename);
			return Build(root);
		}

		internal static SDF Build(Expression e) {
			if (e is LiteralExpression) {
				return BuildLiteral((LiteralExpression) e);
			}

			return BuildNode((ListExpression) e);
		}

		private static Node BuildNode(ListExpression e) {
			if (e.Type != ListBracketsType.Round) {
				throw new InvalidDataException("Syntax error: () list expected while building a Node.");
			}

			if (e.Contents.Count < 1 || e.Contents.Count > 3) {
				throw new InvalidDataException("Syntax error: Node's () list must contain from 1 to 3 element within.");
			}

			string name;
			var attributes = new Dictionary<string, SDF>();
			var children = new List<SDF>();

			// name

			var first = e.Contents[0];
			var literalFirst = first as LiteralExpression;
			if (literalFirst == null || literalFirst.Type != LiteralType.Keyword) {
				throw new InvalidDataException("Syntax error: Node's name must be a keyword.");
			}

			name = literalFirst.Value; // TODO: check name matches regexp

			// attributes / children

			if (e.Contents.Count > 1) {
				var second = e.Contents[1];
				var listSecond = second as ListExpression;

				if (e.Contents.Count > 2) {
					var third = e.Contents[2];
					// if three expressions, expect name, then {}, then [] or (anything but {})
					BuildAttributes(ref attributes, listSecond);
					BuildChildren(ref children, third);
				} else {
					// if two expressions, expect name, then {} or [] or (anything but {})
					if (listSecond == null || listSecond.Type != ListBracketsType.Curly) {
						BuildChildren(ref children, second);
					} else {
						BuildAttributes(ref attributes, listSecond);
					}
				}
			}

			return new Node(name, attributes, children);
		}

		private static void BuildAttributes(ref Dictionary<string, SDF> attributes, ListExpression list) {
			if (list.Type != ListBracketsType.Curly) {
				throw new InvalidDataException("Syntax error: {} list excepted while building Node's attributes.");
			}

			LiteralExpression key = null;
			var odd = true;
			foreach (var expr in list.Contents) {
				if (odd) {
					key = expr as LiteralExpression;
					if (key == null || key.Type == LiteralType.String) {
						throw new InvalidDataException("Syntax error: attribute name must be a keyword.");
					}
				} else {
					attributes.Add(key.Value, Build(expr));
				}

				odd = !odd;
			}
		}

		private static void BuildChildren(ref List<SDF> children, Expression e) {
			var list = e as ListExpression;
			if (list == null) {
				// literal
				children.Add(Build(e));
				return;
			}

			// list
			if (list.Type == ListBracketsType.Curly) {
				throw new InvalidDataException("Syntax error: Node's child or children cannot be represented as {} list.");
			}

			// single node
			if (list.Type == ListBracketsType.Round) {
				children.Add(Build(list));
				return;
			}

			// list of children
			children.AddRange(list.Contents.Select(Build));
		}

		private static SDF BuildLiteral(LiteralExpression e) {
			if (e.Type == LiteralType.String) {
				return new StringLiteral(e.Value);
			}

			var lowercased = e.Value.ToLower();

			if (lowercased == "null") {
				return new NullLiteral();
			}

			if (lowercased == "true" || lowercased == "false") {
				return new BooleanLiteral(lowercased == "true");
			}

			var index = lowercased.IndexOf('.');
			if (index == -1) {
				return new NumberLiteral(long.Parse(lowercased), 0);
			}

			return new NumberLiteral(long.Parse(lowercased.Substring(0, index)), long.Parse(lowercased.Substring(index + 1)));
		}
	}
}