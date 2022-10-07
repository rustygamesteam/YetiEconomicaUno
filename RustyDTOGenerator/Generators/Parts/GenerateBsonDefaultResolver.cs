using System.Text;
using Microsoft.CodeAnalysis;

namespace RustyDTOGenerator.Generators;

internal static partial class SourceGenerationHelper
{
    internal static void GenerateBsonDefaultResolver(StringBuilder sb, string nameSpace, IReadOnlyCollection<PropertyEnumInfo> classes)
    {
        sb.Clear();
        sb.Append(@"#nullable enable

using System;
using LiteDB;
using RustyDTO.Supports;
using RustyDTO.Interfaces;
");
        foreach (var nameSpaceInfo in classes.Select(info => info.Namespace).Distinct(StringComparer.Ordinal))
        {
            sb.Append("using ");
            sb.Append(nameSpaceInfo);
            sb.Append(";\n");
        }

        sb.Append("\nnamespace ");
        sb.Append(nameSpace);
        
        sb.Append(@";

public class RustyPropertyBsonSerializer
{
    private static RustyPropertyBsonSerializer _instance;
    public static RustyPropertyBsonSerializer Instance => (_instance ??= new RustyPropertyBsonSerializer());

    private Func<BsonValue, int, IDescProperty>[] _descFromBson;
    private Func<IDescProperty, BsonValue>[] _descToBson;

    //private Dictionary<Type, Action<Utf8JsonWriter, object, string?>> _customWriters;

    private RustyPropertyBsonSerializer()
    {
        
");
        sb.Append(@"
    }

    */public void SetCustomWriter<T>(Action<Utf8JsonWriter, object, string?> action)
    {
        _customWriters[typeof(T)] = action;
    }/*

    public void SetDescResolver(int type, Func<BsonValue, int, IDescProperty>? fromBson, Func<IDescProperty, BsonValue>? toBson)
    {
        if(fromBson is not null)
            _descFromJson[type] = fromBson;
        if(toBson is not null)
            _descToJson[type] = toBson;
    }

    public BsonValue ToBson(int type, IDescProperty property)
    {
        return _descToBson[type](property);
    }

    public IDescProperty DescFromBson(int type, BsonValue bson, int index)
    {
        return _descFromBson[type](bson, index);
    }
}");
        
        static void GenerateFromTo(StringBuilder sb, string propertyPrefix, string targetType,
            PropertyType type, IReadOnlyCollection<PropertyEnumInfo> classes)
        {
            sb.Append("\n\t\t");

            var from = propertyPrefix + "FromBson";
            sb.Append(from);
            sb.Append(" = new Func<BsonValue, ");
        
            if(type is PropertyType.Desc)
                sb.Append("int, ");
        
            sb.Append("global::RustyDTO.Interfaces.");
            sb.Append(targetType);
            sb.Append(">[RustyDTO.EntityDependencies.");
            var countProperty = type switch
            {
                PropertyType.Mutable => "MutablePropertiesCount",
                PropertyType.Desc => "DescPropertiesCount",
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
            sb.Append(countProperty);
            sb.Append("];\n");
            
            int count = 0;
            foreach (var enumInfo in classes.Where(info => info.HelpType == type))
            {
                if(enumInfo.Options.IsSkipImpl)
                    continue;
                count++;
                AppendFromBson(sb, from, enumInfo);
            }
        }

        static void AppendFromBson(StringBuilder sb, string from, PropertyEnumInfo info)
        {
            sb.Append("\t\t");
            sb.Append(from);
            sb.Append('[');
            sb.Append(info.Options.Index);
            sb.Append("] = (bson");
            
            if(info.HelpType is PropertyType.Desc)
                sb.Append(", index");
            sb.Append(@") ");
            
            sb.Append(@"=> new global::");
        sb.Append(info.Namespace);
        sb.Append(".Impl.");
        sb.Append(info.Prefix);
        sb.Append(info.Name);
        sb.Append("Impl");

        string type;

        switch (info.Members.Length)
        {
            case 0:
                sb.Append(info.HelpType is PropertyType.Desc ? "(index)" : "()");
                break;
            case 1:
                if(info.HelpType is PropertyType.Desc)
                    sb.Append("(index)");

                sb.Append(" {\n");
                var prop = info.Members[0];

                sb.Append("\t\t\t");
                sb.Append(prop.Name);
                type = prop.TypeName;
                sb.Append(" = ");
                if (prop.Kind is TypeKind.Enum)
                {
                    sb.Append('(');
                    sb.Append(type);
                    sb.Append(')');
                    type = "int";
                }

                sb.Append("bson");
                if (prop.SerializedName is null)
                    sb.Append(BsonToT(type));
                else
                {
                    sb.Append("[\"");
                    sb.Append(prop.SerializedName);
                    sb.Append("\"].");
                    sb.Append(BsonToT(type));
                }
                break;
            default:
                if (info.HelpType is PropertyType.Desc)
                    sb.Append("(index)");

                sb.Append(" {\n");

                foreach (var member in info.Members)
                {
                    sb.Append("\t\t\t");
                    sb.Append(member.Name);

                    type = member.TypeName;

                    sb.Append(" = ");
                    if (member.Kind is TypeKind.Enum)
                    {
                        sb.Append('(');
                        sb.Append(type);
                        sb.Append(')');
                        type = "int";
                    }

                    sb.Append("bson[\"");
                    sb.Append(member.SerializedName ?? member.Name);
                    sb.Append("\"].");
                    sb.Append(BsonToT(type));
                    sb.Append(",\n");
                }

                sb.Length -= 2;
                break;
        }

        sb.Append("\n\t\t};\n");
        }

        static string BsonToT(string typeName)
        {
            return typeName switch
            {
                "int" or "global::System.Int32" => "AsInt32",
                "long" or "global::System.Int64" => "AsInt64",
                "byte" or "global::System.Byte" => "AsInt32",
                "bool" or "global::System.Boolean" => "AsBoolean",
                "string" or "global::System.String" => "AsString",
                "double" or "global::System.Double" => "AsDouble",
                _ => $"Deserialize<{typeName}>(_jsonSerializerOptions)"
            };
        }
    }
}