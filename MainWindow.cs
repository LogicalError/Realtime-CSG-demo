using System;
using System.Drawing;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading.Tasks;
using CSGDemo.Geometry;
using CSGDemo.CSGHierarchy;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

namespace CSGDemo
{
	public class MainWindow : GameWindow
	{
		#region Constructor
		/// <summary>Creates a 800x600 window with the specified title.</summary>
		public MainWindow( )
			: base(800, 600)
		{			
			this.VSync = VSyncMode.Off;
			Mouse.ButtonDown += new EventHandler<MouseButtonEventArgs>(OnButtonDown);
			Mouse.ButtonUp += new EventHandler<MouseButtonEventArgs>(OnButtonUp);
		}
		#endregion

		#region OnLoad
		protected override void OnLoad( EventArgs e )
		{
			InitOpenGL();
			InitCSGTree();
		}
		#endregion

		#region OnUnload
		protected override void OnUnload(EventArgs e)
		{
			CleanUpOpenGL();
		}
		#endregion

		#region Fields ...

		#region Input fields ...
		bool	goForward		= false;
		bool	goBackward		= false;
		bool	goLeft			= false;
		bool	goRight			= false;
		bool	goFast			= false;

		bool	cameraRotating	= false;
		bool	showTriangles	= false;
		bool	showBounds		= false;

		float	cameraYaw		= 0;
		float	cameraPitch		= 0;
		Vector3 cameraPosition	= Vector3.Zero;
		#endregion

		#region VBO fields ...
		uint	VertexVBOHandle;
		uint	PolygonIndexVBOHandle;
		uint	LineIndexVBOHandle;
		int		TotalPolygonIndices;
		int		TotalLineIndices;
		#endregion

		List<CSGNode>		instanceNodes;
		List<Vector3>		instanceNodeTranslations;
		List<CSGNode>		allBrushes;
		HashSet<CSGNode>	subInstanceBrushes;
		CSGTree				csgTree;
		
		Stopwatch			frameTimer = new Stopwatch();
		#endregion



		#region InitCSGTree
		void InitCSGTree()
		{
			csgTree = CSGUtility.LoadTree("import.xml", out instanceNodes);
			if (csgTree == null)
			{
				MessageBox.Show("Failed to load file");
				return;
			}

			instanceNodeTranslations = new List<Vector3>();
			if (instanceNodes != null &&
				instanceNodes.Count > 0)
			{
				foreach (var node in instanceNodes)
					instanceNodeTranslations.Add(node.LocalTranslation);

				subInstanceBrushes = new HashSet<CSGNode>();
				foreach (var node in instanceNodes)
				{
					foreach (var brush in CSGUtility.FindChildBrushes(node))
						subInstanceBrushes.Add(brush);
				}

				allBrushes = (from node in
								  CSGUtility.FindChildBrushes(csgTree)
							  where !subInstanceBrushes.Contains(node)
							  select node).ToList();
			} else
				allBrushes = CSGUtility.FindChildBrushes(csgTree).ToList();

			var updateNodes = new List<CSGNode>();
			updateNodes.AddRange(allBrushes);
			updateNodes.AddRange(instanceNodes);

			UpdateMeshes(csgTree, updateNodes);
		}
		#endregion

