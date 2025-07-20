using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;
namespace Klijent
{
    public class Program
    {
        static string izabraniServer = "";
        static string izabraniKanal = "";

        public static void Main(string[] args)
        {


            Console.WriteLine("Dobrodošli u klijentsku aplikaciju!");
            while (true)
            {
                Console.WriteLine("Unesite svoje ime ili nadimak:");
                string korisnickoIme = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(korisnickoIme))
                {
                    Console.WriteLine("Ime ili nadimak ne mogu biti prazni. Pokušajte ponovo.");
                    continue;
                }

                if (!PovezivanjeKorisnika(korisnickoIme))
                    continue;

                if (!OdabirServera())
                    continue;

                if (!OdabirKanala())
                    continue;

                Komunikacija(korisnickoIme);

                
                Console.WriteLine("Da li želite da izadjete? (da/ne)");
                string izlaz = Console.ReadLine();
                if (izlaz.Equals("da", StringComparison.OrdinalIgnoreCase))
                {
                    ZabeleziVremeKadaKlijentPrestane(korisnickoIme, izabraniServer);
                    Console.WriteLine("Zatvaranje aplikacije...");
                    break;
                }
            }
        }

        private static bool PovezivanjeKorisnika(string korisnickoIme)
        {
            Console.WriteLine("Unesite 'PRIJAVA' za nastavak:");
            string unos = Console.ReadLine();

            if (unos.Equals("PRIJAVA", StringComparison.OrdinalIgnoreCase))
            {
                PrijaviSeNaServer(korisnickoIme);
                return true;
            }

            Console.WriteLine("Nevažeća komanda. Pokušajte ponovo.");
            return false;
        }

        private static bool OdabirServera()
        {
            List<string> serveri = DobaviListuServera();
            if (serveri.Count == 0)
            {
                Console.WriteLine("Nema dostupnih servera.");
                return false;
            }

            Console.WriteLine("Dostupni serveri:");
            for (int i = 0; i < serveri.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {serveri[i]}");
            }

            bool validanIzbor = false;
            while (!validanIzbor)
            {
                Console.WriteLine("Izaberite server (unesite broj):");
                if (int.TryParse(Console.ReadLine(), out int izbor) && izbor > 0 && izbor <= serveri.Count)
                {
                    izabraniServer = serveri[izbor - 1];
                    validanIzbor = true;
                }
                else
                {
                    Console.WriteLine("Nevažeći izbor. Pokušajte ponovo.");
                }
            }
            return true;
        }

        private static bool OdabirKanala()
        {
            List<string> kanali = DobaviKanale(izabraniServer);
            if (kanali.Count == 0)
            {
                Console.WriteLine("Server nema dostupnih kanala.");
                return false;
            }

            Console.WriteLine("Dostupni kanali:");
            for (int i = 0; i < kanali.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {kanali[i]}");
            }

            bool validanIzbor = false;
            while (!validanIzbor)
            {
                Console.WriteLine("Izaberite kanal (unesite broj):");
                if (int.TryParse(Console.ReadLine(), out int izbor) && izbor > 0 && izbor <= kanali.Count)
                {
                    izabraniKanal = kanali[izbor - 1];
                    validanIzbor = true;
                }
                else
                {
                    Console.WriteLine("Nevažeći izbor. Pokušajte ponovo.");
                }
            }
            return true;
        }

        private static void Komunikacija(string korisnickoIme)
        {
            Task.Run(() => PrimajPoruke());

            Console.WriteLine("Unesite poruku koju želite da pošaljete:");
            string poruka = Console.ReadLine();

            if (!string.IsNullOrWhiteSpace(poruka))
            {
                PosaljitePoruku(poruka, korisnickoIme);
            }
            else
            {
                Console.WriteLine("Poruka ne može biti prazna.");
            }
        }

