using System;
using sdf.Core;
using sdf.Core.Building;
using sdf.Core.Matching;
using sdf.Core.Parsing;
using sdf.Core.Schema;

namespace sdf {
	internal static class Program {
		//private const int RequiredArgumentsLength = 1;
		//private const int InputSdfFilenameArgumentIndex = 0;

		private const int RequiredArgumentsLength = 2;
		private const int InputSdfFilenameArgumentIndex = 0;
		private const int InputSdfSchemaFilenameArgumentIndex = 1;

		[STAThread]
		private static void Main(string[] args) {
			if (args.Length != RequiredArgumentsLength) {
				Console.Out.WriteLine("Usage: Console <input SDF> <schema SDF>");
				return;
			}

			try {
				var root = Parser.Parse(args[InputSdfFilenameArgumentIndex]);
				var data = Builder.Build(root);
				Printer.Print(data);

				/*
				var matches = data.Find("node");
				Console.WriteLine();
				Console.WriteLine();
				Console.WriteLine(matches.Count + " matches:");
				foreach (var m in matches) {
					Console.WriteLine(m.Path);
					Printer.Print(m.Value, 1);
					Console.WriteLine();
				}
				*/
				var input = data;

				root = Parser.Parse(args[InputSdfSchemaFilenameArgumentIndex]);
				data = Builder.Build(root);
				var schema = new Schema(data);

				Console.WriteLine();
				var res = schema.Validate(input);
				if (!res) Console.WriteLine(schema.ErrorMessage);
				else Console.WriteLine("data matches schema");
			
			} catch (Exception e) {
				Console.Out.WriteLine("Unable to handle the file you've selected.");
				Console.Out.WriteLine();
				Console.Out.WriteLine("Information:");
				Console.Out.WriteLine(e.Message);
			}
			

			Console.ReadLine(); // pause
		}
	}
}