using System;


namespace Fujitsu.eDoc.BOMApplicationDesktopApp
{
    internal class BOMDocument
    {
        public string DokumentID { get; set; }
        public string BrugervendtNoegleTekst { get; set; }
        public string BeskrivelseTekst { get; set; }
        public DateTime BrevDato { get; set; }
        public string TitelTekst { get; set; }
        public string IndholdTekst { get; set; }
        public string MimeTypeTekst { get; set; }
        public string DocumentRecno { get; set; }
        public string FileType
        {
            get
            {
                string filetype = TitelTekst.Substring(TitelTekst.LastIndexOf('.') + 1);
                return filetype.ToLower();
            }
        }
        public bool IsFileTypeValid { get; set; }
    }
}
