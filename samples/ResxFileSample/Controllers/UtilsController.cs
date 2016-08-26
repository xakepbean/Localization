using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using Microsoft.Extensions.Localization;

namespace ResxFileSample.Controllers{
    public class UtilsController:Controller{
        private IStringLocalizer<UtilsController> _localizer=null;
        public UtilsController(IStringLocalizer<UtilsController> localizer){
            string aaa = CultureInfo.CurrentCulture.Name;
            _localizer=localizer;
        }
        
        public IActionResult ShowLocalPage() => View();

        public void Server()
        {
            HttpContext.Response.StatusCode = 200;
            HttpContext.Response.ContentType = "text/html; charset=utf-8";
            HttpContext.Response.WriteAsync($"<h2>Features</h2><br/>");
            foreach (var item in HttpContext.Features)
            {
                HttpContext.Response.WriteAsync($"{item.Key.FullName}<br/>");
            }
            HttpContext.Response.WriteAsync($"<h2>Items</h2><br/>");
            foreach (var item in HttpContext.Items)
            {
                HttpContext.Response.WriteAsync($"{item.Value.ToString()}<br/>");
            }
           
        }

        public void ShowLocal(){
            
            

                HttpContext.Response.StatusCode = 200;
                HttpContext.Response.ContentType = "text/html; charset=utf-8";
           
                var requestCultureFeature = HttpContext.Features.Get<IRequestCultureFeature>();
                var requestCulture = requestCultureFeature.RequestCulture;

                HttpContext.Response.WriteAsync(
$@"<!doctype html>
<html>
<head>
    <title>{_localizer["Request Localization"]}</title>
    <style>
        body {{ font-family: 'Segoe UI', Helvetica, Sans-Serif }}
        h1, h2, h3, h4, th {{ font-family: 'Segoe UI Light', Helvetica, Sans-Serif }}
        th {{ text-align: left }}
    </style>
    <script>
        function useCookie() {{
            var culture = document.getElementById('culture');
            var uiCulture = document.getElementById('uiCulture');
            var cookieValue = '{CookieRequestCultureProvider.DefaultCookieName}=c='+culture.options[culture.selectedIndex].value+'|uic='+uiCulture.options[uiCulture.selectedIndex].value;
            document.cookie = cookieValue;
            window.location = window.location.href.split('?')[0];
        }}

        function clearCookie() {{
            document.cookie='{CookieRequestCultureProvider.DefaultCookieName}=""""';
        }}
    </script>
</head>
<body>");
                 HttpContext.Response.WriteAsync($"<h1>{_localizer["Request Localization Sample"]}</h1>");
                 HttpContext.Response.WriteAsync($"<h1>{_localizer["Hello"]}</h1>");
                 HttpContext.Response.WriteAsync("<form id=\"theForm\" method=\"get\">");
                 HttpContext.Response.WriteAsync($"<label for=\"culture\">{_localizer["Culture"]}: </label>");
                 HttpContext.Response.WriteAsync("<select id=\"culture\" name=\"culture\">");
                 WriteCultureSelectOptions(HttpContext,requestCultureFeature);
                 HttpContext.Response.WriteAsync("</select><br />");
                 HttpContext.Response.WriteAsync($"<label for=\"uiCulture\">{_localizer["UI Culture"]}: </label>");
                 HttpContext.Response.WriteAsync("<select id=\"uiCulture\" name=\"ui-culture\">");
                 WriteCultureSelectOptions(HttpContext,requestCultureFeature);
                 HttpContext.Response.WriteAsync("</select><br />");
                 HttpContext.Response.WriteAsync("<input type=\"submit\" value=\"go QS\" /> ");
                 HttpContext.Response.WriteAsync($"<input type=\"button\" value=\"go cookie\" onclick='useCookie();' /> ");
                 HttpContext.Response.WriteAsync($"<a href=\"/\" onclick='clearCookie();'>{_localizer["reset"]}</a>");
                 HttpContext.Response.WriteAsync("</form>");
                 HttpContext.Response.WriteAsync("<br />");
                 HttpContext.Response.WriteAsync("<table><tbody>");
                 HttpContext.Response.WriteAsync($"<tr><th>Winning provider:</th><td>{requestCultureFeature.Provider.GetType().Name}</td></tr>");
                 HttpContext.Response.WriteAsync($"<tr><th>{_localizer["Current request culture:"]}</th><td>{requestCulture.Culture.DisplayName} ({requestCulture.Culture})</td></tr>");
                 HttpContext.Response.WriteAsync($"<tr><th>{_localizer["Current request UI culture:"]}</th><td>{requestCulture.UICulture.DisplayName} ({requestCulture.UICulture})</td></tr>");
                 HttpContext.Response.WriteAsync($"<tr><th>{_localizer["Current thread culture:"]}</th><td>{CultureInfo.CurrentCulture.DisplayName} ({CultureInfo.CurrentCulture})</td></tr>");
                 HttpContext.Response.WriteAsync($"<tr><th>{_localizer["Current thread UI culture:"]}</th><td>{CultureInfo.CurrentUICulture.DisplayName} ({CultureInfo.CurrentUICulture})</td></tr>");
                 HttpContext.Response.WriteAsync($"<tr><th>{_localizer["Current date (invariant full):"]}</th><td>{DateTime.Now.ToString("F", CultureInfo.InvariantCulture)}</td></tr>");
                 HttpContext.Response.WriteAsync($"<tr><th>{_localizer["Current date (invariant):"]}</th><td>{DateTime.Now.ToString(CultureInfo.InvariantCulture)}</td></tr>");
                 HttpContext.Response.WriteAsync($"<tr><th>{_localizer["Current date (request full):"]}</th><td>{DateTime.Now.ToString("F")}</td></tr>");
                 HttpContext.Response.WriteAsync($"<tr><th>{_localizer["Current date (request):"]}</th><td>{DateTime.Now.ToString()}</td></tr>");
                 HttpContext.Response.WriteAsync($"<tr><th>{_localizer["Current time (invariant):"]}</th><td>{DateTime.Now.ToString("T", CultureInfo.InvariantCulture)}</td></tr>");
                 HttpContext.Response.WriteAsync($"<tr><th>{_localizer["Current time (request):"]}</th><td>{DateTime.Now.ToString("T")}</td></tr>");
                 HttpContext.Response.WriteAsync($"<tr><th>{_localizer["Big number (invariant):"]}</th><td>{(Math.Pow(2, 42) + 0.42).ToString("N", CultureInfo.InvariantCulture)}</td></tr>");
                 HttpContext.Response.WriteAsync($"<tr><th>{_localizer["Big number (request):"]}</th><td>{(Math.Pow(2, 42) + 0.42).ToString("N")}</td></tr>");
                 HttpContext.Response.WriteAsync($"<tr><th>{_localizer["Big number negative (invariant):"]}</th><td>{(-Math.Pow(2, 42) + 0.42).ToString("N", CultureInfo.InvariantCulture)}</td></tr>");
                 HttpContext.Response.WriteAsync($"<tr><th>{_localizer["Big number negative (request):"]}</th><td>{(-Math.Pow(2, 42) + 0.42).ToString("N")}</td></tr>");
                 HttpContext.Response.WriteAsync($"<tr><th>{_localizer["Money (invariant):"]}</th><td>{2199.50.ToString("C", CultureInfo.InvariantCulture)}</td></tr>");
                 HttpContext.Response.WriteAsync($"<tr><th>{_localizer["Money (request):"]}</th><td>{2199.50.ToString("C")}</td></tr>");
                 HttpContext.Response.WriteAsync($"<tr><th>{_localizer["Money negative (invariant):"]}</th><td>{(-2199.50).ToString("C", CultureInfo.InvariantCulture)}</td></tr>");
                 HttpContext.Response.WriteAsync($"<tr><th>{_localizer["Money negative (request):"]}</th><td>{(-2199.50).ToString("C")}</td></tr>");
                 HttpContext.Response.WriteAsync("</tbody></table>");
                 HttpContext.Response.WriteAsync(
@"</body>
</html>");
        }

        private void  WriteCultureSelectOptions(HttpContext context,IRequestCultureFeature requestCulture)
        {
             context.Response.WriteAsync($"    <option value=\"\">-- select --</option>");

             RequestCultureProvider AcceptUI=requestCulture.Provider as RequestCultureProvider;
              foreach (var item in AcceptUI.Options.SupportedUICultures)
              {
                  if(item.Name==requestCulture.RequestCulture.UICulture.Name)
                     context.Response.WriteAsync($"    <option value=\"{item.Name}\" selected=\"true\">{item.DisplayName}</option>");
                  else
                    context.Response.WriteAsync($"    <option value=\"{item.Name}\">{item.DisplayName}</option>");
              }
        }
    }
}