using Newtonsoft.Json;

namespace Vendr.PaymentProviders.Klarna.Api.Models
{
    public class KlarnaMerchantSession
    {
        [JsonProperty("session_id")]
        public string SessionId { get; set; }
    }
}
