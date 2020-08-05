using Newtonsoft.Json;

namespace Vendr.PaymentProviders.Klarna.Api.Models
{
    public class KlarnaMerchantUrls
    {
        [JsonProperty("confirmation")]
        public string Confirmation { get; set; }

        [JsonProperty("notification")]
        public string Notification { get; set; }

        [JsonProperty("push")]
        public string Push { get; set; }
    }
}
