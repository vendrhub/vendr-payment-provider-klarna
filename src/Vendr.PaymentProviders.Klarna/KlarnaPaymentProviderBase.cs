using Newtonsoft.Json;
using System;
using System.IO;
using Vendr.Core.Models;
using Vendr.Core.Web.Api;
using Vendr.Core.Web.PaymentProviders;
using Vendr.PaymentProviders.Klarna.Api.Models;

namespace Vendr.PaymentProviders.Klarna
{
    public abstract class KlarnaPaymentProviderBase<TSettings> : PaymentProviderBase<TSettings>
        where TSettings : KlarnaSettingsBase, new()
    {
        public KlarnaPaymentProviderBase(VendrContext vendr)
            : base(vendr)
        { }

        protected KlarnaClientConfig GetKlarnaClientConfig(KlarnaSettingsBase settings)
        {
            if (!settings.TestMode)
            {
                return new LiveKlarnaClientConfig(settings.LiveApiUsername,
                    settings.LiveApiPassword,
                    (KlarnaApiRegion)Enum.Parse(typeof(KlarnaApiRegion), settings.ApiRegion));
            }
            else
            {
                return new PlaygroundKlarnaClientConfig(settings.TestApiUsername,
                    settings.TestApiPassword,
                    (KlarnaApiRegion)Enum.Parse(typeof(KlarnaApiRegion), settings.ApiRegion));
            }
        }

        public PaymentStatus GetPaymentStatus(KlarnaOrder order)
        {
            var status = PaymentStatus.Authorized;

            switch (order.Status)
            {
                case KlarnaOrder.Statuses.CANCELLED:
                case KlarnaOrder.Statuses.EXPIRED:
                    status = PaymentStatus.Cancelled;
                    break;

                case KlarnaOrder.Statuses.CAPTURED:
                case KlarnaOrder.Statuses.PART_CAPTURED:
                    if (order.RefundedAmount > 0)
                        status = PaymentStatus.Refunded;
                    else
                        status = PaymentStatus.Captured;
                    break;

                case KlarnaOrder.Statuses.REFUNDED:
                    status = PaymentStatus.Refunded;
                    break;

                case KlarnaOrder.Statuses.CLOSED:
                    status = PaymentStatus.Error;
                    break;
            }

            return status;
        }

        protected string AppendQueryString(string url, string qs)
        {
            return url + (url.Contains("?") ? "&" : "?") + qs;
        }

        protected string AppendQueryStringParam(string url, string key, string value)
        {
            return url + (url.Contains("?") ? "&" : "?") + key + "=" + value;
        }
    }
}
