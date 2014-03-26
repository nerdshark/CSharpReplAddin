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
	public class CSharpReplServer : IDisposable
	{
		private CancellationTokenSource cancellationTokenSource;
		private NetMQContext nmqContext;
		private NetMQScheduler nmqScheduler;
		private Poller nmqPoller;
		private NetMQSocket nmqServer;
		private Task serverTask;

		private CSharpRepl repl { get; set; }

		public Uri Address { get; private set; }

		public CSharpReplServer (string protocol, string host, int port) : this (String.Format ("{0}://{0}:{1}",
		                                                                                        protocol,
		                                                                                        host,
		                                                                                        port))
		{
		}

		public CSharpReplServer (string host, int port) : this (String.Format ("tcp://{0}:{1}", host, port))
		{
		}

		public CSharpReplServer (string uri) : this (new Uri (uri))
		{
		}

		public CSharpReplServer (Uri uri) : this ()
		{
			if (uri == null)
				throw new ArgumentNullException ("uri");

			var builder = new UriBuilder (uri);

			if (string.IsNullOrWhiteSpace (builder.Host))
				throw new ArgumentException ("Must supply a valid hostname or IP", "uri");

			if (string.IsNullOrWhiteSpace (builder.Scheme))
			{
				builder.Scheme = "tcp";
				// debug message here
			}

			if (builder.Port == 0)
			{
				builder.Port = NetworkUtilities.GetOpenPort ();
				// debug message here
			}

			Address = builder.Uri;
		}

		protected CSharpReplServer ()
		{
			nmqContext = NetMQContext.Create ();
			repl = new CSharpRepl ();
			cancellationTokenSource = new CancellationTokenSource ();
		}

		public async Task Start ()
		{
			ThrowIfDisposed ();
			var ct = cancellationTokenSource.Token;

			nmqPoller = new Poller ();
			nmqScheduler = new NetMQScheduler (nmqContext, nmqPoller);
			nmqServer = nmqContext.CreateResponseSocket ();
			nmqServer.Bind (Address.AbsoluteUri.TrimEnd ('/'));

			serverTask = Task.Factory.StartNew (() =>
			{
				ct.ThrowIfCancellationRequested ();

				while (true)
				{
					if (ct.IsCancellationRequested)
					{
						// clean up here
						ct.ThrowIfCancellationRequested ();
					}
					var msg = nmqServer.Receive ();
					var request = Request.Deserialize (msg);
					var result = Handle (request);

					byte[] output_buffer = result.Serialize ();
					nmqServer.Send (output_buffer);
				}
			}, ct);

			await serverTask;
		}

		public void Stop ()
		{
			cancellationTokenSource.Cancel ();
		}

		private Result Handle (Request request)
		{
			ThrowIfDisposed ();
			Result result;
			try
			{
				switch (request.Type)
				{
					case RequestType.Evaluate:
						result = repl.evaluate (request.Code);
						break;
					case RequestType.LoadAssembly:
						result = repl.loadAssembly (request.AssemblyToLoad);
						break;
					case RequestType.Usings:
						result = repl.getUsings ();
						break;
					case RequestType.Variables:
						result = repl.getVariables ();
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

		public static async Task Run (string endpointAddress, int port)
		{
			var server = new CSharpReplServer (endpointAddress, port);
			await server.Start ();
		}

		private void ThrowIfDisposed ()
		{
			if (disposed)
				throw new ObjectDisposedException (this.GetType ().Name);
		}

		private bool disposed;

		public void Dispose ()
		{
			if (!disposed)
			{
				disposed = true;
			}
		}
	}
}

