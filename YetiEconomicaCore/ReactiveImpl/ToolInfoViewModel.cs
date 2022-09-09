using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using LiteDB;
using RustyDTO.DescPropertyModels;

namespace YetiEconomicaCore.ReactiveImpl;

public class ToolInfoViewModel : ReactiveObject, IToolSettings
{
    public int Index { get; }

    [Reactive]
    public double Efficiency { get; set; }
    [Reactive]
    public int Strength { get; set; }
    [Reactive]
    public int RechargeEvery { get; set; }

    public ToolInfoViewModel(int index, double efficiency, int strength, int rechargeEvery)
    {
        Index = index;
        Efficiency = efficiency;
        Strength = strength;
        RechargeEvery = rechargeEvery;
    }
}

internal class ReactiveToolInfoFactory : PropertyResolverFactory<IToolSettings>
{
    static ReactiveToolInfoFactory()
    {
        Instance = new ReactiveToolInfoFactory();
    }

    public override BsonValue? SerializeDefault()
    {
        return new BsonDocument
        {
            { "Efficiency", 1d },
            { "Strength", 100 },
            { "RechargeEvery", 14 },
        };
    }

    public override BsonValue Serialize(IToolSettings property)
    {
        return new BsonDocument
        {
            { "Efficiency", property.Efficiency },
            { "Strength", property.Strength },
            { "RechargeEvery", property.RechargeEvery },
        };
    }

    public override IToolSettings ToProperty(int index, BsonValue data)
    {
        var efficiency = data["Efficiency"].AsDouble;
        var strength = data["Strength"].AsInt32;
        var rechargeEvery = data["RechargeEvery"].AsInt32;

        return new ToolInfoViewModel(index, efficiency, strength, rechargeEvery);
    }
}