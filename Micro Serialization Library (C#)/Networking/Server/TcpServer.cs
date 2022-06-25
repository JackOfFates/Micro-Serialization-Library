using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;

using System.Net;
using System.Net.Sockets;
using MicroSerializationLibrary.Serialization;

namespace MicroSerializationLibrary.Networking.Server
{
	/// <summary>
	/// Creates a multithreaded TCP server with concurrent connections with clients. 
	/// </summary>
	/// <remarks></remarks>
	public class TcpServer : BaseTCPSocket
	{
		public event OnConnectedEventHandler OnConnected;
		public delegate void OnConnectedEventHandler(Socket Sender);
		public event OnNewWaitEventHandler OnNewWait;
		public delegate void OnNewWaitEventHandler();
		public event AcceptThreadedConnectionEventHandler AcceptThreadedConnection;
		public delegate void AcceptThreadedConnectionEventHandler();
		public Dictionary<IPEndPoint, ConnectedSocket> ConnectedSockets = new Dictionary<IPEndPoint, ConnectedSocket>();

		private System.Threading.Thread ListenerThread;
		public TcpServer(ISerializationProtocol Protocol, int Port) : base(Protocol, Port)
		{
			OnRelease += Release;
		}
		public void Listen(int backlog)
		{
			ListenerThread = new System.Threading.Thread(() => Listen(backlog, BaseSocket));
			ListenerThread.Start();
		}
		private void Listen(int backlog, Socket Sender)
		{
			base.BaseSocket.Listen(backlog);
			do {
				if (OnNewWait != null) {
					OnNewWait();
				}
				Socket handler = Sender.Accept();
				if (AcceptThreadedConnection != null) {
					AcceptThreadedConnection();
				}
				IPEndPoint RemoteIPEndPoint = (IPEndPoint)handler.RemoteEndPoint;
				ConnectedSockets.Add(RemoteIPEndPoint, new ConnectedSocket(handler));
                Receive(handler);
				if (OnConnected != null) {
					OnConnected(handler);
				}
			} while (true);
		}
		private void Release(Socket sender, IPEndPoint senderIP)
		{
			ConnectedSockets.Remove(senderIP);
		}
		/// <summary>
		/// Send a serializable object with Protocol using an IPEndPoint
		/// </summary>
		/// <param name="Sender">The IPAddress/Port (IPEndPoint) of a connected socket</param>
		/// <param name="Obj">The object you intend to send</param>
		/// <remarks></remarks>
		public void Send(IPEndPoint Sender, object Obj)
		{
			Send(ConnectedSockets[Sender].CurrentSocket, Obj);
		}
		/// <summary>
		/// Send a serializable object with Protocol to an array of connected sockets
		/// </summary>
		/// <param name="Sender">An array of IPAddresses/Ports (IPEndPoints) from the connected sockets dictionary</param>
		/// <param name="Obj">The object you intend to send</param>
		/// <remarks></remarks>
		public void SendBroadcast(IPEndPoint[] Sender, object Obj)
		{
			foreach (IPEndPoint sock_loopVariable in Sender) {
				var sock = sock_loopVariable;
				Send(ConnectedSockets[sock].CurrentSocket, Obj);
			}
		}
		public void SendBroadcast(object Obj)
		{
            // TODO:
            // Needs to be changed to cached method to save processing time on large scale servers.
#if NET20
			List<IPEndPoint> Keys = new List<IPEndPoint>();
			foreach (IPEndPoint Key in Keys) {
				Keys.Add(Key);
			}
			SendBroadcast(Keys.ToArray(), Obj);
#else
            IPEndPoint[] keys = new IPEndPoint[ConnectedSockets.Keys.Count];
            ConnectedSockets.Keys.CopyTo(keys, 0);

            SendBroadcast(keys, Obj);
			#endif
		}
		/// <summary>
		/// Close all connected sockets and close the main listening socket. ConnectedSockets will be cleared.
		/// </summary>
		/// <remarks></remarks>
		public void Close()
		{
			foreach (KeyValuePair<IPEndPoint, ConnectedSocket> ConnectedSocketThread_loopVariable in ConnectedSockets) {
				var ConnectedSocketThread = ConnectedSocketThread_loopVariable;
				ConnectedSocketThread.Value.CurrentSocket.Close();
			}
			ConnectedSockets.Clear();
			ListenerThread.Abort();
			BaseSocket.Close();
		}
	}
}
