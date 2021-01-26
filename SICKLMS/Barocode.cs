using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;

namespace SickBarcodeScanner
{
    public class BarcodeScanner
    {
        #region ENUM

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
            SUCCESSED = 2,
            TIMEOUT = 3,
            ERROR = 4,
            CLIENT_NOT_CONNECTED = 5,
        }

        #endregion

        #region Properties

        public String IpAddress { get; set; }
        public int Port { get; set; }
        public int ReceiveTimeout { get; set; }
        public int SendTimeout { get; set; }
        public int HeartBeatTimeout { get; set; }
        public int WaitReceiveBarcodeTimeOut { get; set; }
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

        private static AutoResetEvent ConnectedHandler = new AutoResetEvent(false);

        private static AutoResetEvent ParssingdHandler = new AutoResetEvent(false);

        private static AutoResetEvent NoReaddHandler = new AutoResetEvent(false);

        private static AutoResetEvent TriggerONdHandler = new AutoResetEvent(false);

        private static AutoResetEvent TriggerOFFdHandler = new AutoResetEvent(false);

        private static AutoResetEvent ResultedHandler = new AutoResetEvent(false);

        private Thread CheckKeepConnectThread;

        private Socket clientSocket;

        #endregion


        #region Constructors

        public BarcodeScanner()
        {

            this.clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { ReceiveTimeout = 1000, SendTimeout = 1000 };
            this.IpAddress = String.Empty;
            this.Port = 0;
            this.HeartBeatTimeout = 30000;
            this.WaitReceiveBarcodeTimeOut = 600000;
            Thread SocketParsingDebugThread = new Thread(() => BufferParsingDebugThread());
            SocketParsingDebugThread.Start();
            SocketBuffer = new byte[socketBufferSize];
            BufferResult = new List<byte>();
            BufferFrameList = new List<string>();
            barcodeLengthSize = 4;
            this.WaitReceiveBarcodeTimeOut = 600000;
            msgTriggerON = new List<byte>() { 0x54, 0x47, 0x4f, 0x4e }; // "TGON"
            msgTriggerOFF = new List<byte>() { 0x54, 0x47, 0x4f, 0x46 };// "TGOF"
            msgTerminatorStart = new List<byte>() { 0x02, 0x3c, 0x53, 0x54, 0x41, 0x52, 0x54, 0x3e }; //"STX<START>"
            msgTerminatorStop = new List<byte>() { 0x3c, 0x53, 0x54, 0x4f, 0x50, 0x3e, 0x03 };        //"<STOP>ETX"
            msgHeartBeat = new List<byte>() { 0x50, 0x49, 0x4e, 0x47 };// "PING"
            msgNoRead = new List<byte>() { 0x4d, 0x49, 0x53, 0x53 };// "MISS"


            Console.WriteLine("");
        }

