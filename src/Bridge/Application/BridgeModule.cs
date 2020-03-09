using Nancy;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace Bridge.Application
{
    public class BridgeModule : NancyModule
    {
        public BridgeModule() : base((ConfigurationManager.AppSettings["BridgeBaseUrl"] ?? "/bridge").ToString() ){
            //TODO: make this route be dynamic enough to handle multi-site 
        }
    }
}