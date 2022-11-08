namespace Demo.Payment.Models
{
    public class PaymentResult
    {
        public string payuResponseCode { get; set; }
        public string url { get; set; }
        public string type { get; set; }
    }

    public class PayUResponseData
    {
        public string? payuPaymentReference { get; set; }
        public string status { get; set; }
        public PaymentResult? paymentResult { get; set; }
        public string message { get; set; }
        public string? merchantPaymentReference { get; set; }
        public int code { get; set; }
        public string? amount { get; set; }
    }




}
