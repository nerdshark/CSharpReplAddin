using System;
using MonoDevelop.CSharpRepl;

namespace CSharpShellServer
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			Console.WriteLine ("Starting C# interactive shell on port {0}", args [0]);
			MonoDevelop.CSharpRepl.CSharpReplServer.Run (args).Wait ();
		}
	}
}
