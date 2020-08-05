using System;
using System.Web;
using System.Web.Mvc;
using Vendr.Core;
using Vendr.Core.Models;
using Vendr.Core.Web.Api;
using Vendr.Core.Web.PaymentProviders;

namespace Vendr.PaymentProviders.Klarna
{
    [PaymentProvider("klarna-hpp", "Klarna (HPP)", "Klarna payment provider using the Klarna Hosted Payment Page (HPP)")]
    public class KlarnaHppPaymentProvider : PaymentProviderBase<KlarnaHppSettings>
    {
        public KlarnaHppPaymentProvider(VendrContext vendr)
            : base(vendr)
        { }

        public override bool CanFetchPaymentStatus => true;
        public override bool CanCancelPayments => true;
        public override bool CanCapturePayments => true;
        public override bool CanRefundPayments => true;

        public override PaymentFormResult GenerateForm(OrderReadOnly order, string continueUrl, string cancelUrl, string callbackUrl, KlarnaHppSettings settings)
        {
            return new PaymentFormResult()
            {
                Form = new PaymentForm(continueUrl, FormMethod.Post)
            };
        }

        public override string GetCancelUrl(OrderReadOnly order, KlarnaHppSettings settings)
        {
            return string.Empty;
        }

        public override string GetErrorUrl(OrderReadOnly order, KlarnaHppSettings settings)
        {
            return string.Empty;
        }

        public override string GetContinueUrl(OrderReadOnly order, KlarnaHppSettings settings)
        {
            settings.MustNotBeNull("settings");
            settings.ContinueUrl.MustNotBeNull("settings.ContinueUrl");

            return settings.ContinueUrl;
        }

        public override CallbackResult ProcessCallback(OrderReadOnly order, HttpRequestBase request, KlarnaHppSettings settings)
        {
            return new CallbackResult
            {
                TransactionInfo = new TransactionInfo
                {
                    AmountAuthorized = order.TotalPrice.Value.WithTax,
                    TransactionFee = 0m,
                    TransactionId = Guid.NewGuid().ToString("N"),
                    PaymentStatus = PaymentStatus.Authorized
                }
            };
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
