using Bridge.Models;
using CMS.DataEngine;
using Mapster;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Conventions;
using Nancy.TinyIoc;

namespace Bridge.Application
{
    public class CustomBootstrapper : DefaultNancyBootstrapper
    {
        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            CMSApplication.Init();
            TypeAdapterConfig<DataClassInfo, BridgeClassInfo>.NewConfig()
                .Ignore(dest => dest.AssignedSites)
                .Ignore(dest => dest.AllowedChildTypes)
                ;
        }
        protected override void ConfigureConventions(NancyConventions nancyConventions)
        {
            nancyConventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddDirectory("Static", @"Static"));
            base.ConfigureConventions(nancyConventions);
        }
    }
}