using System.Collections.Generic;
using JetBrains.Annotations;

namespace sdf.Core.Parsing {
	public enum ListBracketsType {
		Round,
		Square,
		Curly
	}

	public abstract class Expression {}

	public class ListExpression: Expression {
		internal ListBracketsType Type;
		internal List<Expression> Contents;		

		public ListExpression(ListBracketsType t, [NotNull] List<Expression> c) {
			Type = t;
			Contents = c;
		}
	}

	public enum LiteralType {
		String,
		Keyword
	}

	public class LiteralExpression: Expression {
		internal LiteralType Type;
		internal string Value;

		public LiteralExpression(LiteralType t, [NotNull] string v) {
			Type = t;
			Value = v;
		}
	}
}