using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Linq;
using NetMQ;
using Mono.CSharp;

namespace MonoDevelop.CSharpRepl
{
	public class CSharpReplServer 
	{
		private readonly int PortStartRange = 1000;

		private CancellationTokenSource cancellationTokenSource;
		private readonly NetMQContext NmqContext;
		private int Port { get; set; }
		private CSharpRepl Repl { get; set; }

		private int AutoPort { get; set; } // 

		public CSharpReplServer (int p, NetMQContext ctx = null)
		{
			this.Port = p;
			AutoPort = GetOpenPort();

			if(ctx == null)
			{
				NmqContext = NetMQContext.Create ();
			}
			else
			{
				NmqContext = ctx;
			}
		}

		internal int GetOpenPort() {
			var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties ();
			var tcpEndpoints = ipGlobalProperties.GetActiveTcpListeners ();
			var query = tcpEndpoints.OrderBy (endpoint => endpoint.Port)
				.Where (endpoint => endpoint.Port >= PortStartRange)
				.Select (endpoint => endpoint.Port).Distinct ().ToList ();

			int freePort = PortStartRange;
			foreach (var port in query)
			{
				if(port != PortStartRange) break;
				freePort++;
			}
			return freePort;
		}

		public void Start()
		{
			cancellationTokenSource = new CancellationTokenSource ();
			var ct = cancellationTokenSource.Token;
			Task.Factory.StartNew (() =>
			{
				ct.ThrowIfCancellationRequested ();
				Repl = new CSharpRepl();
				using (var server = NmqContext.CreateResponseSocket ())
				{
					server.Bind (String.Format("tcp://*:{0}", Port));
					while (true)
					{
						if (ct.IsCancellationRequested)
						{
							// clean up here
							ct.ThrowIfCancellationRequested ();
						}
						var msg = server.Receive ();
						var request = Request.Deserialize (msg);
						Result result;
						if (request.Type == RequestType.Evaluate) {
							result = this.Repl.evaluate(request.Code);
						} else if (request.Type == RequestType.LoadAssembly) {
							result = this.Repl.loadAssembly(request.AssemblyToLoad);
						} else if (request.Type == RequestType.Variables) {
							result = this.Repl.getVariables();
						} else if (request.Type == RequestType.Usings) {
							result = this.Repl.getUsings();
						} else {
							Console.WriteLine("Received unexpected request type {0}",request.Type);
							break;
						}

						byte[] output_buffer = result.Serialize();
						server.Send (output_buffer);
					}
				}
			}, ct).Wait ();
		}

		public static void Run(string[] args)
		{
			var server = new CSharpReplServer(Int32.Parse(args[0]));
			server.Start();
		}
	}
}

