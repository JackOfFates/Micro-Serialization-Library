using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;



using System.Net;
using System.Net.Sockets;
using MicroSerializationLibrary.Serialization;
using System.Text;

namespace MicroSerializationLibrary.Networking
{
	[Serializable()]
	public class ObjectTicketRequest
	{

		public string ID { get; set; }

		public ObjectTicketRequest()
		{
		}
		public ObjectTicketRequest(string ID)
		{
			this.ID = ID;
		}
	}
	/// <summary>
	/// BaseTCPSocket used for ServerTCPSocket and ClientTCPSocket
	/// </summary>
	/// <remarks></remarks>
	public abstract class BaseTCPSocket
	{

		public event OnConnectionInterruptEventHandler OnConnectionInterrupt;
		public delegate void OnConnectionInterruptEventHandler(Socket sender);
        public event OnReceiveEventHandler OnReceive;
        public delegate void OnReceiveEventHandler(Socket sender, object obj, int BytesReceived);
        public event OnReceiveProgressEventHandler OnReceiveProgress;
        public delegate void OnReceiveProgressEventHandler(Socket sender, long BytesReceived, long BytesToReceive);
        public event OnSentProgressEventHandler OnSentProgress;
        public delegate void OnSentProgressEventHandler(Socket sender, long BytesSent, long BytesToSend);
        public event OnSentEventHandler OnSent;
		public delegate void OnSentEventHandler(Socket sender, int BytesSent);
		public event OnErrorEventHandler OnError;
		public delegate void OnErrorEventHandler(Socket sender, Exception e);
		internal event OnReleaseEventHandler OnRelease;
		internal delegate void OnReleaseEventHandler(Socket sender, IPEndPoint senderIP);

		public bool isPacketFailureRecoveryEnabled { get {return _isPacketFailureRecoveryEnabled; } set {_isPacketFailureRecoveryEnabled = value; } }
        private bool _isPacketFailureRecoveryEnabled = false;
        /// <summary>
        /// How many different messages should be cached for packet failure. Recommended at around 20-40.
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public int PacketFailureCacheSize { get; set; }
		//Private SendCache As Object() = New Object(10) {}
		//Private CacheIndex As Integer = -1


		private int _Port;
		internal class ObjectTicket
		{
			public int ResendRequests { get; set; }
			public byte[] Data;
			public ObjectTicket(byte[] Data)
			{
				this.Data = Data;
			}
		}

		private Dictionary<KeyValuePair<Socket, string>, ObjectTicket> TicketCache = new Dictionary<KeyValuePair<Socket, string>, ObjectTicket>();
		/// <summary>
		/// The main socket that listens to all requests. Using the TCP protocol.
		/// </summary>
		/// <remarks>Uses AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp</remarks>
		public Socket BaseSocket;
		/// <summary>
		/// Make a new TCP socket and bind it instantly.
		/// </summary>
		/// <param name="Port">The port you wish to bind to</param>
		/// <param name="Protocol">The protocol to use for serializing and deserializing information</param>
		/// <remarks></remarks>
		public BaseTCPSocket(ISerializationProtocol Protocol, int Port = 0)
		{
			_Port = Port;
			BaseSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			BaseSocket.ReceiveTimeout = 1000;
			BaseSocket.SendTimeout = 1000;
			try {
				Bind(Port);
			} catch (SocketException ex) {
				throw ex;
			} catch (Exception ex) {
				throw ex;
			}
			this.Protocol = Protocol;

		}
		private void Bind(int Port)
		{
			BaseSocket.Bind(new IPEndPoint(IPAddress.Any, Port));
		}
		public ISerializationProtocol Protocol { get; set; }
		/// <summary>
		/// Returns the binded port number
		/// </summary>
		/// <value>Integer</value>
		/// <returns></returns>
		/// <remarks></remarks>
		public int Port {
			get { return _Port; }
		}
		/// <summary>
		/// Returns the binded IPEndPoint
		/// </summary>
		/// <value>IPEndPoint</value>
		/// <returns></returns>
		/// <remarks></remarks>
		public IPEndPoint LocalIPEndPoint {
			get { return (IPEndPoint)BaseSocket.LocalEndPoint; }
		}
		/// <summary>
		/// Returns a boolean value if the socket's connected to a remote host
		/// </summary>
		/// <value>Boolean</value>
		/// <returns></returns>
		/// <remarks></remarks>
		public bool Connected {
			get { return (BaseSocket != null) ? BaseSocket.Connected : false; }
		}
        public List<StateObject> NetworkStateObjects = new List<StateObject>();
        public void Receive(Socket sender)
		{
			try {
				// Create the state object.
				StateObject state = new StateObject();
				state.workSocket = sender;

                // Get the length of the body data transfer. 
                System.Threading.Thread thread1 = new System.Threading.Thread(() => {
                sender.BeginReceive(state.PaddingBuffer, 0, StateObject.PaddingBufferSize, 0, new AsyncCallback(PaddingCallback), state);
                });
                thread1.Start();

			//} catch (SocketException ex) {
			//	if (OnConnectionInterrupt != null) {
			//		OnConnectionInterrupt(sender);
			//	}
			} catch (Exception ex) {
				if (OnError != null) {
					OnError(sender, ex);
				}
			}

		}
		//Receive

