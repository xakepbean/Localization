using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Localization
{
    public class URLRequestCultureProvider: RequestCultureProvider
    {
        public override Task<ProviderCultureResult> DetermineProviderCultureResult(HttpContext httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            var request = httpContext.Request;
            if (!request.Path.HasValue || request.Path.Value == "/")
            {
                return Task.FromResult((ProviderCultureResult)null);
            }

            var segments = request.Path.Value.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length > 0)
            {
                var vcc = Options.SupportedCultures.Where(w => w.Name.Equals(segments[0], StringComparison.OrdinalIgnoreCase));
                if (vcc.Count() > 0)
                {
                    var UrlCulture = vcc.First();
                    httpContext.Request.Path = segments.Length == 1 ? "/" : "/" + string.Join("/", segments, 1, segments.Length - 1);
                    return Task.FromResult(new ProviderCultureResult(UrlCulture.Name, UrlCulture.Name));
                }
            }

            return Task.FromResult((ProviderCultureResult)null);
        }
    }
}
