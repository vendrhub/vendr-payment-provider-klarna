using Newtonsoft.Json;

namespace Vendr.PaymentProviders.Klarna.Api.Models
{
    public class KlarnaPaymentMethodCategory
    {
        [JsonProperty("identifier")]
        public string Identifier { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("asset_urls")]
        public KlarnaAssetUrls AssetUrls { get; set; }
    }
}
