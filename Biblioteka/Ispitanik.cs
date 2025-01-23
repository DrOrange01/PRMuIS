using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biblioteka
{
    [Serializable]
    public class Ispitanik
    {
        public string Ime { get; set; }
        public string Prezime { get; set; }
        public int Id { get; set; }
        public int BrojPoena { get; set; }
        public string Simbol { get; set; }
        public DateTime VremePrikaza { get; set; }
        public DateTime? VremeOdgovora { get; set; }
    }
}
