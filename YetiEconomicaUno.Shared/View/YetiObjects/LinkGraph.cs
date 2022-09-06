using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Nito.Comparers.Linq;
using RustyDTO;
using RustyDTO.DescPropertyModels;
using YetiEconomicaCore.Services;
using RustyDTO.Interfaces;
using YetiEconomicaCore;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.View.YetiObjects
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public static class LinkGraph
    {
        private static readonly HashSet<int> NodeIds = new();
        private static readonly Dictionary<int, JsonObject> Nodes = new();
        private static readonly Dictionary<int, List<JsonObject>> BuildInfos = new();

        private static readonly Dictionary<RustyEntityType, string> Types = new Dictionary<RustyEntityType, string>
        {
            { RustyEntityType.Build, "build" },
            { RustyEntityType.CraftTask, "craft" },
            { RustyEntityType.Tech, "tech" },
            { RustyEntityType.Tool, "tool" },
            { RustyEntityType.PVE, "pve" },
        };

        private static JsonObject ZeroPosition
        {
            get => new JsonObject
            {
                {"x", 0},
                {"y", 0}
            };
        }

        public static void CreateByProduct()
        {
            Nodes.Clear();
            NodeIds.Clear();
            BuildInfos.Clear();

            var links = new Dictionary<int, List<EntityGroup>>();

            var entityService = RustyEntityService.Instance;
            FillBuildsByGroups(links, entityService.AllEntites());

            var nodes = new JsonArray();
            var edges = new JsonArray();

            AttachBuilds(links, nodes, edges);

            var mask = stackalloc[] { RustyEntityType.PVE, RustyEntityType.Tool }.ToBitmask();
            foreach (var entity in entityService.EntitesWhereTypes(mask))
            {
                if (!TryAttachNode(entity, nodes))
                    continue;

                TryResolveDependents(entity, edges);
            }

            mask = stackalloc[] { RustyEntityType.CraftTask, RustyEntityType.Tech }.ToBitmask();
            foreach (var entity in entityService.EntitesWhereTypes(mask))
            {
                if (!entity.TryGetProperty(out IInBuildProcess inBuild) || inBuild.Build is null)
                    continue;

                if (!BuildInfos.TryGetValue(inBuild.Build.GetIndex(), out var list))
                    continue;
                
                Func<JsonObject, JsonArray> resolveArray;
                string type;

                switch (entity.Type)
                {
                    case RustyEntityType.CraftTask:
                        type = "craft";
                        resolveArray = data =>
                        {
                            if (!data.ContainsKey("crafts"))
                                data.Add("crafts", new JsonArray());
                            return data["crafts"].AsArray();
                        };
                        break;
                    case RustyEntityType.Tech:
                        type = "tech";
                        resolveArray = data =>
                        {
                            if (!data.ContainsKey("techs"))
                                data.Add("techs", new JsonArray());
                            return data["techs"].AsArray();
                        };
                        break;
                    default:
                        continue;
                }

                foreach (var node in list)
                {
                    var array = resolveArray.Invoke(node["data"].AsObject());
                    array.Add(new JsonObject
                    {
                        { "id", entity.GetIndex().ToString(NumberFormatInfo.InvariantInfo) },
                        { "name", entity.FullName },
                        { "icon", type }
                    });
                }
                
            }

            var json = new JsonObject
            {
                {"nodes", nodes},
                {"edges", edges},
            }.ToJsonString(new JsonSerializerOptions(JsonSerializerDefaults.Web));

            OpenGraph(json);
        }

        public static void CreateByDependents()
        {
            Nodes.Clear();
            NodeIds.Clear();
            BuildInfos.Clear();

            var links = new Dictionary<int, List<EntityGroup>>();

            var entityService = RustyEntityService.Instance;
            FillBuildsByGroups(links, entityService.AllEntites());

            var nodes = new JsonArray();
            var edges = new JsonArray();

            AttachBuilds(links, nodes, edges);

            var mask = stackalloc[] {RustyEntityType.CraftTask, RustyEntityType.Tech, RustyEntityType.Tool, RustyEntityType.PVE}
                .ToBitmask();
            foreach (var entity in entityService.EntitesWhereTypes(mask))
            {
                if (!TryAttachNode(entity, nodes)) 
                    continue;

                TryResolveDependents(entity, edges);
            }

            var json = new JsonObject
            {
                {"nodes", nodes},
                {"edges", edges},
            }.ToJsonString(new JsonSerializerOptions(JsonSerializerDefaults.Web));

            OpenGraph(json);
        }

        private static void TryResolveDependents(IRustyEntity entity, JsonArray edges)
        {
            if(!entity.TryGetProperty(out IHasDependents dependents))
                return;

            if (dependents.Required is not null)
                edges.Add(CreateEdge(dependents.Required.GetIndex(), entity.GetIndex(), GroupType.required));

            if (dependents.VisibleAfter is not null)
                edges.Add(CreateEdge(dependents.VisibleAfter.GetIndex(), entity.GetIndex(), GroupType.visible));
        }

        private static bool TryAttachNode(IRustyEntity entity, JsonArray nodes)
        {
            if (!Types.TryGetValue(entity.Type, out var type))
                return false;

            if (TryCreateNode(entity, type, out var result))
            {
                Nodes[entity.GetIndex()] = result;
                if (entity.Type is RustyEntityType.Build)
                {
                    var owner = entity.GetDescUnsafe<IHasOwner>().Owner;
                    if (!BuildInfos.TryGetValue(owner.GetIndex(), out var list))
                        BuildInfos[owner.GetIndex()] = list = new List<JsonObject>();
                    list.Add(result);
                }

                nodes.Add(result);
            }
            return true;
        }

        private static bool TryCreateNode(IRustyEntity entity, string type, out JsonObject node)
        {
            if (NodeIds.Contains(entity.GetIndex()))
            {
                node = null;
                return false;
            }
            NodeIds.Add(entity.GetIndex());

            var data = new JsonObject
            {

                {"type", type},
                {"name", entity.FullName}
            };

            node = new JsonObject
            {
                {"id", entity.GetIndex().ToString(NumberFormatInfo.InvariantInfo)},
                {"data", data},
                {"type", "entityNode"},
                {"position", ZeroPosition}
            };

            return true;
        }

        private static void AttachBuilds(Dictionary<int, List<EntityGroup>> links, JsonArray nodes, JsonArray edges)
        {
            foreach (var group in links.Values.SelectMany(static list => list))
            {
                if (!TryAttachNode(group.Owner, nodes))
                    continue;

                foreach (var child in group.Child)
                    edges.Add(CreateEdge(group.Owner.GetIndex(), child.entity.GetIndex(), child.type));
            }
        }


        private static void FillBuildsByGroups(Dictionary<int, List<EntityGroup>> data, IEnumerable<IRustyEntity> all)
        {
            const int buildIndex = (int)RustyEntityType.Build;
            var buildsByDepth = all.Where(static entity => entity.TypeAsIndex == buildIndex).GroupBy(GetDepth).OrderBy(entities => entities.Key);
            foreach (var builds in buildsByDepth)
            {
                foreach (var rustyEntity in builds)
                {
                    var ownerGroup = builds.Key == 0 ? null : data[builds.Key - 1].FirstOrDefault(entityGroup => IsOwner(entityGroup, rustyEntity));
                    var group = new EntityGroup(ownerGroup, rustyEntity);
                    if (!data.TryGetValue(builds.Key, out var link))
                    {
                        link = new List<EntityGroup>();
                        data[builds.Key] = link;
                    }

                    link.Add(group);
                }
            }
        }

        private static JsonObject CreateEdge(int from, int to, GroupType type)
        {
            return new JsonObject
            {
                {"id", $"e{from}-{to}"},
                {"source", from.ToString(NumberFormatInfo.InvariantInfo)},
                {"target", to.ToString(NumberFormatInfo.InvariantInfo)},
                {"sourceHandle", type.ToString()}
            };
        }

        private static void OpenGraph(string data)
        {
            File.WriteAllText("map.json", data);
            Process.Start(new ProcessStartInfo("graph.exe", "map.json"));
        }

        private static bool IsOwner(EntityGroup group, IRustyEntity entity)
        {
            var dependents = entity.GetDescUnsafe<IHasDependents>();
            return dependents.Required == group.Owner || dependents.VisibleAfter == group.Owner;
        }

        private static int GetDepth(IRustyEntity entity)
        {
            return GetInternalDepth(entity, 0);
        }

        private static int GetInternalDepth(IRustyEntity entity, int depth)
        {
            if (entity == null)
                return depth - 1;

            if (entity.TryGetProperty(out IHasDependents dependents))
                return Math.Max(GetInternalDepth(dependents.Required, depth), GetInternalDepth(dependents.VisibleAfter, depth)) + 1;

            return depth - 1;
        }

        enum GroupType
        {
            none,
            visible,
            required
        }

        private record struct Link(IRustyEntity entity, GroupType type);

        private class EntityGroup
        {
            public EntityGroup(EntityGroup owner, IRustyEntity entity)
            {
                Depth = owner == null ? 0 : owner.Depth + 1;
                Owner = entity;
                Child = new List<Link>();

                if (owner != null)
                {
                    var dependents = entity.GetDescUnsafe<IHasDependents>();
                    var type = dependents.Required == owner.Owner ? GroupType.required : GroupType.visible;
                    owner.Child.Add(new Link(entity, type));
                }
            }


            public IRustyEntity Owner { get; }
            public int Depth { get; }
            public List<Link> Child { get; }
        }
    }
}
