using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using MicroSerializationLibrary;
using MicroSerializationLibrary.Networking;
using MicroSerializationLibrary.Serialization;
using MicroSerializationLibrary.Networking.Client;
using MicroSerializationLibrary.Networking.Server;
using System.Threading;
using System.Net;

namespace MicroSerializationLibrary.Unity {

    [Serializable()]
    public class ComponentUpdate {

        public string rootGameObject;

        public string ID;

        public object data;

        public ComponentUpdate(string ID, string rootGameObject, object newData) {
            this.ID = ID;
            this.data = newData;
            this.rootGameObject = rootGameObject;
        }

        public ComponentUpdate() {

        }

    }

    public class MicroNode : MonoBehaviour {
        public bool AutoLoadGameAfterLogin = false;
        public string GameScene = "";
        public delegate void LoginCompleteDelegate();
        public event LoginCompleteDelegate OnLoginComplete;

        public UnityClientAuthorization UnityAuthorizationKey;
        public NetworkCredential ClientCredentials;

        public List<Type> SupportedTypes;
        public BinaryFormatterSerializer mps;

        #region Client
        public TcpClient client;
        public bool ClientConnected = false;
        public String RemoteIP;
        public int RemotePort;

        private void Client_OnReceive(System.Net.Sockets.Socket sender, object obj, int BytesReceived) {
            if (CanProcessObject(obj.GetType())) {
                ProcessObject_Client(obj);
            }

        }
        private void Client_OnConnected(System.Net.Sockets.Socket c) {
            ClientConnected = true;
            Debug.Log("[C] Connected to " + c.RemoteEndPoint.ToString());
        }
        private void ProcessObject_Client(object o) {
            Debug.Log("[C] Recieved an object of the type " + o.GetType().ToString() + " from [S]ERVER");
            if (o.GetType() == typeof(UnityClientAuthorization))
                PendingEvents.Add((Action)(() => Unity_Auth(o)));

        }

        public void Unity_Auth(object o) {
            UnityClientAuthorization Key = (UnityClientAuthorization)o;
            if (Key.ClientInfo.Username == null) {
                // New Authorization
                Key.SocketHandle = (int)client.BaseSocket.Handle;
                Debug.Log("[C] Authorizing with server");
                Key.ResolveClientInfo(client.BaseSocket, ClientCredentials.UserName, ClientCredentials.Password);
                Client_WaitSend(Key);
            } else {
                // Client Authorized
                this.UnityAuthorizationKey = Key;
                OnLoginComplete();
            }
        }

        private void Logincomplete() {
            if (AutoLoadGameAfterLogin)
                Application.LoadLevel(GameScene);
            Debug.Log("[C] Authorization accepted");
        }

        private void Client_WaitSend(object obj) {
            Thread t = new Thread(new ParameterizedThreadStart(Client_SendWorker));
            t.Start(obj);
        }

        private void Client_SendWorker(object obj) {
            client.Send(obj);
        }

        #endregion

        #region Server

        public Boolean isServer = false;
        public TcpServer server;
        public SortedDictionary<string, System.Net.Sockets.Socket> Players = new SortedDictionary<string, System.Net.Sockets.Socket>();

        private void InitateClient(System.Net.Sockets.Socket c) {
            Debug.Log("[S] Connecting to " + c.RemoteEndPoint.ToString());
            Server_WaitSend(c, new UnityClientAuthorization(c));
        }

        private void Server_OnReceive(System.Net.Sockets.Socket sender, object obj, int BytesReceived)  {
            Debug.Log("[S] Received an object of the type " + obj.GetType().ToString() + " from " + sender.RemoteEndPoint.ToString());
            if (CanProcessObject(obj.GetType())) {
                ProcessObject_Server(sender, obj);
            }
        }

