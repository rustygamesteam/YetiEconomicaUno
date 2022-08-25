using System;
using System.Collections.Generic;
using System.Linq;
using RustyDTO;
using RustyDTO.PropertyModels;
using YetiEconomicaCore.Services;
using YetiEconomicaCore.Database;
using YetiEconomicaUno.Helpers;
using RustyDTO.Interfaces;

namespace YetiEconomicaUno.ViewModels.CalculateBalance.Internal;
public enum ResourceNodeType
{
    Simple = 2,
    Craftable = 0,
    Farm = 3,
    Convert = 1,
    Invalid = -1
}

internal struct ResourceDependenciesTree
{
    public record struct Node(int Depth, ResourceNodeType Type, ResourceStackRecord Current, IEnumerable<Node> Childs);

    public class WorkerNode
    {
        public WorkerNode(Node node)
        {
            Resource = node.Current.Resource;
            Count = node.Current.Value;
            if (Count > 10000000)
                Count = 1;

            Type = node.Type;

            Childs = node.Childs;
        }

        public IRustyEntity Resource { get; }
        public double Count { get; set; }

        public ResourceNodeType Type { get; }

        public IEnumerable<Node> Childs { get; private set; }

        public bool IsComplete { get; private set; }

        public void Complete()
        {
            IsComplete = true;
        }

        public void Attach(Node node)
        {
            Count += node.Current.Value;
            Childs = Childs.Concat(node.Childs);
        }
    }

    public IEnumerable<Node> Nodes;

    private ResourceDependenciesTree(IEnumerable<Node> nodes)
    {
        Nodes = nodes;
    }

    public static ResourceDependenciesTree Build(IEnumerable<ResourceStackRecord> price)
    {
        return new ResourceDependenciesTree(ConvetToNodes(-1, price));
    }

    private static IEnumerable<Node> ConvetToNodes(int depth, IEnumerable<ResourceStackRecord> resourceExchanges)
    {
        foreach (var resourceExchange in resourceExchanges)
            yield return ConvetToNode(depth, resourceExchange);
    }

    private static IEnumerable<Node> ConvetToNodes(int depth, IEnumerable<ResourceStack> resourceExchanges)
    {
        foreach (var resourceExchange in resourceExchanges)
            yield return ConvetToNode(depth, resourceExchange);
    }

    public static Node ConvetToNode(int depth, ResourceStackRecord resourceExchange)
    {
        depth++;

        if (SimpleResources.Instance.IsSimple(resourceExchange.Resource))
            return new Node(depth, ResourceNodeType.Simple, resourceExchange, Enumerable.Empty<Node>());
        else if (PlantsService.Instance.IsPlant(resourceExchange.Resource))
            return new Node(depth, ResourceNodeType.Farm, resourceExchange, Enumerable.Empty<Node>());
        else if (CraftService.Instance.TryGetCraft(resourceExchange.Resource, out var craft))
            return new Node(depth, ResourceNodeType.Craftable, resourceExchange, ConvetToNodes(depth, craft.GetUnsafe<IPayable>().Price));
        else if (ConvertablesService.Instance.TryGetFirstExchnage(resourceExchange.Resource, out var convert))
            return new Node(depth, ResourceNodeType.Convert, resourceExchange, ToEnuberable(ConvetToNode(depth, new ResourceStack(convert.Resource, resourceExchange.Value * Math.Max(convert.Value, 0.001)))));
        else
            return new Node(depth, ResourceNodeType.Invalid, resourceExchange, Enumerable.Empty<Node>());
    }

    private static IEnumerable<Node> ToEnuberable(Node node)
    {
        yield return node;
    }

    public IEnumerator<Node> GetEnumerator()
    {
        foreach (Node node in Nodes)
        {
            var enumerator = NodesEnumerator(node);
            while (enumerator.MoveNext())
                yield return enumerator.Current;
        }
    }

    internal IEnumerable<Node> GetEnumerable()
    {
        foreach (var node in this)
            yield return node;
    }

    private static IEnumerator<Node> NodesEnumerator(Node node)
    {
        yield return node;
        foreach (var child in node.Childs)
        {
            var enumerator = NodesEnumerator(child);
            while (enumerator.MoveNext())
                yield return enumerator.Current;
        }
    }
    private static Dictionary<IRustyEntity, WorkerNode> MergeHelper = new();

    internal static IEnumerable<WorkerNode> Merge(IEnumerable<Node> nodes)
    {
        MergeHelper.Clear();
        foreach (var node in nodes)
        {
            if (MergeHelper.TryGetValue(node.Current.Resource, out var group))
                group.Attach(node);
            else
                MergeHelper[node.Current.Resource] = new WorkerNode(node);
        }

        return MergeHelper.Select(pair => pair.Value);
    }
}
