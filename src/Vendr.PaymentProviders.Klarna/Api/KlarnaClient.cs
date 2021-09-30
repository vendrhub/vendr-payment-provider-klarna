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

        public async Task<KlarnaMerchantSession> CreateMerchantSessionAsync(KlarnaCreateMerchantSessionOptions opts)
        {
            return await RequestAsync("/payments/v1/sessions", async (req) => await req
                .PostJsonAsync(opts)
                .ReceiveJson<KlarnaMerchantSession>());
        }

        public async Task<KlarnaHppSession> CreateHppSessionAsync(KlarnaCreateHppSessionOptions opts)
        {
            return await RequestAsync("/hpp/v1/sessions", async (req) => await req
                .PostJsonAsync(opts)
                .ReceiveJson<KlarnaHppSession>());
        }

        public async Task<KlarnaOrder> GetOrderAsync(string orderId)
        {
            return await RequestAsync($"/ordermanagement/v1/orders/{orderId}", async (req) => await req
                .GetAsync()
                .ReceiveJson<KlarnaOrder>());
        }

        public async Task CancelOrderAsync(string orderId)
        {
            await RequestAsync($"/ordermanagement/v1/orders/{orderId}/cancel", async (req) => await req
                .PostAsync(null));
        }

        public async Task CaptureOrderAsync(string orderId, KlarnaCaptureOptions opts)
        {
            await RequestAsync($"/ordermanagement/v1/orders/{orderId}/captures", async (req) => await req
                .PostJsonAsync(opts));
        }

        public async Task RefundOrderAsync(string orderId, KlarnaRefundOptions opts)
        {
            await RequestAsync($"/ordermanagement/v1/orders/{orderId}/refunds", async (req) => await req
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

        private async Task<TResult> RequestAsync<TResult>(string url, Func<IFlurlRequest, Task<TResult>> func)
        {
            var req = new FlurlRequest(_config.BaseUrl + url)
                .ConfigureRequest(x => x.JsonSerializer = new NewtonsoftJsonSerializer(new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    ObjectCreationHandling = ObjectCreationHandling.Replace,
                })) 
                .WithHeader("Cache-Control", "no-cache")
                .WithBasicAuth(_config.Username, _config.Password);

            return await func.Invoke(req);
        }
    }
}
