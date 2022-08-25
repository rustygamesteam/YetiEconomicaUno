﻿using LiteDB;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Reactive.Linq;
using YetiEconomicaUno.Converters;

namespace YetiEconomicaUno.ViewModels.CalculateBalance;

public class TimeTarget : ReactiveObject
{
    public const string PROP_ID = "TimeTarget";

    [BsonId]
    public string ID { get; } = PROP_ID;

    [BsonCtor]
    public TimeTarget() 
    {
        this.WhenAnyValue(static x => x.Days, static x => x.Hours)
            .Select(static values => DurationLabelConverter.GetDuration((int)(TimeSpan.FromHours(values.Item2) + TimeSpan.FromDays(values.Item1)).TotalSeconds))
            .ToPropertyEx(this, static x => x.ResultInfo);
    }

    [Reactive]
    public int Days { get; set; }
    [Reactive]
    public int Hours { get; set; }

    [ObservableAsProperty, BsonIgnore]
    public string ResultInfo { get; }
}
