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
        static List<Ispitanik> ispitanici = new List<Ispitanik>();
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
                                client.Blocking = true;
                                klijenti.Add(client);
                                Console.WriteLine($"Klijent se povezao sa {client.RemoteEndPoint}");

                                using (MemoryStream ms = new MemoryStream())
                                {
                                    BinaryFormatter bf = new BinaryFormatter();
                                    bf.Serialize(ms, podaci);
                                    client.Send(ms.ToArray());
                                }
                                    brBajta = client.Receive(buffer);
                                    using (MemoryStream ms = new MemoryStream(buffer, 0, brBajta))
                                    {
                                        BinaryFormatter bf = new BinaryFormatter();
                                        Ispitanik noviIspitanik = bf.Deserialize(ms) as Ispitanik;
                                        if (noviIspitanik != null)
                                        {
                                            ispitanici.Add(noviIspitanik);
                                            Console.WriteLine($"Novi ispitanik: {noviIspitanik.Ime} {noviIspitanik.Prezime}, ID: {noviIspitanik.Id}");
                                        }
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
                                            Ispitanik ispitanik = ispitanici.FirstOrDefault(i => i.Id == odgovor.IspitanikId);

                                            if (ispitanik != null)
                                            {
                                                ispitanik.DodajRezultat(odgovor);
                                                //Console.WriteLine($"Dodati rezultati za {ispitanik.Ime} {ispitanik.Prezime}: Simbol: {odgovor.Simbol}, Reakcija: {odgovor.Reakcija}");
                                                if (odgovor.Rezultat != "Propušteno" && odgovor.Rezultat != "Greška")
                                                {
                                                    ispitanik.BrojPoena++;
                                                }
                                                else
                                                {
                                                    ispitanik.BrojPoena --;
                                                }
                                            }
                                            else
                                            {
                                                Console.WriteLine("Neuspešno povezivanje rezultata sa ispitanikom.");
                                            }
                                        }
                                    }
                                }
                            }

                        }
                    }
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(intercept: true).Key;

                        if (key == ConsoleKey.Escape)
                        {
                            break;
                        }
                        else if (key == ConsoleKey.R)
                        {
                            IspisiRezultate();
                        }
                        else if (key == ConsoleKey.S)
                        {
                            string kriterijum = "";
                            bool validanUnos = false;

                            do
                            {
                                Console.WriteLine("\nUnesite kriterijum za sortiranje (ime, poeni, id): ");
                                kriterijum = Console.ReadLine();

                                if (kriterijum.ToLower() == "ime" || kriterijum.ToLower() == "poeni" || kriterijum.ToLower() == "id")
                                {
                                    validanUnos = true;
                                    SortirajIspitanike(kriterijum);
                                }
                                else
                                {
                                    Console.WriteLine("Nevažeći kriterijum. Dozvoljeni: ime, poeni, id.");
                                }

                            } while (!validanUnos);
                        }
                        else if (key == ConsoleKey.J)
                        {
                            string kriterijum = "";
                            bool validanUnos = false;

                            do
                            {
                                Console.WriteLine("\nUnesite kriterijum za pretragu (ime, poeni, id): ");
                                kriterijum = Console.ReadLine();

                                if (kriterijum.ToLower() == "ime" || kriterijum.ToLower() == "poeni" || kriterijum.ToLower() == "id")
                                {
                                    validanUnos = true;
                                    Console.WriteLine("Unesite vrednost za pretragu: ");
                                    string vrednost = Console.ReadLine();
                                    IspisiRezultateZaJednogKorisnika(kriterijum, vrednost);
                                }
                                else
                                {
                                    Console.WriteLine("Nevažeći kriterijum. Dozvoljeni: ime, poeni, id.");
                                }

                            } while (!validanUnos);
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
            Console.WriteLine(" Server zavrsava sa radom");
            Console.ReadKey();
            serverSocket.Close();
        }
        static void IspisiRezultate()
        {
            Console.WriteLine("\n=== REZULTATI TESTA ===");
            foreach (var isp in ispitanici)
            {
                Console.WriteLine($"Ispitanik: {isp.Ime} {isp.Prezime}, ID: {isp.Id}");
                foreach (var r in isp.ListaRezultata)
                {
                    Console.WriteLine($"Simbol: {r.Simbol}, Reakcija: {r.Reakcija}, Rezultat: {r.Rezultat}, Vreme: {r.VremeReakcije}");
                }
                Console.WriteLine($"Ukupan broj poena: {isp.BrojPoena}");
            }
        }
        static void IspisiRezultateZaJednogKorisnika(string kriterijum, string vrednost)
        {
            Console.WriteLine("\n=== REZULTATI TESTA ZA IZABRANOG KORISNIKA ===");
            int id;
            int poeni;
            bool pronadjen = false;
            if (kriterijum.ToLower() == "id")
            {
                id = int.Parse(vrednost);
                foreach (var isp in ispitanici)
                {
                    if (isp.Id == id)
                    {
                        Console.WriteLine($"Ispitanik: {isp.Ime} {isp.Prezime}, ID: {isp.Id}");
                        foreach (var r in isp.ListaRezultata)
                        {
                            Console.WriteLine($"Simbol: {r.Simbol}, Reakcija: {r.Reakcija}, Rezultat: {r.Rezultat}, Vreme: {r.VremeReakcije}");
                        }
                        Console.WriteLine($"Ukupan broj poena: {isp.BrojPoena}");
                        pronadjen = true;
                    }
                }
            }
            else if (kriterijum.ToLower() == "ime")
            {
                foreach (var isp in ispitanici)
                {
                    if (isp.Ime == vrednost)
                    {
                        Console.WriteLine($"Ispitanik: {isp.Ime} {isp.Prezime}, ID: {isp.Id}");
                        foreach (var r in isp.ListaRezultata)
                        {
                            Console.WriteLine($"Simbol: {r.Simbol}, Reakcija: {r.Reakcija}, Rezultat: {r.Rezultat}, Vreme: {r.VremeReakcije}");
                        }
                        Console.WriteLine($"Ukupan broj poena: {isp.BrojPoena}");
                        pronadjen = true;
                    }
                }
            }
            else if (kriterijum.ToLower() == "poeni")
            {
                foreach (var isp in ispitanici)
                {
                    poeni = int.Parse(vrednost);
                    if (isp.BrojPoena == poeni)
                    {
                        Console.WriteLine($"Ispitanik: {isp.Ime} {isp.Prezime}, ID: {isp.Id}");
                        foreach (var r in isp.ListaRezultata)
                        {
                            Console.WriteLine($"Simbol: {r.Simbol}, Reakcija: {r.Reakcija}, Rezultat: {r.Rezultat}, Vreme: {r.VremeReakcije}");
                        }
                        Console.WriteLine($"Ukupan broj poena: {isp.BrojPoena}");
                        pronadjen = true;
                    }
                }
            }
            else
            {
                Console.WriteLine("Nepoznat kriterijum pretrage.");
            }
            if(!pronadjen)
            {
                Console.WriteLine("Korisnik nije pronadjen.");
            }
        }
        static void SortirajIspitanike(string kriterijum)
        {

            switch (kriterijum.ToLower())
            {
                case "ime":
                    ispitanici = ispitanici.OrderBy(i => i.Ime).ThenBy(i => i.Prezime).ToList();
                    break;
                case "poeni":
                    ispitanici = ispitanici.OrderByDescending(i => i.BrojPoena).ToList();
                    break;
                case "id":
                    ispitanici = ispitanici.OrderBy(i => i.Id).ToList();
                    break;
                default:
                    Console.WriteLine("Nepoznat kriterijum. Koristite: ime, poeni ili id.");
                    return;
            }

            Console.WriteLine($"\n=== SORTIRANI ISPITANICI PO: {kriterijum.ToUpper()} ===");
            foreach (var isp in ispitanici)
            {
                Console.WriteLine($"Ispitanik: {isp.Ime} {isp.Prezime}, ID: {isp.Id}, Broj poena: {isp.BrojPoena}");
            }
            Console.WriteLine("=== KRAJ PRIKAZA ===\n");
        }

    }
}
