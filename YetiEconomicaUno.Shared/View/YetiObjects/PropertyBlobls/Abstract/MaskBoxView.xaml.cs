using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using DependencyPropertyGenerator;
using DynamicData.Binding;
using Microsoft.UI.Xaml.Controls;
using ReactiveUI;
using DynamicData;
using static YetiEconomicaUno.ViewModels.CalculateBalance.Internal.ResourceDependenciesTree;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.View.YetiObjects;

[DependencyProperty<string>("Header")]
[DependencyProperty<object>("Value")]
[DependencyProperty<int>("CountValuesFullString", DefaultValue = 5)]
public sealed partial class MaskBoxView : UserControl, IActivatableView
{
    private readonly EnumCacheInfo _cacheInfo = new EnumCacheInfo();

    public bool IsInitialize => _cacheInfo.IsInitialize;
    public ReadOnlyObservableCollection<EnumItem> SelectedValues => _cacheInfo.SelectedValues;
    public int ResultCount => _cacheInfo.ResultCount;

    public MaskBoxView()
    {
        this.InitializeComponent();

        this.WhenActivated(disposable =>
        {
            TreeView treeView = EnumComboBox;

            disposable.Add(_cacheInfo);
            Disposable.Create(this, static view => view.InternalValueUpdate()).DisposeWith(disposable);

            this.WhenAnyValue(static view => view.Value)
                .WhereNotNull()
                .FirstAsync()
                .Subscribe(value =>
                {
                    var valueAsInt = Convert.ToInt32(value);
                    var enumType = value.GetType();
                    var values = Enum.GetValues(enumType);

                    InitializeCache(enumType, value);

                    treeView.RootNodes.Clear();
                    treeView.SelectedNodes.Clear();

                    foreach (var subValue in _cacheInfo.Items.OrderByDescending(subValue =>
                             {
                                 var value = subValue.ValueAsInt;
                                 return value < 0 ? int.MaxValue + value : value;
                             }))
                    {
                        var rootNodes = InternalParentSearch(treeView, subValue)?.Children ?? treeView.RootNodes;
                        var nextNode = new TreeViewNode
                        {
                            Content = subValue,
                            IsExpanded = true,
                        };
                        rootNodes.Insert(0, nextNode);
                    }

                    treeView.SelectedNodes.AddRange(CollectNodes(treeView.RootNodes).Where(node =>
                    {
                        var item = (EnumItem) node.Content;
                        return _cacheInfo.IsSelected(valueAsInt, item);
                    }));
                })
                .DisposeWith(disposable);
        });
    }

    public void InitializeCache(Type enumType, object value)
    {
        _cacheInfo.Intialize(enumType, value);
    }

    private static IEnumerable<TreeViewNode> CollectNodes(IList<TreeViewNode> nodes)
    {
        foreach (var node in nodes)
        {
            yield return node;
            foreach (var childNode in CollectNodes(node.Children))
                yield return childNode;
        }
    }

    private static TreeViewNode InternalParentSearch(TreeView treeView, EnumItem subEnum)
    {
        return InternalParentSearch(treeView.RootNodes, subEnum);
    }

    private static TreeViewNode InternalParentSearch(IList<TreeViewNode> nodes, EnumItem subEnum)
    {
        foreach (var node in nodes)
        {
            if (node.Content is EnumItem enumItem)
            {
                var value = enumItem.ValueAsInt;
                if ((subEnum.ValueAsInt & value) != 0)
                {
                    var result = InternalParentSearch(node.Children, subEnum);
                    return result is null ? node : result;
                }
            }
        }

        return null;
    }

    private void InternalValueUpdate()
    {
        var result = _cacheInfo.Update(EnumComboBox.SelectedNodes);

        if (Convert.ToInt32(Value) != result)
            Value = Enum.ToObject(_cacheInfo.Type, result);
    }

    public string SelectedValuesAsString()
    {
        return _cacheInfo.ToString(CountValuesFullString);
    }
}

public class EnumCacheInfo : IDisposable
{
    private Type _enumType;
    public Type Type => _enumType;

    private List<EnumItem> _enumItems = new();
    private static StringBuilder _sb = new StringBuilder(64);

    private ObservableCollectionExtended<EnumItem> _selectedValues = new();
    public ReadOnlyObservableCollection<EnumItem> SelectedValues { get; }
    public bool IsInitialize => _isIntialize;

    private int _resultAsInt;
    private int _resultCount;
    private bool _isIntialize = false;

    public IEnumerable<EnumItem> Items => _enumItems;
    public int ResultCount => _resultCount;

    public EnumCacheInfo()
    {
        _selectedValues.Clear();
        SelectedValues = new(_selectedValues);
    }

    public void Intialize(Type type, object value)
    {
        _isIntialize = true;

        _enumType = type;
        _resultAsInt = Convert.ToInt32(value);

        var values = Enum.GetValues(type);
        _enumItems.Clear();

        foreach (var enumValue in values)
        {
            var valueAsInt = Convert.ToInt32(enumValue);
            if(valueAsInt == 0)
                continue;
            
            _enumItems.Add(new EnumItem(enumValue));
        }

        using var suspend = _selectedValues.SuspendNotifications();
        _selectedValues.Clear();
        foreach (var node in _enumItems)
        {
            if (IsSelected(_resultAsInt, node))
                _selectedValues.Add(node);
        }
    }

    public bool IsSelected(int valueAsInt, EnumItem item)
    {
        if (item.IsComplex)
        {
            int condition = item.ValueAsInt;
            return condition < 0 ? valueAsInt == condition : valueAsInt >= condition && (valueAsInt & condition) != 0;
        }

        return (valueAsInt & item.ValueAsInt) != 0;
    }

    public int Update(IList<TreeViewNode> nodes)
    {
        using var suspend = _selectedValues.SuspendNotifications();
        _selectedValues.Clear();
        int result = 0;
        foreach (var node in nodes)
        {
            if (node.Content is EnumItem item)
            {
                result |= item.ValueAsInt;
                _selectedValues.Add(item);
            }
        }
        _resultAsInt = result;

        return result;
    }

    public int SelectedValuesAsInt()
    {
        int result = 0;
        foreach (var node in SelectedValues)
            result |= node.ValueAsInt;

        return result;
    }

    public string ToString(int maxItems)
    {
        _resultCount = 1;
        if (_selectedValues.Count == 0)
            return "None";
        else if (_selectedValues.Count >= _enumItems.Count)
            return "All";
        else if (_resultAsInt != 0 && _selectedValues.Count > 1)
        {
            var valueAsInt = SelectedValuesAsInt();
            foreach (var enumItem in _enumItems)
            {
                if(!enumItem.IsComplex)
                    continue;

                if (enumItem.ValueAsInt == valueAsInt)
                    return enumItem.ToString();
            }
        }
        if (_selectedValues.Count > maxItems)
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
            _sb.Append(item.ToString());
        }

        _resultCount = count;
        return _sb.ToString();
    }

    public void Dispose()
    {
        _isIntialize = false;
    }
}

public class EnumItem
{
    private readonly string _label;
    public object Value { get; }
    public int ValueAsInt { get; }

    public bool IsComplex { get; }

    public EnumItem(object value)
    {
        Value = value;
        _label = value.ToString();
        ValueAsInt = Convert.ToInt32(value);

        IsComplex = true;
        for (int i = 0; i < 31; i++)
        {
            if (1 << i == ValueAsInt)
            {
                IsComplex = false;
                break;
            }
        }
    }

    public override string ToString()
    {
        return _label;
    }

    public override int GetHashCode()
    {
        return ValueAsInt;
    }
}