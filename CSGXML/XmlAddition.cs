using System;
using System.Xml.Serialization;
using System.Collections.Generic;
using CSGDemo.CSGHierarchy;

namespace CSGDemo.CSGXML
{
	[XmlRoot("Addition")]
	public class XmlAddition : XmlBranch
	{
		public XmlAddition() : base(CSGNodeType.Addition) { }

		public override XmlNode Clone()
		{
			var brush = new XmlAddition();
			CopyTo(brush);
			return brush;
		}
	}
}
