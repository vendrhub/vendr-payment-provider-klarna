using Newtonsoft.Json;

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
    }
}
