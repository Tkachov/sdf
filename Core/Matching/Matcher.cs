using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sdf.Core.Building;

namespace sdf.Core.Matching {
	internal class MultipleConditions {
		public readonly List<Condition> conditions;

		public bool HasArbitraryHierarchy {
			get {
				foreach (var c in conditions) {
					if (c is ArbitraryNodeHierarchyCondition)
						return true;
				}
				return false;
			}
		}

		public MultipleConditions(List<Condition> l) {
			conditions = l;
		}
		
		internal bool Matches(SDF sdf, SDF parent, string attrbuteName) {
			foreach(var c in conditions) {
				if(!c.Matches(sdf, parent, attrbuteName))
					return false;
			}
			return true; //(conditions.Count == 0);
		}
	}

	public class Matcher {
		public static List<SDF> Match(SDF s, string path) { // TODO: return "match" objects (with path and parent fields apart from SDF itself)
			return MatchConditions(s, ParseConditions(path));
		}

		private static List<MultipleConditions> ParseConditions(string path) {
			if (path.Length > 0 && path[0] != '/') {
				path = "/*/" + path;
			}

			var l = new List<MultipleConditions>();
			var stringConditions = path.Split('/');
			for (int i = 1; i < stringConditions.Length; ++i) { // skip 0th, because it's empty
				l.Add(ParseCondition(stringConditions[i]));
			}
			return l;
		}

		private static MultipleConditions ParseCondition(string s) {
			var p = new ConditionParser(s);
			return new MultipleConditions(p.Parse());
		}

		private static void PrintPath(List<MultipleConditions> conditions) {
			foreach (var c in conditions) {
				Console.Write("/");
				PrintConditions(c.conditions);
			}
			Console.WriteLine();
		}

		private static void PrintConditions(List<Condition> conditions) {
			foreach (var c in conditions) {
				if (c is NodeNameCondition)
					Console.Write((c as NodeNameCondition).NodeName);
				else if (c is AttributeNameCondition)
					Console.Write("@" + (c as AttributeNameCondition).AttributeName);
				else if (c is ArbitraryNodeHierarchyCondition)
					Console.Write((c as ArbitraryNodeHierarchyCondition).AtLeastOne ? "+" : "*");
			}
		}

		private static List<SDF> MatchConditions(SDF s, List<MultipleConditions> conditionsHierarchy, SDF parent = null, string attributeName = null) {
			var res = new List<SDF>();
			
			// find nodes that match [0]th condition
			// recursively run this method on their children/attributes with conditions [1..]
			if (conditionsHierarchy.Count == 0) {
				res.Add(s);
				return res;
			}

			MultipleConditions first = conditionsHierarchy[0];
			List<MultipleConditions> rest = conditionsHierarchy.GetRange(1, conditionsHierarchy.Count - 1);

			List<Condition> conditions = first.conditions;
			bool hasArbitrary = first.HasArbitraryHierarchy;
			bool arbitraryAtLeastOne = false;
			foreach (var c in conditions) {
				if (c is ArbitraryNodeHierarchyCondition) {
					arbitraryAtLeastOne = (c as ArbitraryNodeHierarchyCondition).AtLeastOne;
					break; // TODO: think of how to avoid multiple arbitrary conditions
				}
			}

			////////// DEBUG
			Console.WriteLine();
			PrintPath(conditionsHierarchy);
			Console.WriteLine("hasArbitrary:       "+hasArbitrary);
			Console.WriteLine("needs at least one: "+arbitraryAtLeastOne);
			Console.WriteLine(" - we're on:        "+(s is Node ? (s as Node).Name : "<literal>"));
			Console.WriteLine(" - matches:         "+Matches(s, first, parent, attributeName));
			Console.WriteLine();
			//////////

			if (hasArbitrary && !arbitraryAtLeastOne) { // *
				// try matching without this hierarchy level
				res.AddRange(MatchConditions(s, rest, parent, attributeName));
			}

			if (Matches(s, first, parent, attributeName)) {
				if (rest.Count == 0) {
					res.Add(s);
				} else {
					var n = s as Node;
					if (n == null) return res;

					if(hasArbitrary) {
						// pass arbitrary
						List<MultipleConditions> modifiedConditions = new List<MultipleConditions>();
						if(arbitraryAtLeastOne) {
							// replace + with *, as we have matched one already
							List<Condition> modConditions = new List<Condition>();
							foreach(var c in conditions) {
								if(c is ArbitraryNodeHierarchyCondition) {
									modConditions.Add(new ArbitraryNodeHierarchyCondition(false));
								} else
									modConditions.Add(c);
							}
							modifiedConditions.Add(new MultipleConditions(modConditions));
						} else {
							modifiedConditions.Add(first);
						}
						modifiedConditions.AddRange(rest);

						foreach (var c in n.Children) {
							res.AddRange(MatchConditions(c, modifiedConditions, s, null));
						}

						foreach(var c in n.Attributes) {
							res.AddRange(MatchConditions(c.Value, modifiedConditions, s, c.Key));
						}
					}

					foreach(var c in n.Children) {
						res.AddRange(MatchConditions(c, rest, s, null));
					}

					foreach(var c in n.Attributes) {
						res.AddRange(MatchConditions(c.Value, rest, s, c.Key));
					}
				}
			}

			return res;
		}

		private static bool Matches(SDF sdf, MultipleConditions first, SDF parent, string attrbuteName) {			
			return first.Matches(sdf, parent, attrbuteName);
		}
	}
}
