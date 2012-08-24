using System;
using System.Collections.Generic;

namespace AlbertJan.Funda
{
    public class Metadata
    {
        public string ObjectType { get; set; }
        public string Omschrijving { get; set; }
        public string Titel { get; set; }
    }

    public class FundaResult
    {
        public int TotaalAantalObjecten { get; set; }
        public Metadata Metadata { get; set; }
        public List<RealEstateObject> Objects { get; set; }
        public Paging Paging { get; set; }
    }

    public class RealEstateObject
    {
        private Realtor _realtor;
        public Realtor Realtor
        {
            get { return _realtor ?? (_realtor = new Realtor (this.MakelaarId, this.MakelaarNaam)); }
        }

        public Guid Id { get; set; }
        public string AangebodenSindsTekst { get; set; }
        //should be datetime
        public string AanmeldDatum { get; set; }
        public int AantalKamers { get; set; }
        public string Adres { get; set; }
        public int Afstand { get; set; }
        public string BronCode { get; set; }
        public string Foto { get; set; }
        public int GlobalId { get; set; }
        public bool Heeft360GradenFoto { get; set; }
        public bool HeeftBrochure { get; set; }
        public bool HeeftOverbruggingsgrarantie { get; set; }
        public bool HeeftPlattegrond { get; set; }
        public bool HeeftTophuis { get; set; }
        public bool HeeftVeiling { get; set; }
        public bool HeeftVideo { get; set; }
        public int HuurPrijsTot { get; set; }
        public int Huurprijs { get; set; }
        public string HuurprijsFormaat { get; set; }
        public int Koopprijs { get; set; }
        public string KoopprijsFormaat { get; set; }
        public int KoopprijsTot { get; set; }
        public int MakelaarId { get; set; }
        public string MakelaarNaam { get; set; }
        public string MobileURL { get; set; }
        public object Perceeloppervlakte { get; set; }
        public string Postcode { get; set; }
        public string PrijsGeformatteerdHtml { get; set; }
        public string PrijsGeformatteerdTextHuur { get; set; }
        public string PrijsGeformatteerdTextKoop { get; set; }
        public object ProjectNaam { get; set; }
        public string SoortAanbod { get; set; }
        public string URL { get; set; }
        public string VerkoopStatus { get; set; }
        public double WGS84_X { get; set; }
        public double WGS84_Y { get; set; }
        public int Woonoppervlakte { get; set; }
        public string Woonplaats { get; set; }
    }

    public class Paging
    {
        public int AantalPaginas { get; set; }
        public int HuidigePagina { get; set; }
        public string VolgendeUrl { get; set; }
        public object VorigeUrl { get; set; }
    }

    public class Realtor
    {
        private int _numberOfObjects;
        public event EventHandler<ObjectCountedEventArgs> ObjectCounted;

        public void OnObjectCounted (ObjectCountedEventArgs e)
        {
            EventHandler<ObjectCountedEventArgs> handler = ObjectCounted;
            if (handler != null) handler (this, e);
        }

        public Realtor (int makelaarId, string makelaarNaam)
        {
            RealEstateObjects = new List<RealEstateObject> ();
            Name = makelaarNaam ?? "Makelaar zonder naam (" + makelaarId + ")";
            RealtorID = makelaarId;
        }

        public int RealtorID { get; set; }
        public string Name { get; set; }
        public int NumberOfObjects
        {
            get { return _numberOfObjects; }
            set
            {
                if (value != _numberOfObjects)
                {
                    OnObjectCounted (new ObjectCountedEventArgs { Realtor = this });
                }
                _numberOfObjects = value;
            }
        }

        public List<RealEstateObject> RealEstateObjects { get; set; }
    }
}
