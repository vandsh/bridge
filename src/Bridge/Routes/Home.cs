using Bridge.Application;
using Nancy;
using Nancy.Security;
using System.Configuration;
using System.Web;

namespace Bridge.Routes
{
    public class Home : BridgeModule
    {
        public Home()
        {
            this.RequiresAuthentication(HttpContext.Current);
            Get("/index", parameters => {
                var bridgeCoreConfigs = BridgeConfiguration.GetConfig().CoreConfigs;
                var bridgeContentConfigs = BridgeConfiguration.GetConfig().ContentConfigs;
                var currentRequest = Request.Url;
                currentRequest.Path = (ConfigurationManager.AppSettings["BridgeBaseUrl"] ?? "/Admin/BridgeUI").ToString();
                var model = new { coreConfigs = bridgeCoreConfigs, contentConfigs = bridgeContentConfigs, urlBase = currentRequest.ToString() };
                return View["Index", model];
            });
            Get("/about", parameters => "Hello from Bridge -|--|-");
            //Get("/{all*}", parameters =>
            //{
            //    //If you are having serious problems hitting anything, uncomment this
            //    var test = Request;
            //    return "catchall";
            //});
        }
    }
}