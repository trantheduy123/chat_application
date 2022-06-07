using System;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Chatroom
{
    class Client
    {
        private TcpClient socket;
        private Stream stream;

        public Client(string serverAddress, int serverPort)
        {
            socket = new TcpClient(serverAddress, serverPort);
            stream = socket.GetStream();
        }

        private void Send(string message)
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
            byte[] buffer = new byte[2048];
            try
            {
                while (true)
                {
                    int receivedBytes = stream.Read(buffer, 0, buffer.Length);
                    if (receivedBytes < 1)
                        break;
                    string message = Encoding.UTF8.GetString(buffer, 0, receivedBytes);
                    Console.WriteLine(message);
                }
            }
            catch (IOException) { }
            catch (ObjectDisposedException) { }
            Close();
            Environment.Exit(0);
        }

        private void Close()
        {
            socket.Close();
        }

        static void Main(string[] args)
        {
            static string GetHash(string plainText)
            {
                MD5 md5 = new MD5CryptoServiceProvider();
                // Compute hash from the bytes of text
                md5.ComputeHash(ASCIIEncoding.ASCII.GetBytes(plainText));
                // Get hash result after compute it
                byte[] result = md5.Hash;
                StringBuilder strBuilder = new StringBuilder();
                for (int i = 0; i < result.Length; i++)
                {
                    strBuilder.Append(result[i].ToString("x2"));
                }

                return strBuilder.ToString();
            }

            Client client = null;
            Console.WriteLine(" USERNAME 1: Xin vui nhập tên để được mã hóa: ");
            string username = GetHash(Console.ReadLine());
            Console.WriteLine("ten ban duoc ma hoa thanh day so sau:");
            Console.WriteLine(username);
            try
            {
                client = new Client("localhost", 3393);
                client.Send(username);
                client.Start();
                while (true)
                {
                    string message = Console.ReadLine();
                    client.Send(message);
                }
            }
            catch (IOException) { }
            catch (ObjectDisposedException) { }
            if (client != null)
                client.Close();
        }
    }
}