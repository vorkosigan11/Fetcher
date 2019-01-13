﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Fetcher
{
    public class Twor
    {
        public string Kod { get; set; }

        public int Ilosc { get; set; }

        public string NazwaZSigmy { get; set; }

        public string NumerRysunku { get; set; }

        public string Rewizja { get; set; }

        public string Firma { get; set; }

        public string Path { get; }

        public Boolean Exist { get; set; }

        public int NumerPozycji { get; set; }

        public Twor(string kod, int ilosc, string nazwaZSigmy, string numerRysunku, string rewizja, string firma, string sciezkaDoPRS, int numerPozycji)
        {
            this.Kod = kod;
            this.Ilosc = ilosc;
            this.NazwaZSigmy = nazwaZSigmy;
            this.NumerRysunku = numerRysunku;
            this.Rewizja = rewizja;
            this.Firma = firma;
            this.NumerPozycji = numerPozycji;
            this.Path = sciezkaDoPRS.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar) +
                        System.IO.Path.DirectorySeparatorChar +
                        this.Firma +
                        System.IO.Path.DirectorySeparatorChar +
                        ((Rewizja.Length > 0) ? numerRysunku + "_zm" + Rewizja : NumerRysunku) + ".prs";
            // Checkfile(this.Path);
        }

        internal void Checkfile(string path)
        {
            bool result = File.Exists(path);
            if (result)
            {
                this.Exist = true;
            }
            else
            {
                this.Exist = false;
            }
        }
    }
}