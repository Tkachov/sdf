using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using sdf.Core.Parsing;

namespace sdf.Core.Building {
	class Builder {
		public static SDF Build(Expression e) {
			if (e is LiteralExpression)
				return BuildLiteral((LiteralExpression) e);

			return BuildNode((ListExpression) e);
		}

		private static Node BuildNode(ListExpression e) {
			if (e.Type != ListBracketsType.Round)
				throw new InvalidDataException();

			if (e.Contents.Count < 1 || e.Contents.Count > 3)
				throw new InvalidDataException();

			string name;
			var attributes = new Dictionary<string, SDF>();
			var children = new List<SDF>();

			// name

			var first = e.Contents[0];
			var literalFirst = (first as LiteralExpression);
			if (literalFirst == null || literalFirst.Type != LiteralType.Keyword)
				throw new InvalidDataException();

			name = literalFirst.Value; // TODO: check name matches regexp

			// attributes / children

			if (e.Contents.Count > 1) {
				var second = e.Contents[1];
				var listSecond = (second as ListExpression);

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
			if (list.Type != ListBracketsType.Curly)
				throw new InvalidDataException();

			LiteralExpression key = null;
			var odd = true;
			foreach (var expr in list.Contents) {
				if (odd) {
					key = expr as LiteralExpression;
					if (key == null || key.Type == LiteralType.String)
						throw new InvalidDataException();
				} else {
					attributes.Add(key.Value, Build(expr));
				}
				odd = !odd;
			}
		}

		private static void BuildChildren(ref List<SDF> children, Expression e) {
			var list = (e as ListExpression);
			if (list == null) {
				// literal
				children.Add(Build(e));
				return;
			}

			// list
			if (list.Type == ListBracketsType.Curly)
				throw new InvalidDataException();

			// single node
			if (list.Type == ListBracketsType.Round) {
				children.Add(Build(list));
				return;
			}

			// list of children
			foreach (var expr in list.Contents) {
				children.Add(Build(expr));
			}			
		}

		private static SDF BuildLiteral(LiteralExpression e) {
			if (e.Type == LiteralType.String)
				return new StringLiteral(e.Value);
			
			var lowercased = e.Value.ToLower();

			if (lowercased == "null")
				return new NullLiteral();

			if (lowercased == "true" || lowercased == "false")
				return new BooleanLiteral(lowercased == "true");

			// TODO: build a number literal
			return new NumberLiteral(0, 0);
		}
	}
}
