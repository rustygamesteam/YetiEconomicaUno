using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using DynamicData.Binding;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using ReactiveUI;
using ReactiveUIGenerator;
using RustyDTO;
using RustyDTO.DescPropertyModels;
using RustyDTO.Supports;
using YetiEconomicaCore.Services;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.View.YetiObjects.PropertyBlobls;

[ViewFor<IPveEnemyPower>]
public sealed partial class EnemyPowerBlobView : BaseBlobView
{
    private bool _isInitializing;
    private IPveEnemyUnits _pveEnemyUnits;

    public EnemyPowerBlobView()
    {
        this.InitializeComponent();
    }

    public override void CompleteIntialize(CompositeDisposable disposable)
    {
        _isInitializing = true;
        Initialize(ViewModel, DescPropertyType.PveEnemyPower);
        if (ViewModel.Value is null)
        {
            ViewModel.Value = new ArmyPowerConfig[3]
            {
                new ArmyPowerConfig(1, 1, 1),
                new ArmyPowerConfig(1, 1, 1),
                new ArmyPowerConfig(1, 1, 1)
            };
        }

        var sw = ViewModel.Value[0];
        SwDmg.Value = sw.Damage;
        SwDef.Value = sw.Defense;
        SwSpeed.Value = sw.Speed;

        var sp = ViewModel.Value[1];
        SpDmg.Value = sp.Damage;
        SpDef.Value = sp.Defense;
        SpSpeed.Value = sp.Speed;

        var c = ViewModel.Value[2];
        CDmg.Value = c.Damage;
        CDef.Value = c.Defense;
        CSpeed.Value = c.Speed;


        if (Entity.TryGetProperty(out IPveEnemyUnits units))
        {
            _pveEnemyUnits = units;

            void UnitsOnPropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                UpdateInfoBox(ViewModel.Value);
            }

            units.PropertyChanged += UnitsOnPropertyChanged;
            Disposable.Create(units, units => units.PropertyChanged -= UnitsOnPropertyChanged)
                .DisposeWith(disposable);
        }
        else
            _pveEnemyUnits = null;

        _isInitializing = false;
        ViewModel.WhenAnyValue(static power => power.Value)
            .Subscribe(UpdateInfoBox)
            .DisposeWith(disposable);
    }

    private void UpdateInfoBox(ArmyPowerConfig[] value)
    {
        if (value is null)
        {
            InfoBox.Text = "Power score: 0";
            return;
        }

        var config = GlobalConfigService.Instance.ArmyInfluence.Config;

        if (_pveEnemyUnits is not null)
        {
            int count = 0;
            var score = 0d;

            var availableUnits = (int) _pveEnemyUnits.AvailableUnits;
            for (int i = 0; i < 3; i++)
            {
                if ((availableUnits & (1 << i)) == 0)
                    continue;

                count++;
                score += value[i].ToScore(config);
            }

            if (count > 0)
                score /= count;

            InfoBox.Text = $"Power score: {score:####.##}";
        }
        else
            InfoBox.Text = $"Power score: {value.ToScore(config):####.##}";
    }

    private void OnChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if(_isInitializing)
            return;
        ViewModel.Value = new ArmyPowerConfig[]
        {
            new ArmyPowerConfig(SwDmg.Value, SwDef.Value, SwSpeed.Value),
            new ArmyPowerConfig(SpDmg.Value, SpDef.Value, SpSpeed.Value),
            new ArmyPowerConfig(CDmg.Value, CDef.Value, CSpeed.Value)
        };
    }
}