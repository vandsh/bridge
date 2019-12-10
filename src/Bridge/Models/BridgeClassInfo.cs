
using System;
using System.Collections.Generic;

namespace Bridge.Models
{
    public class BridgeClassInfo
    {
        public string ClassName { get; set; }
        public string ClassTableName { get; set; }
        public bool ClassIsDocumentType { get; set; }
        public List<Guid> AssignedSites { get; set; }
        public List<string> AllowedChildTypes { get; set; }
        public Dictionary<string, object> FieldValues { get; set; }
    }
}