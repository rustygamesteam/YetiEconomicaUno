﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUIGenerator;
using YetiEconomicaCore.Services;
using DynamicData.Binding;
using DependencyPropertyGenerator;
using Splat;
using YetiEconomicaUno.Services;
using YetiEconomicaUno.View.YetiObjects.PropertyBlobls;
using DynamicData;
using RustyDTO;
using RustyDTO.DescPropertyModels;
using RustyDTO.Interfaces;
using YetiEconomicaUno.Helpers;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.View.YetiObjects;

[ViewFor<IReactiveRustyEntity>]
[DependencyProperty<bool>("IsAutoInitialize", DefaultValue = false)]
public sealed partial class YetiGradeObjectView : UserControl
{
    private AttachPropertyBlobView _attachPropertyBlob;

    public YetiGradeObjectView()
    {
        this.InitializeComponent();
        ContextFlyout = null;
    }

    private CompositeDisposable _lastAutoInitilaizeDisposable;
    partial void OnIsAutoInitializeChanged(bool newValue)
    {
        if (newValue)
        {
            this.WhenActivated(disposable =>
            {
                this.WhenAnyValue(view => view.ViewModel)
                    .WhereNotNull()
                    .Subscribe(entity =>
                    {
                        _lastAutoInitilaizeDisposable?.Dispose();
                        _lastAutoInitilaizeDisposable = new CompositeDisposable();

                        AutoInitialize(entity, _lastAutoInitilaizeDisposable);
                    })
                    .DisposeWith(disposable);

                Disposable.Create(this, view =>
                {
                    view._lastAutoInitilaizeDisposable?.Dispose();
                    view._lastAutoInitilaizeDisposable = null;
                }).DisposeWith(disposable);
            });
        }
    }

    public YetiGradeObjectView AutoInitialize(IReactiveRustyEntity entity, CompositeDisposable disposable)
    {
        Initialize(entity, disposable);
        if (entity.HasProperty(DescPropertyType.Payable) || entity.HasProperty(DescPropertyType.FakePayable))
            InjectPriceWithDurantion(entity, disposable);

        if (entity.HasProperty(DescPropertyType.HasRewards))
            InjectRewards(entity, disposable);

        return this;
    }

    public YetiGradeObjectView(IRustyEntity viewModel)
    {
        this.InitializeComponent();
        ContextFlyout = viewModel.HasSpecialMask(EntitySpecialMask.HasParent) ? ActionMenuFlyout : null;
    }

    public YetiGradeObjectView Initialize(IReactiveRustyEntity data, CompositeDisposable disposable)
    {
        _attachPropertyBlob = null;
        ViewModel = data;

        var source = PropertiesRepeater.ItemsSource;

        ObservableCollectionExtended<IViewFor> properties;
        if (source is ObservableCollectionExtended<IViewFor> lastProperties)
            properties = lastProperties;
        else 
            properties = new ObservableCollectionExtended<IViewFor>();

        properties.Clear();
        foreach (var type in data.DescProperties.OrderBy(static type => (int)type))
        {
            switch (type)
            {
                case DescPropertyType.Payable:
                    continue;
                case DescPropertyType.LongExecution:
                    if(data.HasProperty(DescPropertyType.Payable))
                        continue;
                    break;
            }

            if (TryResolveView(type, data.GetDescUnsafe(type), out var view))
                properties.Add(view);
        }

        var optionals = EntityDependencies.GetOptionalProperties(data.Type);

        if (optionals.Count > 0 && _attachPropertyBlob is null)
        {
            _attachPropertyBlob = new AttachPropertyBlobView()
            {
                ViewModel = data
            };
            properties.Add(_attachPropertyBlob);

            ViewModel.PropertiesChangedObserver.Subscribe(change =>
            {
                _attachPropertyBlob.ValidateVisible();
                switch (change.Reason)
                {
                    case ListChangeReason.Add:
                        if (TryResolveView(change.Current.Type, change.Current.Property, out var view))
                            properties.Insert(properties.Count - 1, view);
                        break;
                    case ListChangeReason.Remove:
                        var propertyView = properties.FirstOrDefault(view => ((IDescProperty)view.ViewModel) == change.Current.Property);
                        if (propertyView != null)
                            properties.Remove(propertyView);
                        break;
                }
            }).DisposeWith(disposable);
        }

        foreach (var viewFor in properties)
        {
            if (viewFor is ICompleteViewIntialize completeViewIntialize)
                completeViewIntialize.CompleteIntialize(disposable);
        }
        
        PropertiesRepeater.ItemsSource = properties;
        PropertiesRepeater.ItemsSourceView.WhenAnyValue(static view => view.Count)
            .Select(static count => count == 0 ? Visibility.Collapsed : Visibility.Visible)
            .BindTo(this, static view => view.PropertiesBlockHeader.Visibility)
            .DisposeWith(disposable);
        return this;
    }

    private static bool TryResolveView(DescPropertyType type, IDescProperty viewModel, out IViewFor view)
    {
        var viewRaw = Locator.Current.GetService(AppBootstrapper.PropertyViewerType, type.ToString());
        if (viewRaw is IViewFor viewFor)
        {
            viewFor.ViewModel = viewModel;
            view = viewFor;

            return true;
        }

        view = null;
        return false;
    }

