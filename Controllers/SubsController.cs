using Floaty_Music.Models;
using Floaty_Music.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sindika.AspNet.Midtrans.Contracts;
using Sindika.AspNet.Midtrans.Exceptions;
using Sindika.AspNet.Midtrans.Models.Common;
using Sindika.AspNet.Midtrans.Models.Notification;
using Sindika.AspNet.Midtrans.Models.Request.Snap;
using Sindika.AspNet.Midtrans.Models.Response.Common;
using Sindika.AspNet.Midtrans.Services;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Floaty_Music.Controllers
{
    [Route("subs")]
    public class SubsController : Controller
    {
        private readonly ISnapService _snap;
        private readonly FloatlyContext _db;

        public SubsController(ISnapService snap, FloatlyContext cont)
        {
            _snap = snap;
            _db = cont;
        }
        [HttpPost("pay")]
        public async Task<IActionResult> Pay(string username)
        {
            var orderId = $"TRX-{Guid.NewGuid()}";
            var user = _db.Users.FirstOrDefault(u => u.Username == username);
            if (user == null)
            {
                return BadRequest("User not found");
            }
            await _db.Transaction.AddAsync(new Transaction
            {
                OrderId = orderId,
                UserId = user.Id,
                Amount = 5000,
                PaymentStatus = (int)Sindika.AspNet.Midtrans.Enums.TransactionStatus.Pending,
                CreatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();
            var request = new SnapTransactionRequest
            {
                TransactionDetails = new TransactionDetails
                {
                    OrderId = orderId,
                    GrossAmount = 5000
                },
                CustomerDetails = new CustomerDetails
                {
                    FirstName = user.Username,
                    Email = user.Email
                }
            };
            try
            {
                var token = await _snap.CreateTransactionAsync(request);
                return Ok(token.Token);

            }
            catch (MidtransException ex)
            {
                return BadRequest(ex.InnerException);
            }
        }
        [HttpGet("success")]
        public async Task<IActionResult> Success(string order_id, string transaction_status, string status_code)
        {
            ViewBag.Trx = _db.Transaction.Include(x => x.User).FirstOrDefault(x => x.OrderId == order_id);
            return View("Success");
        }
        [HttpGet("error")]
        public async Task<IActionResult> Error(string order_id, string transaction_status, string status_code)
        {
            return Json("kamu skill isu");
        }

        [IgnoreAntiforgeryToken]
        [HttpPost("/midtrans/notification")]
        public async Task<IActionResult> Notification()
        {
            var raw = await new StreamReader(Request.Body).ReadToEndAsync();
            var n = JsonSerializer.Deserialize<ApiModelMidtransNotification>(raw);
            var order = await _db.Transaction
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.OrderId == n.OrderId);

            if (order == null)
                return Ok();

            var previousStatus = (Sindika.AspNet.Midtrans.Enums.TransactionStatus)order.PaymentStatus;

            // map Midtrans string → enum
            var newStatus = n.TransactionStatus switch
            {
                "settlement" => Sindika.AspNet.Midtrans.Enums.TransactionStatus.Settlement,
                "pending" => Sindika.AspNet.Midtrans.Enums.TransactionStatus.Pending,
                "deny" => Sindika.AspNet.Midtrans.Enums.TransactionStatus.Deny,
                "cancel" => Sindika.AspNet.Midtrans.Enums.TransactionStatus.Cancel,
                "expire" => Sindika.AspNet.Midtrans.Enums.TransactionStatus.Expire,
                "failure" => Sindika.AspNet.Midtrans.Enums.TransactionStatus.Unknown,
                _ => previousStatus
            };

            order.PaymentStatus = (int)newStatus;

            // grant subscription ONLY once
            if (newStatus == Sindika.AspNet.Midtrans.Enums.TransactionStatus.Settlement &&
                previousStatus != Sindika.AspNet.Midtrans.Enums.TransactionStatus.Settlement &&
                order.User != null)
            {
                order.User.PremiumExpired =
                    order.User.PremiumExpired > DateTime.Now
                        ? order.User.PremiumExpired.AddDays(30)
                        : DateTime.Now.AddDays(30);
            }

            await _db.SaveChangesAsync();
            return Ok();
        }


        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Index()
        {
            return View();
        }
    }
    public class ApiModelMidtransNotification
    {
        [JsonPropertyName("transaction_status")]
        public string TransactionStatus { get; set; }

        [JsonPropertyName("order_id")]
        public string OrderId { get; set; }

        [JsonPropertyName("status_code")]
        public string StatusCode { get; set; }

        [JsonPropertyName("gross_amount")]
        public string GrossAmount { get; set; }

        [JsonPropertyName("payment_type")]
        public string PaymentType { get; set; }

        [JsonPropertyName("transaction_time")]
        public string TransactionTime { get; set; }

        [JsonPropertyName("settlement_time")]
        public string SettlementTime { get; set; }

        [JsonPropertyName("fraud_status")]
        public string FraudStatus { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; }

        [JsonPropertyName("signature_key")]
        public string SignatureKey { get; set; }

        [JsonPropertyName("va_numbers")]
        public List<ApiModelVaNumber> VaNumbers { get; set; }

        [JsonPropertyName("payment_amounts")]
        public List<ApiModelPaymentAmount> PaymentAmounts { get; set; }

        [JsonPropertyName("customer_details")]
        public ApiModelCustomerDetails CustomerDetails { get; set; }
    }

    public class ApiModelVaNumber
    {
        [JsonPropertyName("va_number")]
        public string VaNumber { get; set; }

        [JsonPropertyName("bank")]
        public string Bank { get; set; }
    }

    public class ApiModelPaymentAmount
    {
        [JsonPropertyName("paid_at")]
        public string PaidAt { get; set; }

        [JsonPropertyName("amount")]
        public string Amount { get; set; }
    }

    public class ApiModelCustomerDetails
    {
        [JsonPropertyName("full_name")]
        public string FullName { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }
    }

}
