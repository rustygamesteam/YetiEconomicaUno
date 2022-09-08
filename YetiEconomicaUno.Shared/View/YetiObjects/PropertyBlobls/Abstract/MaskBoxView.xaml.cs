using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using Windows.UI.Xaml.Interop;
using DependencyPropertyGenerator;
using DynamicData.Binding;
using Microsoft.UI.Xaml.Controls;
using ReactiveUI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.View.YetiObjects;

[DependencyProperty<string>("Header")]
[DependencyProperty<object>("Value")]
[DependencyProperty<int>("CountValuesFullString", DefaultValue = 3)]
public sealed partial class MaskBoxView : UserControl, IActivatableView
{
    private Type _enumType;
    private List<EnumItem> _enumItems = new();

    private ObservableCollectionExtended<EnumItem> _selectedValues = new();
    public ReadOnlyObservableCollection<EnumItem> SelectedValues { get; }

    private static StringBuilder _sb = new StringBuilder(64);

    public MaskBoxView()
    {
        _selectedValues.Clear();
        SelectedValues = new (_selectedValues);

        this.InitializeComponent();

        if (EnumComboBox.SelectedNodes is INotifyCollectionChanged notifyCollectionChanged)
            notifyCollectionChanged.CollectionChanged += NotifyCollectionChangedOnCollectionChanged;


        this.WhenActivated(disposable =>
        {
            this.WhenAnyValue(static view => view.Value)
                .WhereNotNull()
                .FirstAsync()
                .Subscribe(value =>
                {
                    var enumType = value.GetType();
                    var values = Enum.GetValues(enumType);
                    _enumType = enumType;

                    var itemsSource = Enumerable.Range(0, values.Length)
                        .Where(i => Convert.ToInt32(values.GetValue(i)) is not (0 or int.MaxValue))
                        .Select(i =>
                    {
                        var value = values.GetValue(i);
                        return new EnumItem(value.ToString(), Convert.ToInt32(value), value);
                    });

                    _selectedValues.Clear();
                    _enumItems.AddRange(itemsSource);

                    EnumComboBox.ItemsSource = _enumItems;
                })
                .DisposeWith(disposable);
        });
    }

    private void NotifyCollectionChangedOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        using var suspend = _selectedValues.SuspendNotifications();
        _selectedValues.Clear();
        int result = 0;
        foreach (var node in EnumComboBox.SelectedNodes)
        {
            if (node.Content is EnumItem item)
            {
                result |= item.ValueAsInt;
                _selectedValues.Add(item);
            }
        }

        if(Convert.ToInt32(Value) != result)
            Value = Enum.ToObject(_enumType, result);
    }

    public string SelectedValuesAsString()
    {
        var maxItems = CountValuesFullString;

        if (_selectedValues.Count == 0)
            return "None";
        else if (_selectedValues.Count >= _enumItems.Count)
            return "All";
        else if (_selectedValues.Count > maxItems)
            return "Mixed";

        var count = 0;
        _sb.Clear();
        foreach (var item in _selectedValues)
        {
            count++;
            if (maxItems < count)
                break;

            if (_sb.Length > 0)
                _sb.Append(", ");
            _sb.Append(item.Label);
        }

        return _sb.ToString();
    }
}

public record struct EnumItem(string Label, int ValueAsInt, object Value);