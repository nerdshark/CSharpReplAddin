using System;
using System.Net;
using NetMQ;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mono.CSharp;
using System.Runtime.Remoting.Lifetime;
using System.Net.Configuration;

namespace MonoDevelop.CSharpRepl
{
	//	public class CSharpReplServerProxy : ICSharpRepl, IDisposable
	public class CSharpReplServerProxy : IAsyncCSharpRepl, IDisposable
	{
		private  NetMQContext nmqContext;
		private  NetMQSocket nmqClient;
		private  Poller nmqPoller;
		private  NetMQScheduler nmqScheduler;
		private  string address;

		public CSharpReplServerProxy (string address)
		{
			nmqContext = NetMQContext.Create ();


			this.address = address;
		}

		public void Start ()
		{
			ThrowIfDisposed ();

			nmqPoller = new Poller ();
			nmqClient = nmqContext.CreateRequestSocket ();

			nmqClient.Connect (address);
			nmqScheduler = new NetMQScheduler (nmqContext, nmqPoller);
			Task.Factory.StartNew (() => nmqPoller.Start (), TaskCreationOptions.LongRunning);

		}

		internal async Task Send (Request request)
		{
			byte[] bytes = request.Serialize ();
			Task sendTask = new Task (() => nmqClient.Send (bytes));
			sendTask.Start (nmqScheduler);
			await sendTask;
		}

		internal async Task<Result> Receive ()
		{

			//var resultTask = new Task<byte[]> (() => {

			//});
			//resultTask.Start (nmqScheduler);
			//var result = await resultTask;
			//			return nmqClient.Receive ();
			return Result.Deserialize (nmqClient.Receive ());
		}
		/*
		internal async Task<Result> ReceiveMessage ()
		{
			ThrowIfDisposed ();
			return await Task.Factory.StartNew<Result> (() => {
				bool has_more;
				byte[] incoming_buffer = nmqClient.Receive (out has_more);
				while (has_more)
					incoming_buffer.Concat (nmqClient.Receive (out has_more));
				var result = Result.Deserialize (incoming_buffer);
				return result;
			});
		}

		internal async Task<Result> SendMessage (Request rq)
		{
			ThrowIfDisposed ();
			var result = await Task.Factory.StartNew (() => {
				byte[] outgoing_buffer = rq.Serialize ();
				nmqClient.Send (outgoing_buffer);
			}).ContinueWith<Task<Result>> (
				             task => ReceiveMessage ());

			return await result;
		}
		*/

		#region ICSharpShell implementation

		public async Task<Result> evaluate (string input)
		{
			ThrowIfDisposed ();
			var request = Request.CreateEvaluationRequest (input);
			await Send (request);
			Console.WriteLine ("getting ready to receive");
			var result = await Receive ();
			return result;
		}

		public async Task<Result> loadAssembly (string file)
		{
			ThrowIfDisposed ();
			var request = Request.CreateLoadAssemblyRequest (file);
			await Send (request);
			var result = await Receive ();
			return result;
		}

		public async Task<Result> getVariables ()
		{
			ThrowIfDisposed ();
			var request = Request.CreateGetVariablesRequest ();
			await Send (request);
			var result = await Receive ();
			return result;
		}

		public async Task<Result> getUsings ()
		{
			ThrowIfDisposed ();
			var request = Request.CreateGetUsingsRequest ();
			await Send (request);
			var result = await Receive ();
			return result;
		}

		private void ThrowIfDisposed ()
		{
			if (!disposed)
				return;
			throw new ObjectDisposedException (this.GetType ().Name);
		}

		bool disposed;

		public void Dispose ()
		{
			ThrowIfDisposed ();
			if (!disposed)
			{
				if (nmqClient != null)
				{
					nmqClient.Close ();
					nmqClient.Dispose ();
				}
				if (nmqScheduler != null)
					nmqScheduler.Dispose ();
				if (nmqPoller != null)
				{
					nmqPoller.Stop ();
				}
				if (nmqContext != null)
				{
					nmqContext.Terminate ();
					nmqContext.Dispose ();
				}
				disposed = true;
			}
		}

		#endregion
	}
}

