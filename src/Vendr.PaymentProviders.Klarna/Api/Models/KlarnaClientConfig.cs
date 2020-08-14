namespace Vendr.PaymentProviders.Klarna.Api.Models
{
    public abstract class KlarnaClientConfig
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public KlarnaApiRegion ApiRegion { get; set; }
        public abstract string BaseUrl { get; }

        public KlarnaClientConfig(string username, string password, KlarnaApiRegion apiRegion)
        {
            Username = username;
            Password = password;
            ApiRegion = apiRegion;
        }
    }
}
