namespace PowerLib.System.Collections
{
	public interface IHierarchicalNode<TNode>
		where TNode : IHierarchicalNode<TNode>
	{
		TNode? Parent { get; }
	}
}
