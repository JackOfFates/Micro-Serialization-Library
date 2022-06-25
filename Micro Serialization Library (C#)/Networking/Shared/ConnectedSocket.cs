using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Net.Sockets;

namespace MicroSerializationLibrary.Networking
{
	/// <summary>
	/// This class will contain a connected socket and a running thread that checks for recieved messages
	/// </summary>
	/// <remarks></remarks>
	public class ConnectedSocket
	{
		public Socket CurrentSocket { get; set; }
		public ConnectedSocket(Socket CurrentSocket)
		{
			this.CurrentSocket = CurrentSocket;
		}
	}
}
