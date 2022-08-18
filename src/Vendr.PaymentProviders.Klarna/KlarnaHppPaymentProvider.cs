using System;
using System.Collections.Generic;
using System.Linq;
using Vendr.Core.Models;
using Vendr.Core.Api;
using Vendr.Core.PaymentProviders;
using Vendr.Extensions;
using Vendr.PaymentProviders.Klarna.Api;
using Vendr.PaymentProviders.Klarna.Api.Models;
using System.Threading.Tasks;
using System.Web;
using Vendr.Common.Logging;

namespace Vendr.PaymentProviders.Klarna
{
    [PaymentProvider("klarna-hpp", "Klarna (HPP)", "Klarna payment provider using the Klarna Hosted Payment Page (HPP)")]
    public class KlarnaHppPaymentProvider : KlarnaPaymentProviderBase<KlarnaHppSettings>
    {
        private readonly ILogger<KlarnaHppPaymentProvider> _logger;

        public KlarnaHppPaymentProvider(VendrContext vendr,
            ILogger<KlarnaHppPaymentProvider> logger)
            : base(vendr)
        {
            _logger = logger;
        }

        public override bool CanFetchPaymentStatus => true;
        public override bool CanCancelPayments => true;
        public override bool CanCapturePayments => true;
        public override bool CanRefundPayments => true;

        public override IEnumerable<TransactionMetaDataDefinition> TransactionMetaDataDefinitions => new[]{
            new TransactionMetaDataDefinition("klarnaSessionId", "Klarna Session ID"),
            new TransactionMetaDataDefinition("klarnaOrderId", "Klarna Order ID"),
            new TransactionMetaDataDefinition("klarnaReference", "Klarna Reference")
        };

        public override string GetCancelUrl(PaymentProviderContext<KlarnaHppSettings> ctx)
        {
            ctx.Settings.MustNotBeNull("ctx.Settings");
            ctx.Settings.CancelUrl.MustNotBeNull("ctx.Settings.CancelUrl");

            var cancelUrl = ctx.Settings.CancelUrl;

            if (ctx.Request != null)
            {
                var qs = HttpUtility.ParseQueryString(ctx.Request.RequestUri.Query);

                var reason = qs["reason"];

                cancelUrl = AppendQueryStringParam(cancelUrl, "reason", reason);

                if (!string.IsNullOrWhiteSpace(ctx.Settings.ErrorUrl) && (reason == "failure" || reason == "error"))
                    return AppendQueryStringParam(ctx.Settings.ErrorUrl, "reason", reason);

            }

            return cancelUrl;
        }

