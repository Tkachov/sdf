using System;
using sdf.Core.Parsing;

namespace sdf {
	internal static class Program {
		private const int RequiredArgumentsLength = 1;
		private const int InputSdfFilenameArgumentIndex = 0;

		[STAThread]
		private static void Main(string[] args) {
			if (args.Length != RequiredArgumentsLength) {
				Console.Out.WriteLine("Usage: Console <input SDF>");
				return;
			}

			try {
				var root = Parser.Parse(args[InputSdfFilenameArgumentIndex]);
			} catch (Exception e) {
				Console.Out.WriteLine("Unable to handle the file you've selected.");
				Console.Out.WriteLine();
				Console.Out.WriteLine("Information:");
				Console.Out.WriteLine(e.Message);
			}
		}
	}
}