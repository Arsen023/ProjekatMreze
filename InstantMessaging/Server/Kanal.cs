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
        public List<Poruka> PorukaList { get; set; }

        public Kanal(string naziv) 
        { 
            NazivKanala = naziv;
        }

        //dodavanje poruke u kanal

        public void DodajPoruku(Poruka poruka)
        {
            PorukaList.Add(poruka);
        }

    }
}
