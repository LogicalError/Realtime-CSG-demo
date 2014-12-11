using System;
using System.Xml.Serialization;
using System.Collections.Generic;
using CSGDemo.CSGHierarchy;

namespace CSGDemo.CSGXML
{
	[XmlRoot("Subtraction")]
	public class XmlSubtraction : XmlBranch
	{
		public XmlSubtraction() : base(CSGNodeType.Subtraction) { }

		public override XmlNode Clone()
		{
			var brush = new XmlSubtraction();
			CopyTo(brush);
			return brush;
		}
	}
}
