using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

	public abstract class SDF {}

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
