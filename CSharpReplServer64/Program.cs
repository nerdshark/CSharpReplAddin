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
			MonoDevelop.CSharpRepl.CSharpReplServer.Run ("127.0.0.1", Int32.Parse (args [0])).Wait ();
		}
	}
}
