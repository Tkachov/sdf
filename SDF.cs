using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using sdf.Matching;

namespace sdf {
	/*
	sdf			= node|literal
	node		= \(name [attributes] [children]\)
	name		= [A-Za-z0-9_:.-]+
	attributes	= \{attribute*\}
	attribute	= name sdf
	children	= \[sdf*\]|sdf
	literal		= number|boolean|string|null
	number		= -?[0-9]+(.[0-9])?
	boolean		= true|false
	string		= \"[^"]*\"
	*/

	/// <summary>
	///     <c>SDF</c> class is a representation of SDF data, which is simiar to XML or JSON, but in S-Expressions form.
	/// </summary>
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	public abstract class SDF {
		/// <summary>
		///     Returns all SDF elements, that match given SDF Path.
		/// </summary>
		/// <param name="path">SDF Path for elements to match to.</param>
		/// <returns>List of all SDF elements matching given SDF Path as <c>Match</c> instances.</returns>
		public List<Match> Find(string path) {
			return Matcher.Match(this, path);
		}

		/// <summary>
		///     Returns new SDF hierarchy where all elements matching to given SDF Path are replaced with a copy of given SDF.
		///     If root element matches the given path, given SDF is returned (not a copy of it).
		/// </summary>
		/// <param name="path">SDF Path for elements to match to.</param>
		/// <param name="newValue">SDF to replaced matching elements with.</param>
		/// <returns>Reference to either updated <c>this</c> or <c>newValue</c>.</returns>
		public SDF Replace(string path, SDF newValue) {
			var topMatches = FindTopMatches(path);

			foreach (var m in topMatches) {
				if (m.Parent == null) {
					// if root is being replaced
					return newValue;
				}

				var oldValue = m.Value;
				var parent = m.Parent.Value as Node; // parents can only be nodes
				if (parent == null) {
					throw new InvalidDataException("Element has parent which is not a Node (impossible).");
				}

				var index = parent.Children.IndexOf(oldValue);
				if (index != -1) {
					parent.Children[index] = newValue.DeepCopy();
				} else { // not a child, then must be an attribute
					var found = false;
					foreach (var pair in parent.Attributes) {
						if (pair.Value == oldValue) {
							parent.Attributes[pair.Key] = newValue.DeepCopy();
							found = true;
							break;
						}
					}

					if (!found) {
						throw new InvalidDataException("Cannot replace element because element's parent doesn't have it neither as a child nor as an attribute.");
					}
				}
			}

			return this;
		}

		/// <summary>
		///     Adds an attribute with <c>attributeName</c> name and <c>value</c> value to all elements matching given SDF Path.
		///     Can only apply to nodes.
		///     Throws an exception if attribute with given name already exists on a matching element.
		/// </summary>
		/// <param name="path">SDF Path for elements to match to.</param>
		/// <param name="attributeName">Name of the added attribute.</param>
		/// <param name="value">Value of the added attribute.</param>
		public void AddAttribute(string path, string attributeName, SDF value) {
			var matches = Find(path);
			foreach (var match in matches) {
				var node = match.Value as Node;
				if (node == null) {
					throw new InvalidDataException("Cannot add an attribute to something but a Node.");
				}

				if (node.Attributes.ContainsKey(attributeName)) {
					throw new InvalidDataException("Cannot add an attribute, because attribute with such name already exists.");
				}

				node.Attributes[attributeName] = value.DeepCopy();
			}
		}

		/// <summary>
		///     Adds a clone of <c>value</c> as a child to all elements matching given SDF Path.
		///     Can only apply to nodes.
		/// </summary>
		/// <param name="path">SDF Path for elements to match to.</param>
		/// <param name="value">Value of the added child.</param>
		public void AddChild(string path, SDF value) {
			var matches = Find(path);
			foreach (var match in matches) {
				var node = match.Value as Node;
				if (node == null) {
					throw new InvalidDataException("Cannot add a child to something but a Node.");
				}

				node.Children.Add(value.DeepCopy());
			}
		}

		/// <summary>
		///     Inserts a clone of <c>value</c> as a child to all elements matching given SDF Path at position <c>index</c>.
		/// </summary>
		/// <param name="path">SDF Path for elements to match to.</param>
		/// <param name="index">Position to insert into.</param>
		/// <param name="value">Value to be inserted.</param>
		public void InsertAt(string path, int index, SDF value) {
			var matches = Find(path);
			foreach (var match in matches) {
				var node = match.Value as Node;
				if (node == null) {
					throw new InvalidDataException("Cannot insert a child into something but a Node.");
				}

				node.Children.Insert(index, value.DeepCopy());
			}
		}

		/// <summary>
		///     Inserts a clone of <c>value</c> before all elements matching given SDF Path.
		///     Throws an exception if adding next to root element.
		/// </summary>
		/// <param name="path">SDF Path for elements to match to.</param>
		/// <param name="value">Value to be inserted.</param>
		public void InsertBefore(string path, SDF value) {
			var matches = Find(path);
			foreach (var match in matches) {
				if (match.Parent == null) {
					throw new InvalidDataException("Cannot add something next to root element.");
				}

				var node = match.Parent.Value as Node;
				if (node == null) {
					throw new InvalidDataException("Cannot insert a child into something but a Node.");
				}

				node.InsertBeforeChild(match.Value, value.DeepCopy());
			}
		}

		/// <summary>
		///     Inserts a clone of <c>value</c> after all elements matching given SDF Path.
		///     Throws an exception if adding next to root element.
		/// </summary>
		/// <param name="path">SDF Path for elements to match to.</param>
		/// <param name="value">Value to be inserted.</param>
		public void InsertAfter(string path, SDF value) {
			var matches = Find(path);
			foreach (var match in matches) {
				if (match.Parent == null) {
					throw new InvalidDataException("Cannot add something next to root element.");
				}

				var node = match.Parent.Value as Node;
				if (node == null) {
					throw new InvalidDataException("Cannot insert a child into something but a Node.");
				}

				node.InsertAfterChild(match.Value, value.DeepCopy());
			}
		}

		/// <summary>
		///     Removes all elements matching given SDF Path.
		///     If root element matches the given path, <c>null</c> is returned.
		/// </summary>
		/// <param name="path">SDF Path for elements to match to.</param>
		/// <returns>Reference to either updated <c>this</c> or <c>null</c>.</returns>
		public SDF Remove(string path) {
			var topMatches = FindTopMatches(path);

			foreach (var m in topMatches) {
				if (m.Parent == null) {
					// if root is being removed
					return null;
				}

				var oldValue = m.Value;
				var parent = m.Parent.Value as Node; // parents can only be nodes
				if (parent == null) {
					throw new InvalidDataException("Element has parent which is not a Node (impossible).");
				}

				if (parent.Children.Contains(oldValue)) {
					parent.Children.Remove(oldValue);
				} else { // not a child, then must be an attribute
					var found = false;
					foreach (var pair in parent.Attributes) {
						if (pair.Value == oldValue) {
							parent.Attributes.Remove(pair.Key);
							found = true;
							break;
						}
					}

					if (!found) {
						throw new InvalidDataException("Cannot remove element because element's parent doesn't have it neither as a child nor as an attribute.");
					}
				}
			}

			return this;
		}

		// private methods

		private SDF DeepCopy() {
			var n = this as Node;
			if (n != null) {
				var atrs = new Dictionary<string, SDF>();
				foreach (var kv in n.Attributes) {
					atrs[kv.Key] = kv.Value.DeepCopy();
				}

				var chld = n.Children.Select(child => child.DeepCopy()).ToList();
				return new Node(n.Name, atrs, chld);
			}

			var nl = this as NullLiteral;
			if (nl != null) {
				return new NullLiteral();
			}

			var nml = this as NumberLiteral;
			if (nml != null) {
				return new NumberLiteral(nml.Integer, nml.Fraction);
			}

			var bl = this as BooleanLiteral;
			if (bl != null) {
				return new BooleanLiteral(bl.Value);
			}

			var sl = this as StringLiteral;
			if (sl != null) {
				return new StringLiteral(sl.Value);
			}

			return null;
		}

		private List<Match> FindTopMatches(string path) {
			var matches = Find(path);
			var topMatches = new List<Match>();

			foreach (var m in matches) {
				// check whether <m> has any other match as a parent (not exactly direct)
				var found = false;
				foreach (var o in matches) {
					if (found) {
						break;
					}

					if (m == o) {
						continue;
					}

					var p = m;
					while (p != null) {
						if (p.Parent == o) {
							found = true;
							break;
						}

						p = p.Parent;
					}
				}

				if (!found) {
					topMatches.Add(m);
				}
			}

			return topMatches;
		}
	}

