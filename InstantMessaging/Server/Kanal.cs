using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class Kanal
    {
        public string NazivKanala { get; set; }
        public List<string> PorukaList { get; set; }
        

        public Kanal(string naziv) 
        { 
            NazivKanala = naziv;
            PorukaList = new List<string>();
        }

        //dodavanje poruke u kanal

        public void DodajPoruku(string poruka)
        {
            PorukaList.Add(poruka);
        }

    }
}
