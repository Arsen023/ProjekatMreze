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
                using (UdpClient udpClient = new UdpClient())
                {
                    IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Loopback, 5000);

                    string poruka = "PRIJAVA " + korisnickoIme;
                    byte[] porukaBytes = Encoding.UTF8.GetBytes(poruka);
                    udpClient.Send(porukaBytes, porukaBytes.Length, serverEndPoint);

                    byte[] odgovorBytes = udpClient.Receive(ref serverEndPoint);
                    string odgovor = Encoding.UTF8.GetString(odgovorBytes);
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
                using (UdpClient udpClient = new UdpClient())
                {
                    IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Loopback, 5000);

                    string poruka = "LISTA_SERVERA";
                    byte[] porukaBytes = Encoding.UTF8.GetBytes(poruka);
                    udpClient.Send(porukaBytes, porukaBytes.Length, serverEndPoint);

                    IPEndPoint remoteEndPoint = null;
                    byte[] odgovorBytes = udpClient.Receive(ref remoteEndPoint);
                    string odgovor = Encoding.UTF8.GetString(odgovorBytes);

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
                using (UdpClient udpClient = new UdpClient())
                {
                    IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Loopback, 5000);

                    string poruka = $"LISTA_KANALA;{server}";
                    byte[] porukaBytes = Encoding.UTF8.GetBytes(poruka);
                    udpClient.Send(porukaBytes, porukaBytes.Length, serverEndPoint);

                    IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    byte[] odgovorBytes = udpClient.Receive(ref remoteEndPoint);
                    string odgovor = Encoding.UTF8.GetString(odgovorBytes);

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
                // Kombinovanje korisničkog imena i kanala za ključnu reč
                string key = korisnickoIme + izabraniKanal;

                // Kreiranje Playfair objekta
                Plejfer playfair = new Plejfer(key);

                // Šifrovanje poruke
                string encryptedMessage = playfair.Encrypt(poruka);

                using (UdpClient udpClient = new UdpClient())
                {
                    IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Loopback, 5000);

                    string datumVreme = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    string porukaZaServer = $"[{datumVreme}] - Server: {izabraniServer} - Kanal: {izabraniKanal} - Poruka: {encryptedMessage} - Korisnik: {korisnickoIme}";

                    byte[] porukaBytes = Encoding.UTF8.GetBytes(porukaZaServer);
                    udpClient.Send(porukaBytes, porukaBytes.Length, serverEndPoint);
                    Console.WriteLine("Poruka poslata.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška pri slanju poruke: {ex.Message}");
            }
        }

        private static void SnimiStatusKorisnika(string korisnickoIme, string server, string kanal)
        {
            try
            {
                string vremePrestanak = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string status = $"{korisnickoIme}|{server}|{kanal}|{vremePrestanak}";
                File.WriteAllText("serveri.txt", status);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška pri snimanju statusa: {ex.Message}");
            }
        }

        private static void PrimajPoruke()
        {
            try
            {
                int lokalniPort = 5000 + new Random().Next(1, 1000);

                using (UdpClient udpClient = new UdpClient())
                {
                    udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, lokalniPort));

                    IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Loopback, 5000);

                    while (true)
                    {
                        byte[] odgovorBytes = udpClient.Receive(ref serverEndPoint);
                        string odgovor = Encoding.UTF8.GetString(odgovorBytes);
                        Console.WriteLine("Poruka sa servera: " + odgovor);
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
