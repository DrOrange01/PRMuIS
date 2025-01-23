using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Biblioteka;

namespace Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Loopback, 50001);
            byte[] buffer = new byte[1024];

            Ispitanik ispitanik = new Ispitanik();
            Console.WriteLine("Unesite id: ");
            ispitanik.Id = Convert.ToInt32(Console.ReadLine());
            Console.WriteLine("Unesite ime: ");
            ispitanik.Ime = Console.ReadLine();
            Console.WriteLine("Unesite prezime");
            ispitanik.Prezime = Console.ReadLine();

            try
            {
                clientSocket.Connect(serverEP);
                Console.WriteLine("Klijent je uspesno povezan sa serverom!");
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Greska pri povezivanju sa serverom: {ex.SocketErrorCode}");
                return;
            }

            int brBajta = 0;

            try
            {
                while (true)
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        BinaryFormatter bf = new BinaryFormatter();
                        bf.Serialize(ms, ispitanik);
                        buffer = ms.ToArray();
                        clientSocket.Send(buffer);
                    }

                    Console.WriteLine("Podaci su uspesno poslati! \n\nDa li zelite da posaljete jos? da/ne");

                    if (Console.ReadLine().ToLower() == "ne")
                    {
                        break;
                    }



                }

            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Doslo je do greske tokom slanja:\n{ex}");
            }
            Console.WriteLine("Klijent zavrsava sa radom");
            Console.ReadKey();
            clientSocket.Close();
        }
    }
}
