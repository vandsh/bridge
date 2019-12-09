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

namespace Bridge.Routes
{
    public class Sync : NancyModule
    {
        /// <summary>
        /// Deserialize what is on disk and save back to the Database
        /// </summary>
        public Sync()
        {
            Get("/synccore", parameters =>
            {
                var response = new Response();

                response.ContentType = "text/plain";
                response.Contents = stream =>
                {
                    var deserializer = new DeserializerBuilder().Build();
                    TreeProvider tree = new TreeProvider(MembershipContext.AuthenticatedUser);
                    var watch = new Stopwatch();

                    //have this driven by config
                    var serializationPath = "/serialization/core";
                    var pageTypes = new string[] { "cms.root", "custom.basicpage" };
                    var path = "/%";

                    var concretePath = HttpContext.Current.Server.MapPath(serializationPath);
                    var yamlFiles = Directory.EnumerateFiles(concretePath, "*.yaml", SearchOption.AllDirectories);
                    foreach (string yamlFile in yamlFiles)
                    {
                        watch.Reset();
                        watch.Start();
                        var yamlFileContent = File.ReadAllText(yamlFile);
                        var bridgeClassInfo = deserializer.Deserialize<BridgeClassInfo>(yamlFileContent);

                        var newDCI = new DataClassInfo();
                        foreach(var field in bridgeClassInfo.FieldValues)
                        {
                            newDCI[field.Key] = field.Value;
                        }

                        DataClassInfoProvider.SetDataClassInfo(newDCI);
                        watch.Stop();
                        byte[] bytes = Encoding.UTF8.GetBytes($"Synced: {yamlFile.Replace(concretePath, "")} - {watch.ElapsedMilliseconds}ms");
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
                    var deserializer = new DeserializerBuilder().Build();
                    TreeProvider tree = new TreeProvider(MembershipContext.AuthenticatedUser);
                    var watch = new Stopwatch();

                    //have this driven by config
                    var serializationPath = "/serialization/content";
                    var pageTypes = new string[] { "cms.root", "custom.basicpage" };
                    var path = "/%";

                    var concretePath = HttpContext.Current.Server.MapPath(serializationPath);
                    var yamlFiles = Directory.EnumerateFiles(concretePath, "*.yaml", SearchOption.AllDirectories);
                    foreach (string yamlFile in yamlFiles)
                    {
                        watch.Reset();
                        watch.Start();
                        var yamlFileContent = File.ReadAllText(yamlFile);
                        var bridgeTreeNode = deserializer.Deserialize<BridgeTreeNode>(yamlFileContent);
                        var treeNode = tree.SelectSingleNode(bridgeTreeNode.NodeGUID, bridgeTreeNode.DocumentCulture, bridgeTreeNode.NodeSiteName);
                        if(treeNode != null)
                        {
                            if (bridgeTreeNode.FieldValues != null)
                            {
                                foreach (var fieldValue in bridgeTreeNode.FieldValues)
                                {
                                    treeNode.SetValue(fieldValue.Key, fieldValue.Value);
                                }
                                treeNode.Update();
                            }
                        }
                        else
                        {
                            var nonSpecificCultureTreeNode = tree.SelectSingleNode(bridgeTreeNode.NodeGUID, null, bridgeTreeNode.NodeSiteName, true);
                            if(nonSpecificCultureTreeNode != null)
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
                                var parentTreeNode = tree.SelectSingleNode(bridgeTreeNode.NodeParentID, bridgeTreeNode.DocumentCulture);
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
                        watch.Stop();
                        byte[] bytes = Encoding.UTF8.GetBytes($"Synced: {yamlFile.Replace(concretePath, "")} - {watch.ElapsedMilliseconds}ms");
                        stream.Write(bytes, 0, bytes.Length);
                        stream.WriteByte(10);
                        stream.Flush();
                    }
                };
                return response;
            });
        }
    }
}