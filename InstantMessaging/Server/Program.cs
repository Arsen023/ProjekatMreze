using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    public class Program
    {
        // Lista servera koja će biti poslana klijentima
        static List<string> listaServera = new List<string>()
        {
            "127.0.0.1:12345",  // Server sa TCP portom 12345
            "127.0.0.1:12347",  // Server sa TCP portom 12347
            "127.0.0.1:12348"   // Server sa TCP portom 12348
        };

        static void Main(string[] args)
        {
            // Prijava na server preko UDP
            UdpClient udpserver = new UdpClient(12347);  // UDP port na kojem server čeka prijavu
            Dictionary<string, List<Kanal>> serveri = new Dictionary<string, List<Kanal>>();
            List<int> portovi = new List<int> { 12345, 12347, 12348 };

            // Pokretanje servera na više portova
            foreach (int port in portovi)
            {
                int localPort = port;
                // Pokreći novi zadatak za svaki port (server)
                Task.Run(() => pokreniServer(localPort, serveri));
            }

            Console.WriteLine("************************************************\n");
            Console.WriteLine("Server je pokrenut i čeka na prijavu korisnika...\n");

            // Petlja za primanje poruka (prijava) od klijenata
            while (true)
            {
                // Dobijanje podataka od klijenta putem UDP
                IPEndPoint udpEndPoint = new IPEndPoint(IPAddress.Any, 12347);
                byte[] data = udpserver.Receive(ref udpEndPoint);
                string prijavaPoruka = Encoding.UTF8.GetString(data);
                Console.WriteLine($"Primljena poruka od {udpEndPoint}: {prijavaPoruka}\n");

                // Ako je poruka "PRIJAVA", server šalje listu servera
                if (prijavaPoruka.ToUpper() == "PRIJAVA")
                {
                    string odgovor = "Prijava uspesna. Dobijate TCP konekciju. Lista servera: ";
                    odgovor += string.Join(";", listaServera);

                    byte[] odgovorData = Encoding.UTF8.GetBytes(odgovor);
                    udpserver.Send(odgovorData, odgovorData.Length, udpEndPoint);
                    Console.WriteLine("Lista servera poslata klijentu.");
                }
            }
        }

        static void pokreniServer(int port, Dictionary<string, List<Kanal>> serveri)
        {
            // Povezivanje na TCP server pomoću Sockets (uticnice)
            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), port));  // Bind na TCP port
            serverSocket.Listen(10);  // Čeka maksimalno 10 konekcija

            Console.WriteLine($"TCP server je pokrenut i čeka na konekcije na portu {port}...");

            // Petlja za prihvatanje klijenata
            while (true)
            {
                Socket clientSocket = serverSocket.Accept();  // Prihvata konekciju od klijenta
                Console.WriteLine("Klijent se povezao na server.");

                // Komunikacija sa klijentom preko TCP socket-a
                byte[] welcomeMessage = Encoding.UTF8.GetBytes("Dobrodošli na server!");
                clientSocket.Send(welcomeMessage);

                // Kreiranje novih servera i kanala
                Console.WriteLine("----------KREIRANJE NOVOG SERVERA-----------");
                Console.WriteLine("Unesite IP adresu i port novog servera u formatu '127.0.0.1:12345': ");
                string unosServera = Console.ReadLine();

                // Definisanje varijable za server adresu
                string serverAdresa = string.Empty;

                // Parsiranje unosa
                string[] delovi = unosServera.Split(':');
                if (delovi.Length == 2)
                {
                    string ip = delovi[0];
                    string portStr = delovi[1];

                    if (int.TryParse(portStr, out int portNovo))
                    {
                        serverAdresa = $"{ip}:{portNovo}";

                        // Dodavanje servera u listu servera ako nije već prisutan
                        if (!listaServera.Contains(serverAdresa))
                        {
                            listaServera.Add(serverAdresa);
                            Console.WriteLine($"Server '{serverAdresa}' je uspešno dodat u listu servera.");
                        }
                        else
                        {
                            Console.WriteLine($"Server '{serverAdresa}' već postoji.");
                        }

                        // Dodaj server u dictionary ako ne postoji
                        if (!serveri.ContainsKey(serverAdresa))
                        {
                            serveri[serverAdresa] = new List<Kanal>();
                        }
                    }
                    else
                    {
                        Console.WriteLine("Nep validan port.");
                    }
                }
                else
                {
                    Console.WriteLine("Format unosa nije ispravan. Unesite IP adresu i port u formatu '127.0.0.1:12345'.");
                }

                if (string.IsNullOrEmpty(serverAdresa)) continue;

                // Kreiranje novog kanala
                Console.WriteLine("Unesite naziv kanala koji želite da kreirate:");
                string kanalNaziv = Console.ReadLine();
                Console.WriteLine("Naziv kanala je: " + kanalNaziv); // Debug poruka
                Kanal noviKanal = new Kanal(kanalNaziv);
                serveri[serverAdresa].Add(noviKanal);
                Console.WriteLine($"Kanal '{kanalNaziv}' je uspešno kreiran na serveru '{serverAdresa}'.");

                // Dodavanje poruke u kanal
                Console.WriteLine("Unesite sadržaj poruke: ");
                string sadrzajPoruke = Console.ReadLine();
                Poruka novaPoruka = new Poruka("Korisnik", DateTime.Now.ToString(), sadrzajPoruke);
                noviKanal.DodajPoruku(novaPoruka);
                Console.WriteLine("Poruka je dodata u kanal");

                clientSocket.Close();  // Zatvaramo konekciju sa klijentom
            }
        }
    }
}