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
                
                Socket udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                udpSocket.Bind(new IPEndPoint(IPAddress.Any, 5000));

                Console.WriteLine("UDP server pokrenut na portu 5000.");

                byte[] buffer = new byte[1024];
                EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

                while (true)
                {
                    try
                    {
                        
                        int receivedLength = udpSocket.ReceiveFrom(buffer, ref remoteEndPoint);
                        string receivedMessage = Encoding.UTF8.GetString(buffer, 0, receivedLength);

                        if (receivedMessage == "ZahtevZaServere")
                        {
                            
                            PosaljiListuServera(udpSocket, remoteEndPoint);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Greška u UDP serveru: {ex.Message}");
                    }
                }
            });
        }
        public static void PosaljiListuServera(Socket udpSocket, EndPoint clientEndPoint)
        {
            try
            {
                if (serveri.Count == 0)
                {
                    Console.WriteLine("Nema servera za slanje.");
                    return;
                }

                
                StringBuilder listaServera = new StringBuilder();
                foreach (var server in serveri)
                {
                    listaServera.AppendLine($"{server.Key}:6000"); // Format: serverNaziv:port
                }

                byte[] porukaBytes = Encoding.UTF8.GetBytes(listaServera.ToString());
                udpSocket.SendTo(porukaBytes, clientEndPoint);
                Console.WriteLine("Lista servera poslata klijentima.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška pri slanju liste servera: {ex.Message}");
            }
        }
    }
}
