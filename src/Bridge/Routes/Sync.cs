using Bridge.Models;
using CMS.DataEngine;
using CMS.DocumentEngine;
using CMS.Membership;
using Nancy;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Web;
using YamlDotNet.Serialization;
using CMS.SiteProvider;
using System.Linq;
using System.Collections.Generic;
using Bridge.Application;
using Nancy.Security;

namespace Bridge.Routes
{
    public class Sync : NancyModule
    {
        /// <summary>
        /// Deserialize what is on disk and save back to the Database
        /// </summary>
        public Sync()
        {
            this.RequiresAuthentication();
            Get("/synccore/{id}", parameters =>
            {
                var response = new Response();

                response.ContentType = "text/plain";
                response.Contents = stream =>
                {
                    var bridgeCoreConfigs = BridgeConfiguration.GetConfig().CoreConfigs;
                    string configName = parameters.id;
                    foreach (BridgeCoreConfig coreConfig in bridgeCoreConfigs)
                    {
                        if (configName.ToLower() == coreConfig.Name.ToLower())
                        {
                            _processCoreConfig(coreConfig, stream);
                        }
                        byte[] bytes = Encoding.UTF8.GetBytes($"Completed sync!");
                        stream.Write(bytes, 0, bytes.Length);
                        stream.WriteByte(10);
                        stream.Flush();
                    }
                };
                return response;
            });

            Get("/synccore", parameters =>
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
                    byte[] bytes = Encoding.UTF8.GetBytes($"Completed sync!");
                    stream.Write(bytes, 0, bytes.Length);
                    stream.WriteByte(10);
                    stream.Flush();

                };
                return response;
            });

            Get("/synccontent/{id}", parameters =>
            {
                var response = new Response();

                response.ContentType = "text/plain";
                response.Contents = stream =>
                {
                    var bridgecontentConfigs = BridgeConfiguration.GetConfig().ContentConfigs;
                    string configName = parameters.id;
                    foreach (BridgeContentConfig contentConfig in bridgecontentConfigs)
                    {
                        if (configName.ToLower() == contentConfig.Name.ToLower())
                        {
                            _processContentConfing(contentConfig, stream);
                        }
                        byte[] bytes = Encoding.UTF8.GetBytes($"Completed sync!");
                        stream.Write(bytes, 0, bytes.Length);
                        stream.WriteByte(10);
                        stream.Flush();
                    }
                };
                return response;
            });