		private void PaddingCallback(IAsyncResult ar)
		{
            // Retrieve the state object and the client socket 
            // from the asynchronous state object.
			StateObject state = (StateObject)ar.AsyncState;
			Socket client = state.workSocket;
            try {
                // Read the length of the body.
                int bytesRead = client.EndReceive(ar);
				state.TotalBytesRead += bytesRead;
  
                string StringData = ASCIIEncoding.ASCII.GetString(state.PaddingBuffer);
				int BodyLength = Int32.Parse(StringData.Remove(64));
				string MessageID = StringData.Remove(0, 64);
				state.TotalBytesToRead = BodyLength;
				state.ID = MessageID;
                System.Threading.Thread.Sleep(1);
                NetworkStateObjects.Add(state);
                // Begin receiving the data from the remote device.
                System.Threading.Thread thread1 = new System.Threading.Thread(() => {
                    DoRecursiveReceive(state);
                });
                thread1.Start();
            } catch (Exception ex) {
				if (OnError != null) {
					OnError(client, ex);
				}
			}
		}
		private void ReceiveCallback(IAsyncResult ar)
		{
			// Retrieve the state object and the client socket 
			// from the asynchronous state object.
			StateObject state = (StateObject)ar.AsyncState;
			Socket client = state.workSocket;
            System.Threading.Thread.Sleep(1);
            try {
				// Read data from the remote device.
				int bytesRead = client.EndReceive(ar);

				// Add temporary buffer data to final output buffer.
				state.ObjectData.AddRange(state.buffer);
				state.TotalBytesRead += bytesRead;

                // Begin receiving the data from the remote device.
                System.Threading.Thread thread1 = new System.Threading.Thread(() => {
                    DoRecursiveReceive(state);
                });
                thread1.Start();

			} catch (Exception ex) {
				if (OnError != null) {
					OnError(client, ex);
				}
			}

		}

