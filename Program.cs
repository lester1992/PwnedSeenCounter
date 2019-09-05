using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PwnedCounter
{
    class Program
    {
        
        public static void Main(string[] args)
        {

            RestAPI client = new RestAPI();
            int pwnedcount =  client.PwnedCount("11223344").Result;
            Console.WriteLine("This password has been seen " + pwnedcount + " times before, This password has previously appeared in a data breach and should never be used. If you've ever used it anywhere before, change it!");
        }
        
       
    }
    public class RestAPI
    {
        private static readonly HttpClient client = new HttpClient();
        private readonly string PwnedPasswordURL = @"https://api.pwnedpasswords.com";
        private StringBuilder HashMyPassword(string password)
        {
            SHA1 sha1 = SHA1.Create();
            byte[] byteString = Encoding.UTF8.GetBytes(password);
            byte[] hashBytes = sha1.ComputeHash(byteString);
            StringBuilder stringBulder = new StringBuilder();
            foreach (byte b in hashBytes) {
                stringBulder.Append(b.ToString("X2"));
            }
            return stringBulder;
        }
        public async Task<int> PwnedCount(string password)
        {
            int pwnedcount = 0;
            string hashString = "";
            hashString = HashMyPassword(password).ToString();

            string hashFirstFive = hashString.Substring(0, 5);
            string hashLeftover = hashString.Substring(5, hashString.Length - 5);

            var response = await RequestAsync($"range/{hashFirstFive}", PwnedPasswordURL).ConfigureAwait(false);

            if (response.StatusCode == "OK") {
                var passlist = response.Body.Split('\n');
                for (var i = 0; i < passlist.Length; i++) {
                    if (passlist[i].Split(':')[0].Contains(hashLeftover)) {
                        pwnedcount = Convert.ToInt32(passlist[i].Split(':')[1].Replace("\r", ""));
                    }
                }
            }
            return pwnedcount;

        }
        private async Task<Response> RequestAsync(string parameters, string overrideURL)
        {
            Response RestResponse = new Response();
            Uri uri = new Uri($"{overrideURL}/{parameters}");

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
            HttpResponseMessage response = null;            

            try {
                response = await client.SendAsync(request).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                string statusCode = response.StatusCode.ToString();

                RestResponse.Body = responseBody;
                RestResponse.StatusCode = statusCode;

                return RestResponse;
            } catch (HttpRequestException e) {
                RestResponse.Body = null;
                if (response != null) RestResponse.StatusCode = response.StatusCode.ToString();
                RestResponse.HttpException = e.Message;
                return RestResponse;
            }
        }
    }
}
