using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;

namespace BSICK.Sensors.LMS1xx
{
    public class BarcodeScanner
    {
        #region Enumérations

        public enum SocketConnectionResult
        {
            CONNECTED = 0,
            CONNECT_TIMEOUT = 1,
            CONNECT_ERROR = 2,
            DISCONNECTED = 3,
            DISCONNECT_TIMEOUT = 4,
            DISCONNECT_ERROR = 5,
        }
        public enum NetworkStreamResult
        {
            STARTED = 0,
            STOPPED = 1,
            TIMEOUT = 2,
            ERROR = 3,
            CLIENT_NOT_CONNECTED = 4,
        }

        #endregion

        #region Propriétés publiques

        public String IpAddress { get; set; }
        public int Port { get; set; }
        public int ReceiveTimeout { get; set; }
        public int SendTimeout { get; set; }
        public int HeartBeatTimeout { get; set; }
        private bool IsAutoConntecSet { get; set; }
        private bool IsNoReadOk { get; set; }
        private bool IsHeartBeatOk { get; set; }
        private bool IsTriggerOnOk { get; set; }
        private bool IsTriggerOffOk { get; set; }


        private byte[] SocketBuffer;
        public int socketBufferSize { get; set; } = 1024;
        public int barcodeLengthSize { get; set; }

        private List<byte> BufferResult = new List<byte>();
        public List<String> BufferFrameList { get; set; }
        public List<byte> msgTriggerON { get; set; }
        public List<byte> msgTriggerOFF { get; set; }
        public List<byte> msgTerminatorStart { get; set; }
        public List<byte> msgTerminatorStop { get; set; }
        public List<byte> msgHeartBeat { get; set; }
        public List<byte> msgNoRead { get; set; }

        // Case NOREAD     02 3c 53 54 41 52 54 3e 4d 49 53 53 3c 53 54 4f 50 3e 03
        // Case READ OK    02 3C 53 54 41 52 54 3E 30 35 30 2B 30 31 32 33 34 35 36 37 38 2D 30 31 32 33 34 35 36 37 38 2D 30 31 32 33 34 35 36 37 38 2D 30 31 32 33 34 35 36 37 38 2D 30 31 32 33 34 35 36 37 38 2D 3C 53 54 4F 50 3E 03
        // Case TRIGGERON  02 3c 53 54 41 52 54 3e 54 47 4f 4e 3c 53 54 4f 50 3e 03
        // Case TRIGGEROFF 02 3c 53 54 41 52 54 3e 54 47 4f 46 3c 53 54 4f 50 3e 03
        // Case PING       02 3C 53 54 41 52 54 3E 50 49 4E 47 3C 53 54 4F 50 3E 03

        private static ManualResetEvent ConnectedHandler = new ManualResetEvent(false);

        private static ManualResetEvent ParssingdHandler = new ManualResetEvent(false);

        private Thread CheckKeepConnectThread;

        #endregion

        #region Properties

        private Socket clientSocket;

        #endregion

        #region Constructeurs

        public BarcodeScanner()
        {

            this.clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { ReceiveTimeout = 1000, SendTimeout = 1000 };
            this.IpAddress = String.Empty;
            this.Port = 0;
            this.HeartBeatTimeout = 30000;
            //CheckKeepConnectThread = new Thread(() => CheckKeepConnect());
            Thread SocketParsingDebugThread = new Thread(() => BufferParsingDebugThread());
            SocketParsingDebugThread.Start();
            Console.WriteLine("Thread is ok");
            SocketBuffer = new byte[socketBufferSize];
            BufferResult = new List<byte>();
            BufferFrameList = new List<string>();
            barcodeLengthSize = 4;
            msgTriggerON = new List<byte>() { 0x54, 0x47, 0x4f, 0x4e }; // "TGON"
            msgTriggerOFF = new List<byte>() { 0x54, 0x47, 0x4f, 0x46 };// "TGOF"
            msgTerminatorStart = new List<byte>() { 0x02, 0x3c, 0x53, 0x54, 0x41, 0x52, 0x54, 0x3e }; //"STX<START>"
            msgTerminatorStop = new List<byte>() { 0x3c, 0x53, 0x54, 0x4f, 0x50, 0x3e, 0x03 };        //"<STOP>ETX"
            msgHeartBeat = new List<byte>() { 0x50, 0x49, 0x4e, 0x47 };// "PING"
            msgNoRead = new List<byte>() { 0x4d, 0x49, 0x53, 0x53 };// "MISS"


        }

