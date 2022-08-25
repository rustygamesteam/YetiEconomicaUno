﻿using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using DynamicData;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ReactiveUI;
using ReactiveUIGenerator;
using RustyDTO;
using RustyDTO.Interfaces;
using YetiEconomicaCore.Services;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.View.YetiObjects.PropertyBlobls;

[ViewFor<IRustyEntity>]
public sealed partial class AttachPropertyBlobView : Button
{
    private readonly ReactiveCommand<EntityPropertyType, Unit> _command;

    public AttachPropertyBlobView()
    {
        this.DefaultStyleKey = typeof(BaseBlobView);

        _command = ReactiveCommand.Create<EntityPropertyType>(OnAttachRequest);

        this.InitializeComponent();

        this.WhenActivated(disposables =>
        {
            Fill();
        });
    }

    protected override void OnApplyTemplate()
    {
        Resources["ButtonBackgroundPointerOver"] = Background;
        Resources["ButtonBackgroundPressed"] = Background;

        base.OnApplyTemplate();
    }

    private void Fill()
    {
        var optional = EntityDependencies.GetOptionalProperties(ViewModel.Type);

        ItemsList.Items.Clear();
        ItemsList.Items.AddRange(optional.SkipWhile(ViewModel.HasProperty).Select(type => new MenuFlyoutItem
        {
            Text = type.ToString(),
            CommandParameter = type,
            Command = _command
        }));

        Visibility = ItemsList.Items.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
    }

    private void OnAttachRequest(EntityPropertyType type)
    {
        RustyEntityService.Instance.TryAttachProperty(ViewModel, type);
    }

    public void ValidateVisible()
    {
        Fill();
    }
}