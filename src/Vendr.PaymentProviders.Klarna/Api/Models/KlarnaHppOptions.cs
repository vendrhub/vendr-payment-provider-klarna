using Newtonsoft.Json;
using System.Collections.Generic;

namespace Vendr.PaymentProviders.Klarna.Api.Models
{
    public class KlarnaHppOptions
    {
        public static class PlaceOrderModes
        {
            public const string NONE = "NONE";
            public const string PLACE_ORDER = "PLACE_ORDER";
            public const string CAPTURE_ORDER = "CAPTURE_ORDER";
        }

        [JsonProperty("place_order_mode")]
        public string PlaceOrderMode { get; set; }

        [JsonProperty("logo_url")]
        public string LogoUrl { get; set; }

        [JsonProperty("page_title")]
        public string PageTitle { get; set; }

        [JsonProperty("payment_method_categories")]
        public string[] PaymentMethodCategories { get; set; }

        [JsonProperty("payment_method_category")]
        public string PaymentMethodCategory { get; set; }

        [JsonProperty("payment_fallback")]
        public bool PaymentFallback { get; set; }
    }
}
