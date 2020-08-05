using Newtonsoft.Json;
using System.Collections.Generic;

namespace Vendr.PaymentProviders.Klarna.Api.Models
{
    public class KlarnaMerchantSession
    {
        [JsonProperty("session_id")]
        public string SessionId { get; set; }

        //[JsonProperty("client_token")]
        //public string ClientToken { get; set; }

        //[JsonProperty("payment_method_categories")]
        //public IEnumerable<KlarnaPaymentMethodCategory> PaymentMethodCategories { get; set; }
    }
}
