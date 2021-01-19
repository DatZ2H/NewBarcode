using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;


namespace Common
{
    public class ClientSocket
    {
        public Socket clientSocket;
        public int bufferSize { get; set; } = 1024;
        public byte[] Buffer { get; private set; }
        public List<byte> BufferList { get; set; }
        public string ipAddress { get; set; }
        private int _port = 2112;
        public int Port
        {
            get => _port;
            set => _port = (value > 65535) || (value < 0) ? throw new ArgumentOutOfRangeException("Port", "Port can be from 0 to 65535") : value;
        }
    
        public int ReconnectTime { get; set; } = 30000;
        public int ReceiveTimeout { get; set; }
        public int SendTimeout { get; set; }
        public ClientSocket()
        {

        }
        public static Socket CreateSocket()
        {
            return new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }
        private void Connect()
        {
            Console.WriteLine("Connecting...");
            CreateSocket();
            int attempts = 0;
            while (!clientSocket.Connected)
            {
                try
                {
                    attempts++;
                    Console.WriteLine("Connection attempt " + attempts);
                    clientSocket.Connect(ipAddress, Port);

                }
                catch (SocketException)
                {
                    Console.Clear();
                }
            }
            Console.Clear();
            Console.WriteLine("Connected");

           
        }
        private void Disconnect()
        {
            clientSocket.Close();
            Console.WriteLine("Disconnected");
        }
        private void Reconnect()
        {
            int i = 1;
            while(clientSocket.Connected == false)
            {
               
                    Console.WriteLine(" Socket is not connected. Connection attempt {0}", i);
                    Connect();
                
            }
        }
    }
}
