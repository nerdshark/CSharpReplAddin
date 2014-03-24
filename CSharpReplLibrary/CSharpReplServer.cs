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
		private readonly NetMQContext nmqContext;
		private readonly NetMQScheduler nmqScheduler;

		private int Port { get; set; }

		private CSharpRepl Repl { get; set; }

		private int AutoPort { get; set; }
		//
		public CSharpReplServer (int p, NetMQContext ctx = null)
		{
			this.Port = p;
			AutoPort = GetOpenPort ();

			nmqContext = NetMQContext.Create ();
			//nmqScheduler = new NetMQScheduler (nmqContext);
			cancellationTokenSource = new CancellationTokenSource ();
		}

		internal int GetOpenPort ()
		{
			var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties ();
			var tcpEndpoints = ipGlobalProperties.GetActiveTcpListeners ();
			var query = tcpEndpoints.OrderBy (endpoint => endpoint.Port)
				.Where (endpoint => endpoint.Port >= PortStartRange)
				.Select (endpoint => endpoint.Port).Distinct ().ToList ();

			int freePort = PortStartRange;
			foreach (var port in query)
			{
				if (port != PortStartRange)
					break;
				freePort++;
			}
			return freePort;
		}

		public async Task Start ()
		{
			var ct = cancellationTokenSource.Token;
			await Task.Factory.StartNew (() =>
			{
				ct.ThrowIfCancellationRequested ();
				Repl = new CSharpRepl ();
				using (var server = nmqContext.CreateResponseSocket ())
				{
					server.Bind (String.Format ("tcp://127.0.0.1:{0}", Port));

					while (true)
					{
						if (ct.IsCancellationRequested)
						{
							// clean up here
							ct.ThrowIfCancellationRequested ();
						}
						var msg = server.Receive ();
						var request = Request.Deserialize (msg);
						var result = Handle (request);

						byte[] output_buffer = result.Serialize ();
						server.Send (output_buffer);
					}
				}
			}, ct);
		}

		private Result Handle (Request request)
		{
			Result result;
			try
			{
				switch (request.Type)
				{
					case RequestType.Evaluate:
						result = Repl.evaluate (request.Code);
						break;
					case RequestType.LoadAssembly:
						result = Repl.loadAssembly (request.AssemblyToLoad);
						break;
					case RequestType.Usings:
						result = Repl.getUsings ();
						break;
					case RequestType.Variables:
						result = Repl.getVariables ();
						break;
					default:
						result = CreateInvalidRequestInfo (request);
						break;
				}
				return result;
			}
			catch (Exception e)
			{
				Console.WriteLine (e);
				return new Result (ResultType.FAILED, "Error: Caught exception:\n" + e.Message);
			}
		}

		private Result CreateInvalidRequestInfo (Request request)
		{
			var sb = new StringBuilder ();
			sb
				.AppendLine ("Invalid REPL request: ")
				.AppendLine ("\tRequest type: ").Append (request.Type);
				
			return new Result (ResultType.FAILED, sb.ToString ());
		}

		public static async Task Run (string[] args)
		{
			var server = new CSharpReplServer (Int32.Parse (args [0]));
			await server.Start ();
		}
	}
}

