using System;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.ComponentModel;
using OpenTK;

namespace CSGDemo.CSGXML
{
	[XmlRoot("Instance")]
	public class XmlInstance : XmlNode
	{
		[XmlAttribute("src")]
		public string InstanceID { get; set; }

		public override CSGHierarchy.CSGNode ToCSGNode(CSGHierarchy.CSGNode parent, HashSet<CSGHierarchy.CSGNode> animateBrushes, bool ignoreAnimate)
		{
			throw new NotImplementedException();
		}

		public override XmlNode Clone()
		{
			XmlInstance instance = new XmlInstance();
			CopyTo(instance);
			instance.InstanceID = this.InstanceID;
			return instance;
		}
	}
}
