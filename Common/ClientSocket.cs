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

        public string ipAddress  { get; set; }

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
            this.ipAddress = IPAddress.Loopback.ToString();
            clientSocket = CreateSocket();
 
        }
        public ClientSocket(string ipAddress, int Port)
        {
            this.ipAddress = ipAddress;
            this.Port = Port;
            clientSocket=CreateSocket();
  
        }
        public static Socket CreateSocket()
        {
            return new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }
        public void Connect()
        {
            Console.WriteLine("Connecting...");
            Console.WriteLine("Watting connect with ipaddress is : {0} and port is : {1}" , ipAddress, Port);

         
            int attempts = 0;
            Console.WriteLine("gia tri cuar attempts {0} va connected la {1}", attempts, IsConnected());
            while (!IsConnected())
            {
                try
                {
                    attempts++;
                    Console.WriteLine("Connection attempt " + attempts);
                    clientSocket.Connect(ipAddress, Port);

                }
                catch (SocketException)
                {
                    Console.WriteLine("out ham oroi ");
                    Reconnect();
                    
                  //  Console.Clear();
                }
            }
          //  Console.Clear();
            Console.WriteLine("Connected");

           
        }
        private void Disconnect()
        {
            clientSocket.Close();
            Console.WriteLine("Disconnected");
        }
        public void Reconnect()
        {
            int i = 1;
            while(IsConnected()== false)
            {
               
                    Console.WriteLine(" Socket is not connected. Connection attempt {0}", i);
                    Connect();
                
            }
        }
        public bool IsConnected()
        {
            return clientSocket.Connected;
        }
        public void Read()
        {
            if (clientSocket.Connected == false)
            {
                Reconnect();
            }
            try
            {
                clientSocket.BeginReceive(Buffer, 0,bufferSize, SocketFlags.None, new AsyncCallback(ReadCallBack), this);
                Console.WriteLine("den read roi");
            }
            catch
            {

            }
        }
        private void ReadCallBack(IAsyncResult ar)
        {
            int bytesRead;
            try
            {
                bytesRead = clientSocket.EndReceive(ar);
                for(int i =0; i < bytesRead; i++)
                {
                    BufferList.Add(Buffer[i]);
                }
                Console.WriteLine("den read roi =====");
            }
            catch(SocketException)
            {
                Connect();
                
            }

        }
    }
}
