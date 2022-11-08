using System.Collections.Generic;

namespace Demo.Payment.Models
{
    public class PayURequestData
    {
        public string merchantPaymentReference { get; set; }
        public string currency { get; set; }
        public string returnUrl { get; set; }
        public Authorization authorization { get; set; }
        public Client client { get; set; }
        public List<Product> products { get; set; }
        public StoredCredentials storedCredentials { get; set; }
    }


    public class Authorization
    {
        public string paymentMethod { get; set; }
        public string usePaymentPage { get; set; }
        public MerchantToken merchantToken { get; set; }
    }

    public class Billing
    {
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string email { get; set; }
        public string phone { get; set; }
        public string city { get; set; }
        public string countryCode { get; set; }
        public string state { get; set; }
        public string addressLine1 { get; set; }
        public string addressLine2 { get; set; }
        public string zipCode { get; set; }
        public string companyName { get; set; }
        public string taxId { get; set; }
    }

    public class Client
    {
        public Billing billing { get; set; }
        public Delivery delivery { get; set; }
        public string communicationLanguage { get; set; }
    }

    public class Delivery
    {
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string phone { get; set; }
        public string addressLine1 { get; set; }
        public string addressLine2 { get; set; }
        public string zipCode { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string countryCode { get; set; }
        public string email { get; set; }
    }

    public class Product
    {
        public string name { get; set; }
        public string sku { get; set; }
        public string additionalDetails { get; set; }
        public decimal unitPrice { get; set; }
        public int quantity { get; set; }
        public int vat { get; set; }
    }

    public class StoredCredentials
    {
        public string consentType { get; set; }
        public string useType { get; set; }
    }


}
