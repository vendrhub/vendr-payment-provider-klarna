using Vendr.Core.PaymentProviders;

namespace Vendr.PaymentProviders.Klarna
{
    public class KlarnaHppSettings : KlarnaSettingsBase
    {

        [PaymentProviderSetting(Name = "Fee Label Template",
            Description = "Template string to use for formatting fee order line labels such as shipping or payment method fees. Defaults to '{0} Fee'.",
            SortOrder = 700,
            IsAdvanced = true)]
        public string FeeLabelTemplate { get; set; }

        [PaymentProviderSetting(Name = "Discounts Label",
            Description = "The label to use for the discounts order line. Defaults to 'Discounts'.",
            SortOrder = 800,
            IsAdvanced = true)]
        public string DiscountsLabel { get; set; }

        [PaymentProviderSetting(Name = "Additional Fees Label",
            Description = "The label to use for the additional fees order line. Defaults to 'Additional Fees'.",
            SortOrder = 800,
            IsAdvanced = true)]
        public string AdditionalFeesLabel { get; set; }
    }
}
