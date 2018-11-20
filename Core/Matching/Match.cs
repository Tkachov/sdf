using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using sdf.Core.Building;

namespace sdf.Core.Matching {
	public class Match {
		public readonly string Path;
		public readonly Match Parent;
		public readonly SDF Value;

		public Match(SDF value, string path, Match parent) {
			Value = value;
			Path = path;
			Parent = parent;
		}

		public static Match MakeRootMatch(SDF root) {
			return MakeMatch(root, null, 0, false);
		}

		public static Match MakeMatch(SDF v, [NotNull] string path, Match parent) {
			if (v is Node)
				return new MatchNode(v, path, parent);
			
			return new Match(v, path, parent);
		}

		public static Match MakeMatch(SDF v, Match parent, int index, bool moreThanOneChild) {
			// make children's path
			string path;
			if (parent == null)
				path = "/";
			else
				path = parent.Path + "/";

			if (v is Node)
				path += (v as Node).Name;

			if (moreThanOneChild || !(v is Node))
				path += "#" + index;

			return MakeMatch(v, path, parent);
		}

		public static Match MakeMatch(SDF v, Match parent, string attributeName) {
			// make attribute's path
			string path;
			if (parent == null)
				path = "/";
			else
				path = parent.Path + "/";

			path += "@" + attributeName;			

			return MakeMatch(v, path, parent);
		}
	}

	public class MatchNode: Match {
		public readonly List<Match> Children;
		public readonly Dictionary<string, Match> Attributes;
		
		public MatchNode(SDF value, string path, Match parent): base(value, path, parent) {
			var n = value as Node;
			if (n == null)
				throw new InvalidDataException("Cannot create MatchNode from something but a Node.");

			var index = 0;
			Children = n.Children.Select(c => MakeMatch(c, this, index++, n.Children.Count > 1)).ToList();
			Attributes = n.Attributes.ToDictionary(a => a.Key, a => MakeMatch(a.Value, this, a.Key));
		}
	}
}
