using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sdf.Core.Matching;

namespace sdf.Core.Building {
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

	public abstract class SDF {
		public List<Match> Find(string path) {
			return Matcher.Match(this, path);
		}

		public SDF Replace(string path, SDF newValue) {
			var matches = Find(path);
			var topMatches = new List<Match>();
			foreach (var m in matches) {
				// check whether <m> has any other match as a parent (not exactly direct)
				var found = false;
				foreach (var o in matches) {
					if (found) break;
					if (m == o) continue;
					var p = m;
					while (p != null) {
						if (p.Parent == o) {
							found = true;
							break;
						}

						p = p.Parent;
					}
				}

				if (!found) topMatches.Add(m);
			}
			
			foreach (var m in topMatches) {
				if (m.Parent == null) {
					// if root is being replaced
					return newValue;
				}

				var oldValue = m.Value;
				var parent = m.Parent.Value as Node; // parents can only be nodes
				if (parent == null) {
					throw new InvalidDataException();
				}

				var index = parent.Children.IndexOf(oldValue);
				if (index != -1) {
					parent.Children[index] = newValue;
				} else { // not a child, then must be an attribute
					var found = false;
					foreach (var pair in parent.Attributes) {
						if (pair.Value == oldValue) {
							parent.Attributes[pair.Key] = newValue;
							found = true;
							break;
						}
                    }
					if (!found) {
						throw new InvalidDataException();
					}
				}
			}

			return this;
		}

		// the following two work only on Nodes
		public void AddAttribute(string path, string attributeName, SDF value) {
			var matches = Find(path);
			foreach (var match in matches) {
				var node = match.Value as Node;
				if (node == null)
					throw new InvalidDataException();

				if (node.Attributes.ContainsKey(attributeName))
					throw new InvalidDataException();

				node.Attributes[attributeName] = value;
			}
		}
		
		public void AddChild(string path, SDF value) {
			var matches = Find(path);
			foreach (var match in matches) {
				var node = match.Value as Node;
				if (node == null)
					throw new InvalidDataException();

				node.Children.Add(value);
			}
		}

		// the following two don't work on root (because path points to Node's child, and root is not a child)
		// TODO: InsertBefore(path, value)
		// TODO: InsertAfter(path, value)
	}

	public class Node: SDF {
		internal string Name;
		internal Dictionary<string, SDF> Attributes;
		internal List<SDF> Children;

		public Node(string name, Dictionary<string, SDF> attributes, List<SDF> children) {
			Name = name;
			Attributes = attributes;
			Children = children;
		}
	}

	// literals

	public class StringLiteral: SDF {
		internal string Value;

		public StringLiteral(string v) {
			Value = v;
		}
	}
	
	public class NumberLiteral: SDF {
		internal long Integer, Fraction;
		public double Double => double.Parse(Integer + CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator + Fraction); // yeah, dumb, I know

		public NumberLiteral(long a, long b) {
			Integer = a;
			Fraction = b;
		}
	}

	public class BooleanLiteral: SDF {
		internal bool Value;

		public BooleanLiteral(bool b) {
			Value = b;
		}
	}

	public class NullLiteral: SDF {
	}
}
