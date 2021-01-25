using System;
using System.Text;
using System.Threading;
using BSICK.Sensors.LMS1xx;

namespace SICKLMS
{
    class Program
    {
        private static ManualResetEvent _connected = new ManualResetEvent(false);
        static byte[] test = { 0x66, 0x60 };
        static BarcodeScanner Sick = new BarcodeScanner("192.168.1.110",2112,30000,30000,60000);
        static void Main(string[] args)
        {

                Console.WriteLine("Hello World!");
                
                Console.WriteLine("gia tri connected is {0}", Sick.Connect());
             //   Console.WriteLine("gia tri ctheard {0}", Sick.IsCheckKeepConnectThreadAlive());


                //

                //     Console.WriteLine("Hello World!{0}", Sick.Start());
                Console.WriteLine("                         ");
                _connected.WaitOne(500);
               // Console.Clear();
                //  Console.ReadLine();
                //  Console.WriteLine("Hello World!{0}", Encoding.Defau7lt.GetString(Sick.ExecuteRaw(test)));
                //   Console.ReadLine();
                // Console.WriteLine("Hello World!{0}", Sick.Stop());
                // Console.ReadKey();
            
        }
    }
}
