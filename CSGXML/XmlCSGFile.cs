using System;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace CSGDemo.CSGXML
{
	[XmlRoot("CSG")]
	public class XmlCSGFile
	{
		[XmlElement("Tree", typeof(XmlTree))]
		public XmlTree XmlTree = new XmlTree();

		[XmlElement("Instances", typeof(XmlInstances))]
		public XmlInstances Instances = new XmlInstances();
	}
}
