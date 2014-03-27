using System;
using MonoDevelop.CSharpRepl;
using System.Threading;
using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;

namespace ServerTestApp
{
	public class MainClass
	{
		public static void PrintResult (Result result)
		{

			Console.WriteLine ("Result Type: {0}\nMessage:\n{1}", result.Type, result.ResultMessage);
		}

		public async static Task RunTests (CSharpReplServerProxy proxy)
		{
			var output = await proxy.getUsings ();
			PrintResult (output);
		}

		public static void Main (string[] args)
		{
			var proxies = new List<CSharpReplServerProxy> ();
			proxies.Add (new CSharpReplServerProxy ("127.0.0.1", 33333));
			proxies.Add (new CSharpReplServerProxy ("tcp://127.0.0.1:33333"));

			for (int testIter = 0; testIter < proxies.Count; testIter++) {
				Console.WriteLine ("Test iteration {0}\n", testIter);
				var proxy = proxies [testIter];
				proxy.Start ();
				RunTests (proxy).Wait (); 
				proxy.Dispose ();
			}
		}
	}
}
