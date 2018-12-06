using System;
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
			ReadInputWithStreaming(args);
			//ReadInputAndVerifyBySchema(args);
		}

		private static void ReadInputWithStreaming(string[] args) {
			if (args.Length != RequiredArgumentsLength) {
				Console.Out.WriteLine("Usage: Console <input SDF> <schema SDF>");
				return;
			}

			//try {
				var p = new SdfStreamingParser(new StreamReader(args[InputSdfFilenameArgumentIndex], Encoding.UTF8));
				while (!p.Ended && !p.HasError) {
					var t = p.readNext();
					if (t == TokenType.DocumentStart) continue;
					if (t == TokenType.DocumentEnd) break;
					switch (t) {
						case TokenType.Literal:
							Console.Out.WriteLine("literal");
						break;
							
						case TokenType.NodeStart:
							Console.Out.WriteLine("(" + p.NodeName);
						break;

						case TokenType.NodeAttributeListStart:
							Console.Out.WriteLine("{");
							break;

						case TokenType.NodeAttributeListEnd:
							Console.Out.WriteLine("}");
							break;
						case TokenType.NodeAttributeStart:
							Console.Out.WriteLine("attribute " + p.AttributeName);
							break;
						case TokenType.NodeAttributeEnd:
							Console.Out.WriteLine("attribute ended");
							break;
						case TokenType.NodeAttributeListAfterOne:
							Console.Out.WriteLine("attribute ended, attribute or list end expected now");
							break;
						case TokenType.NodeChildrenListStart:
							Console.Out.WriteLine("[");
							break;
						case TokenType.NodeChildrenListEnd:
							Console.Out.WriteLine("]");
							break;
						case TokenType.NodeEnd:
							Console.Out.WriteLine(")");
							break;
						case TokenType.DocumentEnd:
							Console.Out.WriteLine("document ended");
							break;
						case TokenType.NodeAfterAttributes:
							Console.Out.WriteLine("attributes ended, children list or node end expected now");
							break;
						case TokenType.NodeChildrenListAfterChild:
							Console.Out.WriteLine("child ended, child or list end expected now");
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}
					//Console.ReadLine();
				}

			if (p.HasError) {
				Console.Out.WriteLine("Error while stream parsing the file:");
				Console.Out.WriteLine(p.Error);
			} else {
				Printer.Print(p.Document);
			}
				/*
			} catch (Exception e) {
				Console.Out.WriteLine("Unable to handle the file you've selected.");
				Console.Out.WriteLine();
				Console.Out.WriteLine("Information:");
				Console.Out.WriteLine(e.Message);
			}
			*/


			Console.ReadLine(); // pause
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


			Console.ReadLine(); // pause
		}
	}
}