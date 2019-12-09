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
            Get("/about", parameters => "Hello BridgeCI");
        }
    }
}