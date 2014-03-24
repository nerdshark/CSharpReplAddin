using System;
using MonoDevelop.CSharpRepl;
using System.Threading;

namespace ServerTestApp
{
	public class MainClass
	{
		public static void Main (string[] args)
		{
			Console.WriteLine ("derp");
			var proxy = new CSharpReplServerProxy ("tcp://localhost:33333");
			proxy.Start ();
			//Thread.Sleep (1000);

			var output = proxy.evaluate ("var x = 10;");
			output.Wait ();
			Console.WriteLine ("Result Type: {0}\nMessage:\n{1}", output.Result.Type, output.Result.ResultMessage);
			/*var output = proxy.evaluate ("var x = 10;")
				.ContinueWith (task => {
				Console.WriteLine ("Result Type: {0}\nMessage:\n{1}", task.Result.Type, task.Result.ResultMessage);
			});*/
			

			/*output = proxy.evaluate ("for (int ii = 0; ii < 3; ii++) { Console.WriteLine(ii); }").ContinueWith (task => {
				Console.WriteLine ("Result Type: {0}\nMessage:\n{1}", task.Result.Type, task.Result.ResultMessage);
		});*/

//			var repl = new CSharpRepl();
//			var output = repl.evaluate("10*10;");

			//Console.WriteLine(output.Result.Type.ToString());
//			output = repl.evaluate("var x = 10;");
//			Console.WriteLine("{0}: {1}",output.ResultType.ToString(),output.Result);
//			output = repl.evaluate("x;");
//			Console.WriteLine("{0}: {1}",output.ResultType.ToString(),output.Result);
		}
	}
}
