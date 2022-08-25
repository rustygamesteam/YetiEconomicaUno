
namespace YetiEconomicaUno.ViewModels.Crafts;

/*
public record CraftViewModel(Craft Craft) : ReactiveRecord, IDisposable
{
    private ObservableAsPropertyHelper<string> _durationLabel;
    private ObservableAsPropertyHelper<string> _craftCountLabel;
    private ObservableAsPropertyHelper<string> _resourcesToCraftingLabel;

    public string DurationLabel => _durationLabel.Value;
    public string CraftCountLabel => _craftCountLabel.Value;
    public string ResourcesToCraftingLabel => _resourcesToCraftingLabel.Value;

    private bool _isInitialize;
    private CompositeDisposable _disposables;

    public void Intialize()
    {
        if (_isInitialize)
            return;
        _isInitialize = true;

        var disposed = Interlocked.Exchange(ref _disposables, new CompositeDisposable());
        disposed?.Dispose();

        Craft.WhenAnyValue(static craft => craft.Duration)
            .Select(static duration => $"Duration crafting: {DurationLabelConverter.GetDuration(duration)}")
            .ToProperty(this, static vm => vm.DurationLabel, out _durationLabel)
            .DisposeWith(_disposables);

        Craft.WhenAnyValue(static craft => craft.Count)
            .Select(static value => $"Craft count: {value}")
            .ToProperty(this, static vm => vm.CraftCountLabel, out _craftCountLabel)
            .DisposeWith(_disposables);

        Craft.WhenAnyValue(static craft => craft.Price.Count)
            .Select(static value => $"Resources for crafting: {value}")
            .ToProperty(this, static vm => vm.ResourcesToCraftingLabel, out _resourcesToCraftingLabel)
            .DisposeWith(_disposables);
    }

    public void Remove_OnClicked()
    {
        CraftsService.Instance.Remove(Craft);
    }

    public void Dispose()
    {
        _isInitialize = false;
        var disposed = Interlocked.Exchange(ref _disposables, null);
        disposed?.Dispose();
    }
}
*/