        private long[] getBytesToReceiveTotal() {
            long output = 0; long currentBytes = 0;
            for (int i = 0; i < NetworkStateObjects.Count; i++) {
                output += NetworkStateObjects[i].TotalBytesToRead;
                currentBytes += NetworkStateObjects[i].TotalBytesRead;
            }
            return new long[] { currentBytes, output };
        }
		//ReceiveCallback
        // .... RECEIVE PROGRESS EVENT .... \\
		private void DoRecursiveReceive(StateObject state)
		{
			Socket client = state.workSocket;
			// Check if we need more data or have finished the transfer.
			int Difference = state.TotalBytesToRead - state.TotalBytesRead;
            long[] ReceivedTotal = getBytesToReceiveTotal();

            OnReceiveProgress(state.workSocket, ReceivedTotal[0], ReceivedTotal[1]);
            System.Threading.Thread.Sleep(1);
            if (Difference < 0) {
				// Message corrupt
				if (isPacketFailureRecoveryEnabled) {
					// Attempt To Recover
					Send(client, new ObjectTicketRequest(state.ID));
				} else {
					if (OnError != null) {
						OnError(client, new SocketException((int)SocketError.NoRecovery));
					}
				}
			} else if (Difference <= StateObject.BufferSize) {
				// Done After Next Receive.
				client.BeginReceive(state.buffer, 0, Difference, 0, new AsyncCallback(FinishReceiveCallback), state);
			} else {
				// Get the rest of the data.
				client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
			}
		}


		private void FinishReceiveCallback(IAsyncResult ar)
		{
			// Retrieve the state object and the client socket 
			// from the asynchronous state object.
			StateObject state = (StateObject)ar.AsyncState;
			Socket client = state.workSocket;
            NetworkStateObjects.Remove(state);

            // Read data from the remote device.
            int bytesRead = client.EndReceive(ar);

			// Add temporary buffer data to final output buffer.
			List<byte> BufferArrayList = new List<byte>(state.buffer);
			state.ObjectData.AddRange(BufferArrayList.GetRange(0, bytesRead));
			state.TotalBytesRead += bytesRead;
			try {
				object data = Protocol.Deserialize(state.ObjectData.ToArray());

				if ((data != null) & ((data is ObjectTicketRequest) == false)) {
					if (OnReceive != null) {
						OnReceive(client, data, state.TotalBytesRead);
					}

				} else if ((data != null) & (data) is ObjectTicketRequest) {
					ResendData(client, (ObjectTicketRequest)data);
				}
			} catch (System.Runtime.Serialization.SerializationException ex) {
				Send(client, new ObjectTicketRequest(state.ID));
			}

            FinishReceiveClean(ref state);
		}
		private void ResendData(Socket Sender, ObjectTicketRequest Obj)
		{
			//If we receive a TicketRequest (data failure) 
			if (TicketCache.ContainsKey(new KeyValuePair<Socket, string>(Sender, Obj.ID))) {
				//If we still have the ticket for this client
				ObjectTicket Ticket = TicketCache[new KeyValuePair<Socket, string>(Sender, Obj.ID)];
				//and the client has only been resent it less than 3 times
				if (Ticket.ResendRequests < 3) {
					//Resend the data
					Ticket.ResendRequests += 1;
					Send(Sender, Ticket.Data);
				}
				//If the ticket requests are 3 or over, the ticket expires
				if (Ticket.ResendRequests >= 3) {
					TicketCache.Remove(new KeyValuePair<Socket, string>(Sender, Obj.ID));
				}
			}
		}
		private void FinishReceiveClean(ref StateObject State)
		{
			// Clean Up
			Socket client = State.workSocket;
			State.TotalBytesToRead = 0;
			State.TotalBytesRead = 0;
			State.buffer = new byte[StateObject.BufferSize];
			State.PaddingBuffer = new byte[StateObject.PaddingBufferSize];
			State.ObjectData.Clear();
			State = null;

			Receive(client);
		}
		private void UpdateRecoveryCache(Socket Client, string MessageID, byte[] Data)
		{
			TicketCache.Add(new KeyValuePair<Socket, string>(Client, MessageID), new ObjectTicket(Data));
			//Add our new ticket
			List<string> DifferentMessageIDs = new List<string>();
			foreach (KeyValuePair<KeyValuePair<Socket, string>, BaseTCPSocket.ObjectTicket> item_loopVariable in TicketCache) {
				var item = item_loopVariable;
				//If the id has not been checked before, add it to our list
				if (!DifferentMessageIDs.Contains(item.Key.Value)) {
					DifferentMessageIDs.Add(item.Key.Value);
				}
			}
			//If we contain over 10 different messages
			if (DifferentMessageIDs.Count > PacketFailureCacheSize) {

				for (int i = 0; i <= DifferentMessageIDs.Count - PacketFailureCacheSize; i++) {
					#if NET20
					TicketCache.Remove(GetFirstTicketKey());
					#else
					TicketCache.Remove(GetFirstTicketKey());
					#endif
					// Remove the amount we need to achieve our count back to 10 again from top, as top is the oldest
				}
			}
		}
		//#if NET20
		private KeyValuePair<Socket, string> GetFirstTicketKey()
		{
			foreach (KeyValuePair<Socket, string> Key in TicketCache.Keys) {
				return Key;
			}
			return new KeyValuePair<Socket, string>();
		}
		//#endif
        
