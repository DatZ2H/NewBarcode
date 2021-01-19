using System;
using System.Text;
using BSICK.Sensors.LMS1xx;

namespace SICKLMS
{
    class Program
    {
        static byte[] test = { 0x66, 0x60 };
        static LMS1XX Sick = new LMS1XX("127.0.0.1",2112,30000,30000);
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            Console.WriteLine("Hello World!{0}",Sick.Connect() );
            Console.WriteLine("Hello World!{0}", Sick.Start());
            Console.ReadLine();
            Console.WriteLine("Hello World!{0}", Encoding.Default.GetString(Sick.ExecuteRaw(test)));
            Console.ReadLine();
            // Console.WriteLine("Hello World!{0}", Sick.Stop());
        }
    }
}
