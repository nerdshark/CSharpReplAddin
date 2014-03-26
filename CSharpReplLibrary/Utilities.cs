using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Linq;

namespace MonoDevelop.CSharpRepl
{
	public static class NetworkUtilities
	{
		public static int GetOpenPort (int portStartRange = 10000)
		{
			var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties ();
			var tcpEndpoints = ipGlobalProperties.GetActiveTcpListeners ();
			var query = tcpEndpoints.OrderBy (endpoint => endpoint.Port)
				.Where (endpoint => endpoint.Port >= portStartRange)
				.Select (endpoint => endpoint.Port).Distinct ().ToList ();

			int freePort = portStartRange;
			foreach (var port in query) {
				if (port != portStartRange)
					break;
				freePort++;
			}
			return freePort;
		}
	}
}

