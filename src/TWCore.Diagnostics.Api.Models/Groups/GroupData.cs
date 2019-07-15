using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using TWCore.Collections;

namespace TWCore.Diagnostics.Api.Models.Groups
{
    [DataContract]
    public class GroupData
    {
        [XmlElement, DataMember]
        public string Environment { get; set; }

        [XmlElement, DataMember]
        public string Group { get; set; }

        [XmlElement, DataMember]
        public KeyValue[] Metadata { get; set; }

        [XmlElement, DataMember]
        public List<NodeInfo> Data { get; set; }
    }
}