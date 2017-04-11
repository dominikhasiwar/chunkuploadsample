using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.Hosting;
using System.Web.Http.WebHost;

namespace UploadApi
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
            config.EnableCors(new EnableCorsAttribute("*", "*", "*"));
            config.MessageHandlers.Add(new MyHandler());
            config.Services.Replace(typeof(IHostBufferPolicySelector), new NoBufferPolicySelector());
        }
    }

    public class MyHandler : DelegatingHandler
    {
        public MyHandler()
        {

        }

        protected MyHandler(HttpMessageHandler innerHandler) : base(innerHandler)
        {
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return base.SendAsync(request, cancellationToken);
        }
    }

    public class NoBufferPolicySelector : WebHostBufferPolicySelector
    {
        public override bool UseBufferedInputStream(object hostContext)
        {
            //var context = hostContext as HttpContextBase;

            //if (context != null)
            //{
            //    if (string.Equals(context.Request.RequestContext.RouteData.Values["controller"].ToString(), "uploading", StringComparison.InvariantCultureIgnoreCase))
            //        return false;
            //}

            //return true;
            return false;
        }

        public override bool UseBufferedOutputStream(HttpResponseMessage response)
        {
            return base.UseBufferedOutputStream(response);
        }
    }
}
