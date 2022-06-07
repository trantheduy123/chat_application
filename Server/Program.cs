using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Security.Cryptography;
using System.Text;

namespace Chatroom
{

    delegate void MessageEventHandler(object sender, MessageEventArgs e);

    class MessageEventArgs : EventArgs
    {
        public string Message { get; private set; }

        public MessageEventArgs(string message)
        {
            this.Message = message;
        }
    }

    class Server
    {
        private TcpListener serverSocket;
        private List<Worker> workers = new List<Worker>();

        public Server(int port)
        {
            //serverSocket = new TcpListener(port);// deprecated
            // the same way
            serverSocket = new TcpListener(IPAddress.Any, port);
            serverSocket.Start();
        }

        private void WaitForConnection()
        {
            while (true)
            {
                TcpClient socket = serverSocket.AcceptTcpClient();
                Worker worker = new Worker(socket);
                AddWorker(worker);
                worker.Start();
            }
        }

        private void Worker_MessageReceived(object sender, MessageEventArgs e)
        {
            BroadcastMessage(sender as Worker, e.Message);
        }

        private void Worker_Disconnected(object sender, EventArgs e)
        {
            RemoveWorker(sender as Worker);
        }

        private void AddWorker(Worker worker)
        {
            lock (this)
            {
                workers.Add(worker);
                worker.Disconnected += Worker_Disconnected;
                worker.MessageReceived += Worker_MessageReceived;
            }
        }

        private void RemoveWorker(Worker worker)
        {
            lock (this)
            {
                worker.Disconnected -= Worker_Disconnected;
                worker.MessageReceived -= Worker_MessageReceived;
                workers.Remove(worker);
                worker.Close();
            }
        }

        private void BroadcastMessage(Worker from, String message)
        {
            lock (this)
            {
                message = string.Format("{0}: {1}", from.Username, message);
                for (int i = 0; i < workers.Count; i++)
                {
                    Worker worker = workers[i];
                    if (!worker.Equals(from))
                    {
                        try
                        {
                            worker.Send(message);
                        }
                        catch (Exception)
                        {
                            workers.RemoveAt(i--);
                            worker.Close();
                        }
                    }
                }
            }
        }

        class Worker
        {
            public event MessageEventHandler MessageReceived;
            public event EventHandler Disconnected;
            private readonly TcpClient socket;
            private readonly Stream stream;
            public string Username { get; private set; } = null;

            public Worker(TcpClient socket)
            {
                this.socket = socket;
                this.stream = socket.GetStream();
            }

            public void Send(string message)
            {
                byte[] buffer = Encoding.UTF8.GetBytes(message);
                stream.Write(buffer, 0, buffer.Length);
            }

            public void Start()
            {
                new Thread(Run).Start();
            }

            private void Run()
            {
                byte[] buffer = new byte[2018];
                try
                {
                    while (true)
                    {
                        int receivedBytes = stream.Read(buffer, 0, buffer.Length);
                        if (receivedBytes < 1)
                            break;
                        string message = Encoding.UTF8.GetString(buffer, 0, receivedBytes);
                        if (Username == null)
                            Username = message;
                        else
                            MessageReceived?.Invoke(this, new MessageEventArgs(message));
                    }
                }
                catch (IOException) { }
                catch (ObjectDisposedException) { }
                Disconnected?.Invoke(this, EventArgs.Empty);
            }

            public void Close()
            {
                socket.Close();
            }
        }

        static void Main(string[] args)
        {
            try
            {
                Server server = new Server(3393);
                server.WaitForConnection();
            }
            catch (IOException) { }
        }
    }
}