	/// <inheritdoc />
	/// <summary>
	///     Representation of SDF node: with a name, list of children and attributes (key-value set).
	/// </summary>
	public class Node : SDF {
		internal Dictionary<string, SDF> Attributes;
		internal List<SDF> Children;
		internal string Name;

		/// <summary>
		///     Create new <c>Node</c> with given name, attributes and children.
		/// </summary>
		/// <param name="name">Name of the node.</param>
		/// <param name="attributes">Attributes of the node.</param>
		/// <param name="children">Children of the node.</param>
		public Node(string name, Dictionary<string, SDF> attributes, List<SDF> children) {
			Name = name;
			Attributes = attributes;
			Children = children;
		}

		/// <summary>
		///     Insert given SDF value before given child of this node.
		/// </summary>
		/// <param name="child">Reference to SDF representation of child, before which <c>value</c> should be inserted before.</param>
		/// <param name="value">Reference to SDF representation to be inserted before <c>child</c>.</param>
		public void InsertBeforeChild(SDF child, SDF value) {
			var index = Children.IndexOf(child);
			if (index == -1) {
				throw new ArgumentException("Argument passed as <child> is not a child of Node, thus <value> cannot be inserted before it.");
			}

			Children.Insert(index, value);
		}

		/// <summary>
		///     Insert given SDF value after given child of this node.
		/// </summary>
		/// <param name="child">Reference to SDF representation of child, after which <c>value</c> should be inserted before.</param>
		/// <param name="value">Reference to SDF representation to be inserted after <c>child</c>.</param>
		public void InsertAfterChild(SDF child, SDF value) {
			var index = Children.IndexOf(child);
			if (index == -1) {
				throw new ArgumentException("Argument passed as <child> is not a child of Node, thus <value> cannot be inserted after it.");
			}

			Children.Insert(index + 1, value);
		}
	}

