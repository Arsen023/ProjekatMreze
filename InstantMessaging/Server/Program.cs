using System;
using System.Collections.Generic;
using System.IO;
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
        static List<Socket> activeSockets = new List<Socket>(); // TCP Sockets za povezivanje sa klijentima

        static void Main(string[] args)
        {
            Console.WriteLine("Pokretanje aplikacije za upravljanje serverima...");

            UcitajServere();
            // Pokreni asinhronu metodu koja osluškuje UDP zahteve
            Task.Run(() => OsluskivanjeZahteva());

            // Pokreni TCP server za povezivanje sa klijentima
            Task.Run(() => OsluskivanjeTCPKlijenata());

            while (true)
            {
                Console.WriteLine("\nOpcije: ");
                Console.WriteLine("1 - Kreiraj novi server");
                Console.WriteLine("2 - Dodaj kanal na postojeci server");
                Console.WriteLine("3 - Prikazi stanje servera");
                Console.WriteLine("0 - Zatvori server");
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

            // Čuvanje novog servera u tekstualnu datoteku
            using (StreamWriter writer = new StreamWriter("serveri.txt", true))
            {
                writer.WriteLine(nazivServera);
            }
        }

        static void UcitajServere()
        {
            try
            {
                using (StreamReader reader = new StreamReader("serveri.txt"))
                {
                    string serverName;
                    while ((serverName = reader.ReadLine()) != null)
                    {
                        if (!serveri.ContainsKey(serverName))
                        {
                            serveri[serverName] = new List<Kanal>();  // Dodajemo server u memoriju
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška prilikom učitavanja servera: {ex.Message}");
            }
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
                        Console.WriteLine($" - Kanal: {kanal.NazivKanala}");
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
                        string serverNaziv = delovi[1].Replace("[", "").Replace("]", "");
                        string kanalNaziv = delovi[2].Replace("[", "").Replace("]", "");
                        string poruka2 = delovi[3].Replace("[", "").Replace("]", "");

                        string korisnickoIme = delovi[0].Replace("[", "").Replace("]", "");

                        // Kombinovanje korisničkog imena i kanala za ključnu reč
                        string key = korisnickoIme + kanalNaziv;



                        // Kreiranje Playfair objekta
                        Plejfer playfair = new Plejfer(key);

                        // Dešifrovanje poruke
                        string decryptedMessage = playfair.Decrypt(poruka2);

                        if (serveri.ContainsKey(serverNaziv))
                        {
                            var kanal = serveri[serverNaziv].Find(k => k.NazivKanala == kanalNaziv);
                            if (kanal != null)
                            {
                                kanal.DodajPoruku(decryptedMessage);
                                string datumVreme = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                Console.WriteLine($"{datumVreme} - {serverNaziv}: {kanalNaziv}: {decryptedMessage}");
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

        static void OsluskivanjeTCPKlijenata()
        {
            // TCP Server koji prihvata klijente
            TcpListener tcpListener = new TcpListener(IPAddress.Any, 6000);
            tcpListener.Start();
            Console.WriteLine("TCP server osluškuje na portu 6000...");

            while (true)
            {
                try
                {
                    Socket tcpSocket = tcpListener.AcceptSocket();
                    activeSockets.Add(tcpSocket);
                    Console.WriteLine($"Nov klijent se povezao preko TCP: {tcpSocket.RemoteEndPoint}");

                    // Za svakog klijenta pokreni zasebnu nit za obradu komunikacije
                    Task.Run(() => HandleTcpClient(tcpSocket));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Greška prilikom prihvatanja TCP klijenta: {ex.Message}");
                }
            }
        }

        static void HandleTcpClient(Socket tcpSocket)
        {
            try
            {
                byte[] buffer = new byte[1024];
                int bytesRead;
                while ((bytesRead = tcpSocket.Receive(buffer)) > 0)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"Primljena TCP poruka: {message}");

                    
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška u komunikaciji sa TCP klijentom: {ex.Message}");
            }
            finally
            {
                tcpSocket.Close();
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