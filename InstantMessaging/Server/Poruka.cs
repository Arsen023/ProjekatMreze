using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class Poruka
    {
        public string Posiljalac { get; set; }
        public string VremenskiTrenutak { get; set; }
        public string Sadrzaj { get; set; }

        public Poruka(string posiljalac, string vremenskiTrenutak, string sadrzaj)
        {
            Posiljalac = posiljalac;
            VremenskiTrenutak = vremenskiTrenutak;
            Sadrzaj = sadrzaj;
        }
    }
}
