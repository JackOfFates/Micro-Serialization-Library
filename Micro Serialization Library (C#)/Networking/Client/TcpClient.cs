using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;

using System.Net;
using System.Net.Sockets;
using MicroSerializationLibrary.Serialization;

namespace MicroSerializationLibrary.Networking.Client {
    /// <summary>
    /// An easy to use, multithreaded TCP client
    /// </summary>
    /// <remarks></remarks>
    public class TcpClient : BaseTCPSocket {
        public event OnConnectedEventHandler OnConnected;
        public event OnErrorEventHandler OnErrorHandler;
        public delegate void OnConnectedEventHandler(Socket Sender);
        /// <summary>
        /// Make a TCP client, binded to a port (if specified). Using IPAddress.Any
        /// </summary>
        /// <param name="Protocol"></param>
        /// <param name="Port">(Optional) Bind to the specified port</param>
        /// <remarks>See bind: http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.bind(v=vs.110).aspx 
        /// See IPEndPoint: http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.bind(v=vs.110).aspx 
        /// </remarks>
        public TcpClient(ISerializationProtocol Protocol, int Port = 0) : base(Protocol, Port) {
			OnReceive += TcpClient_OnReceive;
		}

        /// <summary>
        /// Try to connect to the host and port specified
        /// </summary>
        /// <param name="Host">The host you intend to try and connect to (e.g. localhost, 127.0.0.1 etc..)</param>
        /// <param name="Port">The port the host uses</param>
        /// <remarks></remarks>
        public void Connect(string Host, int Port) {
            try {
                IPHostEntry hostEntry = Dns.GetHostEntry(Host);
                IPAddress address = null;
                foreach (IPAddress address_loopVariable in ResolveAllIPv4FromHostEntry(hostEntry)) {
                    address = address_loopVariable;
                    IPEndPoint endPoint = new IPEndPoint(address, Port);
                    try { BaseSocket.Connect(endPoint); } catch (Exception ex) { OnErrorHandler?.Invoke(BaseSocket, ex); }
                    if (BaseSocket.Connected) {
                        Receive(BaseSocket);
                        OnConnected(BaseSocket);
                        break;
                    }
                }
            } catch (Exception ex) {
                OnErrorHandler?.Invoke(BaseSocket, ex);
            }
		}

		private IPAddress[] ResolveAllIPv4FromHostEntry(IPHostEntry HostEntry) {
			// Loop through the AddressList to obtain the supported AddressFamily. This is to avoid 
			// an exception that occurs when the host host IP Address is not compatible with the address family 
			// (typical in the IPv6 case). 
			List<IPAddress> IPv4Addresses = new List<IPAddress>();
			foreach (IPAddress IP in HostEntry.AddressList) { if (!IP.ToString().Contains(":")) IPv4Addresses.Add(IP); }
			return IPv4Addresses.ToArray();
		}

		public void SendWithReturnPromise(object Obj, Promise p) {
			promises.Add(p.ID, p);
			p.ObjectData = Obj;
			Send(BaseSocket, p);
		}

		public void Send(object Obj) { Send(BaseSocket, Obj); }

		private Dictionary<string, Promise> promises = new Dictionary<string, Promise>();
		private void TcpClient_OnReceive(Socket sender, object obj, int BytesReceived) {
			if (object.ReferenceEquals(obj.GetType(), typeof(Promise))) {
				Promise p = (Promise)obj;

			}
		}
	}

}

namespace MicroSerializationLibrary.Networking
{

	public class Promise {

		public object ObjectData { get; set; }

		public string ID { get; set; }

		public Action<object> OnCompleteAction { get; set; }

		public Promise(object sender, Action<object> onComplete) {
			this.ID = Guid.NewGuid().ToString().Replace("-", "");
			ObjectData = sender;
			OnCompleteAction = onComplete;
		}

		public static Promise Create(object sender, Action<object> onComplete) {
			Promise p = new Promise(sender, onComplete);
			return p;
		}

	}

}
