using System;
using System.Threading;

namespace TEST
{
    class Program
    {
        private static ManualResetEvent ConnectedHandler = new ManualResetEvent(false);
        static string IsStatus;
        static bool ok = false;
        static void Main(string[] args)
        {
            Thread receiveThread = new Thread(() => Receive());
            receiveThread.Start();
            while (true)
            {
                
                Console.WriteLine("Hello World!");
                Console.WriteLine("this connec{0}", ConnectedHandler.WaitOne(5000, ok));
                ConnectedHandler.Reset();
                Console.WriteLine("Hello-------------");
                Console.WriteLine("--------------------- World!");
            }
        }
        public static void Receive()
        {
            while (true) {
                IsStatus = "Null";
                Console.WriteLine("Nhap key");
                IsStatus = Console.ReadLine();
                if (IsStatus == "1")
                {
                    ConnectedHandler.Set();
                    Console.WriteLine("Nhap set");
                }
                if (IsStatus == "0")
                {
                    ConnectedHandler.Reset();
                    Console.WriteLine("Nhap Resset");
                }
            }
        }


    }
}
