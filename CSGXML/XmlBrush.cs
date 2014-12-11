using System.Collections.Generic;
using System.Xml.Serialization;
using CSGDemo.Geometry;
using CSGDemo.CSGHierarchy;
using OpenTK;

namespace CSGDemo.CSGXML
{
	[XmlRoot("Brush")]
	public class XmlBrush : XmlNode
	{
		public XmlBrush() { }
		public XmlBrush(List<Plane> planes) { Planes.AddRange(planes); }

		public List<Plane> Planes = new List<Plane>();

		public override CSGNode ToCSGNode(CSGNode parent, HashSet<CSGNode> animateNodes, bool ignoreAnimate)
		{
			var leaf = new CSGNode(this.ID, Planes);
			leaf.Parent				= parent;
			leaf.LocalTranslation	= this.Translation;
			if (parent != null)
				leaf.Translation = Vector3.Add(parent.Translation, leaf.LocalTranslation);
			else
				leaf.Translation = leaf.LocalTranslation;

			if (Animate && !ignoreAnimate)
				animateNodes.Add(leaf);

			return leaf;
		}

		protected override void CopyTo(XmlNode node)
		{
			base.CopyTo(node);
			var brush = node as XmlBrush;
			if (brush == null)
				return;

			var planes = new List<Plane>();
			foreach (var plane in this.Planes)
				planes.Add(new Plane(plane));
			brush.Planes = planes;
		}

		public override XmlNode Clone()
		{
			var brush = new XmlBrush();
			CopyTo(brush);
			return brush;
		}
	}
}
