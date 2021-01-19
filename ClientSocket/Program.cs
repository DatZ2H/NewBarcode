using System;
using Common;


namespace Client
{
    class Program
    {
        static ClientSocket ClientTCP = new ClientSocket();
        static void Main(string[] args)
        {

               Console.WriteLine("Hello World!");
               ClientTCP.Connect();


            ClientTCP.Read();
            //   Console.ReadKey();
            
        }
    }
}
