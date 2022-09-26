using System;
using System.Linq;
using System.Reactive;
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
    private readonly ReactiveCommand<DescPropertyType, Unit> _command;

    public AttachPropertyBlobView()
    {
        this.DefaultStyleKey = typeof(BaseBlobView);

        _command = ReactiveCommand.Create<DescPropertyType>(OnAttachRequest);

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

        CornerRadius = new CornerRadius(10);
        Padding = new Thickness(10, 3, 10, 3);
        Margin = new Thickness(0, 7, 0, 0);

        base.OnApplyTemplate();
    }

    private void Fill()
    {
        var optional = EntityDependencies.GetOptionalProperties(ViewModel.Type);

        ItemsList.Items.Clear();
        ItemsList.Items.AddRange(optional.Where(type => !ViewModel.HasProperty(type)).Select(type => new MenuFlyoutItem
        {
            Text = type.ToString(),
            CommandParameter = type,
            Command = _command
        }));

        Visibility = ItemsList.Items.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
    }

    private void OnAttachRequest(DescPropertyType type)
    {
        RustyEntityService.Instance.TryAttachProperty(ViewModel, type);
    }

    public void ValidateVisible()
    {
        Fill();
    }
}