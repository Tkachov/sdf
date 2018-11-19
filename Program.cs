using System;
using sdf.Core;
using sdf.Core.Building;
using sdf.Core.Matching;
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
				var data = Builder.Build(root);
				Printer.Print(data);

				var matches = Matcher.Match(data, "/node/");
				Console.WriteLine();
				Console.WriteLine();
				Console.WriteLine(matches.Count + " matches:");
				foreach (var m in matches) {
					Console.WriteLine(m.Path);
					Printer.Print(m.Value, 1);
					Console.WriteLine();
				}

				Console.ReadLine(); // pause
			} catch (Exception e) {
				Console.Out.WriteLine("Unable to handle the file you've selected.");
				Console.Out.WriteLine();
				Console.Out.WriteLine("Information:");
				Console.Out.WriteLine(e.Message);
			}
		}
	}
}