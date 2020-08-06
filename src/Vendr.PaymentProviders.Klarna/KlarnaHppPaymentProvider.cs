using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Vendr.Core;
using Vendr.Core.Models;
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

            // Prepair order lines
            // NB: We add order lines without any discounts applied as we'll then add
            // one global discount amount at the end. This is just the easiest way to
            // allow everything to add up and successfully validate at the Klarna end.
            var orderLines = order.OrderLines.Select(orderLine => new KlarnaOrderLine
            {
                Reference = orderLine.Sku,
                Name = orderLine.Name,
                Type = !string.IsNullOrWhiteSpace(settings.ProductTypePropertyAlias) && orderLine.Properties.ContainsKey(settings.ProductTypePropertyAlias)
                    ? orderLine.Properties[settings.ProductTypePropertyAlias]?.Value
                    : null,
                TaxRate = (int)(orderLine.TaxRate.Value * 10000),
                UnitPrice = (int)AmountToMinorUnits(orderLine.UnitPrice.WithoutDiscounts.WithTax),
                Quantity = (int)orderLine.Quantity,
                TotalAmount = (int)AmountToMinorUnits(orderLine.TotalPrice.WithoutDiscounts.WithTax),
                TotalTaxAmount = (int)AmountToMinorUnits(orderLine.TotalPrice.WithoutDiscounts.Tax)
            }).ToList();

            // Add shipping method fee orderline
            if (order.ShippingInfo.ShippingMethodId.HasValue && order.ShippingInfo.TotalPrice.Value.WithTax > 0) 
            {
                var shippingMethod = Vendr.Services.ShippingMethodService.GetShippingMethod(order.ShippingInfo.ShippingMethodId.Value);
                
                orderLines.Add(new KlarnaOrderLine
                {
                    Reference = shippingMethod.Sku,
                    Name = shippingMethod.Name + " Fee",
                    Type = KlarnaOrderLine.Types.SHIPPING_FEE,
                    TaxRate = (int)(order.ShippingInfo.TaxRate * 10000),
                    UnitPrice = (int)AmountToMinorUnits(order.ShippingInfo.TotalPrice.WithoutDiscounts.WithTax),
                    Quantity = 1,
                    TotalAmount = (int)AmountToMinorUnits(order.ShippingInfo.TotalPrice.WithoutDiscounts.WithTax),
                    TotalTaxAmount = (int)AmountToMinorUnits(order.ShippingInfo.TotalPrice.WithoutDiscounts.Tax),
                });
            }

            // Add payment method fee (as surcharge) orderline
            if (order.PaymentInfo.TotalPrice.Value.WithTax > 0)
            {
                var paymentMethod = Vendr.Services.PaymentMethodService.GetPaymentMethod(order.PaymentInfo.PaymentMethodId.Value);
                
                orderLines.Add(new KlarnaOrderLine
                {
                    Reference = paymentMethod.Sku,
                    Name = paymentMethod.Name + " Fee",
                    Type = KlarnaOrderLine.Types.SURCHARGE,
                    TaxRate = (int)(order.PaymentInfo.TaxRate * 10000),
                    UnitPrice = (int)AmountToMinorUnits(order.PaymentInfo.TotalPrice.WithoutDiscounts.WithTax),
                    Quantity = 1,
                    TotalAmount = (int)AmountToMinorUnits(order.PaymentInfo.TotalPrice.WithoutDiscounts.WithTax),
                    TotalTaxAmount = (int)AmountToMinorUnits(order.PaymentInfo.TotalPrice.WithoutDiscounts.Tax),
                });
            }

            // Add any discounts
            if (order.TotalPrice.TotalDiscount > 0)
            {
                orderLines.Add(new KlarnaOrderLine
                {
                    Reference = "DISCOUNT",
                    Name = "Discounts",
                    Type = KlarnaOrderLine.Types.DISCOUNT,
                    TaxRate = (int)(order.TaxRate * 10000),
                    UnitPrice = 0,
                    Quantity = 1,
                    TotalDiscountAmount = (int)AmountToMinorUnits(order.TotalPrice.TotalDiscount.WithTax),
                    TotalAmount = (int)AmountToMinorUnits(order.TotalPrice.TotalDiscount.WithTax) * -1,
                    TotalTaxAmount = (int)AmountToMinorUnits(order.TotalPrice.TotalDiscount.Tax) * -1,
                });
            }

            // Create a merchant session
            var resp1 = client.CreateMerchantSession(new KlarnaCreateMerchantSessionOptions
            {
                MerchantReference1 = order.OrderNumber,
                PurchaseCountry = billingCountryCode,
                PurchaseCurrency = currencyCode,
                Locale = order.LanguageIsoCode, // TODO: Validate?

                OrderLines = orderLines,
                OrderAmount = (int)AmountToMinorUnits(order.TotalPrice.Value.WithTax),
                OrderTaxAmount = (int)AmountToMinorUnits(order.TotalPrice.Value.Tax),

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
            var resp2 = client.CreateHppSession(new KlarnaCreateHppSessionOptions
            {
                PaymentSessionUrl = $"{clientConfig.BaseUrl}/payments/v1/sessions/{resp1.SessionId}",
                Options = new KlarnaHppOptions
                {
                    PlaceOrderMode = settings.Capture 
                        ? KlarnaHppOptions.PlaceOrderModes.CAPTURE_ORDER
                        : KlarnaHppOptions.PlaceOrderModes.PLACE_ORDER,
                    LogoUrl = !string.IsNullOrWhiteSpace(settings.LogoUrl)
                        ? settings.LogoUrl.Trim()
                        : null,
                    PageTitle = !string.IsNullOrWhiteSpace(settings.PageTitle)
                        ? settings.PageTitle.Trim()
                        : null,
                    PaymentMethodCategories = !string.IsNullOrWhiteSpace(settings.PaymentMethodCategories)
                        ? settings.PaymentMethodCategories.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(x => x.Trim())
                            .Where(x => !string.IsNullOrWhiteSpace(x))
                            .ToArray()
                        : null,
                    PaymentMethodCategory = !string.IsNullOrWhiteSpace(settings.PaymentMethodCategory)
                        ? settings.PaymentMethodCategory.Trim()
                        : null,
                    PaymentFallback = settings.EnableFallbacks
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
                    var klarnaOrder = client.GetOrder(evt.Session.OrderId);

                    return new CallbackResult
                    {
                        TransactionInfo = new TransactionInfo
                        {
                            AmountAuthorized = AmountFromMinorUnits(klarnaOrder.OriginalOrderAmount),
                            TransactionFee = 0m,
                            TransactionId = klarnaOrder.OrderId,
                            PaymentStatus = GetPaymentStatus(klarnaOrder)
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

        public override ApiResult FetchPaymentStatus(OrderReadOnly order, KlarnaHppSettings settings)
        {
            try
            {
                var orderId = order.TransactionInfo.TransactionId;

                var clientConfig = GetKlarnaClientConfig(settings);
                var client = new KlarnaClient(clientConfig);

                var klarnaOrder = client.GetOrder(orderId);
                if (klarnaOrder != null)
                {
                    return new ApiResult
                    {
                        TransactionInfo = new TransactionInfoUpdate
                        {
                            TransactionId = klarnaOrder.OrderId,
                            PaymentStatus = GetPaymentStatus(klarnaOrder)
                        }
                    };
                }

            }
            catch (Exception ex)
            {
                Vendr.Log.Error<KlarnaHppPaymentProvider>(ex, "Error fetching Klarna payment status for order {OrderNumber}", order.OrderNumber);
            }

            return ApiResult.Empty;
        }

        public override ApiResult CapturePayment(OrderReadOnly order, KlarnaHppSettings settings)
        {
            try
            {
                var orderId = order.TransactionInfo.TransactionId;

                var clientConfig = GetKlarnaClientConfig(settings);
                var client = new KlarnaClient(clientConfig);

                client.CaptureOrder(orderId, new KlarnaCaptureOptions
                {
                    Description = $"Capture Order {order.OrderNumber}",
                    CapturedAmount = (int)AmountToMinorUnits(order.TransactionInfo.AmountAuthorized.Value)
                });

                return new ApiResult
                {
                    TransactionInfo = new TransactionInfoUpdate
                    {
                        TransactionId = orderId,
                        PaymentStatus = PaymentStatus.Captured
                    }
                };

            }
            catch (Exception ex)
            {
                Vendr.Log.Error<KlarnaHppPaymentProvider>(ex, "Error capturing Klarna payment for order {OrderNumber}", order.OrderNumber);
            }

            return ApiResult.Empty;
        }

        public override ApiResult RefundPayment(OrderReadOnly order, KlarnaHppSettings settings)
        {
            try
            {
                var orderId = order.TransactionInfo.TransactionId;

                var clientConfig = GetKlarnaClientConfig(settings);
                var client = new KlarnaClient(clientConfig);

                client.RefundOrder(orderId, new KlarnaRefundOptions
                {
                    Description = $"Refund Order {order.OrderNumber}",
                    RefundAmount = (int)AmountToMinorUnits(order.TransactionInfo.AmountAuthorized.Value)
                });

                return new ApiResult
                {
                    TransactionInfo = new TransactionInfoUpdate
                    {
                        TransactionId = orderId,
                        PaymentStatus = PaymentStatus.Refunded
                    }
                };

            }
            catch (Exception ex)
            {
                Vendr.Log.Error<KlarnaHppPaymentProvider>(ex, "Error refunding Klarna payment for order {OrderNumber}", order.OrderNumber);
            }

            return ApiResult.Empty;
        }

        public override ApiResult CancelPayment(OrderReadOnly order, KlarnaHppSettings settings)
        {
            try
            {
                var orderId = order.TransactionInfo.TransactionId;

                var clientConfig = GetKlarnaClientConfig(settings);
                var client = new KlarnaClient(clientConfig);

                client.CancelOrder(orderId);

                return new ApiResult
                {
                    TransactionInfo = new TransactionInfoUpdate
                    {
                        TransactionId = orderId,
                        PaymentStatus = PaymentStatus.Cancelled
                    }
                };

            }
            catch (Exception ex)
            {
                Vendr.Log.Error<KlarnaHppPaymentProvider>(ex, "Error canceling Klarna payment for order {OrderNumber}", order.OrderNumber);
            }

            return ApiResult.Empty;
        }
    }
}
