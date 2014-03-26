using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Mono.CSharp;
using NetMQ;

namespace MonoDevelop.CSharpRepl
{
	//	public class CSharpReplServerProxy : ICSharpRepl, IDisposable
	public class CSharpReplServerProxy : IAsyncCSharpRepl, IDisposable
	{
		private NetMQContext nmqContext;
		private NetMQSocket nmqClient;
		private Poller nmqPoller;
		private NetMQScheduler nmqScheduler;

		public Uri Address { get; private set; }

		protected CSharpReplServerProxy ()
		{
			nmqContext = NetMQContext.Create ();
		}

		public CSharpReplServerProxy (string protocol, string host, int port) : this (String.Format ("{0}://{0}:{1}", protocol, host, port))
		{
		}

		public CSharpReplServerProxy (string host, int port) : this (String.Format ("tcp://{0}:{1}", host, port))
		{
		}

		public CSharpReplServerProxy (string uri) : this (new Uri (uri))
		{
		}

		public CSharpReplServerProxy (Uri uri) : this ()
		{
			if (uri == null)
				throw new ArgumentNullException ("uri");

			var builder = new UriBuilder (uri);

			if (string.IsNullOrWhiteSpace (builder.Host))
				throw new ArgumentException ("Must supply a valid hostname or IP", "uri");

			if (string.IsNullOrWhiteSpace (builder.Scheme)) {
				builder.Scheme = "tcp";
				// debug message here
			}

			if (builder.Port == 0) {
				builder.Port = NetworkUtilities.GetOpenPort ();
				// debug message here
			}

			Address = builder.Uri;
		}

		public void Start ()
		{
			ThrowIfDisposed ();
			nmqPoller = new Poller ();
			nmqClient = nmqContext.CreateRequestSocket ();
			nmqClient.Connect (Address.AbsoluteUri.TrimEnd ('/'));
			nmqScheduler = new NetMQScheduler (nmqContext, nmqPoller);
			Task.Factory.StartNew (() => nmqPoller.Start (), TaskCreationOptions.LongRunning);
		}

		private void Send (Request request)
		{
			byte[] data = request.Serialize ();
			Task sendTask = new Task (() => nmqClient.Send (data));
			sendTask.Start (nmqScheduler);
		}

		private async Task<Result> Receive ()
		{
			var resultTask = new Task<byte[]> (() => {
				var data = nmqClient.Receive ();
				return data;
			});
			resultTask.Start (nmqScheduler);
			await resultTask;
			var result = Result.Deserialize (resultTask.Result);
			return result;
		}

		private async Task<Result> sendAndReceive (Request request)
		{
			Send (request);
			return await Receive ();
		}

		public async Task<Result> evaluate (string input)
		{
			ThrowIfDisposed ();
			return await sendAndReceive (Request.CreateEvaluationRequest (input));
		}

		public async Task<Result> loadAssembly (string file)
		{
			ThrowIfDisposed ();
			return await sendAndReceive (Request.CreateLoadAssemblyRequest (file));
		}

		public async Task<Result> getVariables ()
		{
			ThrowIfDisposed ();
			return await sendAndReceive (Request.CreateGetVariablesRequest ());
		}

		public async Task<Result> getUsings ()
		{
			ThrowIfDisposed ();
			return await sendAndReceive (Request.CreateGetUsingsRequest ());
		}

		private void ThrowIfDisposed ()
		{
			if (disposed)
				throw new ObjectDisposedException (this.GetType ().Name);
		}

		bool disposed;

		public void Dispose ()
		{
			ThrowIfDisposed ();
			if (!disposed) {
				if (nmqClient != null) {
					nmqPoller.RemoveSocket (nmqClient);
					nmqClient.Close ();
					nmqClient.Dispose ();
				}
				if (nmqPoller != null) {
					nmqPoller.Stop (false);
				}
				if (nmqScheduler != null) {
					nmqScheduler.Dispose ();
				}
				if (nmqContext != null) {
					nmqContext.Terminate ();
					nmqContext.Dispose ();
				}
				nmqClient = null;
				nmqPoller = null;
				nmqScheduler = null;
				nmqContext = null;
				disposed = true;
			}
		}
	}
}
