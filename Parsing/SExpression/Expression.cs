using System.Collections.Generic;
using JetBrains.Annotations;

namespace sdf.Parsing.SExpression {
	/// <summary>
	///     Enumeration of supported S-Expression list types:
	///     - round -- ();
	///     - square -- [];
	///     - curly -- {}.
	/// </summary>
	public enum ListBracketsType {
		/// <summary>	S-Expression list with round () brackets. </summary>
		Round,

		/// <summary>	S-Expression list with square [] brackets. </summary>
		Square,

		/// <summary>	S-Expression list with curly {} brackets. </summary>
		Curly
	}

	/// <summary>
	///     Base class for all S-Expressions.
	/// </summary>
	public abstract class Expression { }

	/// <inheritdoc />
	/// <summary>
	///     List S-Expression, which contains arbitrary amount of expressions inside.
	/// </summary>
	public sealed class ListExpression : Expression {
		internal List<Expression> Contents;
		internal ListBracketsType Type;

		/// <summary>
		///     Create new List S-Expression.
		/// </summary>
		/// <param name="t">List type (round, square or curly).</param>
		/// <param name="c">Expressions within list.</param>
		public ListExpression(ListBracketsType t, [NotNull] List<Expression> c) {
			Type = t;
			Contents = c;
		}
	}

	/// <summary>
	///     Enumeration of supported S-Expression literal types:
	///     - string -- sequence of characters within "", supports some escape-sequences;
	///     - keyword -- sequence of non-whitespace characters.
	/// </summary>
	public enum LiteralType {
		/// <summary>	String literal. </summary>
		String,

		/// <summary>	Keyword literal. </summary>
		Keyword
	}

	/// <inheritdoc />
	/// <summary>
	///     Literal S-Expression.
	/// </summary>
	public sealed class LiteralExpression : Expression {
		internal LiteralType Type;
		internal string Value;

		/// <summary>
		///     Create new Literal S-Expression.
		/// </summary>
		/// <param name="t">Literal type (string or keyword).</param>
		/// <param name="v">Literal string value.</param>
		public LiteralExpression(LiteralType t, [NotNull] string v) {
			Type = t;
			Value = v;
		}
	}
}