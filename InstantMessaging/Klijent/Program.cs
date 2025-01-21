using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;

namespace Klijent
{
    public class Program
    {
        static string listaServeraPutanja = "lista_servera.txt";
        static string izabraniServer = ""; // Držanje trenutnog izabranog servera

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

            // Pokreni UDP vezu i primi listu servera
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
            Console.WriteLine("Izaberite server za povezivanje(unesite broj):");
            int izbor = int.Parse(Console.ReadLine()) - 1;

            if (izbor < 0 || izbor >= serveri.Count)
            {
                Console.WriteLine("Nevalidan izbor.");
                return;
            }

            izabraniServer = serveri[izbor];
            Console.WriteLine($"Povezivanje na server: {izabraniServer}");

            // Čuvanje izabranog servera u fajlu za kasnije
            SpremiServer(izabraniServer);

            // Omogućiti korisniku da pošalje PRIJAVU kada poželi
            bool prijavaPoslana = false;
            while (!prijavaPoslana)
            {
                Console.WriteLine("\nPritisnite 'P' za prijavu na server.");
                string unos = Console.ReadLine().ToUpper();

                if (unos == "P")
                {
                    // Povezivanje putem UDP-a za prijavu
                    UdpPrijava(korisnickoIme, izabraniServer);
                    prijavaPoslana = true;
                }
                else
                {
                    Console.WriteLine("Pogrešan unos. Pritisnite 'P' za prijavu.");
                }
            }
        }

        private static List<string> DobaviListuServera()
        {
            List<string> serveri = new List<string>();
            try
            {
                using (UdpClient udpClient = new UdpClient())
                {
                    IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Loopback, 5000); // Pretpostavljamo da server koristi lokalni IP i port 5000

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

        private static void SpremiServer(string server)
        {
            try
            {
                File.WriteAllText(listaServeraPutanja, server);
                Console.WriteLine("Server je sačuvan za buduće povezivanje.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška pri čuvanju servera: {ex.Message}");
            }
        }

        private static void UdpPrijava(string korisnickoIme, string izabraniServer)
        {
            try
            {
                using (UdpClient udpClient = new UdpClient())
                {
                    IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Loopback, 5000); // Pretpostavljamo da server koristi lokalni IP i port 5000

                    // Korisnik šalje prijavu na server
                    string prijavaPoruka = $"PRIJAVA {korisnickoIme}";
                    byte[] prijavaBytes = Encoding.UTF8.GetBytes(prijavaPoruka);
                    udpClient.Send(prijavaBytes, prijavaBytes.Length, serverEndPoint);

                    // Primanje odgovora sa TCP informacijama
                    IPEndPoint remoteEndPoint = null;
                    byte[] tcpInfoBytes = udpClient.Receive(ref remoteEndPoint);
                    string tcpInfo = Encoding.UTF8.GetString(tcpInfoBytes);

                    Console.WriteLine($"TCP Informacije: {tcpInfo}");
                    // Dalje se povezuje putem TCP protokola koristeći tcpInfo
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška pri prijavi: {ex.Message}");
            }
        }
    }
}