	// literals
	
	/// <inheritdoc />
	/// <summary>
	///     SDF string literal
	/// </summary>
	public class StringLiteral: SDF {
		internal string Value;

		/// <summary>
		///     Create new SDF string literal
		/// </summary>
		/// <param name="v">string value of the literal</param>
		public StringLiteral(string v) {
			Value = v;
		}
	}

	/// <inheritdoc />
	/// <summary>
	///     SDF number literal
	/// </summary>
	public class NumberLiteral : SDF {
		internal long Integer, Fraction;

		/// <summary>
		///     Returns double representation of the literal
		/// </summary>
		public double Double => double.Parse(Integer + CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator + Fraction); // yeah, dumb, I know

		/// <summary>
		///     Create new SDF number literal
		/// </summary>
		/// <param name="a">integer part of the literal</param>
		/// <param name="b">fraction part of the literal</param>
		public NumberLiteral(long a, long b) {
			Integer = a;
			Fraction = b;
		}
	}

	/// <inheritdoc />
	/// <summary>
	///     SDF boolean literal
	/// </summary>
	public class BooleanLiteral : SDF {
		internal bool Value;

		/// <summary>
		///     Create new SDF boolean literal
		/// </summary>
		/// <param name="b">boolean value of the literal</param>
		public BooleanLiteral(bool b) {
			Value = b;
		}
	}

	/// <inheritdoc />
	/// <summary>
	///     SDF null literal
	/// </summary>
	public class NullLiteral : SDF { }
}