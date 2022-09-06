using System.Collections.Generic;
using System.Linq;
using ReactiveUI;
using RustyDTO;
using RustyDTO.DescPropertyModels;
using RustyDTO.Interfaces;
using YetiEconomicaCore.Services;
using YetiEconomicaUno.Converters;
using YetiEconomicaUno.Models.Resources;

namespace YetiEconomicaUno.ViewModels
{
    public class ResourceViewModel : ReactiveObject
    {
        public IRustyEntity Model { get; }

        public string IndexLabel => $"Index: {Model.ID}";

        public IRustyEntity Group
        {
            get => Model.GetDescUnsafe<IHasOwner>().Owner;
            set => RustyEntityService.Instance.ReplaceOwner(Model, value);
        }

        public int Tear
        {
            get => Model.GetDescUnsafe<IHasOwner>().Tear;
            set => Model.GetDescUnsafe<IHasOwner>().Tear = value;
        }

        public int UseInBuildsCount { get; }
        public int UseInCraftsCount { get; }
        public int UseInConvertable { get; }

        public IList<ResourceStatistics> Statistics { get; }

        public ResourceViewModel(IRustyEntity value)
        {
            Model = value;

            var service = RustyEntityService.Instance;

            Statistics = new List<ResourceStatistics>();
            var exchangeTo = ConvertablesService.Instance.GetConvertibleToResource(value).ToArray();
            var exchangeFrom = ConvertablesService.Instance.GetConvertibleFromResource(value);
            UseInConvertable = exchangeTo.Length;

            int useInBuildsCount = 0;
            var useInCrafts = new List<string>();
            var useInSingleCreated = new List<string>();
            var priceOwners = service.GetPriceOwnersWithResource(value);
            

            foreach (var priceOwner in priceOwners)
            {
                if (priceOwner.Type is RustyEntityType.CraftTask)
                    useInCrafts.Add($"• {priceOwner.GetDescUnsafe<IHasSingleReward>().Entity.FullName}");
                else
                    useInSingleCreated.Add($"• {priceOwner.FullName}");
            }

            UseInBuildsCount = useInBuildsCount;
            UseInCraftsCount = useInCrafts.Count;

            int craftCount = 0;
            int craftDuration = 0;
            IEnumerable<string> craftPrice;
            if (CraftService.Instance.TryGetCraft(value, out var craft))
            {
                craftCount = craft.GetDescUnsafe<IHasSingleReward>().Count;
                craftDuration = craft.GetDescUnsafe<ILongExecution>().Duration;
                craftPrice = craft.GetDescUnsafe<IPayable>().Price.Select(static stack => $"• {stack.Resource.FullName}: {stack.Value:0.##}");
            }
            else
                craftPrice = Enumerable.Empty<string>();

            Statistics = new List<ResourceStatistics>
            {
                new ResourceStatistics("Exchange for 1 unit:", exchangeTo),
                new ResourceStatistics("Exchange 1 unit to:", exchangeFrom),
                new ResourceStatistics($"Craft {craftCount} units by {DurationLabelConverter.GetDuration(craftDuration)} from:", craftPrice),
                new ResourceStatistics($"Usage in {UseInCraftsCount} crafts:", useInCrafts),
                new ResourceStatistics($"Usage in {useInSingleCreated.Count} single created entities:", useInSingleCreated.OrderBy(text => text)),
            };

            for (int i = Statistics.Count - 1; i >= 0; i--)
            {
                if (!Statistics[i].IsValid)
                    Statistics.RemoveAt(i);
            }
        }
    }
}
