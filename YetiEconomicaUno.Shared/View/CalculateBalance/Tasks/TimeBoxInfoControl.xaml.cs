using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using YetiEconomicaCore;
using YetiEconomicaCore.Database;
using static YetiEconomicaUno.ViewModels.CalculateBalance.Progress.ProgressTask;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using DependencyPropertyGenerator;
using YetiEconomicaUno.Converters;
using YetiEconomicaUno.ViewModels.CalculateBalance.Progress;
using YetiEconomicaCore.Services;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.View.CalculateBalance.Tasks;

[DependencyProperty<ObservableCollection<StatisticLine>>("Source")]
public sealed partial class TimeBoxInfoControl : UserControl, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    //private static Dictionary<Resource, double> _tmpDict = new();
    //private static List<ResourceStackRecord> _tmpList = new();
    private static readonly StringBuilder SB = new(64);

    public enum LineType
    {
        Duration,
        Int
    }

    private CompositeDisposable _disposable;

    public TimeBoxInfoControl()
    {
        this.InitializeComponent();
        _disposable = new CompositeDisposable();

        Unloaded += OnUnloaded;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _disposable ??= new CompositeDisposable();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _disposable?.Dispose();
        _disposable = null;
    }

    partial void OnSourceChanged(ObservableCollection<StatisticLine> newValue)
    {
        SourceList.ItemsSource = GetSourceInfo();

        Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                handler => newValue.CollectionChanged += handler,
                handler => newValue.CollectionChanged -= handler)
            .Subscribe(pattern => SourceList.ItemsSource = GetSourceInfo()).DisposeWith(_disposable);
    }

    public IEnumerable<string> GetSourceInfo()
    {
        Visibility = Source == null || Source.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        if (Source == null)
            yield break;

        foreach (var line in Source)
        {
            var info = line.Type switch
            {
                StatisticInfo.Process => ("Time to process", LineType.Duration, true),
                StatisticInfo.Idle => ("Idle time", LineType.Duration, true),
                StatisticInfo.CollectResources => ("Collect resources", LineType.Duration, false),
                StatisticInfo.PreCraft => ("Craft resources for receipt", LineType.Duration, false),
                StatisticInfo.FarmPlants => ("Growing plants", LineType.Duration, false),
                StatisticInfo.FarmCycles => ("Growing cycles", LineType.Int, false),
                _ => throw new NotImplementedException()
            };
            yield return GenerateLine(info.Item1, line.Value, info.Item2, info.Item3);
        }
    }

    /*private static void FillClearResources(Dictionary<Resource, double> dict, ResourceStackRecord resourceExchange, double circles)
    {
        var craft = CraftsService.Instance.Crafts[resourceExchange.Resource.Index];

        dict.TryGetValue(resourceExchange.Resource, out var count);
        count += resourceExchange.Value * circles;
        dict[resourceExchange.Resource] = count;

        if (craft != null)
        {
            circles *= resourceExchange.Value / craft.Count;

            var subResources = craft.GetPrice.Resources;
            foreach (var subResource in subResources)
                FillClearResources(dict, subResource, circles);
        }
    }*/

    private static string GenerateLine(string line, int value, LineType type, bool isPrimary)
    {
        SB.Length = 0;
        if (!isPrimary)
            SB.Append("    ");
        SB.Append("• ");
        SB.Append(line);
        SB.Append(": ");

        switch (type)
        {
            case LineType.Duration:
                SB.Append(DurationLabelConverter.GetDuration(value));
                break;
            case LineType.Int:
                SB.Append(value);
                break;
        }

        return SB.ToString();
    }

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}