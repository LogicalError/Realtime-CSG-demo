using System.Xml.Serialization;
using System.Collections.Generic;
using CSGDemo.CSGHierarchy;

namespace CSGDemo.CSGXML
{
	[XmlRoot("Tree")]
	public class XmlTree
	{
		[XmlElement("Addition", 	typeof(XmlAddition))]
		[XmlElement("Subtraction", 	typeof(XmlSubtraction))]
		[XmlElement("Common", 		typeof(XmlCommon))]
		[XmlElement("Brush",     	typeof(XmlBrush))]
		[XmlElement("Instance",     typeof(XmlInstance))]
		public XmlNode RootNode;

		public CSGTree ToCSGTree(out HashSet<CSGNode> animatedNodes)
		{
			animatedNodes = new HashSet<CSGNode>();
			var tree = new CSGTree();
			tree.RootNode = RootNode.ToCSGNode(null, animatedNodes, false);
			return tree;
		}
	}
}
