using System;
namespace Biblioteka
{
    [Serializable]
    public class Rezultati
    {
        public int IspitanikId { get; set; }
        public char Simbol { get; set; }
        public bool Reakcija { get; set; }
        public string Rezultat { get; set; }
        public TimeSpan VremeReakcije { get; set; }
    }
}
