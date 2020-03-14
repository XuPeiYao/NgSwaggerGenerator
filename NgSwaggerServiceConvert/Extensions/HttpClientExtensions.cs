using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace NgSwaggerServiceConvert.Extensions
{
    public static class HttpClientExtensions
    {
        public static async Task<JToken> GetJTokenAsync(this HttpClient http, string url)
        {
            return JToken.Parse(await http.GetStringAsync(url));
        }
    }
}
