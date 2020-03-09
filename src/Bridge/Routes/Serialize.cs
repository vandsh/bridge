using Bridge.Application;
using Bridge.Models;
using CMS.DataEngine;
using CMS.DocumentEngine;
using CMS.SiteProvider;
using Mapster;
using Nancy;
using Nancy.Security;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using YamlDotNet.Serialization;

namespace Bridge.Routes
{
    public class Serialize : BridgeModule
    {
        /// <summary>
        /// Take whats in the database and serialize it
        /// </summary>
        public Serialize()
        {
            this.RequiresAuthentication();
            Get("/serializecore/{id}", parameters =>
            {
                var response = new Response();
                response.ContentType = "text/plain";
                response.Contents = stream =>
                {
                    var bridgeCoreConfigs = BridgeConfiguration.GetConfig().CoreConfigs;
                    string configName = parameters.id;
                    foreach (BridgeCoreConfig coreConfig in bridgeCoreConfigs)
                    {
                        if(configName.ToLower() == coreConfig.Name.ToLower())
                        {
                            _processCoreConfig(coreConfig, stream);
                        }
                        byte[] bytes = Encoding.UTF8.GetBytes($"Completed serialize!");
                        stream.Write(bytes, 0, bytes.Length);
                        stream.WriteByte(10);
                        stream.Flush();
                    }
                };

                return response;
            });

            Get("/serializecore", parameters =>
            {
                var response = new Response();
                response.ContentType = "text/plain";
                response.Contents = stream =>
                {
                    var bridgeCoreConfigs = BridgeConfiguration.GetConfig().CoreConfigs;

                    foreach (BridgeCoreConfig coreConfig in bridgeCoreConfigs)
                    {
                        _processCoreConfig(coreConfig, stream);
                    }
                    byte[] bytes = Encoding.UTF8.GetBytes($"Completed serialize!");
                    stream.Write(bytes, 0, bytes.Length);
                    stream.WriteByte(10);
                    stream.Flush();
                };

                return response;
            });

            Get("/serializecontent/{id}", parameters =>
            {
                var response = new Response();
                response.ContentType = "text/plain";
                response.Contents = stream =>
                {
                    var bridgeContentConfigs = BridgeConfiguration.GetConfig().ContentConfigs;
                    string configName = parameters.id;
                    foreach (BridgeContentConfig contentConfig in bridgeContentConfigs)
                    {
                        if (configName.ToLower() == contentConfig.Name.ToLower())
                        {
                            _processContentConfig(contentConfig, stream);
                        }
                        byte[] bytes = Encoding.UTF8.GetBytes($"Completed serialize!");
                        stream.Write(bytes, 0, bytes.Length);
                        stream.WriteByte(10);
                        stream.Flush();
                    }
                };

                return response;
            });

            Get("/serializecontent", parameters =>
            {
                var response = new Response();
                response.ContentType = "text/plain";
                response.Contents = stream =>
                {
                    var bridgeContentConfigs = BridgeConfiguration.GetConfig().ContentConfigs;
                    foreach (BridgeContentConfig contentConfig in bridgeContentConfigs)
                    {
                        _processContentConfig(contentConfig, stream);
                    }
                    byte[] bytes = Encoding.UTF8.GetBytes($"Completed serialize!");
                    stream.Write(bytes, 0, bytes.Length);
                    stream.WriteByte(10);
                    stream.Flush();
                };

                return response;
            });
        }

        private void _processCoreConfig(BridgeCoreConfig coreConfig, Stream stream)
        {
            var serializer = new SerializerBuilder().ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull).Build();
            var watch = new Stopwatch();
            string serializationFolder = BridgeConfiguration.GetConfig().SerializationFolder;
            //have this driven by config
            var serializationPath = $"{serializationFolder}/core/{coreConfig.Name}";
            var classTypes = coreConfig.GetClassTypes();
            var fieldsToIgnore = coreConfig.GetIgnoreFields();

