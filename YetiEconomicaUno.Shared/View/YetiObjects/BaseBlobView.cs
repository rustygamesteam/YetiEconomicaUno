using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Reactive.Disposables;
using RustyDTO;
using RustyDTO.Interfaces;
using YetiEconomicaCore.Services;
using YetiEconomicaUno.View.Flyouts;

namespace YetiEconomicaUno.View.YetiObjects;

public abstract class BaseBlobView : Button
{
    public bool IsRequired { get; private set; }
    private CompositeDisposable _flyoutDisposable;

    private IRustyEntity _entity;
    private DescPropertyType _propertyType;

    protected IRustyEntity Entity => _entity;
    protected DescPropertyType PropertyType => _propertyType;

    public BaseBlobView()
    {
        this.DefaultStyleKey = typeof(BaseBlobView);
    }

    protected override void OnApplyTemplate()
    {
        Resources["ButtonBackgroundPointerOver"] = Background;
        Resources["ButtonBackgroundPressed"] = Background;

        base.OnApplyTemplate();
    }

    protected void Initialize(int entityIndex, DescPropertyType type)
    {
        Initialize(entityIndex, type, out _);
    }

    protected void Initialize(int entityIndex, DescPropertyType type, out IRustyEntity entity)
    {
        _entity = entity = GetEntity(entityIndex);
        _propertyType = type;

        IsRequired = entity.IsPropertyRequired(type);
        Background = new SolidColorBrush(IsRequired
            ? Colors.DarkRed
            : Colors.Indigo);
    }

    private IRustyEntity GetEntity(int entityIndex)
    {
        return RustyEntityService.Instance.GetEntity(entityIndex);
    }


    protected void Flyout_OnOpened(object sender, object args)
    {
        var flyout = (FlyoutWithBtn)sender;
        flyout.SetBtnVisibility(!IsRequired);
        flyout.Click += Flyout_OnRemoveClicked;

        _flyoutDisposable = new CompositeDisposable();

        Disposable.Create(() => flyout.Click -= Flyout_OnRemoveClicked).DisposeWith(_flyoutDisposable);
        FlyoutOpened(_flyoutDisposable);
    }

    protected abstract void FlyoutOpened(CompositeDisposable disposable);

    protected void Flyout_OnRemoveClicked(object sender, RoutedEventArgs e)
    {
        RustyEntityService.Instance.TryRemoveProperty(_entity, _propertyType);
    }

    protected virtual void OnFlyoutDisposing()
    {

    }

    protected void Flyout_OnClosing(object sender, object args)
    {
        OnFlyoutDisposing();
        _flyoutDisposable?.Dispose();
        _flyoutDisposable = null;
    }
}