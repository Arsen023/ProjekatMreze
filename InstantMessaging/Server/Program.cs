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
        
        static Socket udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        static List<Socket> activeSockets = new List<Socket>(); 
        
        static void Main(string[] args)
          
        {
            
            Console.WriteLine("Pokretanje aplikacije za upravljanje serverima...");

            UcitajServere();
            
            Task.Run(() => OsluskivanjeZahteva());

          
            Task.Run(() => OsluskivanjeTCPKlijenata());

            while (true)
            {
                Console.WriteLine("\nOpcije: ");
                Console.WriteLine("1 - Kreiraj novi server");
                Console.WriteLine("2 - Dodaj kanal na postojeci server");
                Console.WriteLine("3 - Prikazi listu servera");
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
                            serveri[serverName] = new List<Kanal>();  
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
            IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Any, 5000);
            udpSocket.Bind(serverEndPoint);  // obavezno bind za primanje

            byte[] buffer = new byte[1024];

            EndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);

            while (true)
            {
                try
                {
                    int receivedLength = udpSocket.ReceiveFrom(buffer, ref clientEndPoint);
                    string porukaTekst = Encoding.UTF8.GetString(buffer, 0, receivedLength);
                    Console.WriteLine($"Primljena poruka: {porukaTekst}");

                    if (porukaTekst == "LISTA_SERVERA")
                    {
                        string listaServera = string.Join(";", serveri.Keys);
                        byte[] listaBytes = Encoding.UTF8.GetBytes(listaServera);
                        udpSocket.SendTo(listaBytes, clientEndPoint);
                        Console.WriteLine("Poslata lista servera.");
                    }

                    else if (porukaTekst.StartsWith("PRIJAVA"))
                    {
                        string tcpInfo = $"{((IPEndPoint)clientEndPoint).Address}:6000";
                        byte[] tcpInfoBytes = Encoding.UTF8.GetBytes(tcpInfo);
                        udpSocket.SendTo(tcpInfoBytes, clientEndPoint);
                        Console.WriteLine($"Poslata TCP informacija za prijavu: {tcpInfo}");
                    }

                    else if (porukaTekst.StartsWith("PORUKA"))
                    {
                        string[] delovi = porukaTekst.Split(';');
                        string serverNaziv = delovi[1].Replace("[", "").Replace("]", "");
                        string kanalNaziv = delovi[2].Replace("[", "").Replace("]", "");
                        string poruka2 = delovi[3].Replace("[", "").Replace("]", "");
                        string korisnickoIme = delovi[0].Replace("[", "").Replace("]", "");

                        string key = korisnickoIme + kanalNaziv;
                        Plejfer playfair = new Plejfer(key);
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

                    else if (porukaTekst.StartsWith("LISTA_KANALA"))
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
                            string listaKanala = string.Join(";", kanali.Select(k => $"{k.NazivKanala} ({k.NepocitanePoruke} nepročitanih poruka)"));
                            byte[] listaBytes = Encoding.UTF8.GetBytes(listaKanala);
                            udpSocket.SendTo(listaBytes, clientEndPoint);
                            Console.WriteLine($"Poslata lista kanala za server {serverNaziv}: {listaKanala}");
                        }
                        else
                        {
                            byte[] errorBytes = Encoding.UTF8.GetBytes("GRESKA: Server ne postoji.");
                            udpSocket.SendTo(errorBytes, clientEndPoint);
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
            
            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 6000);
            serverSocket.Bind(endPoint);
            serverSocket.Listen(10);  // Dozvoljava do 10 klijenata u redu čekanja

            Console.WriteLine("TCP server osluškuje na portu 6000...");

            while (true)
            {
                try
                {
                    
                    Socket tcpSocket = serverSocket.Accept();
                    activeSockets.Add(tcpSocket);
                    Console.WriteLine($"Nov klijent se povezao preko TCP: {tcpSocket.RemoteEndPoint}");

                    
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
            string serverNaziv = ""; 

            try
            {
                byte[] buffer = new byte[1024];
                int bytesRead;

                
                while ((bytesRead = tcpSocket.Receive(buffer)) > 0)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"Primljena TCP poruka: {message}");

                    if (string.IsNullOrEmpty(serverNaziv))
                    {
                        serverNaziv = message;  
                        Console.WriteLine($"Klijent se povezao na server: {serverNaziv}");
                        continue; 
                    }

                    
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška u komunikaciji sa TCP klijentom: {ex.Message}");
            }
            finally
            {
                
                ZabeleziVremeKadaKlijentPrestane(tcpSocket, serverNaziv); 
                tcpSocket.Close();
            }
        }
        static void ZabeleziVremeKadaKlijentPrestane(Socket tcpSocket, string serverNaziv)
        {
            string clientEndPoint = tcpSocket.RemoteEndPoint.ToString();
            string vremePrekida = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

           
            using (StreamWriter writer = new StreamWriter("serveri.txt", true))
            {
                writer.WriteLine($"{clientEndPoint} | {serverNaziv} | {vremePrekida}");
            }
        }


        
        static void ZatvoriServer()
        {
            Console.WriteLine("Zatvaranje servera...");
            udpSocket.Close();
            Console.WriteLine("Server je zatvoren.");
        }
    }
}