        private void ProcessObject_Server(System.Net.Sockets.Socket sender, object o) {
            Debug.Log("[S] Processing " + o.GetType().ToString() + " from " + sender.RemoteEndPoint.ToString());
            if (o.GetType() == typeof(UnityClientAuthorization)) {
                PendingEvents.Add((Action)(() => Unity_Login(sender, o)));
            } else if (o.GetType() == typeof(Component)) {
                PendingEvents.Add((Action)(() => Unity_Component(sender, o)));
            } else if (o.GetType() == typeof(GameObject)) {
                PendingEvents.Add((Action)(() => Unity_GameObject(sender, o)));
            } else if (o.GetType() == typeof(Transform)) {
                PendingEvents.Add((Action)(() => Unity_Transform(sender, o)));
            }

        }

        private void FindCreateUpdateObjectByName(object o) {
            GameObject go = (GameObject)o;
            GameObject ServerObject = GameObject.Find(go.name);
            if (ServerObject != null) {
                // Update Object
               Component[] Components = ServerObject.GetComponents<Component>();
                for (int i = 0; i < Components.Length; i++) {
                    Component c = Components[i];
                    
                }
            } else {
                // Create Object

            }
            server.SendBroadcast(o);
        }

        public void Unity_Component(System.Net.Sockets.Socket sender, object o) {
            FindCreateUpdateObjectByName(o);
        }

        public void Unity_GameObject(System.Net.Sockets.Socket sender, object o) {
            FindCreateUpdateObjectByName(o);
        }

        public void Unity_Transform(System.Net.Sockets.Socket sender, object o) {
            FindCreateUpdateObjectByName(o);
        }
        
        public void Unity_Login(System.Net.Sockets.Socket sender, object o) {
            // Start Authorization Process
            UnityClientAuthorization UCA = (UnityClientAuthorization)o;
            Debug.Log("[S] User '" + UCA.ClientInfo.Username + "' logging into the server.");

            UCA.ClientInfo.Attributes.Add("isAuthorized", true);
            Server_WaitSend(sender, UCA);

            // Register the user in memory
            Players.Add(UCA.ClientInfo.pID, sender);

        }

        public Boolean CanProcessObject(Type t) {
            return SupportedTypes.Contains(t);
        }

        private void Server_WaitSend(System.Net.Sockets.Socket sender, object obj) {
            object[] o = new object[2];
            o[0] = sender;
            o[1] = obj;
            Server_WaitSend(o);
        }

        private void Server_WaitSend(object obj) {
            Thread t = new Thread(new ParameterizedThreadStart(Server_SendWorker));
            t.Start(obj);
        }

        private void Server_SendWorker(object obj)
        {
            object[] o = (object[])obj;
            System.Net.Sockets.Socket c = (System.Net.Sockets.Socket)o[0];
            server.Send((System.Net.IPEndPoint)c.RemoteEndPoint, o[1]);
        }


        #endregion


        void Start() {
            // Required for any additional communication.
            SupportedTypes = new List<Type>();
            mps = new BinaryFormatterSerializer();
            // Client Auth Object
            SupportedTypes.Add(typeof(UnityClientAuthorization));
            // Unity Objects
            SupportedTypes.Add(typeof(GameObject));
            SupportedTypes.Add(typeof(Transform));
            SupportedTypes.Add(typeof(Component));

        }

        public void Client_Connect() {
            if(!ClientConnected && ClientCredentials != null) {
                client = new TcpClient(mps);
                client.OnReceive += Client_OnReceive;
                client.OnConnected += Client_OnConnected;
                client.OnConnectionInterrupt += Client_OnConnectionInterrupt;
                client.OnError += Client_OnError;
                client.Connect(RemoteIP, RemotePort);
                server.Listen(100);
                OnLoginComplete = new LoginCompleteDelegate(() => Logincomplete());
            } else if (ClientCredentials == null) {
                Debug.Log("[C] No credentials.");
            } else if (!ClientConnected)  {
                Debug.Log("[C] Already connected.");
            }
        }


        private void Client_OnConnectionInterrupt(System.Net.Sockets.Socket sender) {
            ClientConnected = false;
        }

