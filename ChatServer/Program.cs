using System;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ChatServer
{
    // The ChatServer executable is in bin\Release directory. 
    // It runs on Microsoft Windows OS with .Net Framework version >= 4.5.2. 
    // It opens a console window and run chat server listening on tcp port 10000. When the telnet client connects, 
    // server assign a client id to the new client and run new thread th handle communications between client and 
    // server. Logging messages is minimal.
    
    class Program
    {
        const int TCPSERVERPORT = 10000;

        // lock the dictionary entry to safe access
        static readonly object _lock = new object();

        // clients List for messages broadcast
        static readonly Dictionary<int, TcpClient> clientsList = new Dictionary<int, TcpClient>();

        static void Main(string[] args)
        {
            int clientId = 1;

            // start server on tcp port TCPSERVERPORT
            TcpListener ServerSocket = new TcpListener(IPAddress.Any, TCPSERVERPORT);
            ServerSocket.Start();
            Console.WriteLine("Chat Server Started ....");

            while (true) // wait for client connection
            {
                TcpClient clientSocket = ServerSocket.AcceptTcpClient();

                // client has connected 
                lock (_lock) clientsList.Add(clientId, clientSocket);
                
                Console.WriteLine("Someone connected!!");

                // start new thread to handle this client
                Thread t = new Thread(HandleClients);
                t.Start(clientId);
                clientId++;
            }
        }

        /// <summary>
        /// Handle client communication
        /// </summary>
        /// <param name="clientId">the client id</param>
        public static void HandleClients(object clientId)
        {
            int id = (int)clientId;
            TcpClient client;

            // lock the dictionary entry to safe access
            lock (_lock) client = clientsList[id];
            
            // to print welcome message with client id info
            bool firstMessage = true;

            while (true)
            {
                // allocate message buffer
                byte[] buffer = new byte[(int)client.ReceiveBufferSize];

                // get data stream from client
                NetworkStream stream = client.GetStream();

                // if byte_count = 0 then shutdown this connection and remove client from list
                int byte_count = stream.Read(buffer, 0, (int)client.ReceiveBufferSize);
                if (byte_count == 0)
                {
                    BroadcastMessages("Closed connection" + Environment.NewLine, id);
                    lock (_lock) clientsList.Remove(id);

                    // shutdown client connection
                    client.Client.Shutdown(SocketShutdown.Both);
                    client.Close();
                    
                    // exit from loop
                    break;
                }

                if (buffer[0] != 0xFF)
                {
                    string data = Encoding.ASCII.GetString(buffer);
                    if (!data.StartsWith("\u001b"))
                    {
                        BroadcastMessages(data, id);
                    }
                }

                // welcome message to the new client
                if (firstMessage)
                {
                    firstMessage = false;
                    byte[] welcomeMessage = Encoding.ASCII.GetBytes(string.Format("Welcome. Your Client Id is: {0}", id) + Environment.NewLine);
                    stream.Write(welcomeMessage, 0, welcomeMessage.Length);
                    BroadcastMessages("Is now connected" + Environment.NewLine, id);
                }
            }
        }

        /// <summary>
        /// Broadcast messages to all client
        /// </summary>
        /// <param name="data">The message</param>
        /// <param name="id">The message's Client Id </param>
        public static void BroadcastMessages(string data, int id)
        {
            try
            {
                if (!data.StartsWith("\r\n")) // suppress extra new line
                {
                    data = string.Format("[Client {0}] {1}", id, data);
                }

                byte[] buffer = Encoding.ASCII.GetBytes(data);
                lock (_lock)
                {
                    // broadcast to all clients with clientId different from id
                    var clients = clientsList.Where(x => x.Key != id).Select(x => x.Value);

                    // send message to all other clients
                    foreach (TcpClient client in clients)
                    {
                        NetworkStream stream = client.GetStream();
                        stream.Write(buffer, 0, buffer.Length);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Communication error while broadcasting message: {0}", ex.Message));
            }
        }
    }
}