    internal YetiGradeObjectView InjectName(IRustyEntity data, CompositeDisposable disposable)
    {
        NameBox.Text = data.DisplayName;

        NameBox.WhenAnyValue(box => box.Text)
            .BindTo(data, static data => data.DisplayName)
            .DisposeWith(disposable);

        Disposable.Create(() => NameBox.Text = string.Empty)
            .DisposeWith(disposable);

        NameBox.Visibility = Visibility.Visible;
        return this;
    }

    internal YetiGradeObjectView InjectPriceWithDurantion(IRustyEntity entity, CompositeDisposable disposable)
    {
        ICollection<ResourceStack> price;
        if (entity.TryGetProperty(out IPayable payable))
            price = payable.Price;
        else
            price = entity.GetDescUnsafe<IFakePayable>().Price;

        if (entity.TryGetProperty(out ILongExecution duration))
        {
            PriceList.HasDuration = true;
            duration.WhenAnyValue(model => model.Duration)
                .BindTo(PriceList, static view => view.Duration)
                .DisposeWith(disposable);
        }
        else
        {
            PriceList.HasDuration = false;
        }

        PriceList.ItemsSource = price;
        PriceList.Visibility = Visibility.Visible;

        if (entity.TryGetProperty<IHasSingleReward>(out var reward))
            PriceList.ExcludeEntity = reward.Entity;
        else if (entity.TryGetProperty<ILink>(out var link))
            PriceList.ExcludeEntity = link.Entity;
        else if (entity.TryGetProperty<IHasExchange>(out var exchange))
            PriceList.ExcludeEntity = exchange.FromEntity;

        Disposable.Create(PriceList, static list => 
        {
            try
            {
                list.ItemsSource = Array.Empty<ResourceStack>();
                list.ExcludeEntity = null;
                list.Visibility = Visibility.Collapsed;
            }
            catch
            {

            }
        }).DisposeWith(disposable);
        return this;
    }


    internal YetiGradeObjectView InjectRewards(IRustyEntity entity, CompositeDisposable disposable)
    {
        var rewardsInfo = entity.GetDescUnsafe<IHasRewards>();

        RewardsList.ItemsSource = rewardsInfo.Rewards;
        RewardsList.Visibility = Visibility.Visible;

        Disposable.Create(RewardsList, static list =>
        {
            try
            {
                list.ItemsSource = Array.Empty<ResourceStack>();
                list.Visibility = Visibility.Collapsed;
            }
            catch
            {

            }
        }).DisposeWith(disposable);
        return this;
    }

    private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        RustyEntityService.Instance.Remove(ViewModel);
    }

    internal YetiGradeObjectView InjectAsTool(IRustyEntity owner, IRustyEntity data, CompositeDisposable disposables)
    {
        Func<IToolSettings, string> onUpdate;
        switch(owner.DisplayName)
        {
            case "Axe":
                onUpdate = static toolInfo =>
                {
                    var session = toolInfo.Efficiency * toolInfo.Strength;
                    return $"Wood [session/day]: {session:F0}/{session * CalculateBalanceService.Instance.SessionUseInDay(toolInfo.RechargeEvery):F0}";
                };
                break;
            case "Pick":
                onUpdate = static toolInfo =>
                {
                    var sessionsInDay = CalculateBalanceService.Instance.SessionUseInDay(toolInfo.RechargeEvery);

                    var multiplayer = CalculateMinigamesService.Instance.GetMineValuesByClick();
                    var sessionByStone = multiplayer.Stone * toolInfo.Efficiency * toolInfo.Strength;
                    var sessionByOre = multiplayer.Ore * toolInfo.Efficiency * toolInfo.Strength;

                    return $"Stone [session/day]: {sessionByStone:F0}/{sessionByStone * sessionsInDay:F0}\n" +
                    $"Ore [session/day]: {sessionByOre:F0}/{sessionByOre * sessionsInDay:F0}";
                };
                break;
            default:
                onUpdate = static _ => string.Empty;
                break;
        }

        var toolInfo = data.GetDescUnsafe<IToolSettings>();
        toolInfo.WhenAnyPropertyChanged()
            .Subscribe(toolInfo => onUpdate.Invoke(toolInfo))
            .DisposeWith(disposables);

        SetInfo(onUpdate.Invoke(toolInfo));

        return this;
    }

    internal YetiGradeObjectView InjectAsBuild(IRustyEntity viewModel, IRustyEntity data, CompositeDisposable disposables)
    {
        SetInfo(string.Empty);
        return this;
    }

    private void SetInfo(string value)
    {
        InfoBlockHeader.Visibility = string.IsNullOrEmpty(value) ? Visibility.Collapsed : Visibility.Visible;
        InfoBlock.Text = value;
    }

    private void InternalInjectProperty(NumberBox box, string header, double minimum, double maximum)
    {
        box.Header = header;
        box.Minimum = minimum;
        box.Maximum = maximum;
        box.ValidationMode = NumberBoxValidationMode.InvalidInputOverwritten;
    }
}
