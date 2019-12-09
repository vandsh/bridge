using CMS.DataEngine;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.TinyIoc;

namespace Bridge.Application
{
    public class CustomBootstrapper : DefaultNancyBootstrapper
    {
        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            CMSApplication.Init();
        }
    }
}