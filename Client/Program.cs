﻿using System;
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

            Test podaci = null;
            try
            {
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
            DateTime startTime = DateTime.Now;
            DateTime endTime = startTime.AddSeconds(podaci.VremeTrajanja);

            Console.WriteLine("\nTest počinje! Reagujte na simbol 'O' (pritisnite Space), ignorišite simbol 'X'.");
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
                if (simbol == 'O' && reakcija)
                {
                    ispitanik.BrojPoena++;
                    Console.WriteLine("Tačno!");
                }
                else if (simbol == 'O' && !reakcija)
                {
                    Console.WriteLine("Propušteno!");
                }
                else if (simbol == 'X' && reakcija)
                {
                    Console.WriteLine("Greška! Nije trebalo da reagujete.");
                }
                else
                {
                    ispitanik.BrojPoena++;
                    Console.WriteLine("Ispravno ignorisano.");
                }

                // Pauza pre sledećeg simbola
                System.Threading.Thread.Sleep(500);
            }

            Console.WriteLine("\nTest je završen!");
            Console.WriteLine($"Ukupan broj poena: {ispitanik.BrojPoena}");

            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(ms, ispitanik);
                    buffer = ms.ToArray();
                    clientSocket.Send(buffer);
                }
                Console.WriteLine("Rezultati su uspesno poslati!");
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
