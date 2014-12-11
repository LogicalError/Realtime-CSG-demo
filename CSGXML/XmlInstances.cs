using System;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.ComponentModel;
using OpenTK;

namespace CSGDemo.CSGXML
{
	[XmlRoot("Instances")]
	public class XmlInstances
	{
		[XmlElement("Brush", typeof(XmlBrush))]
		[XmlElement("Addition", typeof(XmlAddition))]
		[XmlElement("Subtraction", typeof(XmlSubtraction))]
		[XmlElement("Common", typeof(XmlCommon))]
		[XmlElement("Instance", typeof(XmlInstance))]
		public readonly List<XmlNode> Nodes = new List<XmlNode>();
	}
}
