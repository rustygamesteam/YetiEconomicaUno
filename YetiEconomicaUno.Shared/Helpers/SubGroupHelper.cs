using System.Collections.Generic;
using System.Linq;
using RustyDTO;
using RustyDTO.DescPropertyModels;
using YetiEconomicaCore.Services;

namespace YetiEconomicaUno.Helpers;

public static class SubGroupHelper
{
    private class SubGroupInfo
    {
        public SubGroupInfo(string group)
        {
            Group = group;
            Count = 1;
        }

        public string Group { get; set; }
        public int Count { get; set; }
    }

    private static Dictionary<RustyEntityType, List<SubGroupInfo>> _subGroupsByType = new();

    static SubGroupHelper()
    {
        foreach (var rustyEntity in RustyEntityService.Instance.AllEntites())
        {
            if (!rustyEntity.TryGetProperty(DescPropertyType.SubGroup, out ISubGroup subGroup) || string.IsNullOrWhiteSpace(subGroup.Group))
                continue;


            var type = rustyEntity.Type;
            if (!_subGroupsByType.TryGetValue(type, out var list))
                _subGroupsByType[type] = list = new List<SubGroupInfo>();

            var result = list.FirstOrDefault(info => info.Group == subGroup.Group);
            if(result is null)
                list.Add(new SubGroupInfo(subGroup.Group));
            else
                result.Count++;
        }
    }


    public static IEnumerable<string> ResolveByType(RustyEntityType type)
    {
        if (!_subGroupsByType.TryGetValue(type, out var list))
            return Enumerable.Empty<string>();

        return list.Select(static info => info.Group);
    }

    public static void Update(RustyEntityType type, string oldValue, string newValue)
    {
        if (!_subGroupsByType.TryGetValue(type, out var list))
        {
            list = new();
            _subGroupsByType[type] = list;
        }

        var result = list.FirstOrDefault(info => info.Group == oldValue);
        if (result != null)
        {
            result.Count--;
            if (result.Count == 0)
                list.Remove(result);
        }
        result = list.FirstOrDefault(info => info.Group == newValue);
        if (result == null)
            list.Add(new SubGroupInfo(newValue));
        else
            result.Count++;
    }
}