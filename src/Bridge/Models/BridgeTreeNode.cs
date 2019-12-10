using CMS.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Bridge.Models
{
    public class BridgeTreeNode
    {
        public string ClassName { get; set; }
        public string NodeAliasPath { get; set; }
        public string NodeSiteName { get; set; }
        public Guid NodeGUID { get; set; }
        public string NodeName { get; set; }
        public Guid? ParentNodeGUID { get; set; }

        public string DocumentCulture { get; set; }

        public Dictionary<string, object> FieldValues { get; set; }
    }
}