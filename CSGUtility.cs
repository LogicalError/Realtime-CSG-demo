using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using System.Threading.Tasks;
using CSGDemo.CSGXML;
using CSGDemo.Geometry;
using CSGDemo.CSGHierarchy;
using OpenTK;

namespace CSGDemo
{
	public static class CSGUtility
	{
		#region FindChildNodes
		public static IEnumerable<CSGNode> FindChildNodes(CSGNode node)
		{
			yield return node;
			if (node.NodeType != CSGNodeType.Brush)
			{
				foreach (var child in FindChildNodes(node.Left))
					yield return child;
				foreach (var child in FindChildNodes(node.Right))
					yield return child;
			}
		}
		#endregion

		#region FindChildBrushes
		public static IEnumerable<CSGNode> FindChildBrushes(CSGNode node)
		{
			if (node.NodeType != CSGNodeType.Brush)
			{
				foreach (var brush in FindChildBrushes(node.Left))
					yield return brush;
				foreach (var brush in FindChildBrushes(node.Right))
					yield return brush;
				yield break;
			}
			else
				yield return node;
		}

		public static IEnumerable<CSGNode> FindChildBrushes(CSGTree tree)
		{
			return FindChildBrushes(tree.RootNode);
		}
		#endregion

		#region UpdateChildTransformations
		public static void UpdateChildTransformations(CSGNode node, Vector3 parentTranslation)
		{
			node.Translation = Vector3.Add(parentTranslation, node.LocalTranslation);
			if (node.NodeType == CSGNodeType.Brush)
				return;
			UpdateChildTransformations(node.Left, node.Translation);
			UpdateChildTransformations(node.Right, node.Translation);
		}
		public static void UpdateChildTransformations(CSGNode node)
		{
			if (node.NodeType == CSGNodeType.Brush)
				return;
			UpdateChildTransformations(node.Left, node.Translation);
			UpdateChildTransformations(node.Right, node.Translation);
		}
		#endregion

		#region UpdateBounds
		public static void UpdateBounds(CSGNode node)
		{
			if (node.NodeType != CSGNodeType.Brush)
			{
				var leftNode = node.Left;
				var rightNode = node.Right;
				UpdateBounds(leftNode);
				UpdateBounds(rightNode);

				node.Bounds.Clear();
				node.Bounds.Add(leftNode.Bounds.Translated(Vector3.Subtract(leftNode.Translation, node.Translation)));
				node.Bounds.Add(rightNode.Bounds.Translated(Vector3.Subtract(rightNode.Translation, node.Translation)));
			}
		}
		#endregion

		#region RemoveInstances (private)
		static XmlNode RemoveInstances(XmlNode node, Dictionary<string, XmlNode> lookup)
		{
			if (node == null)
				return null;
			var instance = node as XmlInstance;
			if (instance != null)
			{
				if (string.IsNullOrWhiteSpace(instance.InstanceID))
					return null;

				XmlNode lookupNode;
				if (!lookup.TryGetValue(instance.InstanceID, out lookupNode))
					return null;

				lookupNode = lookupNode.Clone();
				lookupNode.ID = instance.ID;
				if (lookupNode == null)
					return null;

				var result = RemoveInstances(lookupNode, lookup);
				if (result == null)
					return null;

				result.ID = instance.ID;
				result.Translation = Vector3.Add(instance.Translation, result.Translation);
				result.Animate = result.Animate || instance.Animate;
				return result;
			}
			var branch = node as XmlBranch;
			if (branch != null)
			{
				branch.Left = RemoveInstances(branch.Left, lookup);
				branch.Right = RemoveInstances(branch.Right, lookup);
				if (branch.Left == null ||
					branch.Right == null)
					return null;
			}
			return node;
		}
		#endregion


		#region LoadTree
		static XmlSerializer importSerializer = new XmlSerializer(typeof(XmlCSGFile));
		public static CSGTree LoadTree(string filename, out List<CSGNode> animatedNodes)
		{
			using (var myReadStream = File.OpenRead(filename))
			{
				var csgFile = importSerializer.Deserialize(myReadStream) as XmlCSGFile;

				var lookup = new Dictionary<string, XmlNode>();
				foreach (var instance in csgFile.Instances.Nodes)
					lookup.Add(instance.ID, instance);

				csgFile.XmlTree.RootNode = RemoveInstances(csgFile.XmlTree.RootNode, lookup);
				if (csgFile.XmlTree.RootNode == null)
				{
					animatedNodes = null;
					return null;
				}

				HashSet<CSGNode> animatedNodesHash;
				var csgTree = csgFile.XmlTree.ToCSGTree(out animatedNodesHash);
				animatedNodes = animatedNodesHash.ToList();
				return csgTree;
			}
		}
		#endregion
	}
}