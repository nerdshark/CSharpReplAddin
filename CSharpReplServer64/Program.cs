using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSharpReplServer64
{
	class Program
	{
		public static void Main (string[] args)
		{
			Console.WriteLine ("Starting C# interactive shell on port {0}", args [0]);
			MonoDevelop.CSharpRepl.CSharpReplServer.Run ("*", Int32.Parse (args [0])).Wait ();
		}
	}
}
