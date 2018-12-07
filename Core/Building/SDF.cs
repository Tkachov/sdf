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
			if (nl != null) return new NullLiteral();

			var nml = this as NumberLiteral;
			if (nml != null) return new NumberLiteral(nml.Integer, nml.Fraction);

			var bl = this as BooleanLiteral;
			if (bl != null) return new BooleanLiteral(bl.Value);

			var sl = this as StringLiteral;
			if (sl != null) return new StringLiteral(sl.Value);

			return null;
		}

		public List<Match> Find(string path) {
			return Matcher.Match(this, path);
		}

		private List<Match> FindTopMatches(string path) {
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

			return topMatches;
		}

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

		// the following two work only on Nodes
		public void AddAttribute(string path, string attributeName, SDF value) {
			var matches = Find(path);
			foreach (var match in matches) {
				var node = match.Value as Node;
				if (node == null)
					throw new InvalidDataException("Cannot add an attribute to something but a Node.");

				if (node.Attributes.ContainsKey(attributeName))
					throw new InvalidDataException("Cannot add an attribute, because attribute with such name already exists.");

				node.Attributes[attributeName] = value.DeepCopy();
			}
		}
		
		public void AddChild(string path, SDF value) {
			var matches = Find(path);
			foreach (var match in matches) {
				var node = match.Value as Node;
				if (node == null)
					throw new InvalidDataException("Cannot add a child to something but a Node.");

				node.Children.Add(value.DeepCopy());
			}
		}

		public void InsertAt(string path, int index, SDF value) {
			var matches = Find(path);
			foreach (var match in matches) {
				var node = match.Value as Node;
				if (node == null)
					throw new InvalidDataException("Cannot insert a child into something but a Node.");

				node.Children.Insert(index, value.DeepCopy());
			}
		}

		// the following two don't work on root (because path points to Node's child, and root is not a child)
		public void InsertBefore(string path, SDF value) {
			var matches = Find(path);
			foreach (var match in matches) {
				if (match.Parent == null)
					throw new InvalidDataException("Cannot add something next to root element.");

				var node = match.Parent.Value as Node;
				if (node == null)
					throw new InvalidDataException("Cannot insert a child into something but a Node.");
				
				node.InsertBeforeChild(match.Value, value.DeepCopy());
			}
		}

		public void InsertAfter(string path, SDF value) {
			var matches = Find(path);
			foreach (var match in matches) {
				if (match.Parent == null)
					throw new InvalidDataException("Cannot add something next to root element.");

				var node = match.Parent.Value as Node;
				if (node == null)
					throw new InvalidDataException("Cannot insert a child into something but a Node.");

				node.InsertAfterChild(match.Value, value.DeepCopy());
			}
		}

		// looks similar to Replace, but simply removes matching elements instead of replacing with a new value
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
		
		public void InsertBeforeChild(SDF child, SDF value) {
			var index = Children.IndexOf(child);
			if (index == -1)
				throw new ArgumentException("Argument passed as <child> is not a child of Node, thus <value> cannot be inserted before it.");

			Children.Insert(index, value);
		}

		public void InsertAfterChild(SDF child, SDF value) {
			var index = Children.IndexOf(child);
			if (index == -1)
				throw new ArgumentException("Argument passed as <child> is not a child of Node, thus <value> cannot be inserted after it.");
			
			Children.Insert(index+1, value);
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