        public BarcodeScanner(string ipAdress, int port, int receiveTimeout, int sendTimeout, int heartBeatTimeout)
        {
            this.clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { ReceiveTimeout = receiveTimeout, SendTimeout = sendTimeout };
            this.IpAddress = ipAdress;
            this.Port = port;
            this.HeartBeatTimeout = heartBeatTimeout;
            //CheckKeepConnectThread = new Thread(() => CheckKeepConnect());
            Thread SocketParsingDebugThread = new Thread(() => BufferParsingDebugThread());
            SocketParsingDebugThread.Start();
            Console.WriteLine("Thread is ok");
            SocketBuffer = new byte[socketBufferSize];
            BufferResult = new List<byte>();
            BufferFrameList = new List<string>();
            barcodeLengthSize = 4;
            msgTriggerON = new List<byte>() { 0x54, 0x47, 0x4f, 0x4e }; // "TGON"
            msgTriggerOFF = new List<byte>() { 0x54, 0x47, 0x4f, 0x46 };// "TGOF"
            msgTerminatorStart = new List<byte>() { 0x02, 0x3c, 0x53, 0x54, 0x41, 0x52, 0x54, 0x3e }; //"STX<START>"
            msgTerminatorStop = new List<byte>() { 0x3c, 0x53, 0x54, 0x4f, 0x50, 0x3e, 0x03 };        //"<STOP>ETX"
            msgHeartBeat = new List<byte>() { 0x50, 0x49, 0x4e, 0x47 };// "PING"
            msgNoRead = new List<byte>() { 0x4d, 0x49, 0x53, 0x53 };// "MISS"

        }

        #endregion

        #region Methodes de base pour le pilotage du capteur

        public bool IsSocketConnected()
        {
            return clientSocket.Connected;
        }
        public bool IsCheckKeepConnectThreadAlive()
        {
            return CheckKeepConnectThread.IsAlive;
        }
        public bool IsNumber(byte value) // 48 is "0"  and 57 is "9"
        {
            Console.WriteLine("gia tri value {0}  ", value);
            if (48 <= value && value <= 57)
            {
                return true;
            }
            else
            {
                return false;
            }

        }
        public bool IsEqualMsg(List<byte> bufferIn, int startIndex, List<byte> bufferOut)
        {
            bool ketqua;
            ketqua = bufferIn.GetRange(startIndex, bufferOut.Count).SequenceEqual(bufferOut);
            return bufferIn.GetRange(startIndex, bufferOut.Count).SequenceEqual(bufferOut);


        }
        public void ClearBufferResult(int StartIndex, int Index)
        {
            BufferResult.RemoveRange(StartIndex, Index);
        }
        public void CheckKeepConnect()
        {
            Console.WriteLine("the system  check keep connect ");
            while (IsSocketConnected())
            {
                if (!ConnectedHandler.WaitOne(this.HeartBeatTimeout))
                {
                    ConnectedHandler.Reset();
                    Console.WriteLine("the system CheckKeepConnect {0}", this.Disconnect());
                    Console.WriteLine("the system disconnect because don't catch heartbeat");

                    break;

                }
                // Thread.Sleep(2000);

            }
            Console.WriteLine("aaaaaaaaaaaaaaaaaaaaaaaaaa");
            Console.WriteLine("aaaaaaaaaaaaaaaaaaaaaaaaaa          {0}", IsSocketConnected());
            CheckKeepConnectThread = null;
        }

