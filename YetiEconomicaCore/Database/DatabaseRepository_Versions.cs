using LiteDB;
using ReactiveUI.Fody.Helpers;
using ReactiveUI;
using RustyDTO;

namespace YetiEconomicaCore.Services;

public partial class DatabaseRepository
{
    private const int DEFAULT_VERSION = 5;
    private BD_version _version;

    private void Validate(BD_version versionConfig)
    {
        var entitise = Database.GetCollection("entities", BsonAutoId.Int32);
        var properties = Database.GetCollection("properties", BsonAutoId.Int32);
        var items_of = Database.GetCollection("items_of", BsonAutoId.Int32);

        if (versionConfig.Version == 0)
        {
            Version_0(entitise, items_of, properties);
            versionConfig.Version++;
        }

        if (versionConfig.Version == 1)
        {
            Version_1(entitise);
            versionConfig.Version++;
        }

        if (versionConfig.Version == 2)
        {
            Version_2(entitise, properties);
            versionConfig.Version++;
        }

        if (versionConfig.Version == 3)
        {
            Version_3(entitise, properties);
            versionConfig.Version++;
        }
        if (versionConfig.Version == 4)
        {
            Version_4(entitise, properties);
            versionConfig.Version++;
        }
    }

    private void Version_4(ILiteCollection<BsonDocument> entitise, ILiteCollection<BsonDocument> properties)
    {
        const int pveType = (int) RustyEntityType.PVE;
        var removes = entitise.FindAll().Where(static document => document["Type"].AsInt32 == pveType).Select(static document => document["_id"]).ToArray();
        foreach (var remove in removes)
        {
            entitise.Delete(remove);
            properties.Delete(remove);
        }
    }

    private static void Version_3(ILiteCollection<BsonDocument> entitise, ILiteCollection<BsonDocument> properties)
    {
        var boostSpeedIndex = new BsonValue(5);
        
        var craftSpeedIndex = new BsonValue(31);
        var techSpeedIndex = new BsonValue(32);

        foreach (var entity in entitise.FindAll())
        {
            var props = entity["Properties"].AsArray;
            if (props.Contains(boostSpeedIndex))
            {
                props.Remove(boostSpeedIndex);
                props.Add(craftSpeedIndex);
                props.Add(techSpeedIndex);

                entitise.Update(entity);
            }

        }

        foreach (var doc in properties.FindAll())
        {
            bool hasUpdate = false;
            //BOOST_SPEED
            if (doc.TryGetValue("5", out var info))
            {
                doc.Add("31", info["CraftSpeed"]);
                doc.Add("32", info["TechSpeed"]);

                doc.Remove("5");

                hasUpdate = true;
            }

            //longExecution
            if (doc.TryGetValue("4", out var longExecutionInfo))
            {
                longExecutionInfo["IsCancelable"] = new BsonValue(true);
                hasUpdate = true;
            }

            var nullValue = new BsonValue(int.MinValue);

            //In build process
            if (doc.TryGetValue("7", out var inBuildProcess))
            {
                if(inBuildProcess["Build"].AsInt32 == -1)
                    doc["7"] = nullValue;
                else
                    doc["7"] = inBuildProcess["Build"];
                hasUpdate = true;
            }


            if (doc.TryGetValue("2", out var hasDependents))
            {
                if (hasDependents["Required"].AsInt32 == -1)
                {
                    hasDependents["Required"] = nullValue;
                    hasUpdate = true;
                }

                if (hasDependents["VisibleAfter"].AsInt32 == -1)
                {
                    hasDependents["VisibleAfter"] = nullValue;
                    hasUpdate = true;
                }
            }

            if (hasUpdate)
                properties.Update(doc);
        }
    }

    private static void Version_2(ILiteCollection<BsonDocument> entitise, ILiteCollection<BsonDocument> properties)
    {
        var expandexIndex = new BsonValue(999);
        foreach (var entity in entitise.FindAll())
        {
            var props = entity["Properties"].AsArray;
            if (props.Contains(expandexIndex))
            {
                props.Remove(expandexIndex);
                entitise.Update(entity);
            }
        }

        foreach (var doc in properties.FindAll())
        {
            if (doc.Remove("999"))
                properties.Update(doc);
        }
    }

    private static void Version_1(ILiteCollection<BsonDocument> entitise)
    {
        var hasChildIndex = new BsonValue(10);

        foreach (var entity in entitise.FindAll())
        {
            var props = entity["Properties"].AsArray;
            if (props.Contains(hasChildIndex))
            {
                props.Remove(hasChildIndex);
                entitise.Update(entity);
            }
        }
    }

    private static void Version_0(ILiteCollection<BsonDocument> entitise, ILiteCollection<BsonDocument> items_of, ILiteCollection<BsonDocument> properties)
    {
        var groups = new List<RustyEntity>();

        const int exchangeIndex = (int) RustyEntityType.ExchageTask;
        const int exchangeGroupIndex = 10; //(int)RustyEntityType.ExchageGroup;
        const int hasExchangeIndex = (int) DescPropertyType.HasExchange;
        string exchangeInfo = ((int) DescPropertyType.HasExchange).ToString();
        string linkInfo = ((int) DescPropertyType.Link).ToString();

        var exchanges = entitise.FindAll().Where(static document => document["Type"].AsInt32 == exchangeIndex).Where(
            static document =>
            {
                var props = document["Properties"];
                return props.AsArray.Count > 1 || props[0].AsInt32 != hasExchangeIndex;
            }).GroupBy(document =>
        {
            var id = document["_id"].AsInt32;
            return items_of.FindById(id)["Owner"];
        });

        foreach (var exchangeGroup in exchanges)
        {
            entitise.Delete(exchangeGroup.Key);
            var to = properties.FindById(exchangeGroup.Key)[linkInfo];
            properties.Delete(exchangeGroup.Key);

            foreach (var exchange in exchangeGroup)
            {
                var exchangeID = new BsonValue(exchange["_id"].AsInt32);
                items_of.Delete(exchangeID);

                exchange["Properties"] = new BsonArray(new BsonValue[] {hasExchangeIndex});
                entitise.Update(exchangeID, exchange);

                var props = properties.FindById(exchangeID)[exchangeInfo];
                var info = new BsonDocument
                {
                    {"From", props["FromEntity"]},
                    {"To", to},
                    {"FromRate", props["Count"]},
                };
                properties.Update(exchangeID, new BsonDocument
                {
                    {exchangeInfo, info}
                });
            }
        }

        var invalidGroups = entitise.FindAll().Where(static document => document["Type"].AsInt32 == exchangeGroupIndex)
            .Select(static document => document["_id"]);
        foreach (var id in invalidGroups)
        {
            entitise.Delete(id);
            properties.Delete(id);
        }
    }


    private class BD_version : ReactiveObject
    {
        [BsonCtor]
        public BD_version(string key)
        {
            Key = key;
            Version = DEFAULT_VERSION;
        }

        [BsonId]
        public string Key { get; }

        [Reactive]
        public int Version { get; set; }
    }
}