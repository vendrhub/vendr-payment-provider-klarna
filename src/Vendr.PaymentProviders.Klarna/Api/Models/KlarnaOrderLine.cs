using Newtonsoft.Json;

namespace Vendr.PaymentProviders.Klarna.Api.Models
{
    public class KlarnaOrderLine
    {
        public class Types
        {
            public const string PHYSICAL = "physical";
            public const string DIGITAL = "digital";
            public const string GIFT_CARD = "gift_card";
            public const string DISCOUNT = "discount";
            public const string SHIPPING_FEE = "shipping_fee";
            public const string SALES_TAX = "sales_tax";
            public const string STORE_CREDIT = "store_credit";
            public const string SURCHARGE = "surcharge";
        }

        [JsonProperty("reference")]
        public string Reference { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("tax_rate")]
        public int? TaxRate { get; set; }

        [JsonProperty("unit_price")]
        public int? UnitPrice { get; set; }

        [JsonProperty("quantity")]
        public int? Quantity { get; set; }

        [JsonProperty("total_discount_amount")]
        public int? TotalDiscountAmount { get; set; }

        [JsonProperty("total_tax_amount")]
        public int? TotalTaxAmount { get; set; }

        [JsonProperty("total_amount")]
        public int? TotalAmount { get; set; }
    }
}
