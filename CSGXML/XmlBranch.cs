using System.Collections.Generic;
using System.Xml.Serialization;
using CSGDemo.CSGHierarchy;
using OpenTK;

namespace CSGDemo.CSGXML
{
	[XmlRoot("Branch")]
	public abstract class XmlBranch : XmlNode
	{
		public XmlBranch(CSGNodeType branchOperator) { Operator = branchOperator; }

		[XmlIgnore]
		public CSGNodeType Operator;

		[XmlElement("Addition", 	typeof(XmlAddition), 	Order = 1, IsNullable = false)]
		[XmlElement("Subtraction", 	typeof(XmlSubtraction),	Order = 1, IsNullable = false)]
		[XmlElement("Common", 		typeof(XmlCommon), 		Order = 1, IsNullable = false)]
		[XmlElement("Brush",		typeof(XmlBrush),		Order = 1, IsNullable = false)]
		[XmlElement("Instance",		typeof(XmlInstance),	Order = 1, IsNullable = false)]
		public XmlNode Left;
		
		[XmlElement("Addition", 	typeof(XmlAddition), 	Order = 2, IsNullable = false)]
		[XmlElement("Subtraction", 	typeof(XmlSubtraction), Order = 2, IsNullable = false)]
		[XmlElement("Common", 		typeof(XmlCommon), 		Order = 2, IsNullable = false)]
		[XmlElement("Brush",	 	typeof(XmlBrush),  		Order = 2, IsNullable = false)]
		[XmlElement("Instance",		typeof(XmlInstance),	Order = 2, IsNullable = false)]
		public XmlNode Right;

		public override CSGNode ToCSGNode(CSGNode parent, HashSet<CSGNode> animateNodes, bool ignoreAnimate)
		{
			var branch = new CSGNode(this.ID, Operator);
			branch.Parent			= parent;
			branch.LocalTranslation = this.Translation;
			if (parent != null)
				branch.Translation = Vector3.Add(parent.Translation, branch.LocalTranslation);
			else
				branch.Translation = branch.LocalTranslation;
			
			branch.Left		= Left.ToCSGNode(branch, animateNodes, Animate || ignoreAnimate);
			branch.Right	= Right.ToCSGNode(branch, animateNodes, Animate || ignoreAnimate);

			if (Animate && !ignoreAnimate)
				animateNodes.Add(branch);
			
			return branch;
		}

		protected override void CopyTo(XmlNode node)
		{
			base.CopyTo(node);
			var branch = node as XmlBranch;
			if (branch == null)
				return;

			branch.Operator = this.Operator;
			if (this.Left != null)
				branch.Left = this.Left.Clone();
			else
				branch.Left = null;
			if (this.Right != null)
				branch.Right = this.Right.Clone();
			else
				branch.Right = null;
		}
	}
}
