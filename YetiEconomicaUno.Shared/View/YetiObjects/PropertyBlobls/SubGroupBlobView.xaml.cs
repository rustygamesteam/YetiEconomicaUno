using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Reactive.Disposables;
using ReactiveUIGenerator;
using RustyDTO.DescPropertyModels;
using ReactiveUI;
using YetiEconomicaUno.Helpers;
using RustyDTO;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.View.YetiObjects.PropertyBlobls;

[ViewFor<ISubGroup>]
public sealed partial class SubGroupBlobView : BaseBlobView
{
    public SubGroupBlobView()
    {
        this.InitializeComponent();
    }

    public override void CompleteIntialize(CompositeDisposable disposable)
    {
        Initialize(ViewModel, DescPropertyType.SubGroup);
        SubGroupBox.ItemsSource = SubGroupHelper.ResolveByType(Entity.Type);
        InfoBox.Text = $"SubGroup: {ViewModel.Group ?? "NONE"}";
    }

    private string _lastText;
    protected override void FlyoutOpened(CompositeDisposable disposable)
    {
        _lastText = SubGroupBox.Text;
        SubGroupBox.TextChanged += SubGroupBox_OnTextChanged;
        Disposable.Create(this, view => view.SubGroupBox.TextChanged -= view.SubGroupBox_OnTextChanged)
            .DisposeWith(disposable);
    }

    private void SubGroupBox_OnTextChanged(object sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        OnTextChange(SubGroupBox.Text);
    }

    private void OnGotFocus(object sender, RoutedEventArgs args)
    {
        SubGroupBox.IsSuggestionListOpen = true;
    }

    private void OnTextChange(string value)
    {
        value = value.Trim();
        if (_lastText == value)
            return;

        SubGroupHelper.Update(Entity.Type, _lastText, value);
        _lastText = value;

        InfoBox.Text = $"SubGroup: {value}";
    }
}