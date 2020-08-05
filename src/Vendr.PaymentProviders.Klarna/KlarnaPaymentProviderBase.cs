using Newtonsoft.Json;
using System;
using System.IO;
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
