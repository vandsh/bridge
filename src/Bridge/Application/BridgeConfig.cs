using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace Bridge.Application
{
    public static class BridgeConfiguration
    {
        public static BridgeConfigSection GetConfig()
        {
            var config = ConfigurationManager.GetSection("BridgeConfigSection") as BridgeConfigSection;
            return config;
        }
    }

    public class BridgeConfigSection : ConfigurationSection
    {
        /// <summary>
        /// The name of this section in the app.config.
        /// </summary>
        public const string SectionName = "BridgeConfigSection";
        private const string CoreConfigsCollectionName = "CoreConfigs";
        private const string ContentConfigsCollectionName = "ContentConfigs";

        [ConfigurationProperty(CoreConfigsCollectionName)]
        [ConfigurationCollection(typeof(BridgeCoreConfigs), AddItemName = "add")]
        public BridgeCoreConfigs CoreConfigs { get { return (BridgeCoreConfigs)base[CoreConfigsCollectionName]; } }

        [ConfigurationProperty(ContentConfigsCollectionName)]
        [ConfigurationCollection(typeof(BridgeContentConfigs), AddItemName = "add")]
        public BridgeContentConfigs ContentConfigs { get { return (BridgeContentConfigs)base[ContentConfigsCollectionName]; } }

    }

    public class BridgeCoreConfigs : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new BridgeCoreConfig();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((BridgeCoreConfig)element).Name;
        }
    }

    public class BridgeContentConfigs : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new BridgeContentConfig();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((BridgeContentConfig)element).Name;
        }
    }

    public class BridgeCoreConfig : ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired = true)]
        public string Name
        {
            get
            {
                return this["name"] as string;
            }
        }
        [ConfigurationProperty("classtypes", IsRequired = true)]
        public string ClassTypes
        {
            get
            {
                return this["classtypes"] as string;
            }
        }

        public IEnumerable<string> GetClassTypes()
        {
            return ClassTypes.Split(',').ToList();
        }

        [ConfigurationProperty("ignorefields", IsRequired = false)]
        public string IgnoreFields
        {
            get
            {
                return this["ignorefields"] as string;
            }
        }

        public IEnumerable<string> GetIgnoreFields()
        {
            return IgnoreFields.Split(',').ToList();
        }
    }

    public class BridgeContentConfig : ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired = true)]
        public string Name
        {
            get
            {
                return this["name"] as string;
            }
        }
        [ConfigurationProperty("query", IsRequired = true)]
        public string Query
        {
            get
            {
                return this["query"] as string;
            }
        }
        [ConfigurationProperty("pagetypes", IsRequired = true)]
        public string PageTypes
        {
            get
            {
                return this["pagetypes"] as string;
            }
        }

        public IEnumerable<string> GetPageTypes()
        {
            return PageTypes.Split(',').ToList();
        }

        [ConfigurationProperty("ignorefields", IsRequired = false)]
        public string IgnoreFields
        {
            get
            {
                return this["ignorefields"] as string;
            }
        }

        public IEnumerable<string> GetIgnoreFields()
        {
            return IgnoreFields.Split(',').ToList();
        }
    }
}