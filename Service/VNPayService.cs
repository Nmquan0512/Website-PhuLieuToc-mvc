using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace PhuLieuToc.Service
{
    public class VNPayOptions
    {
        public string TmnCode { get; set; } = string.Empty;
        public string HashSecret { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
        public string ReturnUrl { get; set; } = string.Empty;
    }

    public class VNPayService
    {
        private readonly VNPayOptions _options;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public VNPayService(VNPayOptions options, IHttpContextAccessor accessor)
        {
            _options = options;
            _httpContextAccessor = accessor;
        }

        public string CreatePaymentUrl(Guid orderId, decimal amount, string orderInfo)
        {
            var ip = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var vnpParams = new SortedDictionary<string, string>
            {
                ["vnp_Version"] = "2.1.0",
                ["vnp_Command"] = "pay",
                ["vnp_TmnCode"] = _options.TmnCode,
                ["vnp_Amount"] = ((long)(amount * 100)).ToString(),
                ["vnp_CurrCode"] = "VND",
                ["vnp_TxnRef"] = orderId.ToString("N").Substring(0, 12).ToUpper(),
                ["vnp_OrderInfo"] = orderInfo,
                ["vnp_OrderType"] = "other",
                ["vnp_Locale"] = "vn",
                ["vnp_ReturnUrl"] = _options.ReturnUrl,
                ["vnp_IpAddr"] = ip,
                ["vnp_CreateDate"] = DateTime.Now.ToString("yyyyMMddHHmmss")
            };

            var query = BuildQuery(vnpParams);
            var sign = HmacSHA512(_options.HashSecret, query);
            return _options.BaseUrl + "?" + query + "&vnp_SecureHash=" + sign;
        }

        public bool ValidateReturn(IQueryCollection query, out string rspCode, out string txnRef)
        {
            rspCode = query["vnp_ResponseCode"].ToString();
            txnRef = query["vnp_TxnRef"].ToString();

            var data = new SortedDictionary<string, string>();
            foreach (var kv in query)
            {
                if (kv.Key.StartsWith("vnp_", StringComparison.OrdinalIgnoreCase) && kv.Key != "vnp_SecureHash")
                {
                    data[kv.Key] = kv.Value.ToString();
                }
            }
            var raw = BuildQuery(data);
            var expected = HmacSHA512(_options.HashSecret, raw);
            var actual = query["vnp_SecureHash"].ToString();
            return string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildQuery(SortedDictionary<string, string> dict)
        {
            var sb = new StringBuilder();
            foreach (var kv in dict)
            {
                if (sb.Length > 0) sb.Append('&');
                sb.Append(Uri.EscapeDataString(kv.Key));
                sb.Append('=');
                sb.Append(Uri.EscapeDataString(kv.Value));
            }
            return sb.ToString();
        }

        private static string HmacSHA512(string key, string data)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return string.Concat(hash.Select(b => b.ToString("x2")));
        }
    }
}


