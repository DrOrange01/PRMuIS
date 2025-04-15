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
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(ms, ispitanik);
                    buffer = ms.ToArray();
                    clientSocket.Send(buffer);
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Greska pri povezivanju sa serverom: {ex.SocketErrorCode}");
                return;
            }

            Test podaci = null;
            try
            {
                while (clientSocket.Available == 0) { }
                int brBajta = clientSocket.Receive(buffer);
                using (MemoryStream ms = new MemoryStream(buffer, 0, brBajta))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    podaci = bf.Deserialize(ms) as Test;
                }

                Console.WriteLine("Podaci o testu primljeni:");
                Console.WriteLine($"Trajanje testa: {podaci.VremeTrajanja} sekundi");
                Console.WriteLine($"Format prikaza: {podaci.FormatPrikaza}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška prilikom prijema podataka o testu: {ex.Message}");
                clientSocket.Close();
                return;
            }

            Random random = new Random();
            

            Console.WriteLine("\nTest počinje! Reagujte na simbol 'O' (pritisnite Space), ignorišite simbol 'X'.");
            System.Threading.Thread.Sleep(500);
            Console.WriteLine("3");
            System.Threading.Thread.Sleep(500);
            Console.WriteLine("2");
            System.Threading.Thread.Sleep(500);
            Console.WriteLine("1");
            System.Threading.Thread.Sleep(500);
            DateTime startTime = DateTime.Now;
            DateTime endTime = startTime.AddSeconds(podaci.VremeTrajanja);
            while (DateTime.Now < endTime)
            {
                // Generisanje slučajnog simbola (X ili O)
                char simbol = random.Next(0, 2) == 0 ? 'X' : 'O';
                Console.WriteLine($"Simbol: {simbol}");

                DateTime prikazVreme = DateTime.Now;

                bool reakcija = false;
                while ((DateTime.Now - prikazVreme).TotalSeconds < 2)
                {
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(intercept: true).Key;
                        if (key == ConsoleKey.Spacebar)
                        {
                            reakcija = true;
                            break;
                        }
                    }
                }

                // Obrada reakcije
                string rezultat = simbol == 'O' && reakcija ? "Tačno" :
                                  simbol == 'O' && !reakcija ? "Propušteno" :
                                  simbol == 'X' && reakcija ? "Greška" : "Ispravno ignorisano";

                Console.WriteLine(rezultat);

                if(rezultat!="Propušteno" && rezultat != "Greška")
                {
                    ispitanik.BrojPoena++;
                }
                else
                {
                    ispitanik.BrojPoena--;
                }

                try
                {
                    var rezultatTesta = new Rezultati
                    {
                        Simbol = simbol,
                        Reakcija = reakcija,
                        Rezultat = rezultat,
                        VremeReakcije = DateTime.Now - prikazVreme,
                        IspitanikId = ispitanik.Id
                    };

                    using (MemoryStream ms = new MemoryStream())
                    {
                        BinaryFormatter bf = new BinaryFormatter();
                        bf.Serialize(ms, rezultatTesta);
                        buffer = ms.ToArray();
                        clientSocket.Send(buffer);
                    }
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"Greška pri slanju odgovora serveru: {ex}");
                }

                // Pauza pre sledećeg simbola
                System.Threading.Thread.Sleep(500);
            }

            Console.WriteLine("\nTest je završen!");
            Console.WriteLine($"Ukupan broj poena: {ispitanik.BrojPoena}");

            clientSocket.Send(new byte[0]);

            Console.WriteLine("Klijent zavrsava sa radom");
            Console.ReadKey();
            clientSocket.Close();
        }
    }
}
