namespace Vendr.PaymentProviders.Klarna.Api.Models
{
    public class PlaygroundKlarnaClientConfig : KlarnaClientConfig
    {
        public override string BaseUrl
        {
            get
            {
                if (ApiRegion == KlarnaApiRegion.Europe)
                    return KlarnaClient.EuPlaygroundApiUrl;

                if (ApiRegion == KlarnaApiRegion.NorthAmerica)
                    return KlarnaClient.NaPlaygroundApiUrl;

                if (ApiRegion == KlarnaApiRegion.Oceania)
                    return KlarnaClient.OcPlaygroundApiUrl;

                return null;
            }
        }

        public PlaygroundKlarnaClientConfig(string username, string password, KlarnaApiRegion apiRegion) 
            : base(username, password, apiRegion)
        { }
    }
}
