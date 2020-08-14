using Newtonsoft.Json;

namespace Vendr.PaymentProviders.Klarna.Api.Models
{
    public class KlarnaCreateHppSessionOptions
    {
        [JsonProperty("payment_session_url")]
        public string PaymentSessionUrl { get; set; }

        [JsonProperty("merchant_urls")]
        public KlarnaHppMerchantUrls MerchantUrls { get; set; }

        [JsonProperty("options")]
        public KlarnaHppOptions Options { get; set; }

        public KlarnaCreateHppSessionOptions()
        {
            MerchantUrls = new KlarnaHppMerchantUrls();
            Options = new KlarnaHppOptions();
        }
    }
}
