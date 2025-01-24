using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System;

namespace Server
{
    public class UdpServer
    {
        
        static Dictionary<string, List<Kanal>> serveri = new Dictionary<string, List<Kanal>>();

        public static void PokreniUdpServer()
        {
            Task.Run(() =>
            {
                UdpClient udpServer = new UdpClient(5000);
                Console.WriteLine("UDP server pokrenut na portu 5000.");

                while (true)
                {
                    IPEndPoint remoteEndPoint = null;
                    byte[] receivedBytes = udpServer.Receive(ref remoteEndPoint);
                    string receivedMessage = Encoding.UTF8.GetString(receivedBytes);

                    if (receivedMessage == "ZahtevZaServere")
                    {
                        // Poslati listu servera klijentu
                        PosaljiListuServera(udpServer, remoteEndPoint);
                    }
                }
            });
        }

        public static void PosaljiListuServera(UdpClient udpClient, IPEndPoint clientEndPoint)
        {
            try
            {
                if (serveri.Count == 0)
                {
                    Console.WriteLine("Nema servera za slanje.");
                    return;
                }

                // Spremi listu servera u string
                StringBuilder listaServera = new StringBuilder();
                foreach (var server in serveri)
                {
                    listaServera.AppendLine($"{server.Key}:6000"); // Format: serverNaziv:port
                }

                
                byte[] porukaBytes = Encoding.UTF8.GetBytes(listaServera.ToString());
                udpClient.Send(porukaBytes, porukaBytes.Length, clientEndPoint);
                Console.WriteLine("Lista servera poslata klijentima.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška pri slanju liste servera: {ex.Message}");
            }
        }


    }
}
