using System;
using System.IO;
using System.Threading.Tasks;
using API.Errors;
using Core.Entities;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Stripe;
using Order = Core.Entities.OrderAggregate.Order;

namespace API.Controllers
{
    public class PaymentsController : BaseApiController
    {
        private readonly string _whSecret;
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(IPaymentService paymentService, ILogger<PaymentsController> logger,IConfiguration config)
        {
            _logger = logger;
            _paymentService = paymentService;
            _whSecret = config.GetSection("StripeSettings:WhSecret").Value;
        }

        [Authorize]
        [HttpPost("{basketId}")]
        public async Task<ActionResult<CustomerBasket>> CreateOrUpdatePaymentIntent(string basketId)
        {
            var basket = await _paymentService.CreateOrUpdatePaymentIntent(basketId);

            if (basket == null) return BadRequest(new ApiResponse(400, "Problem with your basket"));

            return basket;
        }

        [HttpPost("webhook")]
        public async Task<ActionResult> StripeWebhook()
        {
            var json = await new StreamReader(Request.Body).ReadToEndAsync();

            try
            {
                var stripeEvent = EventUtility.ConstructEvent(json, Request.Headers["Stripe-Signature"], _whSecret, throwOnApiVersionMismatch: false);

                PaymentIntent intent;
                Order order;

                switch (stripeEvent.Type)
                {
                    case "payment_intent.succeeded":
                        intent = (PaymentIntent)stripeEvent.Data.Object;
                        _logger.LogInformation("Payment succeeded: {Id}", intent.Id);

                        order = await _paymentService.UpdateOrderPaymentSucceeded(intent.Id);

                        if (order != null)
                        {
                            _logger.LogInformation("Order updated to payment received: {OrderId}", order.Id);
                        }
                        else
                        {
                            _logger.LogWarning("Order with PaymentIntent {Id} not found in DB", intent.Id);
                        }
                        break;

                    case "payment_intent.payment_failed":
                        intent = (PaymentIntent)stripeEvent.Data.Object;
                        _logger.LogInformation("Payment failed: {Id}", intent.Id);

                        order = await _paymentService.UpdateOrderPaymentFailed(intent.Id);

                        if (order != null)
                        {
                            _logger.LogInformation("Order updated to payment failed: {OrderId}", order.Id);
                        }
                        else
                        {
                            _logger.LogWarning("Order with PaymentIntent {Id} not found in DB", intent.Id);
                        }
                        break;
                }

                return new EmptyResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stripe Webhook Error");
                return BadRequest(); 
            }
        }
    }
}