        public BarcodeScanner(string ipAdress, int port, int receiveTimeout, int sendTimeout, int heartBeatTimeout)
        {
            this.clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { ReceiveTimeout = receiveTimeout, SendTimeout = sendTimeout };
            this.IpAddress = ipAdress;
            this.Port = port;
            this.HeartBeatTimeout = heartBeatTimeout;
            this.WaitReceiveBarcodeTimeOut = 600000;
            Thread SocketParsingDebugThread = new Thread(() => BufferParsingDebugThread());
            SocketParsingDebugThread.Start();
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

        #region Method

        public bool IsSocketConnected()
        {
            return clientSocket.Connected;
        }
        public bool IsCheckKeepConnectThreadAlive()
        {
            return CheckKeepConnectThread.IsAlive;
        }
        private bool IsNumber(byte value) // 48 is "0"  and 57 is "9"
        {
            if (48 <= value && value <= 57)
            {
                return true;
            }
            else
            {
                return false;
            }

        }
        private bool IsEqualMsg(List<byte> bufferIn, int startIndex, List<byte> bufferOut)
        {
            bool ketqua;
            ketqua = bufferIn.GetRange(startIndex, bufferOut.Count).SequenceEqual(bufferOut);
            return bufferIn.GetRange(startIndex, bufferOut.Count).SequenceEqual(bufferOut);


        }
        private void ClearBufferResult(int StartIndex, int Index)
        {
            BufferResult.RemoveRange(StartIndex, Index);
        }
        public void CheckKeepConnect()
        {
            while (IsSocketConnected())
            {
                if (!ConnectedHandler.WaitOne(this.HeartBeatTimeout))
                {

                    Console.WriteLine("CheckKeepConnect is {0}", this.Disconnect());

                    break;

                }

            }
            Console.WriteLine("CheckKeepConnect is {0} out ", this.Disconnect());
            CheckKeepConnectThread = null;
        }

        public SocketConnectionResult Connect()
        {
            Console.WriteLine("Connect System connecting...");
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

                try
                {
                    clientSocket.Connect(this.IpAddress, this.Port);
                    status = SocketConnectionResult.CONNECTED;
                    this.SocketBeginReceive();

                    if (CheckKeepConnectThread == null)
                    {
                        CheckKeepConnectThread = new Thread(() => CheckKeepConnect());
                        CheckKeepConnectThread.Start();
                        Console.WriteLine("Connect try beginReceive,CheckKeepConnectThread Start.. ");
                    }
                    else
                    {
                        Console.WriteLine("Thread is running");
                    }
                }
                catch (TimeoutException)
                {
                    status = SocketConnectionResult.CONNECT_TIMEOUT;

                    Console.WriteLine("The System Connect TimeoutException {0}", this.Disconnect());
                    return status;
                }
                catch (SystemException)
                {
                    status = SocketConnectionResult.CONNECT_ERROR;
                    Console.WriteLine("The System Connect SystemException {0}", this.Disconnect());
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
                if (IsSocketConnected())
                {

                    SocketParsing();
                    Console.WriteLine("BufferParsingDebugThread Runing and SocketParsing   ");
                }
            }



        }
        private void SocketParsing()
        {


            while ((BufferResult.Count >= (msgTerminatorStart.Count + barcodeLengthSize + msgTerminatorStop.Count)) && IsSocketConnected())
            {

                bool IsMsgTerminatorStart = true;
                bool IsMsgTerminatorStop = true;
                for (int i = 0; i < msgTerminatorStart.Count; i++)
                {

                    if (BufferResult[i] != msgTerminatorStart[i])
                    {
                        IsMsgTerminatorStart = false;
                        ClearBufferResult(0, 1);

                        break;
                    }

                }
                if (IsMsgTerminatorStart)
                {

                    if ((IsNumber(BufferResult[msgTerminatorStart.Count])) && (IsNumber(BufferResult[msgTerminatorStart.Count + 1])) && (IsNumber(BufferResult[msgTerminatorStart.Count + 2])))
                    {
                        Console.WriteLine("SocketParsing => IS NUMBER");
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
                            ResultedHandler.Set();
                            ClearBufferResult(0, (LengthToMsgStop + msgTerminatorStop.Count));
                            BufferFrameList.ForEach(Console.WriteLine);
                            Console.WriteLine("SocketParsing => Resulted is ok");
                        }


                    }
                    else
                    {
                        Console.WriteLine("SocketParsing => is FUNCTION");
                        if (IsEqualMsg(BufferResult, msgTerminatorStart.Count, msgNoRead))
                        {
                            IsNoReadOk = true;
                            NoReaddHandler.Set();
                            Console.WriteLine("SocketParsing => NOREAD");
                            ClearBufferResult(0, msgNoRead.Count);
                        }
                        else if (IsEqualMsg(BufferResult, msgTerminatorStart.Count, msgHeartBeat))
                        {

                            IsHeartBeatOk = true;
                            ConnectedHandler.Set();
                            ClearBufferResult(0, msgHeartBeat.Count);
                            Console.WriteLine("SocketParsing => PING");
                        }
                        else if (IsEqualMsg(BufferResult, msgTerminatorStart.Count, msgTriggerON))
                        {
                            IsTriggerOnOk = true;
                            TriggerONdHandler.Set();
                            ClearBufferResult(0, msgTriggerON.Count);
                            Console.WriteLine("SocketParsing => TRIGGER ON OK");
                        }
                        else if (IsEqualMsg(BufferResult, msgTerminatorStart.Count, msgTriggerOFF))
                        {
                            IsTriggerOffOk = true;
                            TriggerOFFdHandler.Set();
                            ClearBufferResult(0, msgTriggerOFF.Count);
                            Console.WriteLine("SocketParsing TRIGGER OFF OK");
                        }
                        else
                        {
                            ClearBufferResult(0, 4);
                            Console.WriteLine("SocketParsing => ERROR");
                        }
                    }

                }
            }

        }

