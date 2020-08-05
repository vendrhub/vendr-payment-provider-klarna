using Newtonsoft.Json;
using Org.BouncyCastle.Bcpg.OpenPgp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Vendr.Core;
using Vendr.Core.Models;
using Vendr.Core.Web;
using Vendr.Core.Web.Api;
using Vendr.Core.Web.PaymentProviders;
using Vendr.PaymentProviders.Klarna.Api;
using Vendr.PaymentProviders.Klarna.Api.Models;

namespace Vendr.PaymentProviders.Klarna
{
    [PaymentProvider("klarna-hpp", "Klarna (HPP)", "Klarna payment provider using the Klarna Hosted Payment Page (HPP)")]
    public class KlarnaHppPaymentProvider : KlarnaPaymentProviderBase<KlarnaHppSettings>
    {
        public KlarnaHppPaymentProvider(VendrContext vendr)
            : base(vendr)
        { }

        public override bool CanFetchPaymentStatus => true;
        public override bool CanCancelPayments => true;
        public override bool CanCapturePayments => true;
        public override bool CanRefundPayments => true;

        public override IEnumerable<TransactionMetaDataDefinition> TransactionMetaDataDefinitions => new[]{
            new TransactionMetaDataDefinition("klarnaSessionId", "Klarna Session ID"),
            new TransactionMetaDataDefinition("klarnaOrderId", "Klarna Order ID"),
            new TransactionMetaDataDefinition("klarnaReference", "Klarna Reference")
        };

        public override PaymentFormResult GenerateForm(OrderReadOnly order, string continueUrl, string cancelUrl, string callbackUrl, KlarnaHppSettings settings)
        {
            var klarnaSecretToken = Guid.NewGuid().ToString("N");

            var clientConfig = GetKlarnaClientConfig(settings);
            var client = new KlarnaClient(clientConfig);

            // Get currency information
            var billingCountry = Vendr.Services.CountryService.GetCountry(order.PaymentInfo.CountryId.Value);
            var billingCountryCode = billingCountry.Code.ToUpperInvariant();

            // Ensure billing country has valid ISO 3166 code
            var iso3166Countries = Vendr.Services.CountryService.GetIso3166CountryRegions();
            if (!iso3166Countries.Any(x => x.Code == billingCountryCode))
            {
                throw new Exception("Country must be a valid ISO 3166 billing country code: " + billingCountry.Name);
            }

            var currency = Vendr.Services.CurrencyService.GetCurrency(order.CurrencyId);
            var currencyCode = currency.Code.ToUpperInvariant();

            // Ensure currency has valid ISO 4217 code
            if (!Iso4217.CurrencyCodes.ContainsKey(currencyCode))
            {
                throw new Exception("Currency must be a valid ISO 4217 currency code: " + currency.Name);
            }

            // Create a merchant session
            var resp1 = client.CreateMerchantSession(new KlarnaCreateMerchantSessionRequest
            {
                MerchantReference1 = order.OrderNumber,
                PurchaseCountry = billingCountryCode,
                PurchaseCurrency = currencyCode,
                Locale = order.LanguageIsoCode, // TODO: Validate?

                OrderLines = order.OrderLines.Select(orderLine => new KlarnaOrderLine
                {
                    Reference = orderLine.Sku,
                    Name = orderLine.Name,
                    UnitPrice = (int)AmountToMinorUnits(orderLine.UnitPrice.WithoutDiscounts.WithTax),
                    Quantity = (int)orderLine.Quantity,
                    TotalAmount = (int)AmountToMinorUnits(orderLine.TotalPrice.Value.WithTax),
                    TotalDiscountAmount = (int)AmountToMinorUnits(orderLine.TotalPrice.TotalDiscount.WithTax),
                }).ToList(),
                OrderAmount = (int)AmountToMinorUnits(order.TotalPrice.Value.WithTax),

                BillingAddress = new KlarnaAddress
                {
                    GivenName = order.CustomerInfo.FirstName,
                    FamilyName = order.CustomerInfo.LastName,
                    Email = order.CustomerInfo.Email,
                    StreetAddress = !string.IsNullOrWhiteSpace(settings.BillingAddressLine1PropertyAlias)
                        ? order.Properties[settings.BillingAddressLine1PropertyAlias]?.Value : null,
                    StreetAddress2 = !string.IsNullOrWhiteSpace(settings.BillingAddressLine2PropertyAlias)
                        ? order.Properties[settings.BillingAddressLine2PropertyAlias]?.Value : null,
                    City = !string.IsNullOrWhiteSpace(settings.BillingAddressCityPropertyAlias)
                        ? order.Properties[settings.BillingAddressCityPropertyAlias]?.Value : null,
                    Region = !string.IsNullOrWhiteSpace(settings.BillingAddressStatePropertyAlias)
                        ? order.Properties[settings.BillingAddressStatePropertyAlias]?.Value : null,
                    PostalCode = !string.IsNullOrWhiteSpace(settings.BillingAddressZipCodePropertyAlias)
                        ? order.Properties[settings.BillingAddressZipCodePropertyAlias]?.Value : null,
                    Country = billingCountryCode
                }
            });;

            // Create a HPP session
            var resp2 = client.CreateHppSession(new KlarnaCreateHppSessionRequest
            {
                PaymentSessionUrl = $"{clientConfig.BaseUrl}/payments/v1/sessions/{resp1.SessionId}",
                Options = new KlarnaHppOptions
                {
                    PlaceOrderMode = settings.Capture 
                        ? KlarnaHppOptions.PlaceOrderModes.CAPTURE_ORDER
                        : KlarnaHppOptions.PlaceOrderModes.PLACE_ORDER
                },
                MerchantUrls = new KlarnaHppMerchantUrls
                {
                    Success = continueUrl,
                    Cancel = AppendQueryString(cancelUrl, "reason=cancel"),
                    Back = AppendQueryString(cancelUrl, "reason=back"),
                    Failure = AppendQueryString(cancelUrl, "reason=failure"),
                    Error = AppendQueryString(cancelUrl, "reason=error"),
                    StatusUpdate = AppendQueryString(callbackUrl, "sid={{session_id}}&token="+ klarnaSecretToken),
                }
            });

            return new PaymentFormResult()
            {
                Form = new PaymentForm(resp2.RedirectUrl, FormMethod.Get),
                MetaData = new Dictionary<string, string>
                {
                    { "klarnaSessionId", resp2.SessionId },
                    { "klarnaSecretToken", klarnaSecretToken }
                }
            };
        }

