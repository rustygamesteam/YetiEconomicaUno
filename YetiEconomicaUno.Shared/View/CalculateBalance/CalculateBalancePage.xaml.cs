using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml.Controls;
using YetiEconomicaUno.ViewModels.CalculateBalance;
using ReactiveUIGenerator;
using YetiEconomicaUno.Services;
using System.Reactive.Disposables;
using ReactiveUI;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData;
using RustyDTO;
using YetiEconomicaUno.ViewModels.CalculateBalance.Progress;
using YetiEconomicaCore.Services;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.View.CalculateBalance;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[ViewFor<CalculateBalanceViewModel>]
public sealed partial class CalculateBalancePage : Page, IDisposable
{
    public CalculateBalanceService Service { get; } = CalculateBalanceService.Instance;

    public CalculateBalancePage()
    {
        this.InitializeComponent();

        ModelSelectBox.ItemsSource = Service.BalanceModels;

        ModelSelectBox.SelectionChanged += (sender, args) =>
        {
            var value = ModelSelectBox.SelectedValue as string;
            if (value != CalculateBalanceService.CurrentModel.Value)
                CalculateBalanceService.CurrentModel.OnNext(value);

            SessionList.ItemsSource = Service.Sessions;
        };
        CalculateBalanceService.CurrentModel.Subscribe(value => ModelSelectBox.SelectedValue = value);

        //RemoveModelBtn

        this.WhenActivated(disposables =>
        {
            ViewModel.Initialize(disposables, ListView);

            Service.BalanceModels.AsObservableChangeSet()
                .Subscribe(set => RemoveModelBtn.IsEnabled = Service.BalanceModels.Count > 1).DisposeWith(disposables);

            SessionList.WhenAnyValue(static list => list.SelectedIndex)
                .Select(static count => count != -1)
                .BindTo(this, static view => view.SessionRemoveBtn.IsEnabled)
                .DisposeWith(disposables);

            ViewModel.WhenAnyValue(static vm => vm.SelectDataDump).Subscribe(OnDumpUpdate).DisposeWith(disposables);

            ListView.WhenAnyValue(static view => view.SelectedIndex)
                .Subscribe(ListView_SelectionChanged)
                .DisposeWith(disposables);

            this.DisposeWith(disposables);
        });
    }

    private void OnDumpUpdate(UserDataDump dataDump)
    {
        FarmCells_InfoBox.Text = $"Farm cells: {dataDump.FarmCells}";
        MineCells_InfoBox.Text = $"Mine cells: {dataDump.MineSize.X}x{dataDump.MineSize.Y}";

        var yetiService = RustyEntityService.Instance;
        var tools = dataDump.Tools;

        void SetToolInfo(ToolsEnum @enum, TextBlock textBlock)
        {
            if (tools != null && tools.TryGetValue(@enum, out var progressInfo))
            {
                var entity = yetiService.GetEntity(progressInfo.Index);
                textBlock.Text = $"{@enum}: {entity.DisplayNameWithTear}";
            }
            else
                textBlock.Text = $"{@enum}: Unknown";
        }

        SetToolInfo(ToolsEnum.Axe, AxeTool_InfoBox);
        SetToolInfo(ToolsEnum.Pick, PickTool_InfoBox);
        SetToolInfo(ToolsEnum.Shovel, ShovelTool_InfoBox);
    }

    private void CloneModel_OnClick()
    {
        Service.CreateModel(CloneModelNameBox.Text);
    }

    private void RemoveModel_OnClick()
    {
        Service.RemoveModel();
    }

    private async void AddSessionTime_OnClick()
    {
        await Task.Delay(50);
        Service.AddSessionTime(SessionTimeRecord.Create((int)NewSessionHours.Value));
    }

    private void RemoveSessionTime_OnClick()
    {
        if(SessionList.Items.Count < 2)
            return;

        Service.RemoveSessionTime((SessionTimeRecord)SessionList.SelectedValue);
    }
    
    private async void OnCreateDialog(int toIndex)
    {
        ContentDialog dialog = new ContentDialog();
        dialog.XamlRoot = this.XamlRoot;

        var isLast = toIndex == ViewModel.Progress.Count;
        var progress = ViewModel.Progress.Take(toIndex).Where(static x => x.Type == ProgressType.YetiObject);
        var dump = isLast ? ViewModel.LastDataDump : ViewModel.PreSelectDataDump;

        var service = RustyEntityService.Instance;

        var content = new CreateUserTargetDialogPopup(dump);
        dialog.Content = content;
        dialog.Title = "Create user target";
        dialog.PrimaryButtonText = "Create";
        dialog.CloseButtonText = "Cancel";

        var result = await dialog.ShowAsync();

        if (result is ContentDialogResult.Primary && content.HasResult())
        {
            var resultTask = content.GetResult();
            resultTask.Order = toIndex;

            if (isLast)
                Service.Progress.Add(resultTask);
            else
            {
                Service.Progress.Edit(list =>
                {
                    resultTask.Order = toIndex;
                    list.Insert(toIndex, resultTask);

                    if (resultTask is CreateYetiObjectTask createYetiObjectTask && createYetiObjectTask.Target.HasSpecialMask(EntitySpecialMask.IsInstance) && !createYetiObjectTask.Target.HasMutable(MutablePropertyType.Count))
                    {
                        var index = createYetiObjectTask.Target.ID;
                        for (int i = list.Count - 1; i > toIndex; i--)
                        {
                            if (list[i] is CreateYetiObjectTask task && task.Target.ID == index)
                                list.RemoveAt(i);
                        }
                    }
                });
            }

            await Task.Delay(50);

            ListView.SelectedIndex = isLast ? ViewModel.Progress.Count - 1 : toIndex;

            if (dialog.Content is IDisposable disposable)
                disposable.Dispose();
        }
    }

    private void ListView_SelectionChanged(int index)
    {
        RemoveItemBtn.IsEnabled = InsertItemBtn.IsEnabled = index != -1; 

        if (index != -1)
        {
            ToUpItemBtn.IsEnabled = ViewModel.IsCanMoveFromTo(index, index - 1);
            ToDonwItemBtn.IsEnabled = ViewModel.IsCanMoveFromTo(index, index + 1);
        }
        else
            ToDonwItemBtn.IsEnabled = ToUpItemBtn.IsEnabled = false;
    }

    private void OnCreateDialog_Invoke()
    {
        OnCreateDialog(ViewModel.Progress.Count);
    }

    private void InsertItemBtn_Click()
    {
        OnCreateDialog(ViewModel.SelectedIndex);
    }

    private void RemoveItemBtn_Click()
    {
        Service.Progress.RemoveAt(ViewModel.SelectedIndex);
    }

    private void TryMoveToUp_OnClick()
    {
        InternalMove(ViewModel.SelectedIndex - 1);
    }

    private void TryMoveToDown_OnClick()
    {
        InternalMove(ViewModel.SelectedIndex + 1);
    }

    private void InternalMove(int next)
    {
        void ItemsOnVectorChanged(IObservableVector<object> sender, IVectorChangedEventArgs @event)
        {
            ListView.Items.VectorChanged -= ItemsOnVectorChanged;
            ListView.SelectedIndex = next;
        }

        ListView.Items.VectorChanged += ItemsOnVectorChanged;
        ViewModel.MoveFromTo(ViewModel.SelectedIndex, next);
    }


    public void Dispose()
    {
        SessionList.ItemsSource = Array.Empty<SessionTimeRecord>();
    }
}