		#region AnimateInstanceNodes
		void AnimateInstanceNodes(double time)
		{
			if (instanceNodes == null ||
				instanceNodes.Count == 0)
				return;

			var revolutionsPerSecond	= 0.75f;
			var t						= ((time * revolutionsPerSecond) * Math.PI);
			var radiusX					= 15.0f;
			var radiusY					= 15.0f;

			var translation		= new Vector3((float)(Math.Cos(t) * radiusX), (float)(Math.Sin(t) * radiusY), 0);
			
			var foundNodes = new HashSet<CSGNode>();
			for (int i = 0; i < instanceNodes.Count; ++i)
			{
				var node		= instanceNodes[i];
				var nodeBounds	= node.Bounds;
				var nodeTrans	= node.Translation;
				for (int j = 0; j < allBrushes.Count; ++j)
				{
					var otherBrush = allBrushes[j];
					if (foundNodes.Contains(otherBrush))
						continue;

					var otherBrushBounds	= otherBrush.Bounds;
					var otherBrushTrans		= otherBrush.Translation;
					var relativeTrans		= Vector3.Subtract(nodeTrans, otherBrushTrans);
					if (!AABB.IsOutside(nodeBounds, relativeTrans, otherBrushBounds))
						foundNodes.Add(otherBrush);
				}
			}

			for (int i = 0; i < instanceNodes.Count; ++i)
			{
				var node = instanceNodes[i];
				node.LocalTranslation = Vector3.Add(instanceNodeTranslations[i], translation);
				if (node.Parent != null)
					node.Translation = node.LocalTranslation + node.Parent.Translation;
				else
					node.Translation = node.LocalTranslation;
				CSGUtility.UpdateChildTransformations(node);
				foundNodes.Add(node);
			}

			for (int i = 0; i < instanceNodes.Count; ++i)
			{
				var node		= instanceNodes[i];
				var nodeBounds	= node.Bounds;
				var nodeTrans	= node.Translation;
				for (int j = 0; j < allBrushes.Count; ++j)
				{
					var otherBrush = allBrushes[j];
					if (foundNodes.Contains(otherBrush))
						continue;
					
					var otherBrushBounds	= otherBrush.Bounds;
					var otherBrushTrans		= otherBrush.Translation;
					var relativeTrans		= Vector3.Subtract(nodeTrans, otherBrushTrans);
					if (!AABB.IsOutside(node.Bounds, relativeTrans, otherBrush.Bounds))
						foundNodes.Add(otherBrush);
				}
			}

			UpdateMeshes(csgTree, foundNodes);
		}
		#endregion

		

		#region CreateListsFromMeshes
		static Dictionary<Vector4, short>	vertexLookup		= new Dictionary<Vector4, short>();
		static short[]						vertexIndexLookup	= new short[65535];
		public static void CreateListsFromMeshes(Dictionary<CSGNode, CSGMesh> meshes, 
												 Vector4[]	vertices, out int vertexCount, 
												 short[]	polyIndices, out int polyIndexCount, 
												 short[]	lineIndices, out int lineIndexCount)
		{
			vertexCount		= 0;
			polyIndexCount	= 0;
			lineIndexCount	= 0;
			vertexLookup.Clear();

			foreach (var item in meshes)
			{
				var node			= item.Key;
				var mesh			= item.Value;
				var meshVertices	= mesh.Vertices;
				var offset			= node.Translation;
				for(int i=0;i<meshVertices.Count;i++)
				{
					var meshVertex = meshVertices[i];
					short index;
					var vertex = new Vector4(meshVertex.X + offset.X, meshVertex.Y + offset.Y, meshVertex.Z + offset.Z, 1);
					if (!vertexLookup.TryGetValue(vertex, out index))
					{
						index = (short)vertexCount;
						vertexLookup.Add(vertex, index);
						vertices[vertexCount] = vertex;
						vertexCount++;
					}
					vertexIndexLookup[i] = index;
				}

				var polygons		= mesh.Polygons;
				var edges			= mesh.Edges;
				var planes			= mesh.Planes;
				foreach (var polygon in polygons)
				{
					if (!polygon.Visible ||
						polygon.FirstIndex == -1)
						continue;

					var iterator = edges[polygon.FirstIndex];
					if (iterator == null)
						continue;

					short first				= vertexIndexLookup[iterator.VertexIndex];
					iterator = edges[iterator.NextIndex];
					short second			= vertexIndexLookup[iterator.VertexIndex];

					var twin				= edges[iterator.TwinIndex];
					var twinPolygonIndex	= twin.PolygonIndex;
					var twinPolygon			= polygons[twinPolygonIndex];
					var twinPlane			= planes[twinPolygon.PlaneIndex];
					var curPolygonIndex		= iterator.PolygonIndex;
					var curPolygon			= polygons[curPolygonIndex];
					var curPlane			= planes[curPolygon.PlaneIndex];

					if (!twinPolygon.Visible ||
						!curPlane.Equals(twinPlane))
					{
						if (lineIndexCount <= lineIndices.Length - 2)
						{
							lineIndices[lineIndexCount] = first;	lineIndexCount++;
							lineIndices[lineIndexCount] = second;	lineIndexCount++;
						}
					}

					var previous		= second;
					var polygonFirst	= edges[polygon.FirstIndex];
					while (iterator != polygonFirst)
					{
						iterator			= edges[iterator.NextIndex];
						
						curPolygonIndex		= iterator.PolygonIndex;
						curPolygon			= polygons[curPolygonIndex];
						curPlane			= planes[curPolygon.PlaneIndex];

						twin				= edges[iterator.TwinIndex];
						twinPolygonIndex	= twin.PolygonIndex;
						twinPolygon			= polygons[twinPolygonIndex];
						twinPlane			= planes[twinPolygon.PlaneIndex];

						short third = vertexIndexLookup[iterator.VertexIndex];

						if (!curPlane.Equals(twinPlane))
						{
							if (lineIndexCount <= lineIndices.Length - 2)
							{
								lineIndices[lineIndexCount] = previous;	lineIndexCount++;
								lineIndices[lineIndexCount] = third;	lineIndexCount++;
							}
						} else
						if (!twinPolygon.Visible)
						{
							if (lineIndexCount <= lineIndices.Length - 2)
							{
								lineIndices[lineIndexCount] = previous;	lineIndexCount++;
								lineIndices[lineIndexCount] = third;	lineIndexCount++;
							}
						}
							
						previous = third;
						if (polyIndexCount < polyIndices.Length - 3)
						{
							polyIndices[polyIndexCount] = first;	polyIndexCount++;
							polyIndices[polyIndexCount] = second;	polyIndexCount++;
							polyIndices[polyIndexCount] = third;	polyIndexCount++;
						}

						second = third;
					}
				}
			}
		}
		#endregion  
		
