using Bridge.Models;
using CMS.DataEngine;
using CMS.DocumentEngine;
using Mapster;
using Nancy;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using YamlDotNet.Serialization;

namespace Bridge.Routes
{
    public class Serialize : NancyModule
    {
        /// <summary>
        /// Take whats in the database and serialize it
        /// </summary>
        public Serialize()
        {
            Get("/serializecore", parameters =>
            {
                var response = new Response();

                response.ContentType = "text/plain";
                response.Contents = stream =>
                {
                    var serializer = new SerializerBuilder().ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull).Build();
                    var watch = new Stopwatch();

                    //have this driven by config
                    var serializationPath = "/serialization/core";
                    var pageTypes = new string[] { "cms.root", "custom.basicpage" };
                    var path = "/%";

                    foreach (var pageType in pageTypes)
                    {
                        watch.Reset();
                        watch.Start();
                        var dci = DataClassInfoProvider.GetDataClassInfo(pageType);
                        var mappedItem = dci.Adapt<BridgeClassInfo>();
                        mappedItem.FieldValues = new Dictionary<string, object>();

                        foreach (string columnName in dci.ColumnNames)
                        {
                            var columnValue = dci.GetValue(columnName);
                            mappedItem.FieldValues.Add(columnName, columnValue);
                        }

                        var stringBuilder = new StringBuilder();
                        var res = serializer.Serialize(mappedItem);
                        stringBuilder.AppendLine(res);
                        var pathToWriteTo = $"{serializationPath}/{mappedItem.ClassName.ToLower()}.yaml";
                        var concretePath = HttpContext.Current.Server.MapPath(pathToWriteTo);
                        FileInfo file = new FileInfo(concretePath);
                        file.Directory.Create(); // If the directory already exists, this method does nothing.
                        File.WriteAllText(concretePath, res);
                        watch.Stop();
                        byte[] bytes = Encoding.UTF8.GetBytes($"Serialized: {mappedItem.ClassName.ToLower()}.yaml - {watch.ElapsedMilliseconds}ms");
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
                    var serializer = new SerializerBuilder().ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull).Build();
                    var watch = new Stopwatch();

                    //have this driven by config
                    var serializationPath = "/serialization/content";
                    var pageTypes = new string[] { "cms.root", "custom.basicpage" };
                    var path = "/%";

                    MultiDocumentQuery docs = DocumentHelper.GetDocuments().Path(path).Types(pageTypes).AllCultures().FilterDuplicates();
                    var treeNodes = docs.ToList<TreeNode>();
                    foreach (var treeNode in treeNodes)
                    {
                        watch.Reset();
                        watch.Start();
                        var mappedItem = treeNode.Adapt<BridgeTreeNode>();
                        mappedItem.FieldValues = new Dictionary<string, object>();

                        foreach (string columnName in treeNode.ColumnNames)
                        {
                            var columnValue = treeNode.GetValue(columnName);
                            if (columnValue != null)
                            {
                                mappedItem.FieldValues.Add(columnName, columnValue);
                            }
                        }

                        var stringBuilder = new StringBuilder();
                        var res = serializer.Serialize(mappedItem);
                        stringBuilder.AppendLine(res);
                        var pathToWriteTo = $"{serializationPath}/{mappedItem.NodeAliasPath}#{mappedItem.DocumentCulture}.yaml";
                        var concretePath = HttpContext.Current.Server.MapPath(pathToWriteTo);
                        FileInfo file = new FileInfo(concretePath);
                        file.Directory.Create(); // If the directory already exists, this method does nothing.
                        File.WriteAllText(concretePath, res);
                        watch.Stop();
                        byte[] bytes = Encoding.UTF8.GetBytes($"Serialized: {mappedItem.NodeAliasPath}#{mappedItem.DocumentCulture}.yaml - {watch.ElapsedMilliseconds}ms");
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