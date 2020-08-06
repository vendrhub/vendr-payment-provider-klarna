using Vendr.Core.Web.PaymentProviders;

namespace Vendr.PaymentProviders.Klarna
{
    public class KlarnaSettingsBase
    {
        [PaymentProviderSetting(Name = "Continue URL",
            Description = "The URL to continue to after this provider has done processing. eg: /continue/",
            SortOrder = 100)]
        public string ContinueUrl { get; set; }

        [PaymentProviderSetting(Name = "Cancel URL",
            Description = "The URL to return to if the payment attempt is canceled. eg: /cancel/",
            SortOrder = 200)]
        public string CancelUrl { get; set; }

        [PaymentProviderSetting(Name = "Error URL",
            Description = "The URL to return to if the payment attempt errors. eg: /error/",
            SortOrder = 300)]
        public string ErrorUrl { get; set; }


        //[PaymentProviderSetting(Name = "Product Type Property Alias",
        //    Description = "The order line property alias containing the type of the product. Can be one of 'physical', 'digital' or 'gift_card'. Defaults to 'physical'.",
        //    SortOrder = 400)]
        //public string ProductTypePropertyAlias { get; set; }


        [PaymentProviderSetting(Name = "Billing Address (Line 1) Property Alias",
            Description = "The order property alias containing line 1 of the billing address",
            SortOrder = 500)]
        public string BillingAddressLine1PropertyAlias { get; set; }

        [PaymentProviderSetting(Name = "Billing Address (Line 2) Property Alias",
            Description = "The order property alias containing line 2 of the billing address",
            SortOrder = 600)]
        public string BillingAddressLine2PropertyAlias { get; set; }

        [PaymentProviderSetting(Name = "Billing Address City Property Alias",
            Description = "The order property alias containing the city of the billing address",
            SortOrder = 700)]
        public string BillingAddressCityPropertyAlias { get; set; }

        [PaymentProviderSetting(Name = "Billing Address State Property Alias",
            Description = "The order property alias containing the state of the billing address",
            SortOrder = 800)]
        public string BillingAddressStatePropertyAlias { get; set; }

        [PaymentProviderSetting(Name = "Billing Address ZipCode Property Alias",
            Description = "The order property alias containing the zip code of the billing address",
            SortOrder = 900)]
        public string BillingAddressZipCodePropertyAlias { get; set; }



        [PaymentProviderSetting(Name = "API Region",
            Description = "The Klarna API Region to use. Can be either 'Europe', 'NorthAmerica' or 'Oceana'.",
            SortOrder = 1000)]
        public string ApiRegion { get; set; }



        [PaymentProviderSetting(Name = "Test API Username",
            Description = "The Username to use when connecting to the test Klarna API",
            SortOrder = 1100)]
        public string TestApiUsername { get; set; }

        [PaymentProviderSetting(Name = "Test API Password",
            Description = "The Password to use when connecting to the test Klarna API",
            SortOrder = 1200)]
        public string TestApiPassword { get; set; }



        [PaymentProviderSetting(Name = "Live API Username",
            Description = "The Username to use when connecting to the live Klarna API",
            SortOrder = 1300)]
        public string LiveApiUsername { get; set; }

        [PaymentProviderSetting(Name = "Live API Password",
            Description = "The Password to use when connecting to the live Klarna API",
            SortOrder = 1400)]
        public string LiveApiPassword { get; set; }



        [PaymentProviderSetting(Name = "Capture",
            Description = "Flag indicating whether to immediately capture the payment, or whether to just authorize the payment for later (manual) capture.",
            SortOrder = 1500)]
        public bool Capture { get; set; }



        [PaymentProviderSetting(Name = "Test Mode",
            Description = "Set whether to process payments in test mode.",
            SortOrder = 10000)]
        public bool TestMode { get; set; }
    }
}