		#region UpdateMeshes
		Dictionary<CSGNode, CSGMesh> validMeshes = new Dictionary<CSGNode, CSGMesh>();
		void UpdateMeshes(CSGTree tree, IEnumerable<CSGNode> updateNodes)
		{
			var modifiedMeshes = CSGCategorization.ProcessCSGNodes(tree.RootNode, updateNodes);

			foreach (var item in modifiedMeshes)
				validMeshes[item.Key] = item.Value;

			UpdateVertexBuffers(validMeshes);
		}
		#endregion

		#region UpdateVertexBuffers
		static Vector4[]	vertices	= new Vector4[65535];
		static short[]		polyIndices	= new short[65535 * 4];
		static short[]		lineIndices	= new short[65535 * 3];
		void UpdateVertexBuffers(Dictionary<CSGNode, CSGMesh> allMeshes)
		{
			int vertexCount;
			int polyIndexCount;
			int lineIndexCount;

			CreateListsFromMeshes(allMeshes, vertices, out vertexCount, polyIndices, out polyIndexCount, lineIndices, out lineIndexCount);


			GL.BindBuffer(BufferTarget.ArrayBuffer, VertexVBOHandle);
			GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(vertexCount * sizeof(float) * 4), IntPtr.Zero, BufferUsageHint.StaticDraw);
			
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, PolygonIndexVBOHandle);
			GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(polyIndexCount * sizeof(short)), IntPtr.Zero, BufferUsageHint.StaticDraw);

			GL.BindBuffer(BufferTarget.ElementArrayBuffer, LineIndexVBOHandle);
			GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(lineIndexCount * sizeof(short)), IntPtr.Zero, BufferUsageHint.StaticDraw);



			// Fill newly allocated buffer
			GL.BindBuffer(BufferTarget.ArrayBuffer, VertexVBOHandle);
			GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(vertexCount * sizeof(float) * 4), vertices, BufferUsageHint.StaticDraw);

			GL.BindBuffer(BufferTarget.ElementArrayBuffer, PolygonIndexVBOHandle);
			GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(polyIndexCount * sizeof(short)), polyIndices, BufferUsageHint.StaticDraw);

			GL.BindBuffer(BufferTarget.ElementArrayBuffer, LineIndexVBOHandle);
			GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(lineIndexCount * sizeof(short)), lineIndices, BufferUsageHint.StaticDraw);

			TotalPolygonIndices = polyIndexCount;
			TotalLineIndices	= lineIndexCount;
		}
		#endregion



		#region InitOpenGL
		private void InitOpenGL()
		{
			GL.ClearColor(.1f, 0f, .1f, 0f);
			GL.Enable(EnableCap.DepthTest);

			// Setup parameters for Points
			GL.PointSize(5f);
			GL.Enable(EnableCap.PointSmooth);
			GL.Hint(HintTarget.PointSmoothHint, HintMode.Nicest);


			// Setup VBO state
			GL.EnableClientState(ArrayCap.VertexArray);
			GL.EnableClientState(ArrayCap.IndexArray);

			GL.GenBuffers(1, out VertexVBOHandle);
			GL.BindBuffer(BufferTarget.ArrayBuffer, VertexVBOHandle);
			GL.VertexPointer(4, VertexPointerType.Float, sizeof(float) * 4, 0);

			GL.GenBuffers(1, out PolygonIndexVBOHandle);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, PolygonIndexVBOHandle);
			GL.IndexPointer(IndexPointerType.Short, sizeof(short), 0);

			GL.GenBuffers(1, out LineIndexVBOHandle);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, LineIndexVBOHandle);
			GL.IndexPointer(IndexPointerType.Short, sizeof(short), 0);
		}
		#endregion

		#region CleanUpOpenGL
		private void CleanUpOpenGL()
		{
			GL.DeleteBuffers(1, ref VertexVBOHandle);
			GL.DeleteBuffers(1, ref PolygonIndexVBOHandle);
			GL.DeleteBuffers(1, ref LineIndexVBOHandle);
		}
		#endregion

		#region OnResize
		protected override void OnResize(EventArgs e)
		{
			GL.Viewport(0, 0, Width, Height);

			GL.MatrixMode(MatrixMode.Projection);
			Matrix4 p = Matrix4.CreatePerspectiveFieldOfView(MathHelper.PiOver4, Width / (float)Height, 0.25f, 16384.0f);
			GL.LoadMatrix(ref p);
		}
		#endregion

		#region OnRenderFrame
		protected override void OnRenderFrame( FrameEventArgs e )
		{
			GL.MatrixMode(MatrixMode.Modelview);

			Matrix4 transformation = Matrix4.Mult(Matrix4.CreateRotationY(cameraYaw),
												  Matrix4.CreateRotationX(cameraPitch));
			Matrix4 translation = Matrix4.CreateTranslation(-cameraPosition.X, -cameraPosition.Y, -cameraPosition.Z);
			GL.LoadMatrix(ref transformation);
			GL.MultMatrix(ref translation);

			GL.Clear( ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit );
			GL.PushMatrix( );


			GL.Enable(EnableCap.PolygonOffsetFill);
			GL.PolygonOffset(1.0f, 0.0f);

			GL.ColorMask(false, false, false, false);
			GL.DepthFunc(DepthFunction.Less);
			GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
			GL.BindBuffer(BufferTarget.ArrayBuffer, VertexVBOHandle);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, PolygonIndexVBOHandle);
			GL.DrawElements(BeginMode.Triangles, TotalPolygonIndices, DrawElementsType.UnsignedShort, 0);


			GL.Disable(EnableCap.PolygonOffsetFill);
			GL.Enable(EnableCap.Blend);
			GL.Disable(EnableCap.CullFace);
			GL.Disable(EnableCap.DepthTest);
			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			GL.ColorMask(true, true, true, true);
			GL.DepthFunc(DepthFunction.Less);
			GL.Color4(0.0f, 0.5f, 0.5f, 0.5f);
			GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
			GL.BindBuffer(BufferTarget.ArrayBuffer, VertexVBOHandle);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, PolygonIndexVBOHandle);
			GL.DrawElements(BeginMode.Triangles, TotalPolygonIndices, DrawElementsType.UnsignedShort, 0);

			
			GL.LineWidth(1.0f);
			GL.Color4(1.0f, 1.0f, 1.0f, 0.125f);
			
			if (showTriangles)
			{
				GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
				GL.BindBuffer(BufferTarget.ArrayBuffer, VertexVBOHandle);
				GL.BindBuffer(BufferTarget.ElementArrayBuffer, PolygonIndexVBOHandle);
				GL.DrawElements(BeginMode.Triangles, TotalPolygonIndices, DrawElementsType.UnsignedShort, 0);
			} else
			{
				GL.BindBuffer(BufferTarget.ArrayBuffer, VertexVBOHandle);
				GL.BindBuffer(BufferTarget.ElementArrayBuffer, LineIndexVBOHandle);
				GL.DrawElements(BeginMode.Lines, TotalLineIndices, DrawElementsType.UnsignedShort, 0);
			}
			
			GL.Disable(EnableCap.Blend);
			GL.Enable(EnableCap.DepthTest);

			GL.DepthFunc(DepthFunction.Lequal);
			GL.LineWidth(2.0f);
			GL.Color4(1.0f, 1.0f, 1.0f, 1.0f);
			
			if (showTriangles)
			{
				GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
				GL.DrawElements(BeginMode.Triangles, TotalPolygonIndices, DrawElementsType.UnsignedShort, 0);
			} else
			{
				GL.BindBuffer(BufferTarget.ArrayBuffer, VertexVBOHandle);
				GL.BindBuffer(BufferTarget.ElementArrayBuffer, LineIndexVBOHandle);
				GL.DrawElements(BeginMode.Lines, TotalLineIndices, DrawElementsType.UnsignedShort, 0);
			}

			if (showBounds)
			{
				GL.LineWidth(2.0f);
				GL.Color3(1.0f, 0.0f, 0.0f);
				GL.Begin(BeginMode.Lines);
				foreach (var item in validMeshes)
				{
					var mesh = item.Value;
					var bounds = mesh.Bounds;
					var offset = item.Key.Translation;
					var xp = new float[] { bounds.MinX + offset.X, bounds.MaxX + offset.X };
					var yp = new float[] { bounds.MinY + offset.Y, bounds.MaxY + offset.Y };
					var zp = new float[] { bounds.MinZ + offset.Z, bounds.MaxZ + offset.Z };

					GL.Vertex3(xp[0], yp[0], zp[0]); GL.Vertex3(xp[1], yp[0], zp[0]);
					GL.Vertex3(xp[1], yp[0], zp[0]); GL.Vertex3(xp[1], yp[1], zp[0]);
					GL.Vertex3(xp[1], yp[1], zp[0]); GL.Vertex3(xp[0], yp[1], zp[0]);
					GL.Vertex3(xp[0], yp[1], zp[0]); GL.Vertex3(xp[0], yp[0], zp[0]);

					GL.Vertex3(xp[0], yp[0], zp[1]); GL.Vertex3(xp[1], yp[0], zp[1]);
					GL.Vertex3(xp[1], yp[0], zp[1]); GL.Vertex3(xp[1], yp[1], zp[1]);
					GL.Vertex3(xp[1], yp[1], zp[1]); GL.Vertex3(xp[0], yp[1], zp[1]);
					GL.Vertex3(xp[0], yp[1], zp[1]); GL.Vertex3(xp[0], yp[0], zp[1]);

					GL.Vertex3(xp[0], yp[0], zp[0]); GL.Vertex3(xp[0], yp[0], zp[1]);
					GL.Vertex3(xp[1], yp[0], zp[0]); GL.Vertex3(xp[1], yp[0], zp[1]);
					GL.Vertex3(xp[1], yp[1], zp[0]); GL.Vertex3(xp[1], yp[1], zp[1]);
					GL.Vertex3(xp[0], yp[1], zp[0]); GL.Vertex3(xp[0], yp[1], zp[1]);
				}
				if (subInstanceBrushes != null)
				{
					foreach (var node in subInstanceBrushes)
					{
						if (node.Bounds == null)
							continue;
						var bounds = node.Bounds;
						var offset = node.Translation;
						var xp = new float[] { bounds.MinX + offset.X, bounds.MaxX + offset.X };
						var yp = new float[] { bounds.MinY + offset.Y, bounds.MaxY + offset.Y };
						var zp = new float[] { bounds.MinZ + offset.Z, bounds.MaxZ + offset.Z };

						GL.Vertex3(xp[0], yp[0], zp[0]); GL.Vertex3(xp[1], yp[0], zp[0]);
						GL.Vertex3(xp[1], yp[0], zp[0]); GL.Vertex3(xp[1], yp[1], zp[0]);
						GL.Vertex3(xp[1], yp[1], zp[0]); GL.Vertex3(xp[0], yp[1], zp[0]);
						GL.Vertex3(xp[0], yp[1], zp[0]); GL.Vertex3(xp[0], yp[0], zp[0]);

						GL.Vertex3(xp[0], yp[0], zp[1]); GL.Vertex3(xp[1], yp[0], zp[1]);
						GL.Vertex3(xp[1], yp[0], zp[1]); GL.Vertex3(xp[1], yp[1], zp[1]);
						GL.Vertex3(xp[1], yp[1], zp[1]); GL.Vertex3(xp[0], yp[1], zp[1]);
						GL.Vertex3(xp[0], yp[1], zp[1]); GL.Vertex3(xp[0], yp[0], zp[1]);

						GL.Vertex3(xp[0], yp[0], zp[0]); GL.Vertex3(xp[0], yp[0], zp[1]);
						GL.Vertex3(xp[1], yp[0], zp[0]); GL.Vertex3(xp[1], yp[0], zp[1]);
						GL.Vertex3(xp[1], yp[1], zp[0]); GL.Vertex3(xp[1], yp[1], zp[1]);
						GL.Vertex3(xp[0], yp[1], zp[0]); GL.Vertex3(xp[0], yp[1], zp[1]);
					}
				}
				GL.End();
			}

			GL.PopMatrix( );
			SwapBuffers( );
		}
		#endregion
		
		#region OnUpdateFrame
		double lastTime = 0;
		protected override void OnUpdateFrame( FrameEventArgs e )
		{
			if (!frameTimer.IsRunning)
			{
				frameTimer.Start();
				lastTime = frameTimer.ElapsedMilliseconds / 1000.0;
			}
			double curtime = (frameTimer.ElapsedMilliseconds / 1000.0);
			double time = curtime - lastTime;
			lastTime = curtime;

			UpdateInput();

			#region Translate & Rotate Camera
			Matrix4 invTransformation = Matrix4.Mult(
											Matrix4.CreateRotationX(-cameraPitch),
											Matrix4.CreateRotationY(-cameraYaw));

			Vector3 source = Vector3.Zero;
			if (goForward)	source = Vector3.Add(source, -Vector3.UnitZ);
			if (goBackward)	source = Vector3.Add(source,  Vector3.UnitZ);
			if (goLeft)		source = Vector3.Add(source, -Vector3.UnitX);
			if (goRight)	source = Vector3.Add(source,  Vector3.UnitX);

			if (goFast) Vector3.Multiply(ref source, (float)(1600.0f * time), out source);
			else		Vector3.Multiply(ref source, (float)(250f * time), out source);
				
			Vector3 result;
			Vector3.TransformPosition(ref source, ref invTransformation, out result);
			cameraPosition = Vector3.Add(cameraPosition, result);
			#endregion

			AnimateInstanceNodes(curtime);
		}
		#endregion


		#region Input ..

		bool	pressingT			= false;
		bool	pressingB			= false;
		Point	lastMousePosition	= Point.Empty;

		#region UpdateInput
		void UpdateInput()
		{
			if ( Keyboard[Key.Escape] )
				Exit();

			goForward	= Keyboard[Key.W];
			goBackward	= Keyboard[Key.S];
			goLeft		= Keyboard[Key.A];
			goRight		= Keyboard[Key.D];
			goFast		= Keyboard[Key.ShiftLeft] || Keyboard[Key.ShiftRight];

			if (!Keyboard[Key.T])
			{
				if (pressingT)
					showTriangles = !showTriangles;
				pressingT = false;
			} else
				pressingT = true;

			if (!Keyboard[Key.B])
			{
				if (pressingB)
					showBounds = !showBounds;
				pressingB = false;
			} else
				pressingB = true;
			
			if (cameraRotating)
			{
				cameraYaw	 += (Mouse.X - lastMousePosition.X) / 80.0f;
				cameraPitch += (Mouse.Y - lastMousePosition.Y) / 80.0f;
				lastMousePosition.X = Mouse.X;
				lastMousePosition.Y = Mouse.Y;
			}
		}
		#endregion

		#region OnButtonUp
		void OnButtonUp(object sender, MouseButtonEventArgs e)
		{
			cameraRotating = false;
		}
		#endregion

		#region OnButtonDown
		void OnButtonDown(object sender, MouseButtonEventArgs e)
		{
			lastMousePosition = e.Position;
			cameraRotating = true;
		}
		#endregion

		#endregion


		#region 'Main' - Application entry point
		[STAThread]
		public static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			using (MainWindow game = new MainWindow())
			{
				game.Run();
			}
		}
		#endregion
	}
}