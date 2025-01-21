using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Klijent
{
    public class Program
    {
        static string izabraniServer = ""; // Držanje trenutnog izabranog servera
        static string izabraniKanal = ""; // Držanje izabranog kanala

        public static void Main(string[] args)
        {
            Console.WriteLine("Dobrodošli u klijentsku aplikaciju!");

            // Korisnik unosi svoje ime/nadimak
            Console.WriteLine("Unesite svoje ime ili nadimak:");
            string korisnickoIme = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(korisnickoIme))
            {
                Console.WriteLine("Ime ili nadimak ne mogu biti prazni. Pokušajte ponovo.");
                return;
            }

            // Korisnik mora uneti "PRIJAVA" kako bi nastavio dalje
            while (true)
            {
                Console.WriteLine("Unesite 'PRIJAVA' za nastavak:");
                string unos = Console.ReadLine();

                if (unos.Equals("PRIJAVA", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }
                else
                {
                    Console.WriteLine("Nevažeća komanda. Pokušajte ponovo.");
                }
            }

            // Prijavite se na server
            PrijaviSeNaServer(korisnickoIme);

            // Dalji proces se nastavlja sa dobijanjem liste servera
            List<string> serveri = DobaviListuServera();

            if (serveri.Count == 0)
            {
                Console.WriteLine("Nema dostupnih servera.");
                return;
            }

            // Prikazivanje liste servera korisniku
            Console.WriteLine("Dostupni serveri: ");
            for (int i = 0; i < serveri.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {serveri[i]}");
            }

            // Korisnik bira server
            Console.WriteLine("Izaberite server za povezivanje (unesite broj):");
            int izbor = int.Parse(Console.ReadLine()) - 1;

            if (izbor < 0 || izbor >= serveri.Count)
            {
                Console.WriteLine("Nevalidan izbor.");
                return;
            }

            izabraniServer = serveri[izbor];
            Console.WriteLine($"Povezivanje na server: {izabraniServer}");

            // Dobavi listu kanala za izabrani server
            List<string> kanali = DobaviKanale(izabraniServer);

            if (kanali.Count == 0)
            {
                Console.WriteLine("Server nema dostupnih kanala.");
                return;
            }

            // Prikazivanje liste kanala korisniku
            Console.WriteLine("Dostupni kanali: ");
            for (int i = 0; i < kanali.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {kanali[i]}");
            }

            // Korisnik bira kanal
            Console.WriteLine("Izaberite kanal za slanje poruke:");
            int kanalIzbor = int.Parse(Console.ReadLine()) - 1;

            if (kanalIzbor < 0 || kanalIzbor >= kanali.Count)
            {
                Console.WriteLine("Nevalidan izbor.");
                return;
            }

            izabraniKanal = kanali[kanalIzbor];
            Console.WriteLine($"Izabrali ste kanal: {izabraniKanal}");

            // Omogućiti korisniku da pošalje poruku
            while (true)
            {
                Console.WriteLine("Unesite poruku koju želite da pošaljete:");
                string poruka = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(poruka))
                {
                    Console.WriteLine("Poruka ne može biti prazna. Pokušajte ponovo.");
                    continue;
                }

                // Pošaljite poruku na server
                PosaljitePoruku(poruka, korisnickoIme);
            }
        }

        private static void PrijaviSeNaServer(string korisnickoIme)
        {
            try
            {
                using (UdpClient udpClient = new UdpClient())
                {
                    IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Loopback, 5000);

                    // Slanje PRIJAVA poruke
                    string poruka = "PRIJAVA " + korisnickoIme;
                    byte[] porukaBytes = Encoding.UTF8.GetBytes(poruka);
                    udpClient.Send(porukaBytes, porukaBytes.Length, serverEndPoint);

                    // Prijem odgovora od servera
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
                    IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Loopback, 5000); // Pretpostavljeni UDP port 5000

                    // Slanje poruke za dobijanje liste servera
                    string poruka = "LISTA_SERVERA";
                    byte[] porukaBytes = Encoding.UTF8.GetBytes(poruka);
                    udpClient.Send(porukaBytes, porukaBytes.Length, serverEndPoint);

                    // Prijem odgovora od servera
                    IPEndPoint remoteEndPoint = null;
                    byte[] odgovorBytes = udpClient.Receive(ref remoteEndPoint);
                    string odgovor = Encoding.UTF8.GetString(odgovorBytes);

                    // Razdvajanje odgovora u listu servera
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
            // Ovaj metod bi trebalo da komunicira sa serverom da bi dobio listu kanala
            kanali.Add("General");
            kanali.Add("Help");
            kanali.Add("Off-topic");
            return kanali;
        }

        private static void PosaljitePoruku(string poruka, string korisnickoIme)
        {
            try
            {
                using (UdpClient udpClient = new UdpClient())
                {
                    IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Loopback, 5000); // Lokacija servera

                    // Dodavanje trenutnog datuma i vremena u format: yyyy-MM-dd HH:mm:ss
                    string datumVreme = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                    // Formiranje poruke u traženom formatu
                    string porukaZaServer = $"[{datumVreme}]-[{izabraniServer}]:[{izabraniKanal}]:[{poruka}]-[{korisnickoIme}]";
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
    }
}
