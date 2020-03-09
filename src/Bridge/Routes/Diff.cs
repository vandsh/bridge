using Bridge.Application;
using Bridge.Models;
using CMS.DataEngine;
using CMS.DocumentEngine;
using CMS.SiteProvider;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Mapster;
using Nancy;
using Nancy.Security;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using YamlDotNet.Serialization;

namespace Bridge.Routes
{
    public class Diff : BridgeModule
    {
        public Diff()
        {
            this.RequiresAuthentication();

            Get("/diffcore/{id}", parameters =>
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
                    }
                };
                return response;
            });


            Get("/diffcontent/{id}", parameters =>
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
                    }
                };
                return response;
            });

            Get("/viewdiff", parameters =>
            {
                var response = new Response();

                response.ContentType = "text/plain";
                response.Contents = stream =>
                {
                    var bridgeContentConfigs = BridgeConfiguration.GetConfig().ContentConfigs;
                    string origFolder = Request.Query["origFolder"];
                    string tempFolder = Request.Query["tempFolder"];
                    string file = Request.Query["file"];

                    var origPath = HttpContext.Current.Server.MapPath($"{origFolder}/{file}");
                    var origYamlFileContents = File.ReadAllText(origPath);
                    var tempPath = HttpContext.Current.Server.MapPath($"{tempFolder}/{file}");
                    var tempFileContents = File.ReadAllText(tempPath);

                    var diffBuilder = new InlineDiffBuilder(new Differ());
                    var diff = diffBuilder.BuildDiffModel(origYamlFileContents, tempFileContents);

                    foreach (var line in diff.Lines)
                    {
                        string linePrefix = "";
                        string lineSuffix = "";
                        switch (line.Type)
                        {
                            case ChangeType.Inserted:
                                linePrefix = "+ <strong>";
                                lineSuffix = "</strong>";
                                break;
                            case ChangeType.Deleted:
                                linePrefix = "- <em>";
                                lineSuffix = "</em>";
                                break;
                            default:
                                linePrefix = "  ";
                                lineSuffix = "";
                                break;
                        }
                        _outputToStream(stream, $"{linePrefix}{line.Text}{lineSuffix}");
                    }
                };
                return response;
            });
        }
        private void _processCoreConfig(BridgeCoreConfig coreConfig, Stream stream)
        {
            var serializer = new SerializerBuilder().ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull).Build();
            string serializationFolder = BridgeConfiguration.GetConfig().SerializationFolder;
            var watch = new Stopwatch();
            _clearTempFolder();
            watch.Start();
            //have this driven by config
            var serializationPath = $"{serializationFolder}/core/{coreConfig.Name}";
            var tempGUID = DateTime.Now.Ticks.ToString();
            var tempSerializationPath = $"{serializationFolder}/temp/{tempGUID}/{coreConfig.Name}";
            var classTypes = coreConfig.GetClassTypes();
            var fieldsToIgnore = coreConfig.GetIgnoreFields();

            ProviderHelper.ClearHashtables("cms.class", false);
            foreach (var classType in classTypes)
            {
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

                    var pathToMatching = $"{tempSerializationPath}/{mappedItem.ClassName.ToLower()}.yaml";
                    var tempPath = HttpContext.Current.Server.MapPath(pathToMatching);
                    FileInfo file = new FileInfo(tempPath);
                    file.Directory.Create(); // If the directory already exists, this method does nothing.
                    File.WriteAllText(tempPath, res);
                }
            }
            watch.Stop();
            _outputToStream(stream, $"Generating temp {coreConfig.Name} - {watch.ElapsedMilliseconds}ms");
            _processDifferences(stream, watch, serializationPath, tempSerializationPath);
        }

        private void _processContentConfig(BridgeContentConfig contentConfig, Stream stream)
        {
            var serializer = new SerializerBuilder().ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull).Build();
            var watch = new Stopwatch();
            string serializationFolder = BridgeConfiguration.GetConfig().SerializationFolder;
            _clearTempFolder();
            watch.Start();

            //have this driven by config
            var serializationPath = $"{serializationFolder}/content/{contentConfig.Name}";
            var tempGUID = DateTime.Now.Ticks.ToString();
            var tempSerializationPath = $"{serializationFolder}/temp/{tempGUID}/{contentConfig.Name}";
            var pageTypes = contentConfig.GetPageTypes();
            var fieldsToIgnore = contentConfig.GetIgnoreFields();
            var path = contentConfig.Query;

            //TODO: Query to see if page types exist, if DOESNT, display that...
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
                var pathToWriteTo = $"{tempSerializationPath}/{mappedItem.NodeAliasPath}#{mappedItem.DocumentCulture}.yaml";
                var concretePath = HttpContext.Current.Server.MapPath(pathToWriteTo);
                FileInfo file = new FileInfo(concretePath);
                file.Directory.Create(); // If the directory already exists, this method does nothing.
                File.WriteAllText(concretePath, res);
            }
                watch.Stop();
            _outputToStream(stream, $"Generating temp {contentConfig.Name} - {watch.ElapsedMilliseconds}ms");
            _processDifferences(stream, watch, serializationPath, tempSerializationPath);
        }


        private static void _clearTempFolder()
        {
            try
            {
                //just trying to clean up some temp stuff from prev diffs
                string serializationFolder = BridgeConfiguration.GetConfig().SerializationFolder;
                var tempRoot = HttpContext.Current.Server.MapPath($"{serializationFolder}/temp");
                DirectoryInfo di = new DirectoryInfo(tempRoot);
                foreach (FileInfo file in di.EnumerateFiles())
                {
                    file.Delete();
                }
                foreach (DirectoryInfo dir in di.EnumerateDirectories())
                {
                    dir.Delete(true);
                }
            }
            catch (Exception ex)
            {
                //keep moving
            }
        }

        private static void _processDifferences(Stream stream, Stopwatch watch, string serializationPath, string tempSerializationPath)
        {
            watch.Reset();
            watch.Start();
            var concretePath = HttpContext.Current.Server.MapPath(serializationPath);
            var origYamlFiles = (Directory.Exists(concretePath))? Directory.EnumerateFiles(concretePath, "*.yaml", SearchOption.AllDirectories) : new List<string>();
            var tempDiffPath = HttpContext.Current.Server.MapPath(tempSerializationPath);
            var tempYamlFiles = (Directory.Exists(tempDiffPath)) ? Directory.EnumerateFiles(tempDiffPath, "*.yaml", SearchOption.AllDirectories) : new List<string>();
            var origYamlHash = new Dictionary<string, string>();
            var tempYamlHash = new Dictionary<string, string>();
            foreach (string origYamlFile in origYamlFiles)
            {
                using (var md5 = MD5.Create())
                {
                    using (var ofs = File.OpenRead(origYamlFile))
                    {
                        var hash = md5.ComputeHash(ofs);
                        origYamlHash.Add(origYamlFile.Replace(concretePath, ""), BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant());
                    }
                }
            }

            foreach (string tempYamlFile in tempYamlFiles)
            {
                using (var md5 = MD5.Create())
                {
                    using (var ofs = File.OpenRead(tempYamlFile))
                    {
                        var hash = md5.ComputeHash(ofs);
                        tempYamlHash.Add(tempYamlFile.Replace(tempDiffPath, ""), BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant());
                    }
                }
            }
            //match keys, compare and show missing and diffs
            var missingInTemp = origYamlHash.Where(entry => !tempYamlHash.ContainsKey(entry.Key)).Select(x => x.Key);
            foreach (var missingKeyInTemp in missingInTemp)
            {
                _outputToStream(stream, $"Missing in database: {missingKeyInTemp}");
            }

            var missingInOrig = tempYamlHash.Where(entry => !origYamlHash.ContainsKey(entry.Key)).Select(x => x.Key);
            foreach (var missingKeyInOrig in missingInOrig)
            {
                _outputToStream(stream, $"Missing in serialized: {missingKeyInOrig}");
            }
            var keyMatchHashDifference = tempYamlHash.Where(entry => origYamlHash.ContainsKey(entry.Key) && (origYamlHash[entry.Key] != entry.Value)).Select(x => x.Key);
            foreach (var diff in keyMatchHashDifference)
            {
                _outputToStream(stream, $"Difference found: <code onclick='viewdiff(this,\"{serializationPath}\", \"{tempSerializationPath}\")'>{diff}</code>");
            }
            if (!missingInTemp.Any() && !missingInOrig.Any() && !keyMatchHashDifference.Any())
            {
                _outputToStream(stream, "No differences.");
            }
        }

        private static void _outputToStream(Stream stream, string output)
        {
            byte[] bytes1 = Encoding.UTF8.GetBytes(output);
            stream.Write(bytes1, 0, bytes1.Length);
            stream.WriteByte(10);
            stream.Flush();
        }
    }
}