using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.UI;

namespace MicroSerializationLibrary.Unity {

    [Serializable()]
    public class UnityClientAuthorization {

        public UnityClientAuthorization() {

        }

        public int SocketHandle;
        public UnityClientInfo ClientInfo = new UnityClientInfo();

        public UnityClientAuthorization(System.Net.Sockets.Socket Socket) {
            SocketHandle = (int)Socket.Handle;
        }

        public void ResolveClientInfo(System.Net.Sockets.Socket sender, string username, string password)
        {
            ClientInfo = new UnityClientInfo(username, new Dictionary<string, object>());
            ClientInfo.pID = Guid.NewGuid().ToString().Replace("-", "");
        }

    }

    [Serializable()]
    public class UnityClientInfo {

        public string pID;
        public string Username;
        public Dictionary<string, object> Attributes;

        public UnityClientInfo() {
            Attributes = new Dictionary<string, object>();
        }

        public UnityClientInfo(string ClientName, Dictionary<string,object> Attributes) {
            Username = ClientName;
            this.Attributes = Attributes;
        }

    }
}
