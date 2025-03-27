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

namespace Server
{
    internal class Program
    {
        enum formatPrikazaEnum
        {
            levo,
            desno,
            sredina
        }
        static List<Ispitanik> rezultati = new List<Ispitanik>();
        static List<Rezultati> rez = new List<Rezultati>();
        static int brojIspitanika;
        static int vremeTrajanja;
        static string formatPrikaza;
        static void Main(string[] args)
        {   
            //unosenje podataka za test
            Console.WriteLine($"Unesite broj ispitanika: ");
            brojIspitanika = int.Parse(Console.ReadLine());
            Console.WriteLine($"Unesite vreme trajanja u sekundama: ");
            vremeTrajanja = int.Parse(Console.ReadLine());
            Console.WriteLine($"Unesite format prikaza (levo, desno, sredina): ");
            do
            {
                formatPrikaza = Console.ReadLine();
                if(formatPrikazaEnum.TryParse(formatPrikaza, true, out formatPrikazaEnum format))
                {
                    break;
                }
                else
                {
                    Console.WriteLine("Unesite format prikaza (levo, desno, sredina): ");
                }
            } while (true);

            Test podaci = new Test
            {
                VremeTrajanja = vremeTrajanja,
                FormatPrikaza = formatPrikaza
            };
            //

            //socket
            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Any, 50001);

            serverSocket.Bind(serverEP);
            serverSocket.Blocking = false;
            serverSocket.Listen(brojIspitanika);
            //

            Console.WriteLine($"Server je stavljen u stanje osluskivanja i ocekuje komunikaciju na {serverEP}");

            List<Socket> klijenti = new List<Socket>();
            byte[] buffer = new byte[1024];
            int brBajta = 0;

            try
            {
                Console.WriteLine("Server je pokrenut! Za zavrsetak rada pritisnite Escape");
                while (true)
                {
                    List<Socket> checkRead = new List<Socket>();
                    List<Socket> checkError = new List<Socket>();

                    if (klijenti.Count < brojIspitanika)
                    {
                        checkRead.Add(serverSocket);

                    }
                    checkError.Add(serverSocket);

                    foreach (Socket s in klijenti)
                    {
                        checkRead.Add(s);
                        checkError.Add(s);
                    }


                    Socket.Select(checkRead, null, checkError, 1000);


                    if (checkRead.Count > 0)
                    {
                        foreach (Socket s in checkRead)
                        {
                            if (s == serverSocket)
                            {

                                Socket client = serverSocket.Accept();
                                client.Blocking = false;
                                klijenti.Add(client);
                                Console.WriteLine($"Klijent se povezao sa {client.RemoteEndPoint}");

                                using (MemoryStream ms = new MemoryStream())
                                {
                                    BinaryFormatter bf = new BinaryFormatter();
                                    bf.Serialize(ms, podaci);
                                    client.Send(ms.ToArray());
                                }
                            }
                            else
                            {
                                brBajta = s.Receive(buffer);
                                if (brBajta == 0)
                                {
                                    Console.WriteLine("Klijent je prekinuo komunikaciju");
                                    s.Close();
                                    klijenti.Remove(s);
                                    if(klijenti.Count == 0)
                                    {
                                        break;
                                    }
                                    continue;
                                }
                                else
                                {
                                    using (MemoryStream ms = new MemoryStream(buffer, 0, brBajta))
                                    {
                                        BinaryFormatter bf = new BinaryFormatter();
                                        Rezultati odgovor = bf.Deserialize(ms) as Rezultati;
                                        if (odgovor != null)
                                        {
                                            rez.Add(odgovor);
                                            Console.WriteLine($"Primljen odgovor: Simbol: {odgovor.Simbol}," +
                                                $" Reakcija: {odgovor.Reakcija}, Rezultat: {odgovor.Rezultat}, " +
                                                $"Vreme reakcije: {odgovor.VremeReakcije}");
                                        }
                                    }
                                }
                            }

                        }
                    }
                    if (Console.KeyAvailable)
                    {
                        if (Console.ReadKey().Key == ConsoleKey.Escape)
                        {
                            break;
                        }
                    }
                    checkRead.Clear();
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Doslo je do greske {ex}");
            }


            foreach (Socket s in klijenti)
            {
                try
                {
                    s.Send(Encoding.UTF8.GetBytes("Server je zavrsio sa radom"));
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"Greška pri slanju poruke: {ex.Message}");
                }
                finally
                {
                    s.Close();
                }
            }

            Console.WriteLine("Server zavrsava sa radom");
            Console.ReadKey();
            serverSocket.Close();
        }
    }
}
