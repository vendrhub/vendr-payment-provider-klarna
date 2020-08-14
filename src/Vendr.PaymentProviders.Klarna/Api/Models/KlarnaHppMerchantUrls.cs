using Newtonsoft.Json;

namespace Vendr.PaymentProviders.Klarna.Api.Models
{
    public class KlarnaHppMerchantUrls
    {
        [JsonProperty("success")]
        public string Success { get; set; }

        [JsonProperty("cancel")]
        public string Cancel { get; set; }

        [JsonProperty("back")]
        public string Back { get; set; }

        [JsonProperty("failure")]
        public string Failure { get; set; }

        [JsonProperty("error")]
        public string Error { get; set; }

        [JsonProperty("status_update")]
        public string StatusUpdate { get; set; }
    }
}
