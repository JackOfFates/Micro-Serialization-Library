using MicroSerializationLibrary.Networking.Client;
using MicroSerializationLibrary.Networking.Server;
using MicroSerializationLibrary.Serialization;
using MicroSerializationLibrary.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class Program
    {

       static MicroNode M = new MicroNode();
   

        static void Main(string[] args)
        {
            M.RemoteIP = "127.0.0.1";
            M.RemotePort = 7862;
            M.Start_Server();
            Thread.SpinWait(333);
            M.Client_Connect();
            Thread t = new Thread(new ThreadStart(UpdateServerClientLogicFakeUnity));
            t.Start();
        }

        static private void UpdateServerClientLogicFakeUnity()
        {
            do
            {
                M.Tick();
                Thread.SpinWait(12);
            } while (true);
        }
    }
    [Serializable()]
    public class ComponentUpdate
    {

        public string UpdateHash;

        public string ID;

        public object data;

        public ComponentUpdate(string ID, string hash, object newData)
        {
            this.ID = ID;
            this.data = newData;
            this.UpdateHash = hash;
        }

        public ComponentUpdate()
        {

        }

    }

    public class MicroNode {
        public bool AutoLoadGameAfterLogin = false;
        public string GameScene = "";
        public delegate void LoginCompleteDelegate();
        public event LoginCompleteDelegate OnLoginComplete;

        public UnityClientAuthorization UnityAuthorizationKey;
        public List<Type> SupportedTypes;
        public BinaryFormatterSerializer mps;

        #region Client
        public TcpClient client;
        public bool ClientConnected = false;
        public String RemoteIP;
        public int RemotePort;

        public MicroNode() {
            // Required for any additional communication.
            SupportedTypes = new List<Type>();
            mps = new BinaryFormatterSerializer();
            SupportedTypes.Add(typeof(UnityClientAuthorization));

        }

        private void Client_OnReceive(System.Net.Sockets.Socket sender, object obj, int BytesReceived) {
            if (CanProcessObject(obj.GetType())) {
                ProcessObject_Client(obj);
            }

        }
        private void Client_OnConnected(System.Net.Sockets.Socket c) {
            ClientConnected = true;
            Console.WriteLine("[C] Connected to " + c.RemoteEndPoint.ToString());
        }
        private void ProcessObject_Client(object o) {
            Console.WriteLine("[C] Recieved an object of the type " + o.GetType().ToString() + " from [S]ERVER");
            if (o.GetType() == typeof(UnityClientAuthorization))
                PendingEvents.Add((Action)(() => Unity_Auth(o)));

        }

        public void Unity_Auth(object o) {
            UnityClientAuthorization Key = (UnityClientAuthorization)o;
            if (Key.ClientInfo.Username == null) {
                // New Authorization
                Key.SocketHandle = (int)client.BaseSocket.Handle;
                Console.WriteLine("[C] Authorizing with server");
                Console.WriteLine("Enter a username.");
                string UI_U = Console.ReadLine();
                string UI_P = UI_U;
                Key.ResolveClientInfo(client.BaseSocket, UI_U, UI_P);

                Client_WaitSend(Key);
            } else {
                // Client Authorized
                this.UnityAuthorizationKey = Key;
                OnLoginComplete();
            }
        }

        private void Logincomplete() {
       
            Console.WriteLine("[C] Authorization accepted");
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
            Console.WriteLine("[S] Connecting to " + c.RemoteEndPoint.ToString());
            Server_WaitSend(c, new UnityClientAuthorization(c));
        }

        private void Server_OnReceive(System.Net.Sockets.Socket sender, object obj, int BytesReceived) {
            Console.WriteLine("[S] Received an object of the type " + obj.GetType().ToString() + " from " + sender.RemoteEndPoint.ToString());
            if (CanProcessObject(obj.GetType())) {
                ProcessObject_Server(sender, obj);
            }
        }

        private void ProcessObject_Server(System.Net.Sockets.Socket sender, object o) {
            Console.WriteLine("[S] Processing " + o.GetType().ToString() + " from " + sender.RemoteEndPoint.ToString());
            if (o.GetType() == typeof(UnityClientAuthorization)) {
                PendingEvents.Add((Action)(() => Unity_Login(sender, o)));
            }
        }

        public void Unity_Login(System.Net.Sockets.Socket sender, object o) {
            // Start Authorization Process
            UnityClientAuthorization UCA = (UnityClientAuthorization)o;
            Console.WriteLine("[S] User '" + UCA.ClientInfo.Username + "' logged into the server.");

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

        private void Server_SendWorker(object obj) {
            object[] o = (object[])obj;
            System.Net.Sockets.Socket c = (System.Net.Sockets.Socket)o[0];
            server.Send((System.Net.IPEndPoint)c.RemoteEndPoint, o[1]);
        }


        #endregion


        void Start() {


        }

        public void Client_Connect() {
            if (!ClientConnected) {
                client = new TcpClient(mps);
                client.OnReceive += Client_OnReceive;
                client.OnConnected += Client_OnConnected;
                client.OnConnectionInterrupt += Client_OnConnectionInterrupt;
                client.OnError += Client_OnError;
                client.Connect(RemoteIP, RemotePort);
                server.Listen(100);
                OnLoginComplete = new LoginCompleteDelegate(() => Logincomplete());
            }
        }


        private void Client_OnConnectionInterrupt(System.Net.Sockets.Socket sender) {
            ClientConnected = false;
        }

        private void Client_OnError(System.Net.Sockets.Socket sender, Exception e) {
            Console.WriteLine(e);
        }

        public void Start_Server() {
            server = new TcpServer(mps, RemotePort);
            server.OnReceive += Server_OnReceive;
            server.OnError += Server_OnError;
            server.OnConnected += InitateClient;
            server.Listen(100);
            Console.WriteLine("[S] Started on port '" + RemotePort + "'.");
        }

        private void Server_OnError(System.Net.Sockets.Socket sender, Exception e) {
            Console.WriteLine(e);
        }

        public Dictionary<string, ComponentUpdate> RecentUpdates = new Dictionary<string, ComponentUpdate>();

        void Update() {
            Tick();
            //syncComponentChanges();
            foreach (KeyValuePair<string, System.Net.Sockets.Socket> p in Players) {

            }
        }

        private List<Action> PendingEvents = new List<Action>();

        public void Tick() {
            if (PendingEvents.Count > 0) {
                Action e = PendingEvents[0];
                e.Invoke();
                PendingEvents.Remove(e);
            }
        }

    }

}