        public override async Task<PaymentFormResult> GenerateFormAsync(PaymentProviderContext<KlarnaHppSettings> ctx)
        {
            var klarnaSecretToken = Guid.NewGuid().ToString("N");

            var clientConfig = GetKlarnaClientConfig(ctx.Settings);
            var client = new KlarnaClient(clientConfig);

            // Get currency information
            var billingCountry = Vendr.Services.CountryService.GetCountry(ctx.Order.PaymentInfo.CountryId.Value);
            var billingCountryCode = billingCountry.Code.ToUpperInvariant();

            // Ensure billing country has valid ISO 3166 code
            var iso3166Countries = Vendr.Services.CountryService.GetIso3166CountryRegions();
            if (!iso3166Countries.Any(x => x.Code == billingCountryCode))
            {
                throw new Exception("Country must be a valid ISO 3166 billing country code: " + billingCountry.Name);
            }

            var currency = Vendr.Services.CurrencyService.GetCurrency(ctx.Order.CurrencyId);
            var currencyCode = currency.Code.ToUpperInvariant();

            // Ensure currency has valid ISO 4217 code
            if (!Iso4217.CurrencyCodes.ContainsKey(currencyCode))
            {
                throw new Exception("Currency must be a valid ISO 4217 currency code: " + currency.Name);
            }

            // Prepair ctx.Order lines
            // NB: We add ctx.Order lines without any discounts applied as we'll then add
            // one global discount amount at the end. This is just the easiest way to
            // allow everything to add up and successfully validate at the Klarna end.
            var orderLines = ctx.Order.OrderLines.Select(orderLine => new KlarnaOrderLine
            {
                Reference = orderLine.Sku,
                Name = orderLine.Name,
                Type = !string.IsNullOrWhiteSpace(ctx.Settings.ProductTypePropertyAlias) && orderLine.Properties.ContainsKey(ctx.Settings.ProductTypePropertyAlias)
                    ? orderLine.Properties[ctx.Settings.ProductTypePropertyAlias]?.Value
                    : null,
                TaxRate = (int)(orderLine.TaxRate.Value * 10000),
                UnitPrice = (int)AmountToMinorUnits(orderLine.UnitPrice.WithoutAdjustments.WithTax),
                Quantity = (int)orderLine.Quantity,
                TotalAmount = (int)AmountToMinorUnits(orderLine.TotalPrice.WithoutAdjustments.WithTax),
                TotalTaxAmount = (int)AmountToMinorUnits(orderLine.TotalPrice.WithoutAdjustments.Tax)
            }).ToList();

            // Add shipping method fee ctx.Orderline
            if (ctx.Order.ShippingInfo.ShippingMethodId.HasValue && ctx.Order.ShippingInfo.TotalPrice.WithoutAdjustments.WithTax > 0) 
            {
                var shippingMethod = Vendr.Services.ShippingMethodService.GetShippingMethod(ctx.Order.ShippingInfo.ShippingMethodId.Value);
                
                orderLines.Add(new KlarnaOrderLine
                {
                    Reference = shippingMethod.Sku,
                    Name = string.Format(!string.IsNullOrWhiteSpace(ctx.Settings.FeeLabelTemplate) ? ctx.Settings.FeeLabelTemplate : "{0} Fee", shippingMethod.Name),
                    Type = KlarnaOrderLine.Types.SHIPPING_FEE,
                    TaxRate = (int)(ctx.Order.ShippingInfo.TaxRate * 10000),
                    UnitPrice = (int)AmountToMinorUnits(ctx.Order.ShippingInfo.TotalPrice.WithoutAdjustments.WithTax),
                    Quantity = 1,
                    TotalAmount = (int)AmountToMinorUnits(ctx.Order.ShippingInfo.TotalPrice.WithoutAdjustments.WithTax),
                    TotalTaxAmount = (int)AmountToMinorUnits(ctx.Order.ShippingInfo.TotalPrice.WithoutAdjustments.Tax),
                });
            }

            // Add payment method fee (as surcharge) ctx.Orderline
            if (ctx.Order.PaymentInfo.TotalPrice.WithoutAdjustments.WithTax > 0)
            {
                var paymentMethod = Vendr.Services.PaymentMethodService.GetPaymentMethod(ctx.Order.PaymentInfo.PaymentMethodId.Value);
                
                orderLines.Add(new KlarnaOrderLine
                {
                    Reference = paymentMethod.Sku,
                    Name = string.Format(!string.IsNullOrWhiteSpace(ctx.Settings.FeeLabelTemplate) ? ctx.Settings.FeeLabelTemplate : "{0} Fee", paymentMethod.Name),
                    Type = KlarnaOrderLine.Types.SURCHARGE,
                    TaxRate = (int)(ctx.Order.PaymentInfo.TaxRate * 10000),
                    UnitPrice = (int)AmountToMinorUnits(ctx.Order.PaymentInfo.TotalPrice.WithoutAdjustments.WithTax),
                    Quantity = 1,
                    TotalAmount = (int)AmountToMinorUnits(ctx.Order.PaymentInfo.TotalPrice.WithoutAdjustments.WithTax),
                    TotalTaxAmount = (int)AmountToMinorUnits(ctx.Order.PaymentInfo.TotalPrice.WithoutAdjustments.Tax),
                });
            }

            // Add any discounts
            if (ctx.Order.TotalPrice.TotalAdjustment < 0)
            {
                orderLines.Add(new KlarnaOrderLine
                {
                    Reference = "DISCOUNT",
                    Name = !string.IsNullOrWhiteSpace(ctx.Settings.DiscountsLabel) ? ctx.Settings.DiscountsLabel : "Discounts",
                    Type = KlarnaOrderLine.Types.DISCOUNT,
                    TaxRate = (int)(ctx.Order.TaxRate * 10000),
                    UnitPrice = 0,
                    Quantity = 1,
                    TotalDiscountAmount = (int)AmountToMinorUnits(ctx.Order.TotalPrice.TotalAdjustment.WithTax) * -1,
                    TotalAmount = (int)AmountToMinorUnits(ctx.Order.TotalPrice.TotalAdjustment.WithTax),
                    TotalTaxAmount = (int)AmountToMinorUnits(ctx.Order.TotalPrice.TotalAdjustment.Tax),
                });
            } 
            else if (ctx.Order.TotalPrice.TotalAdjustment > 0)
            {
                orderLines.Add(new KlarnaOrderLine
                {
                    Reference = "SURCHARGE",
                    Name = !string.IsNullOrWhiteSpace(ctx.Settings.AdditionalFeesLabel) ? ctx.Settings.AdditionalFeesLabel : "Additional Fees",
                    Type = KlarnaOrderLine.Types.SURCHARGE,
                    TaxRate = (int)(ctx.Order.TaxRate * 10000),
                    UnitPrice = 0,
                    Quantity = 1,
                    TotalAmount = (int)AmountToMinorUnits(ctx.Order.TotalPrice.TotalAdjustment.WithTax),
                    TotalTaxAmount = (int)AmountToMinorUnits(ctx.Order.TotalPrice.TotalAdjustment.Tax),
                });
            }

            // Create a merchant session
            var resp1 = await client.CreateMerchantSessionAsync(new KlarnaCreateMerchantSessionOptions
            {
                MerchantReference1 = ctx.Order.OrderNumber,
                PurchaseCountry = billingCountryCode,
                PurchaseCurrency = currencyCode,
                Locale = ctx.Order.LanguageIsoCode, // TODO: Validate?

                OrderLines = orderLines,
                OrderAmount = (int)AmountToMinorUnits(ctx.Order.TotalPrice.Value.WithTax),
                OrderTaxAmount = (int)AmountToMinorUnits(ctx.Order.TotalPrice.Value.Tax),

                BillingAddress = new KlarnaAddress
                {
                    GivenName = ctx.Order.CustomerInfo.FirstName,
                    FamilyName = ctx.Order.CustomerInfo.LastName,
                    Email = ctx.Order.CustomerInfo.Email,
                    StreetAddress = !string.IsNullOrWhiteSpace(ctx.Settings.BillingAddressLine1PropertyAlias)
                        ? ctx.Order.Properties[ctx.Settings.BillingAddressLine1PropertyAlias]?.Value : null,
                    StreetAddress2 = !string.IsNullOrWhiteSpace(ctx.Settings.BillingAddressLine2PropertyAlias)
                        ? ctx.Order.Properties[ctx.Settings.BillingAddressLine2PropertyAlias]?.Value : null,
                    City = !string.IsNullOrWhiteSpace(ctx.Settings.BillingAddressCityPropertyAlias)
                        ? ctx.Order.Properties[ctx.Settings.BillingAddressCityPropertyAlias]?.Value : null,
                    Region = !string.IsNullOrWhiteSpace(ctx.Settings.BillingAddressStatePropertyAlias)
                        ? ctx.Order.Properties[ctx.Settings.BillingAddressStatePropertyAlias]?.Value : null,
                    PostalCode = !string.IsNullOrWhiteSpace(ctx.Settings.BillingAddressZipCodePropertyAlias)
                        ? ctx.Order.Properties[ctx.Settings.BillingAddressZipCodePropertyAlias]?.Value : null,
                    Country = billingCountryCode
                }
            });;

            // Create a HPP session
            var resp2 = await client.CreateHppSessionAsync(new KlarnaCreateHppSessionOptions
            {
                PaymentSessionUrl = $"{clientConfig.BaseUrl}/payments/v1/sessions/{resp1.SessionId}",
                Options = new KlarnaHppOptions
                {
                    PlaceOrderMode = ctx.Settings.Capture 
                        ? KlarnaHppOptions.PlaceOrderModes.CAPTURE_ORDER
                        : KlarnaHppOptions.PlaceOrderModes.PLACE_ORDER,
                    LogoUrl = !string.IsNullOrWhiteSpace(ctx.Settings.PaymentPageLogoUrl)
                        ? ctx.Settings.PaymentPageLogoUrl.Trim()
                        : null,
                    PageTitle = !string.IsNullOrWhiteSpace(ctx.Settings.PaymentPagePageTitle)
                        ? ctx.Settings.PaymentPagePageTitle.Trim()
                        : null,
                    PaymentMethodCategories = !string.IsNullOrWhiteSpace(ctx.Settings.PaymentMethodCategories)
                        ? ctx.Settings.PaymentMethodCategories.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(x => x.Trim())
                            .Where(x => !string.IsNullOrWhiteSpace(x))
                            .ToArray()
                        : null,
                    PaymentMethodCategory = !string.IsNullOrWhiteSpace(ctx.Settings.PaymentMethodCategory)
                        ? ctx.Settings.PaymentMethodCategory.Trim()
                        : null,
                    PaymentFallback = ctx.Settings.EnableFallbacks
                },
                MerchantUrls = new KlarnaHppMerchantUrls
                {
                    Success = ctx.Urls.ContinueUrl,
                    Cancel = AppendQueryString(ctx.Urls.CancelUrl, "reason=cancel"),
                    Back = AppendQueryString(ctx.Urls.CancelUrl, "reason=back"),
                    Failure = AppendQueryString(ctx.Urls.CancelUrl, "reason=failure"),
                    Error = AppendQueryString(ctx.Urls.CancelUrl, "reason=error"),
                    StatusUpdate = AppendQueryString(ctx.Urls.CallbackUrl, "sid={{session_id}}&token="+ klarnaSecretToken),
                }
            });

            return new PaymentFormResult()
            {
                Form = new PaymentForm(resp2.RedirectUrl, PaymentFormMethod.Get),
                MetaData = new Dictionary<string, string>
                {
                    { "klarnaSessionId", resp2.SessionId },
                    { "klarnaSecretToken", klarnaSecretToken }
                }
            };
        }

       

