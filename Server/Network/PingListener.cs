using System;
using System.Net;
using System.Net.Sockets;

namespace Server.Network
{
	public sealed class PingListener : IDisposable
	{
		private UdpClient _Listener;
		private const int Port = 12000;

		private static UdpClient Bind(IPEndPoint ipep)
		{
			ipep = new IPEndPoint(ipep.Address, Port);

			UdpClient s = new UdpClient
			{
				Client = new Socket(ipep.AddressFamily, SocketType.Dgram, ProtocolType.Udp)
			};

			try
			{
				s.Client.ExclusiveAddressUse = false;
				s.Client.Bind(ipep);

				return s;
			}
			catch (SocketException e)
			{
				switch (e.ErrorCode)
				{
					case 10048: // WSAEADDRINUSE
					Console.WriteLine("Ping Listener Failed: {0}:{1} (In Use)", ipep.Address, Port);
					break;
					case 10049: // WSAEADDRNOTAVAIL
					Console.WriteLine("Ping Listener Failed: {0}:{1} (Unavailable)", ipep.Address, Port);
					break;
					default:
					{
						Console.WriteLine("Ping Listener Exception:");
						Console.WriteLine(e);
					}
					break;
				}
			}

			return null;
		}

		public PingListener(IPEndPoint ipep)
		{
			_Listener = Bind(ipep);

			BeginReceive();
		}

		private void BeginReceive()
		{
			if (_Listener != null)
			{
				_Listener.BeginReceive(EndReceive, _Listener);
			}
		}

		private void EndReceive(IAsyncResult r)
		{
			IPEndPoint ripep = new IPEndPoint(IPAddress.Any, Port);
			byte[] recvd = _Listener.EndReceive(r, ref ripep);

			BeginSend(recvd, ripep);

			BeginReceive();
		}

		private void BeginSend(byte[] data, IPEndPoint ipep)
		{
            _Listener.BeginSend(data, data.Length, ipep, EndSend, _Listener);
		}

		private void EndSend(IAsyncResult asyncResult)
		{
			_Listener.EndSend(asyncResult);
		}

		public void Dispose()
		{
			_Listener.Close();
			_Listener = null;
		}
	}
}