        public NetworkStreamResult SetTrigger()
        {
            NetworkStreamResult status = NetworkStreamResult.ERROR;
            byte[] cmdTrigger = new byte[msgTerminatorStart.Count + msgTriggerON.Count + msgTerminatorStop.Count];
            Array.Copy(msgTerminatorStart.ToArray(), 0, cmdTrigger, 0, msgTerminatorStart.Count);
            Array.Copy(msgTriggerON.ToArray(), 0, cmdTrigger, msgTerminatorStart.Count, msgTriggerON.Count);
            Array.Copy(msgTerminatorStop.ToArray(), 0, cmdTrigger, msgTerminatorStart.Count + msgTriggerON.Count, msgTerminatorStop.Count);

            for (int i = 0; i < 5; i++)
            {


                if (SocketSend(cmdTrigger) == NetworkStreamResult.STARTED)
                {
                  
                    status = NetworkStreamResult.STARTED;
                }
                else
                {
                    status = NetworkStreamResult.ERROR;
                }

                if (TriggerONdHandler.WaitOne(1000))
                {
                    status = NetworkStreamResult.STOPPED;
                   
                    break;
                }
                else
                {
                    status = NetworkStreamResult.TIMEOUT;
                }
            }
            return status;
        }
        public NetworkStreamResult ResetTrigger()
        {
            NetworkStreamResult status = NetworkStreamResult.ERROR;
            byte[] cmdTrigger = new byte[msgTerminatorStart.Count + msgTriggerOFF.Count + msgTerminatorStop.Count];
            Array.Copy(msgTerminatorStart.ToArray(), 0, cmdTrigger, 0, msgTerminatorStart.Count);
            Array.Copy(msgTriggerOFF.ToArray(), 0, cmdTrigger, msgTerminatorStart.Count, msgTriggerOFF.Count);
            Array.Copy(msgTerminatorStop.ToArray(), 0, cmdTrigger, msgTerminatorStart.Count + msgTriggerOFF.Count, msgTerminatorStop.Count);
            for (int i = 0; i <= 5; i++)
            {

                if (SocketSend(cmdTrigger) == NetworkStreamResult.STARTED)
                {
                    
                }
                else
                {
                    status = NetworkStreamResult.ERROR;
                }

                if (TriggerOFFdHandler.WaitOne(1000))
                {
                    status = NetworkStreamResult.STARTED;
                   
                    break;
                }
                else
                {

                    status = NetworkStreamResult.TIMEOUT;
                }
            }
            return status;
        }
        public NetworkStreamResult SocketSend(byte[] cmd)
        {
            NetworkStreamResult status;
            if (IsSocketConnected())
            {


                try
                {
                    clientSocket.Send(cmd, 0, cmd.Length, SocketFlags.None);
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
                        while (IsSocketConnected()) ;
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


            NetworkStreamResult status;
            if (clientSocket.Connected)
            {
                try
                {

                    SetTrigger();

                    if (ResultedHandler.WaitOne(WaitReceiveBarcodeTimeOut))
                    {
                      
                        status = NetworkStreamResult.STARTED;
                    }
                    else
                    {

                        Console.WriteLine("Start send false ");
                        ResetTrigger();
                        Console.WriteLine("ResetTrigger ... ");
                        status = NetworkStreamResult.TIMEOUT;
                    }

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

        #endregion
    }
}