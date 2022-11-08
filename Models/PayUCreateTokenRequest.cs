using Newtonsoft.Json;

namespace Demo.Payment.Models
{
    public class PayUCreateTokenRequest
    {
        [JsonProperty("payuPaymentReference")]
        public int PayuPaymentReference { get; set; }
    }

}
