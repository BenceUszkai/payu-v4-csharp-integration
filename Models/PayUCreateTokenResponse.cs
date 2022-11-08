using Newtonsoft.Json;

namespace Demo.Payment.Models
{
    public class PayUCreateTokenResponse
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("cardUniqueIdentifier")]
        public string CardUniqueIdentifier { get; set; }

        [JsonProperty("expirationDate")]
        public string ExpirationDate { get; set; }

        [JsonProperty("cardHolderName")]
        public string CardHolderName { get; set; }

        [JsonProperty("tokenStatus")]
        public string TokenStatus { get; set; }

        [JsonProperty("lastFourDigits")]
        public string LastFourDigits { get; set; }

        [JsonProperty("cardExpirationDate")]
        public string CardExpirationDate { get; set; }
    }

}
