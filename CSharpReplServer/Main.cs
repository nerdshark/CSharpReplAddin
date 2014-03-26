using System;
using MonoDevelop.CSharpRepl;

namespace CSharpShellServer
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			MonoDevelop.CSharpRepl.CSharpReplServer.Run ("127.0.0.1", Int32.Parse (args [0])).Wait ();
		}
	}
}
