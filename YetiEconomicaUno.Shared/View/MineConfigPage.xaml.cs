using Microsoft.UI.Xaml.Controls;
using YetiEconomicaUno.ViewModels;
using ReactiveUIGenerator;
using ReactiveUI;
using YetiEconomicaUno.Services;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using System;
using System.Linq.Expressions;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    [ViewFor<MineConfigViewModel>]
    public sealed partial class MineConfigPage : Page
    {
        public MineConfigPage()
        {
            this.InitializeComponent();

            this.WhenActivated(disposables =>
            {
                var service = CalculateMinigamesService.Instance;

                InjectMineCell(service.MineCells, disposables, 
                    (SizeX_Box, static value => value.X), 
                    (SizeY_Box, static value => value.Y));

                InjectProportionsCell(service.MineProportions, disposables,
                    (GroundBox, static value => value.Ground),
                    (StoneBox, static value => value.Stone), 
                    (OreBox, static value => value.Ore));
            });
        }

        private void InjectMineCell(ReactiveVector2Int source, CompositeDisposable disposables, params (NumberBox box, Expression<Func<ReactiveVector2Int, int>> expression)[] values)
        {
            foreach(var value in values)
            {
                value.box.Value = value.expression.Compile().Invoke(source);
                value.box.WhenAnyValue(static view => view.Value)
                    .Select(static x => (int)x)
                    .BindTo(source, value.expression)
                    .DisposeWith(disposables);
            }
        }

        private void InjectProportionsCell(MineProportions source, CompositeDisposable disposables, params (NumberBox box, Expression<Func<MineProportions, int>> expression)[] values)
        {
            foreach (var value in values)
            {
                value.box.Value = value.expression.Compile().Invoke(source);
                value.box.WhenAnyValue(static view => view.Value)
                    .Select(static x => (int)x)
                    .BindTo(source, value.expression)
                    .DisposeWith(disposables);
            }
        }
    }
}
