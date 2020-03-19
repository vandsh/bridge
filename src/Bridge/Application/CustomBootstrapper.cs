using Bridge.Models;
using CMS.DataEngine;
using Mapster;
using Nancy;
using Nancy.Authentication.Basic;
using Nancy.Bootstrapper;
using Nancy.Configuration;
using Nancy.Conventions;
using Nancy.TinyIoc;
using System.Configuration;

namespace Bridge.Application
{
    public class CustomBootstrapper : DefaultNancyBootstrapper
    {
        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            base.ApplicationStartup(container, pipelines);
            CMSApplication.Init();
            TypeAdapterConfig<DataClassInfo, BridgeClassInfo>.NewConfig()
                .Ignore(dest => dest.AssignedSites)
                .Ignore(dest => dest.AllowedChildTypes)
                ;
            
            var userValidator = container.Resolve<IUserValidator>();
            pipelines.EnableBasicAuthentication(new BasicAuthenticationConfiguration(userValidator, "BridgeRealm"));
        }
        protected override void ConfigureConventions(NancyConventions nancyConventions)
        {
            var rootPath = (ConfigurationManager.AppSettings["BridgeBaseUrl"] ?? "/Admin/BridgeUI").ToString();
            nancyConventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddDirectory( $"{rootPath}/Static", @"Static"));
        }

        protected override IRootPathProvider RootPathProvider
        {
            get { return new CustomRootPathProvider(); }
        }
    }
}