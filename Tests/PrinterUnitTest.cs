using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using sdf.Printing;

namespace Tests {
	[TestClass]
	public class PrinterUnitTest {
		[TestMethod]
		public void TestPrinter() {
			var s = TestHelper.ParseString("(node {attr (node \"1\")} [3.7 (subnode null) (node (node true)) false])");

			using (var stringWriter = new StringWriter()) {
				var x = Console.Out;
				Console.SetOut(stringWriter);
				Printer.Print(s);
				Console.SetOut(x);

				var consoleOutput = stringWriter.ToString();
				Assert.AreEqual("(node\r\n\t{\r\n\t\tattr\r\n\t\t\t(node \"1\")\r\n\t}\r\n\t[\r\n\t\t3.7\r\n\t\t(subnode null)\r\n\t\t(node\r\n\t\t\t(node true)\r\n\t\t)\r\n\t\tfalse\r\n\t])\r\n", consoleOutput);
			}
		}
	}
}
