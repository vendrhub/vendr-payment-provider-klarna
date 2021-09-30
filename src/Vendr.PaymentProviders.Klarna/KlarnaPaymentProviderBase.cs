using Vendr.Core.Models;
using Vendr.Core.Api;
using Vendr.Core.PaymentProviders;
using Vendr.PaymentProviders.Klarna.Api.Models;
using Vendr.Extensions;

namespace Vendr.PaymentProviders.Klarna
{
    public abstract class KlarnaPaymentProviderBase<TSettings> : PaymentProviderBase<TSettings>
        where TSettings : KlarnaSettingsBase, new()
    {
        public KlarnaPaymentProviderBase(VendrContext vendr)
            : base(vendr)
        { }

        public override string GetContinueUrl(PaymentProviderContext<TSettings> ctx)
        {
            ctx.Settings.MustNotBeNull("ctx.Settings");
            ctx.Settings.ContinueUrl.MustNotBeNull("ctx.Settings.ContinueUrl");

            return ctx.Settings.ContinueUrl;
        }

        public override string GetCancelUrl(PaymentProviderContext<TSettings> ctx)
        {
            ctx.Settings.MustNotBeNull("ctx.Settings");
            ctx.Settings.CancelUrl.MustNotBeNull("ctx.Settings.CancelUrl");

            return ctx.Settings.CancelUrl;
        }

        public override string GetErrorUrl(PaymentProviderContext<TSettings> ctx)
        {
            ctx.Settings.MustNotBeNull("ctx.Settings");
            ctx.Settings.ErrorUrl.MustNotBeNull("ctx.Settings.ErrorUrl");

            return ctx.Settings.ErrorUrl;
        }

        protected KlarnaClientConfig GetKlarnaClientConfig(KlarnaSettingsBase settings)
        {
            if (!settings.TestMode)
            {
                return new LiveKlarnaClientConfig(settings.LiveApiUsername,
                    settings.LiveApiPassword,
                    settings.ApiRegion);
            }
            else
            {
                return new PlaygroundKlarnaClientConfig(settings.TestApiUsername,
                    settings.TestApiPassword,
                    settings.ApiRegion);
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
