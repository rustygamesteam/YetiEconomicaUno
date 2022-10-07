using System.Buffers;
using System.Text.Json;
using LiteDB;

namespace YetiEconomicaCore.Helper;

public static class JsonEx
{
    private static readonly MemoryStream _stream = new MemoryStream();
    private static Utf8JsonWriter _jsonWriter = new Utf8JsonWriter(_stream);

    private static readonly Action<Utf8JsonWriter, BsonValue>[] _map;
    private static readonly Func<JsonElement, BsonValue>[] _mapToBson;

    static JsonEx()
    {
        _map = new Action<Utf8JsonWriter, BsonValue>[15];

        _map[(int)BsonType.Null] = (writer, value) => writer.WriteNullValue();
        _map[(int)BsonType.Int32] = (writer, value) => writer.WriteNumberValue(value.AsInt32);
        _map[(int)BsonType.Int64] = (writer, value) => writer.WriteNumberValue(value.AsInt64);
        _map[(int)BsonType.Decimal] = (writer, value) => writer.WriteNumberValue(value.AsDecimal);
        _map[(int)BsonType.Double] = (writer, value) => writer.WriteNumberValue(value.AsDouble);
        //_map[(int)BsonType.DateTime] = (writer, value) => writer.WriteNumberValue(value.AsDateTime);
        _map[(int)BsonType.Boolean] = (writer, value) => writer.WriteBooleanValue(value.AsBoolean);
        _map[(int)BsonType.String] = (writer, value) => writer.WriteStringValue(value.AsString);
        _map[(int)BsonType.Array] = (writer, value) =>
        {
            var array = value.AsArray;
            writer.WriteStartArray();
            for (int i = 0; i < array.Count; i++)
            {
                var node = array[i];
                var action = _map[(int)node.Type];
                if(action is null)
                    continue; //TODO!
                action.Invoke(writer, node);
            }
            writer.WriteEndArray();
        };
        _map[(int)BsonType.Document] = (writer, value) =>
        {
            var document = value.AsDocument;
            writer.WriteStartArray();

            foreach (var pair in document)
            {
                var action = _map[(int)pair.Value.Type];
                if(action is null)
                    continue; //TODO!

                writer.WritePropertyName(pair.Key);
                action.Invoke(writer, pair.Value);
            }
            writer.WriteEndArray();
        };

        _mapToBson = new Func<JsonElement, BsonValue>[8];
        _mapToBson[(int)JsonValueKind.Null] = element => BsonValue.Null;
        _mapToBson[(int)JsonValueKind.String] = element => new BsonValue(element.GetString());
        _mapToBson[(int)JsonValueKind.Number] = element => new BsonValue(element.GetDouble());
        _mapToBson[(int)JsonValueKind.True] = element => new BsonValue(true);
        _mapToBson[(int)JsonValueKind.False] = element => new BsonValue(false);
        _mapToBson[(int)JsonValueKind.Array] = element =>
        {
            var array = element.EnumerateArray();
            var result = new BsonArray(element.GetArrayLength());
            foreach (var node in array)
                result.Add(_mapToBson[(int)node.ValueKind].Invoke(node));
            return result;
        };
        _mapToBson[(int)JsonValueKind.Object] = element =>
        {
            var values = element.EnumerateObject();
            var result = new BsonDocument();
            foreach (var node in values)
                result.Add(node.Name, _mapToBson[(int)node.Value.ValueKind].Invoke(node.Value));
            return result;
        };
    }

    public static JsonElement ToJson(this BsonValue value)
    {
        _stream.Position = 0;
        _map[(int)value.Type].Invoke(_jsonWriter, value);

        var reader = new Utf8JsonReader(new ReadOnlySequence<byte>(_stream.GetBuffer(), 0, (int)_stream.Position));
        return JsonElement.ParseValue(ref reader);
    }

    public static BsonValue FromJson(this JsonElement value)
    {
        return _mapToBson[(int)value.ValueKind].Invoke(value);
    }
}