using Nancy;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using Nancy.Extensions;
using Nancy.Security;
using System.IO;
using System.Web.Hosting;

namespace Bridge.Application
{
    public class BridgeModule : NancyModule
    {
        public BridgeModule() : base(){
            //TODO: make this route be dynamic enough to handle multi-site 
            base.ModulePath = (ConfigurationManager.AppSettings["BridgeBaseUrl"] ?? "/Admin/BridgeUI").ToString();
        }

        public string GetRootPath(string morePath)
        {
            var bridgeFolder = (ConfigurationManager.AppSettings["BridgeFolderLocation"] ?? "Bridge").ToString();
            var appRoot = AppDomain.CurrentDomain.BaseDirectory;
            var rootPath = $"{appRoot}\\{bridgeFolder}\\{morePath}";
            return rootPath;
        }
    }

    public static class BridgeModuleExtensions
    {
        public static void RequiresAuthentication(this INancyModule module, HttpContext httpContext)
        {
            module.AddBeforeHookOrExecute(_requiresAuthentication(httpContext), "Requires Authentication");
        }

        private static Func<NancyContext, Response> _requiresAuthentication(HttpContext httpContext)
        {
            return (ctx) =>
            {
                Response response = null;
                var isNancyAuthed = ctx.CurrentUser.IsAuthenticated();
                var isHttpAuthend = httpContext.User.Identity.IsAuthenticated;
                if (!isNancyAuthed && !isHttpAuthend)
                {
                    response = new Response { StatusCode = HttpStatusCode.Unauthorized };
                }

                return response;
            };
        }
    }
}