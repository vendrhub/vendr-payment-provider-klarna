using Flurl.Http;
using Flurl.Http.Configuration;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using Vendr.PaymentProviders.Klarna.Api.Models;

namespace Vendr.PaymentProviders.Klarna.Api
{
    public class KlarnaClient
    {
        public const string EuLiveApiUrl = "https://api.klarna.com";
        public const string NaLiveApiUrl = "https://api-na.klarna.com";
        public const string OcLiveApiUrl = "https://api-oc.klarna.com";

        public const string EuPlaygroundApiUrl = "https://api.playground.klarna.com";
        public const string NaPlaygroundApiUrl = "https://api-na.playground.klarna.com";
        public const string OcPlaygroundApiUrl = "https://api-oc.playground.klarna.com";

        private KlarnaClientConfig _config;

        public KlarnaClient(KlarnaClientConfig config)
        {
            _config = config;
        }

        public KlarnaMerchantSession CreateMerchantSession(KlarnaCreateMerchantSessionOptions opts)
        {
            return Request("/payments/v1/sessions", (req) => req
                .PostJsonAsync(opts)
                .ReceiveJson<KlarnaMerchantSession>());
        }

        public KlarnaHppSession CreateHppSession(KlarnaCreateHppSessionOptions opts)
        {
            return Request("/hpp/v1/sessions", (req) => req
                .PostJsonAsync(opts)
                .ReceiveJson<KlarnaHppSession>());
        }

        public KlarnaOrder GetOrder(string orderId)
        {
            return Request($"/ordermanagement/v1/orders/{orderId}", (req) => req
                .GetAsync()
                .ReceiveJson<KlarnaOrder>());
        }

        public void CancelOrder(string orderId)
        {
            Request($"/ordermanagement/v1/orders/{orderId}/cancel", (req) => req
                .PostAsync(null));
        }

        public void CaptureOrder(string orderId, KlarnaCaptureOptions opts)
        {
            Request($"/ordermanagement/v1/orders/{orderId}/captures", (req) => req
                .PostJsonAsync(opts));
        }

        public void RefundOrder(string orderId, KlarnaRefundOptions opts)
        {
            Request($"/ordermanagement/v1/orders/{orderId}/refunds", (req) => req
                .PostJsonAsync(opts));
        }

        public KlarnaSessionEvent ParseSessionEvent(Stream stream)
        {
            var serializer = new JsonSerializer();

            if (stream.CanSeek)
                stream.Seek(0, 0);

            using (var sr = new StreamReader(stream))
            using (var jsonTextReader = new JsonTextReader(sr))
            {
                return serializer.Deserialize<KlarnaSessionEvent>(jsonTextReader);
            }
        }

        private TResult Request<TResult>(string url, Func<IFlurlRequest, Task<TResult>> func)
        {
            var req = new FlurlRequest(_config.BaseUrl + url)
                .ConfigureRequest(x => x.JsonSerializer = new NewtonsoftJsonSerializer(new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    ObjectCreationHandling = ObjectCreationHandling.Replace
                })) 
                .WithHeader("Cache-Control", "no-cache")
                .WithBasicAuth(_config.Username, _config.Password);

            return func.Invoke(req).Result;
        }
    }
}
