
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
        public Dictionary<string, BridgeClassQuery> Queries { get; set; }
    }

    public class BridgeClassQuery
    {
        public Guid QueryGUID { get; set; }
        public bool QueryIsCustom { get; set; }
        public bool QueryIsLocked { get; set; }
        public bool QueryRequiresTransaction { get; set; }
        public int QueryTypeID { get; set; }
        public string QueryText { get; set; }
        public string QueryConnectionString { get; set; }
    }
}