using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUIGenerator;
using RustyDTO;
using RustyDTO.DescPropertyModels;
using RustyDTO.Interfaces;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.View.YetiObjects.PropertyBlobls;

[ViewFor<IHasDependents>]
public sealed partial class DependentsBlobView : BaseBlobView
{
    private DeepFilter _filter;

    public DependentsBlobView()
    {
        this.InitializeComponent();

        this.WhenActivated(disposables =>
        {
            Initialize(ViewModel.Index, DescPropertyType.HasDependents);
            _filter = new DeepFilter(ViewModel);

            ViewModel.WhenAnyValue(dependents => dependents.Required)
                .CombineLatest(ViewModel.WhenAnyValue(dependents => dependents.VisibleAfter))
                .Select(tuple => $"Required: {tuple.First?.FullName ?? "None"} | VisibleAfter: {tuple.Second?.FullName ?? "None"}")
                .BindTo(this, static view => view.InfoBox.Text)
                .DisposeWith(disposables);
        });
    }

    private record struct DeepFilter(IHasDependents Owner) : IObservable<Func<IRustyEntity, bool>>
    {
        private bool OnFilter(IRustyEntity rustyEntity)
        {
            return OnFilterDeep(Owner, rustyEntity);
        }

        private static bool OnFilterDeep(IHasDependents me, IRustyEntity other)
        {
            if (me.Index == other.ID.Index)
                return false;

            if (other.TryGetProperty<IHasDependents>(out var otherDependents))
            {
                if (otherDependents.Required != null && OnFilterDeep(me, otherDependents.Required) is false)
                    return false;

                if (otherDependents.VisibleAfter != null && OnFilterDeep(me, otherDependents.VisibleAfter) is false)
                    return false;
            }

            return true;
        }

        public IDisposable Subscribe(IObserver<Func<IRustyEntity, bool>> observer)
        {
            observer.OnNext(OnFilter);
            return Disposable.Empty;
        }
    }

    protected override void FlyoutOpened(CompositeDisposable disposable)
    {
        RequredBox.Filter = _filter;
        VisibleBox.Filter = _filter;

        this.Bind(ViewModel, static vm => vm.Required, static view => view.RequredBox.SelectedValue)
            .DisposeWith(disposable);
        this.Bind(ViewModel, static vm => vm.VisibleAfter, static view => view.VisibleBox.SelectedValue)
            .DisposeWith(disposable);
    }
}