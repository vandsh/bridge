using Nancy;
using System;
using System.Configuration;
using System.IO;

namespace Bridge.Application
{
    public class CustomRootPathProvider : IRootPathProvider
    {
        public string GetRootPath()
        {
            var bridgeFolder = (ConfigurationManager.AppSettings["BridgeFolderLocation"] ?? "Bridge").ToString();
            var appRoot = AppDomain.CurrentDomain.BaseDirectory;
            var rootPath = $"{appRoot}\\{bridgeFolder}";
            return rootPath;
        }
    }
}