namespace Vendr.PaymentProviders.Klarna.Api.Models
{
    public class LiveKlarnaClientConfig : KlarnaClientConfig
    {
        public override string BaseUrl
        {
            get
            {
                if (ApiRegion == KlarnaApiRegion.Europe)
                    return KlarnaClient.EuLiveApiUrl;

                if (ApiRegion == KlarnaApiRegion.NorthAmerica)
                    return KlarnaClient.NaLiveApiUrl;

                if (ApiRegion == KlarnaApiRegion.Oceania)
                    return KlarnaClient.OcLiveApiUrl;

                return null;
            }
        }

        public LiveKlarnaClientConfig(string username, string password, KlarnaApiRegion apiRegion)
            : base(username, password, apiRegion)
        { }
    }
}