            Get("/synccontent", parameters =>
            {
                var response = new Response();

                response.ContentType = "text/plain";
                response.Contents = stream =>
                {
                    var bridgecontentConfigs = BridgeConfiguration.GetConfig().ContentConfigs;
                    foreach (BridgeContentConfig contentConfig in bridgecontentConfigs)
                    {
                        _processContentConfing(contentConfig, stream);
                    }
                    byte[] bytes = Encoding.UTF8.GetBytes($"Completed sync!");
                    stream.Write(bytes, 0, bytes.Length);
                    stream.WriteByte(10);
                    stream.Flush();
                };
                return response;
            });
        }

        private void _processCoreConfig(BridgeCoreConfig coreConfig, Stream stream)
        {
            var deserializer = new DeserializerBuilder().Build();
            TreeProvider tree = new TreeProvider(MembershipContext.AuthenticatedUser);
            var watch = new Stopwatch();

            //have this driven by config
            var serializationPath = $"/core/{coreConfig.Name}";
            var classTypes = coreConfig.GetClassTypes();
            var ignoreFields = coreConfig.GetIgnoreFields();
            var allAllowedChildInfos = new List<AllowedChildClassInfo>();


            int retryCount = 3;
            _processClasses(retryCount, allAllowedChildInfos, coreConfig.Name, stream, deserializer, watch, serializationPath, classTypes, ignoreFields);
            //at this point we should have all children, lets post process these
            foreach (var allowedChildClass in allAllowedChildInfos)
            {
                AllowedChildClassInfoProvider.SetAllowedChildClassInfo(allowedChildClass);
            }
        }

        private void _processClasses(int retryCount, List<AllowedChildClassInfo> allAllowedChildInfos, string coreConfigName, Stream stream, IDeserializer deserializer, Stopwatch watch, string serializationPath, IEnumerable<string> classTypes, IEnumerable<string> ignoreFields)
        {
            var retryClasses = new List<string>();
            var concretePath = HttpContext.Current.Server.MapPath(serializationPath);
            var yamlFiles = Directory.EnumerateFiles(concretePath, "*.yaml", SearchOption.AllDirectories);
            foreach (string yamlFile in yamlFiles)
            {
                watch.Reset();
                watch.Start();
                var yamlFileContent = File.ReadAllText(yamlFile);
                var bridgeClassInfo = deserializer.Deserialize<BridgeClassInfo>(yamlFileContent);
                if (classTypes.Any(x => x.ToLower() == bridgeClassInfo.ClassName.ToLower()))
                {
                    var newDCI = DataClassInfoProvider.GetDataClassInfo(bridgeClassInfo.ClassName);
                    if (newDCI == null)
                    {
                        newDCI = new DataClassInfo();
                    }
                    foreach (var field in bridgeClassInfo.FieldValues)
                    {
                        if (!ignoreFields.Contains(field.Key))
                        {
                            newDCI[field.Key] = field.Value;
                        }
                    }

                    DataClassInfoProvider.SetDataClassInfo(newDCI);
                    foreach (var allowedType in bridgeClassInfo.AllowedChildTypes)
                    {
                        var matchingClass = new ObjectQuery("cms.class", false).Where("ClassName", QueryOperator.Equals, allowedType)?.Column("ClassID")?.FirstOrDefault();
                        if (matchingClass != null)
                        {
                            matchingClass["ClassID"]?.ToString();

                            var acci = new AllowedChildClassInfo() { ChildClassID = int.Parse(matchingClass["ClassID"]?.ToString()), ParentClassID = newDCI.ClassID };
                            allAllowedChildInfos.Add(acci);
                        }
                        else
                        {
                            retryClasses.Add(allowedType);
                        }
                    }

                    foreach (var siteGUID in bridgeClassInfo.AssignedSites)
                    {
                        var site = SiteInfoProvider.GetSiteInfoByGUID(siteGUID);
                        ClassSiteInfoProvider.AddClassToSite(newDCI.ClassID, site.SiteID);
                    }

                    var queries = new Dictionary<string, BridgeClassQuery>();
                    foreach (var bcq in bridgeClassInfo.Queries)
                    {
                        var qi = QueryInfoProvider.GetQueries().Where("QueryGUID", QueryOperator.Equals, bcq.Value.QueryGUID).FirstOrDefault();
                        if (qi == null)
                        {
                            qi = new QueryInfo();
                        }
                        qi.ClassID = newDCI.ClassID;
                        qi.QueryName = bcq.Key;
                        qi.QueryTypeID = bcq.Value.QueryTypeID;
                        qi.QueryText = bcq.Value.QueryText;
                        qi.QueryIsLocked = bcq.Value.QueryIsLocked;
                        qi.QueryIsCustom = bcq.Value.QueryIsCustom;
                        qi.QueryRequiresTransaction = bcq.Value.QueryRequiresTransaction;
                        qi.QueryConnectionString = bcq.Value.QueryConnectionString;
                        qi.QueryGUID = bcq.Value.QueryGUID;
                        QueryInfoProvider.SetQueryInfo(qi);
                    }

                    byte[] bytes = Encoding.UTF8.GetBytes($"Synced {coreConfigName}: {yamlFile.Replace(concretePath, "")} - {watch.ElapsedMilliseconds}ms");
                    stream.Write(bytes, 0, bytes.Length);
                    stream.WriteByte(10);
                    stream.Flush();
                }
                watch.Stop();
            }
            retryCount--;
            if(retryCount > 0 && retryClasses.Count > 0)
            {
                _processClasses(retryCount, allAllowedChildInfos, coreConfigName, stream, deserializer, watch, serializationPath, retryClasses, ignoreFields);
            }
        }

        private void _processContentConfing(BridgeContentConfig contentConfig, Stream stream)
        {
            var deserializer = new DeserializerBuilder().Build();
            TreeProvider tree = new TreeProvider(MembershipContext.AuthenticatedUser);
            var watch = new Stopwatch();

            var serializationPath = $"/content/{contentConfig.Name}";
            var pageTypes = contentConfig.GetPageTypes();
            var ignoreFields = contentConfig.GetIgnoreFields();

            var concretePath = HttpContext.Current.Server.MapPath(serializationPath);
            var yamlFiles = Directory.EnumerateFiles(concretePath, "*.yaml", SearchOption.AllDirectories);
            foreach (string yamlFile in yamlFiles)
            {
                watch.Reset();
                watch.Start();
                var yamlFileContent = File.ReadAllText(yamlFile);
                var bridgeTreeNode = deserializer.Deserialize<BridgeTreeNode>(yamlFileContent);
                if (pageTypes.Any(x => x.ToLower() == bridgeTreeNode.ClassName.ToLower()))
                {
                    var treeNode = tree.SelectSingleNode(bridgeTreeNode.NodeGUID, bridgeTreeNode.DocumentCulture, bridgeTreeNode.NodeSiteName);
                    if (treeNode != null)
                    {
                        if (bridgeTreeNode.FieldValues != null)
                        {
                            foreach (var fieldValue in bridgeTreeNode.FieldValues)
                            {
                                if (!ignoreFields.Contains(fieldValue.Key))
                                {
                                    treeNode.SetValue(fieldValue.Key, fieldValue.Value);
                                }
                            }
                            treeNode.Update();
                        }
                    }
                    else
                    {
                        var nonSpecificCultureTreeNode = tree.SelectSingleNode(bridgeTreeNode.NodeGUID, null, bridgeTreeNode.NodeSiteName, true);
                        if (nonSpecificCultureTreeNode != null)
                        {
                            // if we are in here, we have a version of the same page already, make it a culture variant
                            if (bridgeTreeNode.FieldValues != null)
                            {
                                foreach (var fieldValue in bridgeTreeNode.FieldValues)
                                {
                                    nonSpecificCultureTreeNode.SetValue(fieldValue.Key, fieldValue.Value);
                                }
                            }
                            nonSpecificCultureTreeNode.InsertAsNewCultureVersion(bridgeTreeNode.DocumentCulture);
                        }
                        else
                        {
                            //else, we can assume this is a new page and insert
                            var parentTreeNode = tree.SelectSingleNode(bridgeTreeNode.ParentNodeGUID.GetValueOrDefault(), bridgeTreeNode.DocumentCulture, bridgeTreeNode.NodeSiteName);
                            var newTreeNode = TreeNode.New(bridgeTreeNode.ClassName, tree);
                            if (bridgeTreeNode.FieldValues != null)
                            {
                                foreach (var fieldValue in bridgeTreeNode.FieldValues)
                                {
                                    newTreeNode.SetValue(fieldValue.Key, fieldValue.Value);
                                }
                            }
                            newTreeNode.Insert(parentTreeNode);
                        }
                    }
                    byte[] bytes = Encoding.UTF8.GetBytes($"Synced {contentConfig.Name}: {yamlFile.Replace(concretePath, "")} - {watch.ElapsedMilliseconds}ms");
                    stream.Write(bytes, 0, bytes.Length);
                    stream.WriteByte(10);
                    stream.Flush();
                }
                watch.Stop();
            }
        }
    }
}