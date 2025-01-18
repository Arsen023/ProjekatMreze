using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class Program
    {
        static void Main(string[] args)
        {
            //prijava na server
            UdpClient  udpserver = new UdpClient(12345);   //port na kome server ceka
            Console.WriteLine("************************************************\n");
            Console.WriteLine("Server je pokrenut i ceka na prijavu korisnika...\n");

            //dictionary za servere
            Dictionary<string, List<Kanal>> serveri = new Dictionary<string, List<Kanal>>();

            //petlja za primanje poruka(prijava) od klijenata
            while (true)
            {
                try
                {
                    //Dobijanje podataka
                    IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 12345);
                    byte[] data = udpserver.Receive(ref endPoint);

                    //pretvaranje podataka u string
                    string prijavaPoruka = Encoding.UTF8.GetString(data);
                    Console.WriteLine($"Primljena poruka od {endPoint}: {prijavaPoruka}\n");


                    //ako je prouka prijava server odgovara sa potvrdom
                    if(prijavaPoruka == "PRIJAVA")
                    {
                        string odgovor = "Prijava uspesna. Dobijate TCP konekciju.";
                        byte[] odgovorData = Encoding.UTF8.GetBytes(odgovor);

                        //saljemo potvrdu klijentu putem udp
                        udpserver.Send(odgovorData, odgovorData.Length, endPoint);

                    }    
                }catch(Exception ex)
                {
                    Console.WriteLine($"Greska u komunikaciji: {ex.Message}");
                }

                // Simulacija korisnika koji dodaje kanal
                Console.WriteLine("Unesite naziv novog kanala:");
                string nazivKanala = Console.ReadLine(); // Korisnik unosi naziv kanala

                Kanal noviKanal = new Kanal(nazivKanala);
                serveri["Server1"].Add(noviKanal);

                Console.WriteLine($"Kreiran kanal: {nazivKanala}");

                // Dodavanje poruke u kanal
                Console.WriteLine("Unesite sadržaj poruke:");
                string sadrzajPoruke = Console.ReadLine(); // Korisnik unosi sadržaj poruke

                Poruka poruka = new Poruka("Korisnik1", DateTime.Now.ToString(), sadrzajPoruke);
                noviKanal.DodajPoruku(poruka);

                Console.WriteLine($"Poruka je dodata u kanal {noviKanal.NazivKanala}");
            }

        }
    }
}
