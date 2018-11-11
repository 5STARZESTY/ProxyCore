using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace ProxyCore.Middleware
{
    public class ProxyHandler
    {
        private readonly RequestDelegate _next;
        private readonly HttpClient _httpClient;
        private readonly IOptions<ProxyConfiguration> _proxyConfiguration;

        public ProxyHandler(RequestDelegate next,
            HttpClient httpClient,
            IOptions<ProxyConfiguration> proxyConfiguration)
        {
            _next = next;
            _httpClient = httpClient;
            _proxyConfiguration = proxyConfiguration;
        }
        public async Task Invoke(HttpContext context)
        {
            var client = _httpClient;

            string redirectLocation = _proxyConfiguration.Value.RedirectLocation;
            string localPath = context.Request.Path.ToString() ?? "";
            string subApp = _proxyConfiguration.Value.SubApp ?? "";

            string url = redirectLocation
                + ((subApp != "" ? localPath.ToLower().Replace(subApp.ToLower(), "") : localPath)
                + context.Request.QueryString.ToString()).Replace("//","/");

            var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), url);

            if (context.Request.ContentLength > 0)
                request.Content = new StreamContent(context.Request.Body);

            if (request.Content != null)
                foreach (var h in context.Request.Headers)
                {
                    request.Content.Headers.TryAddWithoutValidation(h.Key, h.Value.ToArray());
                }

            var response = await client.SendAsync(request);

            var content = await response.Content.ReadAsStringAsync();

            await context.Response.WriteAsync(content);
            return;
        }
    }
}