        public override string GetCancelUrl(OrderReadOnly order, KlarnaHppSettings settings)
        {
            settings.MustNotBeNull("settings");
            settings.CancelUrl.MustNotBeNull("settings.CancelUrl");

            var cancelUrl = settings.CancelUrl;

            if (HttpContext.Current != null)
            {
                var req = HttpContext.Current.Request;
                var reason = req.QueryString["reason"];

                cancelUrl = AppendQueryStringParam(cancelUrl, "reason", reason);

                if (!string.IsNullOrWhiteSpace(settings.ErrorUrl) && (reason == "failure" || reason == "error"))
                    return AppendQueryStringParam(settings.ErrorUrl, "reason", reason);
                
            }

            return cancelUrl;
        }

        public override string GetErrorUrl(OrderReadOnly order, KlarnaHppSettings settings)
        {
            settings.MustNotBeNull("settings");
            settings.ErrorUrl.MustNotBeNull("settings.ErrorUrl");

            return settings.ErrorUrl;
        }

        public override string GetContinueUrl(OrderReadOnly order, KlarnaHppSettings settings)
        {
            settings.MustNotBeNull("settings");
            settings.ContinueUrl.MustNotBeNull("settings.ContinueUrl");

            return settings.ContinueUrl;
        }

        public override CallbackResult ProcessCallback(OrderReadOnly order, HttpRequestBase request, KlarnaHppSettings settings)
        {
            var sessionId = request.QueryString["sid"];
            var token = request.QueryString["token"];

            if (!string.IsNullOrWhiteSpace(sessionId) && order.Properties["klarnaSessionId"] == sessionId
                && !string.IsNullOrWhiteSpace(token) && order.Properties["klarnaSecretToken"] == token)
            {
                var clientConfig = GetKlarnaClientConfig(settings);
                var client = new KlarnaClient(clientConfig);

                var evt = client.ParseSessionEvent(request.InputStream);
                if (evt != null && evt.Session.Status == KlarnaSession.Statuses.COMPLETED)
                {
                    return new CallbackResult
                    {
                        TransactionInfo = new TransactionInfo
                        {
                            AmountAuthorized = order.TotalPrice.Value.WithTax,
                            TransactionFee = 0m,
                            TransactionId = evt.Session.OrderId,
                            PaymentStatus = settings.Capture
                                ? PaymentStatus.Captured
                                : PaymentStatus.Authorized
                        },
                        MetaData = new Dictionary<string, string>
                        {
                            { "klarnaOrderId", evt.Session.OrderId },
                            { "klarnaReference", evt.Session.KlarnaReference }
                        }
                    };
                }
            }

            return CallbackResult.Ok();
        }

        public override ApiResult CancelPayment(OrderReadOnly order, KlarnaHppSettings settings)
        {
            return new ApiResult()
            {
                TransactionInfo = new TransactionInfoUpdate() {
                    TransactionId = order.TransactionInfo.TransactionId,
                    PaymentStatus = PaymentStatus.Cancelled
                }
            };
        }

        public override ApiResult CapturePayment(OrderReadOnly order, KlarnaHppSettings settings)
        {
            return new ApiResult()
            {
                TransactionInfo = new TransactionInfoUpdate()
                {
                    TransactionId = order.TransactionInfo.TransactionId,
                    PaymentStatus = PaymentStatus.Captured
                }
            };
        }
    }
}
