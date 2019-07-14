using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Xml.Serialization;

namespace TWCore.Diagnostics.Api.Models.Groups
{
    [DataContract]
    public class GroupResult
    {
        [XmlAttribute, DataMember]
        public string Group { get; set; }
        [XmlAttribute, DataMember]
        public int LogsCount { get; set; }
        [XmlAttribute, DataMember]
        public int TracesCount { get; set; }
        [XmlAttribute, DataMember]
        public DateTime Start { get; set; }
        [XmlAttribute, DataMember]
        public DateTime End { get; set; }
        [XmlAttribute, DataMember]
        public bool HasErrors { get; set; }
    }
}
