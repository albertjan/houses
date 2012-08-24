using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DRC;
using ImpromptuInterface;

namespace AlbertJan.Funda
{
    /// <summary>
    /// Interface om de dynamische REST client naar te duck-casten.
    /// </summary>
    public interface IFundaClient
    {
        FundaResult GetJson(string apiKey, object parameters);
    }

    /// <summary>
    /// Class die het ophalen en verwerken regelt.
    /// </summary>
    public class RunningTotal
    {
        /// <summary>
        /// Apikey (is een guid) maar opgelsagen als string met een trailing slash. Om te voorkomen dat de webservice een 307 stuurt. 
        /// De DRC plakt geen extra slash aan het eind van de url.
        /// </summary>
        private const string ApiKey = "a001e6c3ee6e4853ab18fe44cc1494de/";


        /// <summary>
        /// De zoek pattern wat er achter &zo= komt te staan.
        /// </summary>
        private readonly string _pattern;
        
        /// <summary>
        /// Geef aan of er rustig aan gedaan moet worden. 
        /// </summary>
        public bool LimitRate { get; set; }

        /// <summary>
        /// De lijst van Makelaars.
        /// </summary>
        public Dictionary<int, Realtor> Realtors { get; private set; }

        /// <summary>
        /// Event dat af gaat als er een nieuwe makelaar gesignaleerd wordt.
        /// </summary>
        public event EventHandler<NewRealtorEventArgs> NewRealtor;
        
        /// <summary>
        /// Event dat af gaat als het totaal aantal objecten is gevonden.
        /// </summary>
        public event EventHandler<NumberOfObjectsEventArgs> NumberOfObjects;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="pattern">zoek pattern</param>
        /// <param name="limitRate">lief zijn (of niet)</param>
        public RunningTotal(string pattern, bool limitRate)
        {
            LimitRate = limitRate;
            Realtors = new Dictionary<int, Realtor> ();
            _pattern = pattern;

            //de rest client http://nuget.org/packages/DRC en https://github.com/albertjan/DynamicRestClient
            dynamic client = new RESTClient();

            //geen basis url op.
            client.Url = "http://partnerapi.funda.nl/feeds/Aanbod.svc";

            //Iets wat ik een input editor noem een delegate die een webresponse vertaald naar het geweste object.
            //Als ik de duck-casting weg zou laten zou dit automatisch gaan maar dan zou de type-safety verder op.
            //Als de resquest GetJson<FundaResult>(ApiKey, params) zou zijn zou de DRC aan de hand van het
            //generic-type-argument zien waarnaar hij het resultaat moet parsen. Maar ImpromptuInterface ondersteund
            //niet het casten naar Interface's die Generic functies bevatten.
            client.GetJson.In = new Func<WebResponse, FundaResult>(r =>
            {
                using (var sr = new StreamReader(r.GetResponseStream()))
                {
                    return DRC.SimpleJson.DeserializeObject<FundaResult>(sr.ReadToEnd());
                }
            });

            //ImpromptuInterface gebruiken om van de dynamic client een semi typesafe object te maken.
            FundaClient = Impromptu.ActLike<IFundaClient>(client);
        }

        /// <summary>
        /// De Client.
        /// </summary>
        protected IFundaClient FundaClient { get; set; }

        /// <summary>
        /// Start!
        /// </summary>
        public void Start()
        {
            //Eerst een requestje om te zien hoeveel objecten er zijn. Is opzich niet nodig maar omdat de rest allemaal parallel gaat is dit
            //makkelijker. 

            //NB! pagesize = 0 gooit een division by zero exception. :)
            var restults = FundaClient.GetJson(ApiKey, new
            {
                pagesize = -1,
                page = 1,
                type = "koop",
                zo = _pattern
            });

            //Vuur event af dat aangeeft hoeveel objecten er te verwachten zijn.
            OnNumberOfObjects (new NumberOfObjectsEventArgs { NumberOfObjects = restults.TotaalAantalObjecten });

            //LINQ voodoo-magic :)
            //Maak een Enumerable met alle pagina nummers. Begin met 1 en daarom dus ook 1 langer.
            //Start een nieuwe task voor elke pagina. En wacht tot ze allemaal klaar zijn.
            //je zou de afhandeling van de objecten ook nog in een continuation kunnen gooien maar aangezien dat bijna geen tijd kost..
            Task.WaitAll(Enumerable.Range(1, (restults.TotaalAantalObjecten/25) + 1).Select(page => Task.Factory.StartNew(() =>
            {
                Console.WriteLine("Getting page: " + page);
                var sw = new Stopwatch();
                sw.Start();
                //Doe de request voor de pagine
                restults = FundaClient.GetJson(ApiKey, new
                {
                    pagesize = 25, page, type = "koop", zo = _pattern
                });

                //loop door de resultaten heen.
                foreach (var restult in restults.Objects)
                {
                    //als het een onbekende makelaar is voeg em toe en vuur een event af om de interface te updaten
                    if (!Realtors.ContainsKey(restult.Realtor.RealtorID))
                    {
                        Realtors.Add(restult.Realtor.RealtorID, restult.Realtor);
                        OnNewRealtor(new NewRealtorEventArgs { Realtor = restult.Realtor });
                    }

                    //Verhoog het aantal getelde objecten voor de makelaar en voeg het object toe aan zijn lijst met objecten.
                    Realtors[restult.Realtor.RealtorID].NumberOfObjects++;
                    Realtors[restult.Realtor.RealtorID].RealEstateObjects.Add(restult);
                }
                sw.Stop();
                //60000 milliseconden per minuut / 100 requests per minuut maal het aantal paralelle taken - het aantal milisecinde dat deze operatie duurde. 
                //om ervoor te zorgen dat er niet meer dan 100 requests per minuut zijn.
                Console.WriteLine("took: " + sw.Elapsed);
                var waitfor = (int) (((60000/100) * Environment.ProcessorCount) - sw.ElapsedMilliseconds);
                //als rate limiting aanstaat ook echt wachten.
                if (LimitRate) Thread.Sleep(waitfor < 0 ? 0 : waitfor);
            })).ToArray());
        }

        public void OnNumberOfObjects (NumberOfObjectsEventArgs e)
        {
            EventHandler<NumberOfObjectsEventArgs> handler = NumberOfObjects;
            if (handler != null) handler (this, e);
        }

        private void OnNewRealtor (NewRealtorEventArgs e)
        {
            EventHandler<NewRealtorEventArgs> handler = NewRealtor;
            if (handler != null) handler (this, e);
        }

    }
}