        private void Client_OnError(System.Net.Sockets.Socket sender, Exception e)
        {
            Debug.Log(e);
        }

        public void Start_Server() {
            server = new TcpServer(mps, RemotePort);
            server.OnReceive += Server_OnReceive;
            server.OnError += Server_OnError;
            server.OnConnected += InitateClient;
            server.Listen(100);
            Debug.Log("[S] Started on port '" + RemotePort + "'.");
        }

        private void Server_OnError(System.Net.Sockets.Socket sender, Exception e) {
            Debug.Log(e);
        }

        public Dictionary<string, SortedList<string, ComponentUpdate>> RecentUpdates = new Dictionary<string, SortedList<string, ComponentUpdate>>();
        private void syncComponentChanges() {
            //Component[] comps = gameObject.GetComponents<Component>();
            //for (int i = 0; i < comps.Length -1; i++) {
            //    Component c = comps[i];
            //    if (c.GetType() == typeof(Transform)) {
            //        Transform t = (Transform)c;
            //        ComponentUpdate cu = new ComponentUpdate(gameObject.name, t.root.name, t);
            //        if (RecentUpdates.ContainsKey(t.root.name)) {
            //            SortedList<string, ComponentUpdate> l = RecentUpdates[t.root.name];
            //            l.Add(t.GetType().Name + ":" + i, cu);
            //            RecentUpdates[t.root.name] = l;
            //        } else {
            //            SortedList<string, ComponentUpdate> l = new SortedList<string, ComponentUpdate>();
            //            l.Add(t.GetType().Name + ":" + i, cu);
            //            RecentUpdates.Add(t.root.name, l);
            //        }
            //        Debug.Log("[C] Syncing " + t.root.name + ":" + i + " (" + cu.ID + ")");
            //        Client_WaitSend(cu);
            //    }
            //}

            if (gameObject.transform.hasChanged) {
                gameObject.transform.hasChanged = false;
                ComponentUpdate cu = new ComponentUpdate(gameObject.name, gameObject.transform.root.name, gameObject.transform);

                if (RecentUpdates.ContainsKey(gameObject.transform.root.name)) {
                    SortedList<string, ComponentUpdate> l = RecentUpdates[gameObject.transform.root.name];
                    if (!l.ContainsKey(gameObject.transform.GetType().Name)) {
                        l.Add(gameObject.transform.GetType().Name, cu);
                    } else {
                        l[gameObject.transform.GetType().Name] = cu;
                    }
                    RecentUpdates[gameObject.transform.GetType().Name] = l;
                } else {
                    SortedList<string, ComponentUpdate> l = new SortedList<string, ComponentUpdate>();
                    l.Add(gameObject.transform.GetType().Name, cu);
                    if (!RecentUpdates.ContainsKey(gameObject.transform.root.name)) {
                        RecentUpdates.Add(gameObject.transform.root.name, l);
                    } else {
                        SortedList<string, ComponentUpdate> l2 = RecentUpdates[gameObject.transform.root.name];
                        if (!l2.ContainsKey(gameObject.transform.GetType().Name)) {
                            l2.Add(gameObject.transform.GetType().Name, cu);
                        } else {
                            l2[gameObject.transform.GetType().Name] = cu;
                        }
                        RecentUpdates[gameObject.transform.GetType().Name] = l2;
                    }
                }
                Debug.Log("[C] Syncing " + gameObject.transform.root.name + "(" + cu.ID + ")");
                Client_WaitSend(cu);
            }
        }

        void Update() {
            Tick();
            if (client.Connected)  {
                syncComponentChanges();
            }
            foreach (KeyValuePair<string, System.Net.Sockets.Socket> p in Players) {

            }
        }

        private List<Action> PendingEvents = new List<Action>();

        private void Tick() {
            if (PendingEvents.Count > 0) {
                Action e = PendingEvents[0];
                e.Invoke();
                PendingEvents.Remove(e);
            }
        }

    }

}
