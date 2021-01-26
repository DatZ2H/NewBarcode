using System;
using System.Text;
using System.Threading;
using SickBarcodeScanner;

namespace SICKLMS
{
    class Program
    {
        private static ManualResetEvent _connected = new ManualResetEvent(false);
        static byte[] test = { 0x66, 0x60 };
        static BarcodeScanner Sick = new BarcodeScanner("192.168.1.30", 2112, 30000, 30000, 150000);
        static void Main(string[] args)
        {
            Sick.Connect();
            
            for (int i = 0; i < 5; i++)
            {
                Sick.Start();
            }
            _connected.WaitOne(500);
            Console.WriteLine("                         ");
        }
    }
}
