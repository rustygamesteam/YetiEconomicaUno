using LiteDB;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Reactive.Disposables;
using YetiEconomicaCore.Services;
using YetiEconomicaUno.Helpers;

namespace YetiEconomicaUno.Services;

public class CalculateMinigamesService : ReactiveObject, IDisposable
{
    private static readonly Lazy<CalculateMinigamesService> _instance = new (() => new CalculateMinigamesService());
    public static CalculateMinigamesService Instance => _instance.Value;

    private readonly CompositeDisposable _disposables = new();

    public MineProportions MineProportions { get; }
    public ReactiveVector2Int MineCells { get; }
    public (int x, int y) DefaultMineCells { get; } = (4, 5);

    public const int TestCount = 500000;

    public CalculateMinigamesService()
    {
        MineProportions = BDHelper.GetProperty<MineProportions>(MineProportions.PROP_ID, _disposables);
        MineCells = DatabaseRepository.Instance.GetConfig("mine_cells", key => new ReactiveVector2Int(key)
        {
            X = 4,
            Y = 5,
        }, _disposables);
    }

    public (float Stone, float Ore) GetMineValuesByClick()
    {
        return YetiEconmicaMineCalculator.MineCache.GetCacheValue((MineCells.X, MineCells.Y), (MineProportions.Ground, MineProportions.Stone, MineProportions.Ore), TestCount);
    }

    public (float Stone, float Ore) GetMineValuesByClick((int x, int y) size)
    {
        return YetiEconmicaMineCalculator.MineCache.GetCacheValue((size.x, size.y), (MineProportions.Ground, MineProportions.Stone, MineProportions.Ore), TestCount);
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}

public class ReactiveVector2Int : ReactiveObject
{
    [Reactive]
    public int X { get; set; }
    [Reactive]
    public int Y { get; set; }

    [BsonId]
    public string Id { get; }

    [BsonCtor]
    public ReactiveVector2Int(string id)
    {
        Id = id;
    }

    /*
    public ReactiveVector2Int() : this(ObjectId.NewObjectId())
    {
        X = 4;
        Y = 5;
    }*/
}

public class MineProportions : ReactiveObject
{
    public const string PROP_ID = "mine_proportions";

    [BsonId]
    public string Id { get; } = PROP_ID;
    
    [BsonCtor]
    public MineProportions()
    {
        Ground = 1;
        Stone = 1;
        Ore = 1;
    }

    [Reactive]
    public int Ground { get; set; }
    [Reactive]
    public int Stone { get; set; }
    [Reactive]
    public int Ore { get; set; }
}