using System;
using MonoDevelop.CSharpRepl;

namespace CSharpShellServer
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			Console.WriteLine ("Starting C# interactive shell on port {0}", args [0]);
			MonoDevelop.CSharpRepl.CSharpReplServer.Run ("127.0.0.1", Int32.Parse (args [0])).Wait ();
		}
	}
}
