using Demo.Payment.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Demo.Payment.Service
{
    public class PayUService
    {
        private readonly DataServiceProvider _provider;
        private readonly IHttpContextAccessor _httpContext;
        private readonly IHostEnvironment _environment;
        private string _basePath;
        private string _merchant;
        private string _apiSecret;



        public PayUService(IDataServiceProvider provider, IHttpContextAccessor httpContext, IHostEnvironment hostEnvironment)
        {
            _provider = provider as DataServiceProvider;
            _httpContext = httpContext;
            _environment = hostEnvironment;

            _basePath = _environment.IsProduction() ? "https://secure.payu.ro" : "https://sandbox.payu.ro";
            _merchant = // your merchant ID
            _apiSecret = // your secret key
        }

        public async Task<string> InitPayment(object data, bool saveCard, int userid)
        {
            var request = new PayURequestData();
            var path = "/api/v4/payments/authorize";
            SetupGeneralData(request, data);
            SetupAuthorzation(request);
            SetupClient(request, data);
            SetupProducts(request, data);
            if (saveCard) SetupStoredCredentials(request);
            string s = JsonConvert.SerializeObject(request);
            string formattedJSON = FormatJson(s);
            var bodyHash = GetBodyHash(formattedJSON).ToLower();

            using (var httpClient = new HttpClient())
            {
                AddHeaders(httpClient, path, bodyHash,"POST");
                var content = new StringContent(formattedJSON, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(_basePath + path, content);
                var contents = await response.Content.ReadAsStringAsync();
                var responseData = JsonConvert.DeserializeObject<PayUResponseData>(contents);
                if (responseData.code == 200 && responseData.status == "SUCCESS")
                {
                    return responseData.paymentResult.url;
                }
                return "" //your failed payment return URL;
            }


        }

        public async Task SaveCardAsync(PayUResponseData data, int userId)
        {
            var path = "/api/v4/token";
            PayUCreateTokenRequest body = new PayUCreateTokenRequest()
            { PayuPaymentReference = int.Parse(data.payuPaymentReference) };

            string payload = JsonConvert.SerializeObject(body);
            string formattedJSON = FormatJson(payload);
            var bodyHash = GetBodyHash(formattedJSON).ToLower();

            using (var httpClient = new HttpClient())
            {
                AddHeaders(httpClient, path, bodyHash,"POST");
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var content = new StringContent(formattedJSON, Encoding.UTF8, "application/json");
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                var res = await httpClient.PostAsync(_basePath + path, content);
                var contents = await res.Content.ReadAsStringAsync();
                var payuResponse = JsonConvert.DeserializeObject<PayUCreateTokenResponse>(contents);
                if (payuResponse.Code == 200 && payuResponse.Status.ToUpper() == "SUCCESS")
                {
                    SaveBankcardToDB(payuResponse, userId);
                }
            }
        }

        private void SaveBankcardToDB(PayUCreateTokenResponse cardData, int userid)
        {
            try
            {
                var CCExpDate = cardData.CardExpirationDate.Split('-');
                var year = int.Parse(CCExpDate[0]);
                var month = int.Parse(CCExpDate[1]);
                var day = int.Parse(CCExpDate[2]);

                var card = new SavedBankCard()
                {
                    CC_ExpirationDate = new DateTime(year, month, day),
                    CC_Mask = "xxxx-xxxx-xxxx-" + cardData.LastFourDigits,
                    CC_Token = cardData.Token,
                    Created = DateTime.Now,
                    UserId = userid
                };

                this._provider.ctx.SavedBankCard.Add(card);
                this._provider.ctx.SaveChanges();
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        private void SetupStoredCredentials(PayURequestData request)
        {
            request.storedCredentials = new StoredCredentials()
            {
                consentType = "onDemand"
            };
        }

        private string GetBaseUrl()
        {
            var domainName = _httpContext.HttpContext.Request.Host.Value;
            var requestURL = string.Empty;

            if (_environment.IsDevelopment())
            {
                if (domainName.Contains(':'))
                    domainName = domainName.Substring(0, domainName.IndexOf(':'));
                requestURL = $"{domainName}:4200";
            }

            if (_environment.IsProduction())
            {
                if (domainName.Contains(':'))
                    domainName = domainName.Substring(0, domainName.IndexOf(':'));
            }

            return $"{_httpContext.HttpContext.Request.Scheme}://{requestURL}";
        }

        private string GetBodyHash(string data)
        {
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(data);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                //Convert the byte array to hexadecimal string prior to .NET 5
                StringBuilder sb = new System.Text.StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }

        }

        private void AddHeaders(HttpClient httpClient, string path, string bodyHash, string method)
        {
            var date = DateTime.Now.ToString("s");

            var signature = CalculateSignature(_merchant, date, method, path, bodyHash).ToLower();
            httpClient.DefaultRequestHeaders.Add("X-Header-Merchant", _merchant);
            httpClient.DefaultRequestHeaders.Add("X-Header-Date", date);
            httpClient.DefaultRequestHeaders.Add("X-Header-Signature", signature);
        }

        private string CalculateSignature(string merchant, string date, string method, string path, string bodyHash)
        {
            var stringToHash = merchant + date + method + path + bodyHash;
            var hash = Encryption.GetHMAC256Hash(stringToHash, _apiSecret);
            return hash;
        }

        private void SetupGeneralData(PayURequestData request, object data)
        {
            request.merchantPaymentReference = //your payment refrence ID;
            request.currency = data.currency;
            request.returnUrl = //your return URL. in case of successful payment, payu POST FORMDATA to this URL
        }

        public async Task<string> GetResultDataAsync(string data, bool saveCard = false, int userId = 0)
        {
            var res = JsonConvert.DeserializeObject<PayUResponseData>(data);
            if (res != null && res.code == 200 && res.status == "SUCCESS")
            {
                var dataId = res.merchantPaymentReference.Split('_').Last();
                if (saveCard) await SaveCardAsync(res, userId);

                return GetBaseUrl() + $"/offers/order/checkout?status=SUCCESS&dataId={dataId}";
            }
            else
            {
                var dataId = res.merchantPaymentReference.Split('_').Last();
                return GetBaseUrl() + $"/offers/order/checkout?status=FAIL&dataId={dataId}";
            }
        }

        private object GetReturnBaseUrl()
        {
            var domainName = _httpContext.HttpContext.Request.Host.Value;
            return $"{_httpContext.HttpContext.Request.Scheme}://{domainName}";
        }
        private void SetupAuthorzation(PayURequestData request, string token = null)
        {
            request.authorization = new Authorization()
            {
                paymentMethod = "CCVISAMC",
            };

            if (token != null)
            {
                request.authorization.merchantToken = new MerchantToken()
                {
                    tokenHash = token,
                    cvv = "",
                };
            }
            else
            {
                request.authorization.usePaymentPage = "YES";
            }
        }
        private void SetupClient(PayURequestData request, object data)
        {
            request.client = new Client()
            {
                billing = new Billing()
                {
                    firstName = data.FirstName,
                    lastName = data.LastName,
                    email = data.Email,
                    phone = data.Phone,
                    countryCode = data.Country,
                }
            };
        }
        private void SetupProducts(PayURequestData request, object data)
        {
            request.products = new List<Product>();
            request.products.Add(new Product()
            {
                name = data.product.name,
                sku = data.product.sku,
                unitPrice = (decimal)data.amGross,
                quantity = 1,
                vat = 0
            });
        }

        /*
            !!! IMPORTANT !!!

            PayU v4 POST method's body MUST BE INDENTED before MD5 hash
            C# JSON serializer write whole body in one line, and JSON formatter indent only 2 spaces, instead of 4 space or 1 tab.
            Thats the reason to need to implement custom formatter. 
            You can find this formatter below
        */

        //Custom formatter start
        private const string INDENT_STRING = "    ";
        static string FormatJson(string json)
        {

            int indentation = 0;
            int dataCount = 0;
            var request =
                from ch in json
                let datas = ch == '"' ? dataCount++ : dataCount
                let lineBreak = ch == ',' && datas % 2 == 0 ? ch + Environment.NewLine + String.Concat(Enumerable.Repeat(INDENT_STRING, indentation)) : null
                let openChar = ch == '{' || ch == '[' ? ch + Environment.NewLine + String.Concat(Enumerable.Repeat(INDENT_STRING, ++indentation)) : ch.ToString()
                let closeChar = ch == '}' || ch == ']' ? Environment.NewLine + String.Concat(Enumerable.Repeat(INDENT_STRING, --indentation)) + ch : ch.ToString()
                select lineBreak == null
                            ? openChar.Length > 1
                                ? openChar
                                : closeChar
                            : lineBreak;

            var concated = String.Concat(request).Replace("\":", "\": ");
            return concated;
        }
        // Custom formatter end

        public async Task<string> SavedCardPayment(int dataId, string token)
        {
            var request = new PayURequestData();

            var data = _provider.ctx.datas.FirstOrDefault(x => x.iddata == dataId);
            var path = "/api/v4/payments/authorize";

            SetupRequestData(token, request, data);
            string s = JsonConvert.SerializeObject(request);
            string formattedJSON = FormatJson(s);
            var bodyHash = GetBodyHash(formattedJSON).ToLower();

            using (var httpClient = new HttpClient())
            {
                AddHeaders(httpClient, path, bodyHash,"POST");
                var content = new StringContent(formattedJSON, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(_basePath + path, content);
                var contents = await response.Content.ReadAsStringAsync();
                var responseData = JsonConvert.DeserializeObject<PayUResponseData>(contents);
                if (responseData.code == 200 && responseData.status == "SUCCESS")
                {
                    return responseData.paymentResult.url;
                }
                else
                {
                    return //YOUR FAIL URL TO NAVIGATE
                }
            }
        }

        private void SetupRequestData(string token, PayURequestData request, object data)
        {
            SetupGeneralData(request, data);
            SetupAuthorzation(request, token);
            SetupClient(request, data);
            SetupProducts(request, data);
        }

        public async Task<object> DeleteCardAsync(string token)
        {
            var path = "/api/v4/token/" + token;
            var bodyHash = GetBodyHash("").ToLower();

            using (var httpClient = new HttpClient())
            {
                AddHeaders(httpClient, path,bodyHash,"DELETE");
                var response = await httpClient.DeleteAsync(_basePath + path);
                var contents = await response.Content.ReadAsStringAsync();
                var responseData = JsonConvert.DeserializeObject<PayUResponseData>(contents);
                var alreadyCanceled = responseData.message == "TOKEN_ALREADY_CANCELED";
                var sucess = responseData.code == 200 && responseData.status == "SUCCESS";
                if (alreadyCanceled || sucess)
                {
                    this._provider.DeleteCardByToken(token);
                }
                return responseData;
            }
        }
    }
}