        public SocketConnectionResult Connect()
        {
            Console.WriteLine("the system Connecting");
            SocketConnectionResult status;
            if (this.IsSocketConnected())
            {
                status = SocketConnectionResult.CONNECTED;
            }
            else
            {
                status = SocketConnectionResult.DISCONNECTED;
            }

            while (status != SocketConnectionResult.CONNECTED)
            {
                Console.WriteLine("status ----------{0}", status);
                try
                {
                    Thread.Sleep(1000);
                    Console.WriteLine("--------------");
                    clientSocket.Connect(this.IpAddress, this.Port);
                    status = SocketConnectionResult.CONNECTED;
                    this.SocketBeginReceive();

                    Console.WriteLine("the system ConnectEDDDDDD");
                    if (CheckKeepConnectThread == null)
                    {
                        CheckKeepConnectThread = new Thread(() => CheckKeepConnect());
                        CheckKeepConnectThread.Start();
                    }
                    else
                    {
                        Console.WriteLine("Thread is runnign");
                    }
                }
                catch (TimeoutException)
                {
                    status = SocketConnectionResult.CONNECT_TIMEOUT;

                    Console.WriteLine("the system TimeoutException {0}", this.Disconnect());
                    return status;
                }
                catch (SystemException ex)
                {
                    status = SocketConnectionResult.CONNECT_ERROR;
                    Console.WriteLine("this error is {0}", ex);
                    Console.WriteLine("the system SystemException {0}", this.Disconnect());
                    return status;
                }

            }
            return status;
        }
        public SocketConnectionResult AutoConnect()
        {
            SocketConnectionResult status;

            while (!IsSocketConnected())
            {

                this.Connect();

            }
            IsAutoConntecSet = true;
            status = SocketConnectionResult.CONNECTED;
            return status;

        }



