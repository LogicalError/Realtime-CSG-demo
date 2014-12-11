using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;
using CSGDemo.CSGHierarchy;
using OpenTK;

namespace CSGDemo.CSGXML
{
	[DebuggerDisplay("ID: {ID}")]
	public abstract class XmlNode
	{
        public XmlNode() { ID = "Node" + Index; }
		
	 	static uint IndexCounter = 0;
		[XmlIgnore]
		public uint			Index = ++IndexCounter;
		
	  	[XmlAttribute, DefaultValue(null)]
		public string		ID;

		[XmlIgnore]
		public XmlNode		Parent;
		
		[XmlIgnore]
		public Vector3		Translation;
		
		[XmlAttribute("Animate"), DefaultValue(false)]
		public bool			Animate = false;

		[XmlAttribute("X"), DefaultValue(0)]
		public float X { get { return Translation.X; } set { Translation.X = value; } }
		[XmlAttribute("Y"), DefaultValue(0)]
		public float Y { get { return Translation.Y; } set { Translation.Y = value; } }
		[XmlAttribute("Z"), DefaultValue(0)]
		public float Z { get { return Translation.Z; } set { Translation.Z = value; } }

		public abstract CSGNode ToCSGNode(CSGNode parent, HashSet<CSGNode> animateNodes, bool ignoreAnimate);

		protected virtual void CopyTo(XmlNode node)
		{
			node.Animate		= this.Animate;
			node.Translation	= this.Translation;
			node.Parent			= null;
			node.ID				= this.ID;
		}

		public abstract XmlNode Clone();
	}
}
