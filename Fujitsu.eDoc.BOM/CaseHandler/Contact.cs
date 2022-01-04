namespace Fujitsu.eDoc.BOM.CaseHandler
{
    public class Contact
    {
        public string NavnTekst { get; set; }
        public string EmailTekst { get; set; }
        public string TelefonTekst { get; set; }
        public string Address
        {
            get
            {
                string a = StreetName + " " + StreetBuildingIdentifier;
                if (!string.IsNullOrEmpty(FloorIdentifier) || !string.IsNullOrEmpty(SuiteIdentifier))
                {
                    a += ",";
                    if (!string.IsNullOrEmpty(FloorIdentifier))
                    {
                        a += " " + FloorIdentifier;
                    }
                    if (!string.IsNullOrEmpty(SuiteIdentifier))
                    {
                        a += " " + SuiteIdentifier;
                    }
                }
                return a;
            }
        }
        public string StreetName { get; set; }
        public string StreetBuildingIdentifier { get; set; }
        public string FloorIdentifier { get; set; }
        public string SuiteIdentifier { get; set; }
        public string PostCodeIdentifier { get; set; }
        public string DistrictName { get; set; }
        public string CountryIdentificationCode { get; set; }

        public Contact() { }

        public Contact(BOM.BOMSagsbehandling.IndsendelseTypeIndsender indsender)
        {
            NavnTekst = indsender.NavnTekst;
            EmailTekst = indsender.EmailTekst;
            TelefonTekst = indsender.TelefonTekst;
            if (indsender.AddressPostal != null)
            {
                StreetName = indsender.AddressPostal.StreetName;
                StreetBuildingIdentifier = indsender.AddressPostal.StreetBuildingIdentifier;
                FloorIdentifier = indsender.AddressPostal.FloorIdentifier;
                SuiteIdentifier = indsender.AddressPostal.SuiteIdentifier;
                PostCodeIdentifier = indsender.AddressPostal.PostCodeIdentifier;
                DistrictName = indsender.AddressPostal.DistrictName;
                if (indsender.AddressPostal.CountryIdentificationCode != null)
                {
                    CountryIdentificationCode = indsender.AddressPostal.CountryIdentificationCode.Value;
                }
            }
        }
        public Contact(BOM.BOMSagsbehandling.BOMSagTypeTilknyttetAnsoeger ansoeger)
        {
            NavnTekst = ansoeger.NavnTekst;
            EmailTekst = ansoeger.EmailTekst;
            TelefonTekst = ansoeger.TelefonTekst;
            if (ansoeger.AddressPostal != null)
            {
                StreetName = ansoeger.AddressPostal.StreetName;
                StreetBuildingIdentifier = ansoeger.AddressPostal.StreetBuildingIdentifier;
                FloorIdentifier = ansoeger.AddressPostal.FloorIdentifier;
                SuiteIdentifier = ansoeger.AddressPostal.SuiteIdentifier;
                PostCodeIdentifier = ansoeger.AddressPostal.PostCodeIdentifier;
                DistrictName = ansoeger.AddressPostal.DistrictName;
                if (ansoeger.AddressPostal.CountryIdentificationCode != null)
                {
                    CountryIdentificationCode = ansoeger.AddressPostal.CountryIdentificationCode.Value;
                }
            }
        }
    }
}
