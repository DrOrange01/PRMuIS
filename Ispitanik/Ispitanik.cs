using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ispitanik
{
    [Serializable]
    public class Ispitanik
    {
        public string Ime { get; set; }
        public string Prezime { get; set; }
        public int Id { get; set; }
        public int BrojTacnihOdgovora { get; set; }
    }
}