            ProviderHelper.ClearHashtables("cms.class", false);
            foreach (var classType in classTypes)
            {
                watch.Reset();
                watch.Start();
                var dci = DataClassInfoProvider.GetDataClassInfo(classType);
                if (dci != null)
                {
                    var mappedItem = dci.Adapt<BridgeClassInfo>();
                    mappedItem.FieldValues = new Dictionary<string, object>();

                    foreach (string columnName in dci.ColumnNames)
                    {
                        if (!fieldsToIgnore.Contains(columnName))
                        {
                            var columnValue = dci.GetValue(columnName);
                            mappedItem.FieldValues.Add(columnName, columnValue);
                        }
                    }

                    var assignedSites = new List<Guid>();
                    foreach (SiteInfo assignedSite in dci.AssignedSites)
                    {
                        assignedSites.Add(assignedSite.SiteGUID);
                    }
                    mappedItem.AssignedSites = assignedSites;


                    var allowedChildClasses = AllowedChildClassInfoProvider.GetAllowedChildClasses().Where("ParentClassID", QueryOperator.Equals, dci["ClassID"].ToString()).Column("ChildClassID").ToList();
                    var allowedChildrenTypes = new List<string>();
                    foreach (AllowedChildClassInfo allowedChildClass in allowedChildClasses)
                    {
                        var className = new ObjectQuery("cms.class").Where("ClassID", QueryOperator.Equals, allowedChildClass.ChildClassID).Column("ClassName").FirstOrDefault()["ClassName"].ToString();
                        allowedChildrenTypes.Add(className);
                    }
                    mappedItem.AllowedChildTypes = allowedChildrenTypes;

                    var classQueries = QueryInfoProvider.GetQueries().Where("ClassID", QueryOperator.Equals, dci.ClassID).ToList();
                    var queries = new Dictionary<string, BridgeClassQuery>();
                    foreach (var classQuery in classQueries)
                    {
                        var bcq = classQuery.Adapt<BridgeClassQuery>();
                        queries.Add(classQuery.QueryName, bcq);
                    }
                    mappedItem.Queries = queries;

                    var stringBuilder = new StringBuilder();
                    var res = serializer.Serialize(mappedItem);
                    stringBuilder.AppendLine(res);
                    var pathToWriteTo = $"{serializationPath}/{mappedItem.ClassName.ToLower()}.yaml";
                    var concretePath = HttpContext.Current.Server.MapPath(pathToWriteTo);
                    FileInfo file = new FileInfo(concretePath);
                    file.Directory.Create(); // If the directory already exists, this method does nothing.
                    File.WriteAllText(concretePath, res);
                    watch.Stop();
                    byte[] bytes = Encoding.UTF8.GetBytes($"Serialized {coreConfig.Name}: {mappedItem.ClassName.ToLower()}.yaml - {watch.ElapsedMilliseconds}ms");
                    stream.Write(bytes, 0, bytes.Length);
                    stream.WriteByte(10);
                    stream.Flush();
                }
            }
        }

        private void _processContentConfig(BridgeContentConfig contentConfig, Stream stream)
        {
            var serializer = new SerializerBuilder().ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull).Build();
            var watch = new Stopwatch();
            string serializationFolder = BridgeConfiguration.GetConfig().SerializationFolder;

            //have this driven by config
            var serializationPath = $"{serializationFolder}/content/{contentConfig.Name}";
            var pageTypes = contentConfig.GetPageTypes();
            var fieldsToIgnore = contentConfig.GetIgnoreFields(); 
            var path = contentConfig.Query;

            MultiDocumentQuery docs = DocumentHelper.GetDocuments().Path(path).Types(pageTypes.ToArray()).AllCultures().FilterDuplicates();
            var treeNodes = docs.ToList<TreeNode>();
            foreach (var treeNode in treeNodes)
            {
                watch.Reset();
                watch.Start();
                var mappedItem = treeNode.Adapt<BridgeTreeNode>();
                mappedItem.FieldValues = new Dictionary<string, object>();

                foreach (string columnName in treeNode.ColumnNames)
                {
                    if (!fieldsToIgnore.Contains(columnName))
                    {
                        var columnValue = treeNode.GetValue(columnName);
                        if (columnValue != null)
                        {
                            mappedItem.FieldValues.Add(columnName, columnValue);
                        }
                    }
                }
                mappedItem.ParentNodeGUID = treeNode?.Parent?.NodeGUID;
                var stringBuilder = new StringBuilder();
                var res = serializer.Serialize(mappedItem);
                stringBuilder.AppendLine(res);
                var pathToWriteTo = $"{serializationPath}/{mappedItem.NodeAliasPath}#{mappedItem.DocumentCulture}.yaml";
                var concretePath = HttpContext.Current.Server.MapPath(pathToWriteTo);
                FileInfo file = new FileInfo(concretePath);
                file.Directory.Create(); // If the directory already exists, this method does nothing.
                File.WriteAllText(concretePath, res);
                watch.Stop();
                byte[] bytes = Encoding.UTF8.GetBytes($"Serialized {contentConfig.Name}: {mappedItem.NodeAliasPath}#{mappedItem.DocumentCulture}.yaml ({mappedItem.NodeGUID}) - {watch.ElapsedMilliseconds}ms");
                stream.Write(bytes, 0, bytes.Length);
                stream.WriteByte(10);
                stream.Flush();
            }
        }
    }
}