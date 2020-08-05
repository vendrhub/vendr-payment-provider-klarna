using Newtonsoft.Json;

namespace Vendr.PaymentProviders.Klarna.Api.Models
{
    public class KlarnaOrderLine
    {
        [JsonProperty("reference")]
        public string Reference { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("tax_rate")]
        public int? TaxRate { get; set; }

        [JsonProperty("unit_price")]
        public int UnitPrice { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        [JsonProperty("total_discount_amount")]
        public int? TotalDiscountAmount { get; set; }

        [JsonProperty("total_tax_amount")]
        public int? TotalTaxAmount { get; set; }

        [JsonProperty("total_amount")]
        public int TotalAmount { get; set; }
    }
}
