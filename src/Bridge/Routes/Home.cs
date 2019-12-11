using Bridge.Application;
using Nancy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Bridge.Routes
{
    public class Home : NancyModule
    {
        public Home()
        {
            Get("/", parameters => {
                var bridgeCoreConfigs = BridgeConfiguration.GetConfig().CoreConfigs;
                var bridgeContentConfigs = BridgeConfiguration.GetConfig().ContentConfigs;
                var model = new { coreConfigs = bridgeCoreConfigs, contentConfigs = bridgeContentConfigs };
                return View["Views/Index",model];
            });
            Get("/about", parameters => "Hello BridgeCI");
        }
    }
}