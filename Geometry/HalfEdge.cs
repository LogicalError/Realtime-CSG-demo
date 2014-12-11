using System;
using OpenTK;

namespace CSGDemo.Geometry
{
	//           ^
	//           |       polygon
	// next      |
	// half-edge |
	//           |       half-edge
	// vertex	 *<====================== 
	//           ---------------------->*
	//				  twin-half-edge
	public sealed class HalfEdge
	{
		public short NextIndex;
		public short TwinIndex;
		public short VertexIndex;
		public short PolygonIndex;
	}
}