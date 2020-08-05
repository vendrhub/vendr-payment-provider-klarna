using Newtonsoft.Json;
using System;

namespace Vendr.PaymentProviders.Klarna.Api.Models
{
    public class KlarnaHppSession
    {
        [JsonProperty("session_id")]
        public string SessionId { get; set; }

        [JsonProperty("session_url")]
        public string SessionUrl { get; set; }

        [JsonProperty("distribution_url")]
        public string DistributionUrl { get; set; }

        [JsonProperty("redirect_url")]
        public string RedirectUrl { get; set; }

        [JsonProperty("qr_code_url")]
        public string QrCodeUrl { get; set; }

        [JsonProperty("expires_at")]
        public DateTime ExpiresAt { get; set; }
    }
}
