using System;
using System.Diagnostics;
using OpenTK;

namespace CSGDemo.Geometry
{
	[DebuggerDisplay("Visible: {Visible} Category: {Category} PlaneIndex: {PlaneIndex} FirstIndex: {FirstIndex}")]
	public sealed class Polygon
	{
		public short			FirstIndex		= -1;
		public short			PlaneIndex		= -1;
		public PolygonCategory	Category		= PolygonCategory.Aligned;
		public bool				Visible			= false;
		public AABB				Bounds			= new AABB();
	}

	public enum PolygonCategory : byte
	{
		Inside,
		Aligned,
		ReverseAligned,
		Outside
	}
}