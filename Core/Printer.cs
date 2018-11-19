﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sdf.Core.Building;

namespace sdf.Core {
	class Printer {
		public static void Print(SDF s, int offset = 0, bool newLine = false) {
			Node n = s as Node;
			if (n == null) PrintLiteral(s, offset);
			else PrintNode(n, offset);

			if (newLine)
				Console.WriteLine();
		}

		private static void PrintOffset(int offset) {
			for (int i=0; i<offset; ++i)
				Console.Write("\t");
		}

		private static void PrintLiteral(SDF literal, int offset) {
			PrintOffset(offset);
			if (literal is StringLiteral) {
				Console.Write("\"" + (literal as StringLiteral).Value + "\""); // TODO: escape characters
			} else if (literal is NumberLiteral) {
				var number = literal as NumberLiteral;
				Console.Write(number.Integer);
				if (number.Fraction > 0) {
					Console.Write(".");
					Console.Write(number.Fraction);
				}
			} else if (literal is BooleanLiteral) {
				Console.Write((literal as BooleanLiteral).Value ? "true" : "false");
			} else {
				Console.Write("null");
			}
		}

		private static bool PrintOnSameLineIfLiteral(SDF s, int offset, bool newLineIfLiteral) {
			if (s is Node) {
				Console.WriteLine();
				Print(s, offset, true);
				return false;
			}

			Console.Write(" ");
			Print(s, 0, newLineIfLiteral);
			return true;
		}

		private static void PrintNode(Node node, int offset) {
			PrintOffset(offset);
			Console.Write("(" + node.Name);
			if (node.Attributes.Count > 0) {
				Console.WriteLine();

				PrintOffset(offset+1);
				Console.WriteLine("{");

				foreach (var attribute in node.Attributes) {
					PrintOffset(offset+2);
					Console.Write(attribute.Key);
					PrintOnSameLineIfLiteral(attribute.Value, offset+3, true);
				}

				PrintOffset(offset+1);
				Console.Write("}");
			}

			if (node.Children.Count == 1) {
				var child = node.Children[0];
				var isLiteral = PrintOnSameLineIfLiteral(child, offset+1, false);
				if (!isLiteral)
					PrintOffset(offset); // for the last )
			}

			if (node.Children.Count > 1) {
				Console.WriteLine();

				PrintOffset(offset+1);
				Console.WriteLine("[");

				foreach (var child in node.Children) {
					Print(child, offset+2, true);
				}

				PrintOffset(offset+1);
				Console.Write("]");
			}

			Console.Write(")");
		}
	}
}
