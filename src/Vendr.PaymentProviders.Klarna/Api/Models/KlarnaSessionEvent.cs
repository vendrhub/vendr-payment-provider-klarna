using Newtonsoft.Json;
using System;

namespace Vendr.PaymentProviders.Klarna.Api.Models
{
    public class KlarnaSessionEvent
    {
        [JsonProperty("event_id")]
        public string EventId { get; set; }

        [JsonProperty("session")]
        public KlarnaSession Session { get; set; }
    }

    public class KlarnaSession
    {
        public class Statuses
        {
            public const string WAITING = "WAITING";
            public const string IN_PROGRESS = "IN_PROGRESS";
            public const string COMPLETED = "COMPLETED";
            public const string FAILED = "FAILED";
            public const string CANCELLED = "CANCELLED";
            public const string BACK = "BACK";
            public const string ERROR = "ERROR";
            public const string DISABLED = "DISABLED";
        }

        [JsonProperty("session_id")]
        public string SessionId { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("authorization_token")]
        public string AuthorizationToken { get; set; }

        [JsonProperty("order_id")]
        public string OrderId { get; set; }

        [JsonProperty("klarna_reference")]
        public string KlarnaReference { get; set; }

        [JsonProperty("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonProperty("expires_at")]
        public DateTime ExpiresAt { get; set; }
    }
}
