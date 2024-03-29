﻿using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUIGenerator;
using ReactiveUI;
using RustyDTO;
using RustyDTO.DescPropertyModels;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.View.YetiObjects.PropertyBlobls;

[ViewFor<IMineSize>]
public sealed partial class MineSizeBlobView : BaseBlobView
{
    public MineSizeBlobView()
    {
        this.InitializeComponent();
    }

    public override void CompleteIntialize(CompositeDisposable disposable)
    {
        Initialize(ViewModel.Index, DescPropertyType.MineSize);

        ViewModel.WhenAnyValue(static x => x.X)
            .CombineLatest(ViewModel.WhenAnyValue(static x => x.Y))
            .Select(static tuple => $"Mine size: {tuple.First}x{tuple.Second}")
            .BindTo(this, static view => view.InfoBox.Text)
            .DisposeWith(disposable);

        this.WhenAnyValue(static view => view.ViewModel)
            .BindTo(this, static view => view.DataContext)
            .DisposeWith(disposable);
    }

    protected override void FlyoutOpened(CompositeDisposable disposable)
    {
    }
}