        private void BufferParsingDebugThread()
        {
            while (true)
            {

                if (ParssingdHandler.WaitOne(1000)) continue;

                if (!ParssingdHandler.Reset()) continue;

                if (IsSocketConnected())
                {
                    Console.WriteLine("IsMsgTerminatorStart  {0}", IsSocketConnected());

                    SocketParsing();
                }
            }



        }
        private void SocketParsing()
        {


            while ((BufferResult.Count >= (msgTerminatorStart.Count + barcodeLengthSize + msgTerminatorStop.Count)) && IsSocketConnected())
            {
                Console.WriteLine("BufferResult.Count");
                bool IsMsgTerminatorStart = true;
                bool IsMsgTerminatorStop = true;
                for (int i = 0; i < msgTerminatorStart.Count; i++)
                {
                    Console.WriteLine("Start-true");
                    if (BufferResult[i] != msgTerminatorStart[i])
                    {
                        IsMsgTerminatorStart = false;
                        ClearBufferResult(0, 1);
                        Console.WriteLine("Start-true");
                        break;
                    }

                }
                if (IsMsgTerminatorStart)
                {
                    Console.WriteLine("IsMsgTerminatorStart");
                    Console.WriteLine("-----------------------------");
                    Console.WriteLine("IsMsgTerminatorStart 1 {0}", BufferResult[msgTerminatorStart.Count]);
                    Console.WriteLine("IsMsgTerminatorStart 2 {0}", BufferResult[msgTerminatorStart.Count + 1]);
                    Console.WriteLine("IsMsgTerminatorStart 3 {0}", BufferResult[msgTerminatorStart.Count + 2]);
                    Console.WriteLine("IsMsgTerminatorStart --{0}", (IsNumber(BufferResult[msgTerminatorStart.Count])) && (IsNumber(BufferResult[msgTerminatorStart.Count + 1])) && (IsNumber(BufferResult[msgTerminatorStart.Count + 2])));
                    Console.WriteLine("-----------------------------");

                    if ((IsNumber(BufferResult[msgTerminatorStart.Count])) && (IsNumber(BufferResult[msgTerminatorStart.Count + 1])) && (IsNumber(BufferResult[msgTerminatorStart.Count + 2])))
                    {
                        Console.WriteLine("IS NUMBER");
                        int BarcodeLength = 0;
                        BarcodeLength = Int32.Parse(Encoding.ASCII.GetString(BufferResult.ToArray(), msgTerminatorStart.Count, barcodeLengthSize - 1));
                        int LengthToMsgStop = msgTerminatorStart.Count + barcodeLengthSize + BarcodeLength;
                        for (int i = LengthToMsgStop; i < (LengthToMsgStop + msgTerminatorStop.Count); i++)
                        {
                            if (BufferResult[i] != msgTerminatorStop[i - LengthToMsgStop])
                            {
                                IsMsgTerminatorStop = false;
                                ClearBufferResult(0, i);
                                break;
                            }

                        }
                        if (IsMsgTerminatorStart && IsMsgTerminatorStop)
                        {
                            BufferFrameList.Add(Encoding.ASCII.GetString(BufferResult.ToArray(), msgTerminatorStart.Count + barcodeLengthSize, (int)BarcodeLength));
                            ClearBufferResult(0, (LengthToMsgStop + msgTerminatorStop.Count));
                            BufferFrameList.ForEach(Console.WriteLine);
                        }


                    }
                    else
                    {
                        Console.WriteLine("FUNCTION");
                        if (IsEqualMsg(BufferResult, msgTerminatorStart.Count, msgNoRead))
                        {
                            IsNoReadOk = true;
                            Console.WriteLine("NOREAD");
                            ClearBufferResult(0, msgHeartBeat.Count);
                        }
                        else if (IsEqualMsg(BufferResult, msgTerminatorStart.Count, msgHeartBeat))
                        {

                            IsHeartBeatOk = true;
                            ConnectedHandler.Set();
                            ClearBufferResult(0, msgHeartBeat.Count);
                            Console.WriteLine("msgHeartBeat");
                        }
                        else if (IsEqualMsg(BufferResult, msgTerminatorStart.Count, msgTriggerON))
                        {
                            IsTriggerOnOk = true;
                            ClearBufferResult(0, msgHeartBeat.Count);
                            Console.WriteLine("msgTriggerON");
                        }
                        else if (IsEqualMsg(BufferResult, msgTerminatorStart.Count, msgTriggerOFF))
                        {
                            IsTriggerOffOk = true;
                            ClearBufferResult(0, msgHeartBeat.Count);
                            Console.WriteLine("msgTriggerOFF");
                        }
                        else
                        {
                            ClearBufferResult(0, 4);
                            Console.WriteLine("ERROR");
                        }
                    }

                }
            }

        }


        public SocketConnectionResult Disconnect()
        {
            SocketConnectionResult status;
            if (this.IsSocketConnected())
            {
                status = SocketConnectionResult.CONNECTED;
            }
            else
            {
                status = SocketConnectionResult.DISCONNECTED;
            }
            if (status == SocketConnectionResult.CONNECTED)
            {
                try
                {
                    clientSocket.Close();

                    clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { ReceiveTimeout = this.ReceiveTimeout };
                    status = SocketConnectionResult.DISCONNECTED;
                    if (IsAutoConntecSet == true)

                    {
                        Console.WriteLine("STATTTTTTTTTT");
                        while (IsSocketConnected()) ;
                        Console.WriteLine("gaiiiiiiiiiiiiiiiiiiiii{0}", this.Connect());
                        Console.WriteLine("autoconnec laiiiiii");
                        Console.WriteLine("autoconnec laiiiiii{0}", IsSocketConnected());
                    }
                }
                catch (TimeoutException)
                {
                    status = SocketConnectionResult.DISCONNECT_TIMEOUT;
                    return status;
                }
                catch (SystemException)
                {
                    status = SocketConnectionResult.DISCONNECT_ERROR;
                    return status;
                }

            }
            return status;
        }

