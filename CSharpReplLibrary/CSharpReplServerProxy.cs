using System;
using System.Net;
using NetMQ;

namespace MonoDevelop.CSharpRepl
{
	public class CSharpReplServerProxy : ICSharpRepl
	{
		private readonly NetMQContext NmqContext;
		private readonly NetMQSocket Client;
		private readonly int Port;

		public CSharpReplServerProxy(int port, NetMQContext ctx = null)
		{
			NmqContext = ctx ?? NetMQContext.Create ();
			Client = NmqContext.CreateRequestSocket ();
			Port = port;
		}

		public void Start()
		{
			Client.Connect (String.Format("tcp://localhost:{0}", Port));
			       //this.Client.Connect(this.RemoteAddress);
		}

		internal Result ReceiveMessage()
		{
			byte[] incoming_buffer = Client.Receive ();
			var result = Result.Deserialize(incoming_buffer);
			return result;
		}

		internal void SendMessage(Request rq)
		{
			byte[] outgoing_buffer = rq.Serialize();
			Client.Send (outgoing_buffer);
		}

		#region ICSharpShell implementation



		public Result evaluate (string input)
		{
			var request = Request.CreateEvaluationRequest(input);
			SendMessage (request);
			var result = ReceiveMessage ();
			return result;
		}

		public Result loadAssembly(string file)
		{
			var request = Request.CreateLoadAssemblyRequest(file);
			SendMessage (request);
			var result = ReceiveMessage ();
			return result;
		}

		public Result getVariables()
		{
			var request = Request.CreateGetVariablesRequest ();
			SendMessage (request);
			var result = ReceiveMessage ();
			return result;
		}

		public Result getUsings()
		{
			var request = Request.CreateGetUsingsRequest ();
			SendMessage (request);
			var result = ReceiveMessage ();
			return result;
		}

		#endregion
	}
}

