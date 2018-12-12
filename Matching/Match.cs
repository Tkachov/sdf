using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;

namespace sdf.Matching {
	/// <summary>
	///     Representation of SDF fully linked with its parent elements.
	///     Constructs unique SDF Path for each element in the hierarchy.
	/// </summary>
	public class Match {
		/// <summary>
		///     Reference to a parent Match element, or <c>null</c> if element does not have a parent (i.e. is root element).
		/// </summary>
		public readonly Match Parent;

		/// <summary>
		///     Automatically constructed unique SDF Path of this element.
		/// </summary>
		public readonly string Path;

		/// <summary>
		///     Actual SDF element (is a reference to element from original SDF hierarchy upon which Match was built).
		/// </summary>
		public readonly SDF Value;

		internal Match(SDF value, string path, Match parent) {
			Value = value;
			Path = path;
			Parent = parent;
		}

		/// <summary>
		///     Create new Match for given root SDF element (having no parent).
		/// </summary>
		/// <param name="root">Corresponding SDF value.</param>
		/// <returns>Match representation of given root SDF element.</returns>
		public static Match MakeRootMatch(SDF root) {
			return MakeMatch(root, null, 0, false);
		}

		private static Match MakeMatch(SDF v, [NotNull] string path, Match parent) {
			if (v is Node) {
				return new MatchNode(v, path, parent);
			}

			return new Match(v, path, parent);
		}

		internal static Match MakeMatch(SDF v, Match parent, int index, bool moreThanOneChild) {
			// make children's path
			string path;
			if (parent == null) {
				path = "/";
			} else {
				path = parent.Path + "/";
			}

			if (v is Node) {
				path += (v as Node).Name;
			}

			if (moreThanOneChild || !(v is Node)) {
				path += "#" + index;
			}

			return MakeMatch(v, path, parent);
		}

		internal static Match MakeMatch(SDF v, Match parent, string attributeName) {
			// make attribute's path
			string path;
			if (parent == null) {
				path = "/";
			} else {
				path = parent.Path + "/";
			}

			path += "@" + attributeName;

			return MakeMatch(v, path, parent);
		}
	}

	/// <inheritdoc />
	/// <summary>
	///     Match representation of a node SDF element.
	/// </summary>
	public sealed class MatchNode : Match {
		/// <summary>
		///     Node's attributes Match representations.
		/// </summary>
		public readonly Dictionary<string, Match> Attributes;

		/// <summary>
		///     List of node's children Match representations.
		/// </summary>
		public readonly List<Match> Children;

		internal MatchNode(SDF value, string path, Match parent) : base(value, path, parent) {
			var n = value as Node;
			if (n == null) {
				throw new InvalidDataException("Cannot create MatchNode from something but a Node.");
			}

			var index = 0;
			Children = n.Children.Select(c => MakeMatch(c, this, index++, n.Children.Count > 1)).ToList();
			Attributes = n.Attributes.ToDictionary(a => a.Key, a => MakeMatch(a.Value, this, a.Key));
		}
	}
}