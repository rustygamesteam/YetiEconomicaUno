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

public abstract partial class BaseBlobView : Button
{
    public bool IsRequired { get; private set; }
    private CompositeDisposable _flyoutDisposable;

    private IRustyEntity _entity;
    private DescPropertyType _propertyType;

    private FlyoutWithBtn _flyout;

    protected IRustyEntity Entity => _entity;
    protected DescPropertyType PropertyType => _propertyType;
    protected TextBlock InfoBox { get; }

    public static DependencyProperty DetalsProperty { get; } = DependencyProperty.RegisterAttached(nameof(Detals), typeof(UIElement), typeof(BaseBlobView), PropertyMetadata.Create(defaultValue: null));
    public UIElement Detals
    {
        get => (UIElement)GetValue(DetalsProperty);
        set
        {
            _flyout.Content = value;
            SetValue(DetalsProperty, value);
        }
    }

    public void MakeSmallInfo()
    {
        InfoBox.FontSize = 12;
    }

    public BaseBlobView()
    {
        this.DefaultStyleKey = typeof(BaseBlobView);
        Content = InfoBox = new TextBlock();
        ContextFlyout = _flyout = new FlyoutWithBtn()
        {
            Label = "Remove",
            Icon = Symbol.Remove
        };

        ContextFlyout.Opened += Flyout_OnOpened;
        ContextFlyout.Closing += Flyout_OnClosing;
    }

    protected override void OnApplyTemplate()
    {
        Resources["ButtonBackgroundPointerOver"] = Background;
        Resources["ButtonBackgroundPressed"] = Background;

        MinHeight = 27;
        CornerRadius = new CornerRadius(10);
        Padding = new Thickness(10, 3, 10, 3);
        Margin = new Thickness(0, 7, 0, 0);

        base.OnApplyTemplate();
    }

    protected void Initialize(int entityIndex, DescPropertyType type)
    {
        Initialize(entityIndex, type, out _);
    }

    protected void Initialize(IDescProperty viewModel, DescPropertyType type)
    {
        Initialize(viewModel.Index, type, out _);
        DataContext = viewModel;
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