using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    public class Program
    {
        static Dictionary<string, List<Kanal>> serveri = new Dictionary<string, List<Kanal>>();
        static UdpClient udpServer = new UdpClient(5000);  // Globalni UDP server

        static void Main(string[] args)
        {
            Console.WriteLine("Pokretanje aplikacije za upravljanje serverima...");
            // Pokreni asinhronu metodu koja osluškuje UDP zahteve
            Task.Run(() => OsluskivanjeZahteva());

            while (true)
            {
                Console.WriteLine("\nOpcije: ");
                Console.WriteLine("1 - Kreiraj novi server");
                Console.WriteLine("2 - Dodaj kanal na postojeci server");
                Console.WriteLine("3 - Prikazi stanje servera");

                string izbor = Console.ReadLine();

                switch (izbor)
                {
                    case "1":
                        KreirajServer();
                        break;
                    case "2":
                        DodajKanal();
                        break;
                    case "3":
                        PrikaziStanjeServera();
                        break;
                    case "0":
                        ZatvoriServer();
                        return;
                    default:
                        Console.WriteLine("Nepoznata opcija. Pokusajte ponovo");
                        break;
                }
            }
        }

        static void KreirajServer()
        {
            Console.WriteLine("Unesite naziv novog servera: ");
            string nazivServera = Console.ReadLine();

            if (serveri.ContainsKey(nazivServera))
            {
                Console.WriteLine($"Server sa nazivom '{nazivServera}' vec postoji.");
                return;
            }

            serveri[nazivServera] = new List<Kanal>();
            Console.WriteLine($"Server '{nazivServera}' uspesno kreiran.");
        }

        static void DodajKanal()
        {
            Console.WriteLine("Unesite naziv servera na kome zelite da dodajete kanale: ");
            string nazivServera = Console.ReadLine();

            if (!serveri.ContainsKey(nazivServera))
            {
                Console.WriteLine($"Server '{nazivServera}' ne postoji.");
                return;
            }
            Console.WriteLine("Unesite naziv novog kanala: ");
            string nazivKanala = Console.ReadLine();

            serveri[nazivServera].Add(new Kanal(nazivKanala));
            Console.WriteLine($"Kanal '{nazivKanala}' uspesno dodat na server '{nazivServera}'.");
        }

        static void PrikaziStanjeServera()
        {
            if (serveri.Count == 0)
            {
                Console.WriteLine("Nema definisanih servera.");
                return;
            }

            foreach (var server in serveri)
            {
                Console.WriteLine($"Server: {server.Key}");
                if (server.Value.Count == 0)
                {
                    Console.WriteLine(" - Nema kanala.");
                }
                else
                {
                    foreach (Kanal kanal in server.Value)
                    {
                        Console.WriteLine($" - Kanal: {kanal.NazivKanala} (Broj poruka: {kanal.PorukaList.Count})");
                    }
                }
            }
        }

        static void OsluskivanjeZahteva()
        {
            IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 5000);

            while (true)
            {
                try
                {
                    byte[] poruka = udpServer.Receive(ref clientEndPoint);
                    string porukaTekst = Encoding.UTF8.GetString(poruka);
                    Console.WriteLine($"Primljena poruka: {porukaTekst}");

                    if (porukaTekst == "LISTA_SERVERA")
                    {
                        string listaServera = string.Join(";", serveri.Keys);
                        byte[] listaBytes = Encoding.UTF8.GetBytes(listaServera);
                        udpServer.Send(listaBytes, listaBytes.Length, clientEndPoint);
                        Console.WriteLine("Poslata lista servera.");
                    }

                    if (porukaTekst.StartsWith("PRIJAVA"))
                    {
                        string tcpInfo = $"{clientEndPoint.Address.ToString()}:6000";
                        byte[] tcpInfoBytes = Encoding.UTF8.GetBytes(tcpInfo);
                        udpServer.Send(tcpInfoBytes, tcpInfoBytes.Length, clientEndPoint);
                        Console.WriteLine($"Poslata TCP informacija za prijavu: {tcpInfo}");
                    }

                    // Provera za slanje poruka na kanal
                    if (porukaTekst.StartsWith("PORUKA"))
                    {
                        string[] delovi = porukaTekst.Split(';');
                        string serverNaziv = delovi[1];
                        string kanalNaziv = delovi[2];
                        string poruka2 = delovi[3];

                        if (serveri.ContainsKey(serverNaziv))
                        {
                            var kanal = serveri[serverNaziv].Find(k => k.NazivKanala == kanalNaziv);
                            if (kanal != null)
                            {
                                kanal.DodajPoruku(poruka2);
                                string datumVreme = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                Console.WriteLine($"[{datumVreme}] - [{serverNaziv}]: [{kanalNaziv}]: [{poruka}]");
                            }
                        }
                    }

                    if (porukaTekst.StartsWith("LISTA_KANALA"))
                    {
                        string[] delovi = porukaTekst.Split(';');
                        if (delovi.Length < 2)
                        {
                            Console.WriteLine("Neispravan format zahteva za listu kanala.");
                            continue;
                        }

                        string serverNaziv = delovi[1];

                        if (serveri.ContainsKey(serverNaziv))
                        {
                            List<Kanal> kanali = serveri[serverNaziv];
                            string listaKanala = string.Join(";", kanali.Select(k => k.NazivKanala));
                            byte[] listaBytes = Encoding.UTF8.GetBytes(listaKanala);
                            udpServer.Send(listaBytes, listaBytes.Length, clientEndPoint);
                            Console.WriteLine($"Poslata lista kanala za server {serverNaziv}: {listaKanala}");
                        }
                        else
                        {
                            byte[] errorBytes = Encoding.UTF8.GetBytes("GRESKA: Server ne postoji.");
                            udpServer.Send(errorBytes, errorBytes.Length, clientEndPoint);
                            Console.WriteLine($"Klijent trazio kanale za nepostojeći server: {serverNaziv}");
                        }
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Greška prilikom osluškivanja UDP poruka: {ex.Message}");
                }
            }
        }

        static void ZatvoriServer()
        {
            Console.WriteLine("Zatvaranje servera...");
            udpServer.Close();
            Console.WriteLine("Server je zatvoren.");
        }
    }
}



