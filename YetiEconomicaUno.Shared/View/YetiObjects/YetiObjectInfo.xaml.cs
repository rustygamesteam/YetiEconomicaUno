using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ReactiveUIGenerator;
using ReactiveUI;
using YetiEconomicaCore.Services;
using YetiEconomicaUno.Converters;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using RustyDTO;
using RustyDTO.DescPropertyModels;
using RustyDTO.Interfaces;
using YetiEconomicaCore;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.View.YetiObjects;

[ViewFor<IRustyEntity>]
public sealed partial class YetiObjectInfo : UserControl
{
    private CompositeDisposable _disposables;

    public YetiObjectInfo()
    {
        this.InitializeComponent();

        this.WhenActivated(disposable =>
        {
            _disposables = disposable;
            this.WhenAnyValue(static view => view.ViewModel)
                .Subscribe(ViewModel_OnInitialize).DisposeWith(disposable);
        });
    }

    private void Remove_OnClick(object sender, RoutedEventArgs args)
    {
        RustyEntityService.Instance.Remove(ViewModel);
    }

    private void ViewModel_OnInitialize(IRustyEntity value)
    {
        if (value == null)
            return;

        var index = value.GetIndex();
        Visibility hasSpecial = Visibility.Visible;
        switch (ViewModel.Type)
        {
            case RustyEntityType.Tech:
                var secondsViewModel = ViewModel.GetDescUnsafe<ILongExecution>();

                secondsViewModel.WhenAnyValue(static x => x.Duration)
                    .Select(static value => $"Tech duration: {DurationLabelConverter.GetDuration(value)}")
                    .BindTo(this, static view => view.SpecialLine.Text)
                    .DisposeWith(_disposables);
                break;
            case RustyEntityType.UniqueBuild:
            case RustyEntityType.UniqueTool:
                SpecialLine.Text = "Grades count: 0";

                var grades = RustyEntityService.Instance.GetObservableEntitiesForOwner(index).Count();
                grades.Select(static count => $"Grades count: {count}")
                    .BindTo(this, static view => view.SpecialLine.Text)
                    .DisposeWith(_disposables);
                break;
            case RustyEntityType.Superstructure:
            case RustyEntityType.PVE:
                hasSpecial = Visibility.Collapsed;
                break;
        }
        SpecialLine.Visibility = hasSpecial;
    }
}