        private SocketConnectionResult SocketBeginReceive()
        {
            SocketConnectionResult status;
            if (this.IsSocketConnected())
            {
                status = SocketConnectionResult.CONNECTED;
                try
                {
                    clientSocket.BeginReceive(SocketBuffer, 0, SocketBuffer.Length, SocketFlags.None, SocketReceivedCallBack, this);
                }
                catch
                {
                    if (this.IsSocketConnected())
                    {
                        status = SocketConnectionResult.CONNECTED;
                        this.Connect();
                    }
                    else
                    {
                        status = SocketConnectionResult.DISCONNECTED;
                    }
                }
            }
            else
            {
                status = SocketConnectionResult.DISCONNECTED;
            }




            return status;
        }
        private void SocketReceivedCallBack(IAsyncResult ar)
        {

            if (this.IsSocketConnected())
            {

                try
                {
                    int byteRead = clientSocket.EndReceive(ar);
                    byte[] data = new byte[byteRead];
                    Array.Copy(SocketBuffer, 0, data, 0, byteRead);
                    for (int i = 0; i < byteRead; i++)
                    {
                        Console.WriteLine("gia tri nha dk laf laf laf {0},{1}", i, data[i]);

                    }
                    Console.WriteLine("gia tri do laf {0}---------", Encoding.ASCII.GetString(data.ToArray(), 0, data.Length));
                    SocketReceived(data);
                    ConnectedHandler.Set();
                }
                catch
                {
                    if (!this.IsSocketConnected())
                    {

                        this.Connect();
                    }
                    else
                    {

                    }
                }
            }
            else
            {

            }

        }
        private void SocketReceived(byte[] data)

        {

            BufferResult.AddRange(data);


            this.SocketBeginReceive();
        }

        public NetworkStreamResult Start()
        {
            byte[] cmd = new byte[18] { 0x02, 0x73, 0x4D, 0x4E, 0x20, 0x4C, 0x4D, 0x43, 0x73, 0x74, 0x61, 0x72, 0x74, 0x6D, 0x65, 0x61, 0x73, 0x03 };

            NetworkStreamResult status;
            if (clientSocket.Connected)
            {
                try
                {

                    // serverStream.Write(cmd, 0, cmd.Length);
                    status = NetworkStreamResult.STARTED;
                }
                catch (TimeoutException)
                {
                    status = NetworkStreamResult.TIMEOUT;
                    this.Disconnect();
                    return status;
                }
                catch (SystemException)
                {
                    status = NetworkStreamResult.ERROR;
                    this.Disconnect();
                    return status;
                }
            }
            else
            {
                status = NetworkStreamResult.CLIENT_NOT_CONNECTED;
            }

            return status;
        }



        public NetworkStreamResult Stop()
        {
            byte[] cmd = new byte[17] { 0x02, 0x73, 0x4D, 0x4E, 0x20, 0x4C, 0x4D, 0x43, 0x73, 0x74, 0x6F, 0x70, 0x6D, 0x65, 0x61, 0x73, 0x03 };

            NetworkStreamResult status;
            if (clientSocket.Connected)
            {
                try
                {


                    //serverStream.Write(cmd, 0, cmd.Length);
                    status = NetworkStreamResult.STOPPED;
                }
                catch (TimeoutException)
                {
                    status = NetworkStreamResult.TIMEOUT;
                    this.Disconnect();
                    return status;
                }
                catch (SystemException)
                {
                    status = NetworkStreamResult.ERROR;
                    this.Disconnect();
                    return status;
                }
            }
            else
            {
                status = NetworkStreamResult.CLIENT_NOT_CONNECTED;
            }

            return status;
        }


        public byte[] ExecuteRaw(byte[] streamCommand)
        {
            try
            {

                // serverStream.Write(streamCommand, 0, streamCommand.Length);
                //serverStream.Flush();

                byte[] inStream = new byte[clientSocket.ReceiveBufferSize];
                //serverStream.Read(inStream, 0, (int)clientSocket.ReceiveBufferSize);

                return inStream;
            }
            catch (Exception)
            {
                return null;
            }
        }



        #endregion
    }
}