		private void SendCallBack(IAsyncResult ar)
		{

			// Retrieve the socket from the state object.
			Socket client = (Socket)ar.AsyncState;
			// Complete sending the data to the remote device.
			int bytesSent = client.EndSend(ar);
            
            //Console.WriteLine("Sent {0} bytes to server.", bytesSent)
            // Signal that all bytes have been sent.

            if (OnSent != null) {
				OnSent(client, bytesSent);
			}
		}

        private void SendCallBackAsync(IAsyncResult ar) {
            // Retrieve the state object and the client socket 
            // from the asynchronous state object.
            StateObject state = (StateObject)ar.AsyncState;
            Socket client = state.workSocket;

            try {
                // Read data from the remote device.

                // Add temporary buffer data to final output buffer.

                // Begin receiving the data from the remote device.
                DoRecursiveSend(state);
            } catch (Exception ex) {
                if (OnError != null) {
                    OnError(client, ex);
                }
            }

        }

        private void DoRecursiveSend(StateObject state) {
            Socket client = state.workSocket;
            // Check if we need more data or have finished the transfer.
            int Difference = state.TotalBytesToRead - state.TotalBytesRead;
            // long[] SentTotal = getBytesToReceiveTotal();
            // OnSentProgress(state.workSocket, SentTotal[0], SentTotal[1]);

            System.Threading.Thread.Sleep(1);
            if (Difference < 0) {
                // Message corrupt
                if (isPacketFailureRecoveryEnabled) {
                    // Attempt To Recover
                    Send(client, new ObjectTicketRequest(state.ID));
                } else {
                    if (OnError != null) {
                        OnError(client, new SocketException((int)SocketError.NoRecovery));
                    }
                }
            } else if (Difference <= StateObject.BufferSize) {
                // Done After Next Receive.
                client.BeginSend(state.buffer, 0, Difference, 0, new AsyncCallback(FinishSentCallback), state);
            } else {
                // Get the rest of the data.
                client.BeginSend(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(SendCallBackAsync), state);
            }
        }

        private void FinishSentCallback(IAsyncResult ar) {
            if (OnSent != null) {
                StateObject state = (StateObject)ar.AsyncState;

                // Retrieve the state object and the client socket 
                // from the asynchronous state object.
                Socket client = state.workSocket;
                NetworkStateObjects.Remove(state);

                // Read data from the remote device.
                int bytesSent = client.EndSend(ar);

                // Add temporary buffer data to final output buffer.
                state.TotalBytesRead += bytesSent;

                OnSentProgress(state.workSocket, state.TotalBytesRead, state.TotalBytesToRead);
                OnSent(state.workSocket, state.TotalBytesRead);

                state.TotalBytesToRead = 0;
                state.TotalBytesRead = 0;
                state.buffer = new byte[StateObject.BufferSize];
                state.PaddingBuffer = new byte[StateObject.PaddingBufferSize];
                state.ObjectData.Clear();
                state = null;

            }
        }

