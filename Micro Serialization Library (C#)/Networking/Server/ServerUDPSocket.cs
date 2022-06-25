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

	[Obsolete("Use TCP Server instead")]
	public class ServerUsingUDPClient
	{
		public event OnReceivedMessageEventHandler OnReceivedMessage;
		public delegate void OnReceivedMessageEventHandler(object sender);
		public event OnSentMessageEventHandler OnSentMessage;
		public delegate void OnSentMessageEventHandler(object sender);
		public event OnShutdownEventHandler OnShutdown;
		public delegate void OnShutdownEventHandler(object sender, EventArgs e);
		public event OnStartUpEventHandler OnStartUp;
		public delegate void OnStartUpEventHandler(object sender, EventArgs e);
		public event OnInitialisedEventHandler OnInitialised;
		public delegate void OnInitialisedEventHandler(object sender, EventArgs e);
		public event OnSendTimeoutEventHandler OnSendTimeout;
		public delegate void OnSendTimeoutEventHandler(object sender, EventArgs e);
		public ISerializationProtocol Protocol { get; set; }
		private int _Port;
		private bool _Enabled;
		private UdpClient _Client;
		private System.Threading.Thread Listener;
		/// <summary>
		/// Returns the port the UDP Server is binded to
		/// </summary>
		/// <value></value>
		/// <returns>Integer</returns>
		/// <remarks></remarks>
		public int Port {
			get { return _Port; }
		}
		/// <summary>
		/// Returns if the Server is listening to packets and can send packets.
		/// </summary>
		/// <value></value>
		/// <returns>Boolean</returns>
		/// <remarks></remarks>
		public bool Enabled {
			get { return _Enabled; }
		}
		/// <summary>
		/// Make a new UDP server on the specified port.
		/// </summary>
		/// <param name="port">Port to listen on</param>
		/// <param name="StartListening">(Optional) Start when initialised</param>
		/// <remarks></remarks>
		public ServerUsingUDPClient(ISerializationProtocol Protocol, int Port, bool StartListening = false)
		{
			this.Protocol = Protocol;
			this._Port = Port;
			_Client = new UdpClient(this._Port);
			_Client.Client.SendBufferSize = 65527;
			_Client.Client.ReceiveBufferSize = 65527;
			if (StartListening)
				Start();
		}
		/// <summary>
		/// Start listening to packets
		/// </summary>
		/// <remarks></remarks>
		public void Start()
		{
			_Enabled = true;
			Listener = new System.Threading.Thread(() => ListenerThread(_Client, new IPEndPoint(IPAddress.Parse("127.0.0.1"), Port)));
			Listener.Start();
			if (OnStartUp != null) {
				OnStartUp(this, null);
			}
		}
		/// <summary>
		/// Stop listening
		/// </summary>
		/// <remarks></remarks>
		public void Stop()
		{
			_Enabled = false;
			Listener.Abort();
			if (OnShutdown != null) {
				OnShutdown(this, null);
			}
		}
		private void ListenerThread(UdpClient Client, IPEndPoint IPEndPoint)
		{
			object Data = null;
			while (Enabled) {
				byte[] Bytes = Client.Receive(ref IPEndPoint);
				try {
					Data = Protocol.Deserialize(Bytes);
					if (Data != null) {
						if (OnReceivedMessage != null) {
							OnReceivedMessage(Data);
						}
						Data = null;
					}
				} catch (System.IO.FileNotFoundException e) {
					throw new Exception("Add 'Core.AddResolver' to the program's initialiser.");
				} catch (Exception e) {
					Debug.Print("Error in MattJamesLibrary.Networking.ServerUsingUDPClient.ListenerThread:- {0}", e.Message);
				}

			}
		}
		/// <summary>
		/// Send a serializable object with JSON to the address specified.
		/// </summary>
		/// <typeparam name="T">Any type (serializable only)</typeparam>
		/// <param name="Message">The object you intend to send</param>
		/// <param name="Address">The address to send the object to</param>
		/// <remarks></remarks>
		public void Send<T>(T Message, IPEndPoint Address)
		{
			try {
				Send(Protocol.Serialize(Message), Address);
			} catch (System.IO.FileNotFoundException e) {
				throw new Exception("Add 'Core.AddResolver' to the program's initialiser.");
			}
		}
		/// <summary>
		/// Send a serialised object in bytes to the address specified
		/// </summary>
		/// <param name="Bytes">The data you intend to send</param>
		/// <param name="Address">The address the data will be sent to</param>
		/// <remarks></remarks>
		public void Send(byte[] Bytes, IPEndPoint Address)
		{
			_Client.Send(Bytes, Bytes.Length, Address);
			if (OnSentMessage != null) {
				OnSentMessage(this);
			}
		}
	}
}
