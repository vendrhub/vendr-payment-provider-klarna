using Newtonsoft.Json;

namespace Vendr.PaymentProviders.Klarna.Api.Models
{
    public class KlarnaAssetUrls
    {
        [JsonProperty("descriptive")]
        public string Descriptive { get; set; }

        [JsonProperty("standard")]
        public string Standard { get; set; }
    }
}
