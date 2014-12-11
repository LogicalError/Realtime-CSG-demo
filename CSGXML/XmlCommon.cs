using System;
using System.Xml.Serialization;
using System.Collections.Generic;
using CSGDemo.CSGHierarchy;

namespace CSGDemo.CSGXML
{
	[XmlRoot("Common")]
	public class XmlCommon : XmlBranch
	{
		public XmlCommon() : base(CSGNodeType.Common) { }

		public override XmlNode Clone()
		{
			var brush = new XmlCommon();
			CopyTo(brush);
			return brush;
		}
	}
}
