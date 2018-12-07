using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using sdf.Core;
using sdf.Core.Building;
using sdf.Core.Matching;
using sdf.Core.Parsing;
using sdf.Core.Schema;
using TokenType = sdf.Core.Parsing.TokenType;

namespace sdf {
	internal static class Program {
		//private const int RequiredArgumentsLength = 1;
		//private const int InputSdfFilenameArgumentIndex = 0;

		private const int RequiredArgumentsLength = 2;
		private const int InputSdfFilenameArgumentIndex = 0;
		private const int InputSdfSchemaFilenameArgumentIndex = 1;

		[STAThread]
		private static void Main(string[] args) {
			//ReadInputWithStreaming(args);
			//ReadInputWithStreamingAndVerifyBySchema(args);
			ReadInputAndUseMatchingQueries(args);
			//ReadInputAndVerifyBySchema(args);

			Console.ReadLine(); // pause
		}

		private static void ReadInputWithStreaming(string[] args) {
			if (args.Length != RequiredArgumentsLength) {
				Console.Out.WriteLine("Usage: Console <input SDF> <schema SDF>");
				return;
			}

			try {
				var data = SdfStreamingParser.Parse(args[InputSdfFilenameArgumentIndex]);
				Printer.Print(data);
			} catch (Exception e) {
				Console.Out.WriteLine("Unable to handle the file you've selected.");
				Console.Out.WriteLine();
				Console.Out.WriteLine("Information:");
				Console.Out.WriteLine(e.Message);
			}
		}

		private static void ReadInputWithStreamingAndVerifyBySchema(string[] args) {
			if (args.Length != RequiredArgumentsLength) {
				Console.Out.WriteLine("Usage: Console <input SDF> <schema SDF>");
				return;
			}

			try {
				var root = Parser.Parse(args[InputSdfSchemaFilenameArgumentIndex]);
				var data = Builder.Build(root);
				var schema = new Schema(data);

				data = SdfStreamingParser.ParseAndVerifySchema(args[InputSdfFilenameArgumentIndex], schema);
				Printer.Print(data);
			} catch (InvalidDataException e) { //catch (Exception e) {
				Console.Out.WriteLine("Unable to handle the file you've selected.");
				Console.Out.WriteLine();
				Console.Out.WriteLine("Information:");
				Console.Out.WriteLine(e.Message);
			}
		}

		private static void ReadInputAndVerifyBySchema(string[] args) {
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
		}

		private static void ReadInputAndUseMatchingQueries(string[] args) {
			if (args.Length != RequiredArgumentsLength) {
				Console.Out.WriteLine("Usage: Console <input SDF> <schema SDF>");
				return;
			}

			try {
				var root = Parser.Parse(args[InputSdfFilenameArgumentIndex]);
				var data = Builder.Build(root);
				Printer.Print(data);
				
				var matches = data.Find("node");
				Console.WriteLine();
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
				Console.WriteLine();

				Console.ReadLine();
				Console.WriteLine("insert null after each (subnode)");
				data.InsertAfter("subnode", new NullLiteral());
				Printer.Print(data);
				Console.WriteLine();
				Console.WriteLine();

				Console.ReadLine();
				Console.WriteLine("insert true as second child of each (node)");
				data.InsertAt("node", 1, new BooleanLiteral(true));
				Printer.Print(data);
				Console.WriteLine();
				Console.WriteLine();

				Console.ReadLine();
				Console.WriteLine("add \"end\" to children list of each (node)");
				data.AddChild("node", new StringLiteral("end"));
				Printer.Print(data);
				Console.WriteLine();
				Console.WriteLine();

				Console.ReadLine();
				Console.WriteLine("insert empty-attr -1 as attribute of each (empty)");
				data.AddAttribute("empty", "empty-attr", new NumberLiteral(-1, 0));
				Printer.Print(data);
				Console.WriteLine();
				Console.WriteLine();

				Console.ReadLine();
				Console.WriteLine("replace empty-attr of each (empty) with (empty-attr-node)");
				data = data.Replace("empty/@empty-attr", new Node("empty-attr-node", new Dictionary<string, SDF>(), new List<SDF>()));
				Printer.Print(data);
				Console.WriteLine();
				Console.WriteLine();

				Console.ReadLine();
				Console.WriteLine("remove all nodes from (node) children list");
				data = data.Remove("node/^node");
				Printer.Print(data);
				Console.WriteLine();
				Console.WriteLine();

			} catch (Exception e) {
				Console.Out.WriteLine("Unable to handle the file you've selected.");
				Console.Out.WriteLine();
				Console.Out.WriteLine("Information:");
				Console.Out.WriteLine(e.Message);
			}
		}
	}
}