        private static void PrijaviSeNaServer(string korisnickoIme)
        {
            try
            {
                // Kreiraj UDP socket
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Loopback, 5000);
                    EndPoint remoteEP = (EndPoint)serverEndPoint;

                    // Pripremi poruku za slanje
                    string poruka = "PRIJAVA " + korisnickoIme;
                    byte[] porukaBytes = Encoding.UTF8.GetBytes(poruka);

                    // Pošalji poruku serveru
                    socket.SendTo(porukaBytes, remoteEP);

                    // Primi odgovor sa servera
                    byte[] odgovorBytes = new byte[1024];
                    int brojPrimljenihBajtova = socket.ReceiveFrom(odgovorBytes, ref remoteEP);

                    string odgovor = Encoding.UTF8.GetString(odgovorBytes, 0, brojPrimljenihBajtova);
                    Console.WriteLine("Server odgovorio: " + odgovor);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška pri prijavi: {ex.Message}");
            }
        }

        private static List<string> DobaviListuServera()
        {
            List<string> serveri = new List<string>();
            try
            {
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Loopback, 5000);
                    EndPoint remoteEP = (EndPoint)serverEndPoint;

                    string poruka = "LISTA_SERVERA";
                    byte[] porukaBytes = Encoding.UTF8.GetBytes(poruka);

                    // Pošalji zahtev
                    socket.SendTo(porukaBytes, remoteEP);

                    // Primi odgovor
                    byte[] odgovorBytes = new byte[1024];
                    int brojPrimljenihBajtova = socket.ReceiveFrom(odgovorBytes, ref remoteEP);

                    string odgovor = Encoding.UTF8.GetString(odgovorBytes, 0, brojPrimljenihBajtova);
                    serveri.AddRange(odgovor.Split(';'));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška prilikom povezivanja sa serverom: {ex.Message}");
            }

            return serveri;
        }

        private static List<string> DobaviKanale(string server)
        {
            List<string> kanali = new List<string>();

            try
            {
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Loopback, 5000);
                    EndPoint remoteEP = (EndPoint)serverEndPoint;

                    string poruka = $"LISTA_KANALA;{server}";
                    byte[] porukaBytes = Encoding.UTF8.GetBytes(poruka);

                    // Pošalji poruku serveru
                    socket.SendTo(porukaBytes, remoteEP);

                    // Primi odgovor od servera
                    byte[] odgovorBytes = new byte[1024];
                    int brojPrimljenihBajtova = socket.ReceiveFrom(odgovorBytes, ref remoteEP);

                    string odgovor = Encoding.UTF8.GetString(odgovorBytes, 0, brojPrimljenihBajtova);

                    if (!odgovor.StartsWith("GRESKA"))
                    {
                        kanali.AddRange(odgovor.Split(';'));
                    }
                    else
                    {
                        Console.WriteLine($"Greška: {odgovor}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška pri dobavljanju kanala: {ex.Message}");
            }

            return kanali;
        }

        private static void PosaljitePoruku(string poruka, string korisnickoIme)
        {
            try
            {
                
                string key = korisnickoIme + izabraniKanal;

                
                Plejfer playfair = new Plejfer(key);

                
                string encryptedMessage = playfair.Encrypt(poruka);

                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Loopback, 5000);
                    EndPoint remoteEP = (EndPoint)serverEndPoint;

                    string datumVreme = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    string porukaZaServer = $"[{datumVreme}] - Server: {izabraniServer} - Kanal: {izabraniKanal} - Poruka: {encryptedMessage} - Korisnik: {korisnickoIme}";

                    byte[] porukaBytes = Encoding.UTF8.GetBytes(porukaZaServer);

                    socket.SendTo(porukaBytes, remoteEP);
                    Console.WriteLine("Poruka poslata.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška pri slanju poruke: {ex.Message}");
            }
        }

        private static void ZabeleziVremeKadaKlijentPrestane(string korisnickoIme, string server)
        {
            try
            {
                string vremePrestanka = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string fajlPutanja = "statusKlijenata.txt";

                using (StreamWriter writer = new StreamWriter(fajlPutanja, true))
                {
                    writer.WriteLine($"{korisnickoIme} prestao sa radom na serveru {server} u {vremePrestanka}");
                }

                
                Console.WriteLine($"Zabeleženo vreme prestanka rada za korisnika {korisnickoIme} na serveru {server} u {vremePrestanka}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška pri beleženju vremena prestanka: {ex.Message}");
            }
        }




        private static void PrimajPoruke()
        {
            try
            {
                int lokalniPort = 5000 + new Random().Next(1, 1000);

                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    
                    socket.Bind(new IPEndPoint(IPAddress.Any, lokalniPort));
                    Console.WriteLine($"Socket sluša na portu: {lokalniPort}");

                    EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

                    while (true)
                    {
                        byte[] buffer = new byte[1024];
                        int primljenoBajtova = socket.ReceiveFrom(buffer, ref remoteEP);
                        string poruka = Encoding.UTF8.GetString(buffer, 0, primljenoBajtova);
                        Console.WriteLine("Poruka sa servera: " + poruka);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška pri primanju poruka: {ex.Message}");
            }
        }
    }
}







