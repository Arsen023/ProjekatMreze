using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server
{
    public class Program
    {
        static void Main(string[] args)
        {
            // Prijava na server preko UDP
            UdpClient udpserver = new UdpClient(12347);  // UDP port na kojem server čeka prijavu
            Console.WriteLine("************************************************\n");
            Console.WriteLine("Server je pokrenut i čeka na prijavu korisnika...\n");

            // Lista servera koja će biti poslana klijentima
            List<string> listaServera = new List<string>()
            {
                "127.0.0.1:12345",  // Server sa TCP portom 12345
                "127.0.0.1:12347",  // Server sa TCP portom 12347
                "127.0.0.1:12348"   // Server sa TCP portom 12348
            };

            // Čuvanje liste servera u tekstualnu datoteku
            string filePath = "server_list.txt";
            File.WriteAllLines(filePath, listaServera);
            Console.WriteLine("Lista servera je sačuvana u datoteci server_list.txt.");

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

                // Povezivanje na TCP server pomoću Sockets (uticnice)
                Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                serverSocket.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 12347));  // Bind na TCP port
                serverSocket.Listen(10);  // Čeka maksimalno 10 konekcija

                Console.WriteLine("TCP server je pokrenut i čeka na konekcije...");

                Socket clientSocket = serverSocket.Accept();  // Prihvata konekciju od klijenta
                Console.WriteLine("Klijent se povezao na server.");

                // Komunikacija sa klijentom preko TCP socket-a
                byte[] welcomeMessage = Encoding.UTF8.GetBytes("Dobrodošli na server!");
                clientSocket.Send(welcomeMessage);

                clientSocket.Close();  // Zatvaramo konekciju sa klijentom
                serverSocket.Close();  // Zatvaramo server socket
            }
        }
    }
}
