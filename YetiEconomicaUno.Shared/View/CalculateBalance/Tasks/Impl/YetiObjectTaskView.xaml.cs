using System;
using System.Reactive.Disposables;
using Microsoft.UI.Xaml.Controls;
using ReactiveUI;
using ReactiveUIGenerator;
using RustyDTO;
using YetiEconomicaUno.ViewModels.CalculateBalance.Progress;
using System.Reactive.Linq;
using RustyDTO.DescPropertyModels;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace YetiEconomicaUno.View.CalculateBalance.Tasks;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[ViewFor<CreateYetiObjectTask>]
public sealed partial class YetiObjectTaskView : UserControl
{
    public YetiObjectTaskView()
    {
        this.InitializeComponent();

        this.WhenActivated(disposable =>
        {
            this.OneWayBind(ViewModel, static vm => vm.Target.Type, static view => view.TypeBox.Text, static type => $"Type: IRustyEntity [{type}]")
                .DisposeWith(disposable);

            this.WhenAnyValue(static view => view.ViewModel.Target)
                .WhereNotNull()
                .Subscribe(entity => 
                {
                    if (entity.Type is RustyEntityType.FarmObstacleClearing)
                        LabelBox.Text = $"Cells: {entity.GetDescUnsafe<IFarmExpansion>().Count}";
                    else
                        LabelBox.Text = entity.FullName;
                })
                .DisposeWith(disposable);

            this.OneWayBind(ViewModel, static vm => vm.Price, static view => view.PriceBox.Price)
                .DisposeWith(disposable);
        });
    }
}

