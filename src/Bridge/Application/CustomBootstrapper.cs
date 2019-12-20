using Bridge.Models;
using CMS.DataEngine;
using Mapster;
using Nancy;
using Nancy.Authentication.Basic;
using Nancy.Bootstrapper;
using Nancy.Conventions;
using Nancy.TinyIoc;

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

            pipelines.EnableBasicAuthentication(new BasicAuthenticationConfiguration(container.Resolve<IUserValidator>(), "BridgeRealm"));
        }
        protected override void ConfigureConventions(NancyConventions nancyConventions)
        {
            nancyConventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddDirectory("Static", @"Static"));
            base.ConfigureConventions(nancyConventions);
        }
    }
}