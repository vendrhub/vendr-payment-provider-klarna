using Newtonsoft.Json;
using System.Collections.Generic;

namespace Vendr.PaymentProviders.Klarna.Api.Models
{
    public abstract class KlarnaOrderBase
    {
        [JsonProperty("merchant_reference1")]
        public string MerchantReference1 { get; set; }

        [JsonProperty("merchant_reference2")]
        public string MerchantReference2 { get; set; }

        [JsonProperty("purchase_country")]
        public string PurchaseCountry { get; set; }

        [JsonProperty("purchase_currency")]
        public string PurchaseCurrency { get; set; }

        [JsonProperty("locale")]
        public string Locale { get; set; }

        [JsonProperty("order_lines")]
        public List<KlarnaOrderLine> OrderLines { get; set; }

        [JsonProperty("order_amount")]
        public int OrderAmount { get; set; }

        [JsonProperty("order_tax_amount")]
        public int? OrderTaxAmount { get; set; }

        [JsonProperty("billing_address")]
        public KlarnaAddress BillingAddress { get; set; }

        [JsonProperty("shipping_address")]
        public KlarnaAddress ShippingAddress { get; set; }

        public KlarnaOrderBase()
        {
            OrderLines = new List<KlarnaOrderLine>();
        }
    }
}
