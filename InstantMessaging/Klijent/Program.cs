using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Klijent
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Unesite svoj username: ");
            string ime = Console.ReadLine();

            // Unos poruke za prijavu
            string prijavaPoruka = "";
            while (true)
            {
                Console.WriteLine("Unesite poruku za prijavu: ");
                prijavaPoruka = Console.ReadLine();  // Korisnik unosi poruku, npr. "PRIJAVA"

                if (prijavaPoruka.ToUpper() == "PRIJAVA")
                {
                    break;  // Ako je poruka ispravna, izlazi iz petlje
                }
                else
                {
                    Console.WriteLine("Greška: Poruka mora biti 'PRIJAVA'. Pokušajte ponovo.");
                }
            }

            // Priprema UDP klijenta
            UdpClient udpClient = new UdpClient();
            IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 12347);

            // Slanje prijave serveru
            byte[] prijavaPodaci = Encoding.UTF8.GetBytes(prijavaPoruka);
            udpClient.Send(prijavaPodaci, prijavaPodaci.Length, serverEndPoint);
            Console.WriteLine("Poslata prijava serveru...");

            // Primanje odgovora od servera
            byte[] odgovorPodaci = udpClient.Receive(ref serverEndPoint);
            string odgovor = Encoding.UTF8.GetString(odgovorPodaci);
            Console.WriteLine($"Server odgovor: {odgovor}");

            // Ako je odgovor potvrda, korisnik može da bira server
            if (odgovor.Contains("Lista servera:"))
            {
                // Pronađi početnu poziciju "Lista servera:" i izdvoji deo stringa nakon toga
                int startIndex = odgovor.IndexOf("Lista servera:") + "Lista servera:".Length;
                string listaServera = odgovor.Substring(startIndex).Trim(); // Uzimanje dela nakon "Lista servera:" i uklanjanje praznih karaktera

                // Deljenje servera na pojedinačne servere
                string[] serveri = listaServera.Split(';');

                // Prikazivanje servera korisniku
                for (int i = 0; i < serveri.Length; i++)
                {
                    Console.WriteLine($"{i + 1}. {serveri[i]}");
                }

                // Korisnik bira server
                Console.WriteLine("Izaberite server (1, 2, 3):");
                int izbor = int.Parse(Console.ReadLine()) - 1;

                if (izbor >= 0 && izbor < serveri.Length)  // Proverite da li je unos validan
                {
                    string serverOdabrani = serveri[izbor];
                    Console.WriteLine($"Povezivanje na server: {serverOdabrani}");

                    // Izdvajanje IP adrese i porta
                    string[] serverInfo = serverOdabrani.Split(':');  // Popravljen Split
                    string ipAddress = serverInfo[0].Trim(); // Izdvajanje IP adrese
                    int port = int.Parse(serverInfo[1].Trim()); // Izdvajanje porta

                    // Povezivanje na TCP server koristeći Socket
                    Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    try
                    {
                        clientSocket.Connect(new IPEndPoint(IPAddress.Parse(ipAddress), port));  // Povezivanje na server
                        Console.WriteLine("Povezan sa serverom preko TCP-a...");

                        // Očekujte odgovore od servera putem TCP veze
                        byte[] data = new byte[256];
                        int bytesRead = clientSocket.Receive(data);
                        string response = Encoding.UTF8.GetString(data, 0, bytesRead);
                        Console.WriteLine($"Server odgovor: {response}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Greška prilikom povezivanja na server: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("Nevalidan izbor servera.");
                }
            }
            else
            {
                Console.WriteLine("Greška u prijavi.");
            }
        }
    }
}
