using System;
using System.Collections.Generic;
using sdf;
using sdf.Parsing;
using sdf.Printing;
using sdf.Validating;

namespace ConsoleExamples {
	internal static class Program {
		private const int REQUIRED_ARGUMENTS_LENGTH_IS_ONE = 1;
		private const int REQUIRED_ARGUMENTS_LENGTH_IS_TWO = 2;
		private const int INPUT_SDF_FILENAME_ARGUMENT_INDEX = 0;
		private const int INPUT_SDF_SCHEMA_FILENAME_ARGUMENT_INDEX = 1;

		[STAThread]
		private static void Main(string[] args) {
			try {
				//ReadInputWithStreaming(args);
				ReadInputWithStreamingAndValidateBySchema(args);
				//ReadInputAndUseMatchingQueries(args);
				//ReadInputAndValidateBySchema(args);
			} catch (Exception e) {
				Console.Out.WriteLine("Unable to handle the file you've selected.");
				Console.Out.WriteLine();
				Console.Out.WriteLine("Information:");
				Console.Out.WriteLine(e.Message);
			}

			Console.ReadLine(); // pause
		}

		private static void ReadInputWithStreaming(string[] args) {
			if (args.Length != REQUIRED_ARGUMENTS_LENGTH_IS_ONE) {
				Console.Out.WriteLine("Usage: Console <input SDF>");
				return;
			}
			
			var data = StreamingParser.Parse(args[INPUT_SDF_FILENAME_ARGUMENT_INDEX]);
			Printer.Print(data);
		}

		private static void ReadInputWithStreamingAndValidateBySchema(string[] args) {
			if (args.Length != REQUIRED_ARGUMENTS_LENGTH_IS_TWO) {
				Console.Out.WriteLine("Usage: Console <input SDF> <schema SDF>");
				return;
			}
			
			var schema = new Schema(args[INPUT_SDF_SCHEMA_FILENAME_ARGUMENT_INDEX]);
			var data = StreamingParser.ParseAndValidateSchema(args[INPUT_SDF_FILENAME_ARGUMENT_INDEX], schema);
			Printer.Print(data);
		}

		private static void ReadInputAndValidateBySchema(string[] args) {
			if (args.Length != REQUIRED_ARGUMENTS_LENGTH_IS_TWO) {
				Console.Out.WriteLine("Usage: Console <input SDF> <schema SDF>");
				return;
			}
			
			var input = SimpleParser.Parse(args[INPUT_SDF_FILENAME_ARGUMENT_INDEX]);
			Printer.Print(input);

			var schema = new Schema(args[INPUT_SDF_SCHEMA_FILENAME_ARGUMENT_INDEX]);
			var matchesSchema = schema.Validate(input);
			Console.WriteLine(matchesSchema ? "data matches schema" : schema.ErrorMessage);
		}

		private static void ReadInputAndUseMatchingQueries(string[] args) {
			if (args.Length != REQUIRED_ARGUMENTS_LENGTH_IS_ONE) {
				Console.Out.WriteLine("Usage: Console <input SDF>");
				return;
			}
			
			var data = SimpleParser.Parse(args[INPUT_SDF_FILENAME_ARGUMENT_INDEX]);
			Printer.Print(data);

			var matches = data.Find("node");			
			Console.WriteLine();
			Console.WriteLine(matches.Count + " matches:");
			foreach (var m in matches) {
				Console.WriteLine(m.Path);
				Printer.Print(m.Value, 1);
				Console.WriteLine();
			}

			Console.ReadLine();
			Console.WriteLine();
			Console.WriteLine("insert (empty) before each (subnode)");
			data.InsertBefore("subnode", new Node("empty", new Dictionary<string, SDF>(), new List<SDF>()));
			Printer.Print(data);
			Console.WriteLine();

			Console.ReadLine();
			Console.WriteLine("insert null after each (subnode)");
			data.InsertAfter("subnode", new NullLiteral());
			Printer.Print(data);
			Console.WriteLine();

			Console.ReadLine();
			Console.WriteLine("insert true as second child of each (node)");
			data.InsertAt("node", 1, new BooleanLiteral(true));
			Printer.Print(data);
			Console.WriteLine();

			Console.ReadLine();
			Console.WriteLine("add \"end\" to children list of each (node)");
			data.AddChild("node", new StringLiteral("end"));
			Printer.Print(data);
			Console.WriteLine();

			Console.ReadLine();
			Console.WriteLine("insert empty-attr -1 as attribute of each (empty)");
			data.AddAttribute("empty", "empty-attr", new NumberLiteral(-1, 0));
			Printer.Print(data);
			Console.WriteLine();

			Console.ReadLine();
			Console.WriteLine("replace empty-attr of each (empty) with (empty-attr-node)");
			data = data.Replace("empty/@empty-attr", new Node("empty-attr-node", new Dictionary<string, SDF>(), new List<SDF>()));
			Printer.Print(data);
			Console.WriteLine();

			Console.ReadLine();
			Console.WriteLine("remove all nodes from (node) children list");
			data = data.Remove("node/^node");
			Printer.Print(data);
			Console.WriteLine();
		}
	}
}