        internal void Send(Socket sender, object Obj) {
			Send(sender, Protocol.Serialize(Obj));
		}
        public void rawSend(Socket sender, byte[] Bytes) {
            Send(sender, Bytes);
        }
		internal void Send(Socket sender, byte[] Bytes)
		{
			try {
				object[] RAW = PrepareSend(Bytes);
				byte[] ObjectData = (byte[])RAW[0];
				string MessageID = (string)RAW[1];

                StateObject state = new StateObject();
                state.ID = MessageID;
                state.workSocket = sender;
                state.TotalBytesToRead = ObjectData.Length;

                //for (int i = 0; i < ObjectData.Length; i = i + StateObject.BufferSize) {

                //    if (ObjectData.Length > StateObject.BufferSize) {
                //        for (int x = 0; x < StateObject.BufferSize; x++) {
                //            state.ObjectData.Add(ObjectData[x + i]);
                //        }
                //    } else {
                //        for (int x = 0; x < ObjectData.Length; x++) {
                //            state.ObjectData.Add(ObjectData[x + i]);
                //        }
                //    }
                //}
                //state.buffer = state.ObjectData.ToArray();

                if (isPacketFailureRecoveryEnabled)
					UpdateRecoveryCache(sender, MessageID, ObjectData);
               // sender.BeginSend(state.buffer, 0, StateObject.PaddingBufferSize, SocketFlags.None, new AsyncCallback(SendCallBackAsync), state);
                sender.BeginSend(ObjectData, 0, ObjectData.Length, SocketFlags.None, SendCallBack, sender);
            } catch (Exception e) {
				if ((sender != null) & sender.Connected) {
					if (OnError != null) {
						OnError(sender, e);
					}
					//sender.Close();
					return;
				}
			}
		}

        //private object[] PrepareSend(byte[] Data)
        //{
        //	string PaddingInfo = GetPaddingInformation(Data);
        //	string MessageID = PaddingInfo.Substring(10, 32);
        //	string DataLength = PaddingInfo.Substring(0, 10);
        //	byte[] LengthData = System.Text.Encoding.UTF8.GetBytes(PaddingInfo);
        //	return { Helpers.CombineByteArrays({ LengthData, Data }), MessageID };
        //}
        Helpers ByteTools = new Helpers();
        private object[] PrepareSend(byte[] Data) {
            string PaddingInfo = GetPaddingInformation(Data);
            string DataLength = PaddingInfo.Substring(0, 64);
            string MessageID = PaddingInfo.Substring(64, 32);
            byte[] LengthData = ASCIIEncoding.ASCII.GetBytes(PaddingInfo);
            
            object[] objArray = new object[2];
            byte[][] t = new byte[2][];
            t[0] = LengthData;
            t[1] = Data;
            objArray[0] = (object)ByteTools.CombineByteArrays(t);
            objArray[1] = MessageID;
            return objArray;
        }

        private string GetPaddingInformation(byte[] Data) {
			return Convert.ToInt32(Data.Length + StateObject.PaddingBufferSize).ToString("D64") + (Guid.NewGuid().ToString().Replace("-", ""));
		}

		public void CloseSocket(Socket s) {
			if (OnRelease != null) { OnRelease(s, (IPEndPoint)s.RemoteEndPoint); }
			s.Close();
		}

        static byte[] GetBytes(string str) {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        static string GetString(byte[] bytes) {
            char[] chars = new char[bytes.Length / sizeof(char)];
            System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }

        //Private Sub BaseTCPSocket_OnError(sender As Socket, e As Exception) Handles Me.OnError

        //End Sub
    }
    public class StateObject {
        // Client socket.
        public Socket workSocket = null;
        // Size of receive buffer.
        public const int BufferSize = 524288;
        // 32 + 10 (GUID + int)
        public const int PaddingBufferSize = 96;
        public int TotalBytesRead = 0;
        public int TotalBytesToRead = 0;
        // Receive buffer.
        public byte[] buffer = new byte[BufferSize];
        public byte[] PaddingBuffer = new byte[PaddingBufferSize];
        public List<byte> ObjectData = new List<byte>();
        //The GUID of the data we're getting
        public string ID;
    }
    //StateObject

}