        public override async Task<CallbackResult> ProcessCallbackAsync(PaymentProviderContext<KlarnaHppSettings> ctx)
        {
            var qs = HttpUtility.ParseQueryString(ctx.Request.RequestUri.Query);

            var sessionId = qs["sid"];
            var token = qs["token"];

            if (!string.IsNullOrWhiteSpace(sessionId) && ctx.Order.Properties["klarnaSessionId"] == sessionId
                && !string.IsNullOrWhiteSpace(token) && ctx.Order.Properties["klarnaSecretToken"] == token)
            {
                var clientConfig = GetKlarnaClientConfig(ctx.Settings);
                var client = new KlarnaClient(clientConfig);

                using (var stream = await ctx.Request.Content.ReadAsStreamAsync())
                {
                    var evt = client.ParseSessionEvent(stream);
                    if (evt != null && evt.Session.Status == KlarnaSession.Statuses.COMPLETED)
                    {
                        var klarnaOrder = await client.GetOrderAsync(evt.Session.OrderId);

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
            }

            return CallbackResult.Ok();
        }

        public override async Task<ApiResult> FetchPaymentStatusAsync(PaymentProviderContext<KlarnaHppSettings> ctx)
        {
            try
            {
                var orderId = ctx.Order.TransactionInfo.TransactionId;

                var clientConfig = GetKlarnaClientConfig(ctx.Settings);
                var client = new KlarnaClient(clientConfig);

                var klarnaOrder = await client.GetOrderAsync(orderId);
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
                _logger.Error(ex, "Error fetching Klarna payment status for ctx.Order {OrderNumber}", ctx.Order.OrderNumber);
            }

            return ApiResult.Empty;
        }

        public override async Task<ApiResult> CapturePaymentAsync(PaymentProviderContext<KlarnaHppSettings> ctx)
        {
            try
            {
                var orderId = ctx.Order.TransactionInfo.TransactionId;

                var clientConfig = GetKlarnaClientConfig(ctx.Settings);
                var client = new KlarnaClient(clientConfig);

                await client.CaptureOrderAsync(orderId, new KlarnaCaptureOptions
                {
                    Description = $"Capture Order {ctx.Order.OrderNumber}",
                    CapturedAmount = (int)AmountToMinorUnits(ctx.Order.TransactionInfo.AmountAuthorized.Value)
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
                _logger.Error(ex, "Error capturing Klarna payment for ctx.Order {OrderNumber}", ctx.Order.OrderNumber);
            }

            return ApiResult.Empty;
        }

        public override async Task<ApiResult> RefundPaymentAsync(PaymentProviderContext<KlarnaHppSettings> ctx)
        {
            try
            {
                var orderId = ctx.Order.TransactionInfo.TransactionId;

                var clientConfig = GetKlarnaClientConfig(ctx.Settings);
                var client = new KlarnaClient(clientConfig);

                await client.RefundOrderAsync(orderId, new KlarnaRefundOptions
                {
                    Description = $"Refund Order {ctx.Order.OrderNumber}",
                    RefundAmount = (int)AmountToMinorUnits(ctx.Order.TransactionInfo.AmountAuthorized.Value)
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
                _logger.Error(ex, "Error refunding Klarna payment for ctx.Order {OrderNumber}", ctx.Order.OrderNumber);
            }

            return ApiResult.Empty;
        }

        public override async Task<ApiResult> CancelPaymentAsync(PaymentProviderContext<KlarnaHppSettings> ctx)
        {
            try
            {
                var orderId = ctx.Order.TransactionInfo.TransactionId;

                var clientConfig = GetKlarnaClientConfig(ctx.Settings);
                var client = new KlarnaClient(clientConfig);

                await client.CancelOrderAsync(orderId);

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
                _logger.Error(ex, "Error canceling Klarna payment for ctx.Order {OrderNumber}", ctx.Order.OrderNumber);
            }

            return ApiResult.Empty;
        }
    }
}
