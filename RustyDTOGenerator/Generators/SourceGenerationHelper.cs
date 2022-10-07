using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;

namespace RustyDTOGenerator.Generators;

internal static partial class SourceGenerationHelper
{
    public const string Attribute = @"#nullable enable

namespace RustyDTO.Generator;

[global::System.AttributeUsage(global::System.AttributeTargets.Field, AllowMultiple = false)]
internal sealed class SkipCodegenAttribute: global::System.Attribute
{
    public SkipCodegenAttribute(bool skipImpl = false, bool skipResolve = false)
    {
    }
}

[global::System.AttributeUsage(global::System.AttributeTargets.Enum, AllowMultiple = false)]
internal sealed class PropertyImplAttribute<TBase> : global::System.Attribute
{
    public PropertyImplAttribute(string nameSpace, string prefix = null)
    {
    }
}

[global::System.AttributeUsage(global::System.AttributeTargets.Field, AllowMultiple = false)]
internal sealed class OverridePropertyNameAttribute<TType> : global::System.Attribute
{
    public OverridePropertyNameAttribute(string name)
    {
    }
}

[global::System.AttributeUsage(global::System.AttributeTargets.Field, AllowMultiple = true)]
internal sealed class PropertyHaveAttribute<TType> : global::System.Attribute
{
    public PropertyHaveAttribute(string name, bool isReadOnly = false, bool isNullable = false, object? defaultValue = null, string? serializedName = null)
    {
    }
}";

    public const string SupportAttribute = @"#nullable enable

namespace RustyDTO.Supports;

[global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = false)]
internal sealed class PropertyImplInfoAttribute<TEnum, TBase> : global::System.Attribute
    where TEnum : Enum
{
    public int Value { get; }

    public PropertyImplInfoAttribute(int value)
    {
        Value = value;
    }
}";

    private static string ToLowerFirst(string value)
    {
        return char.ToLower(value[0]) + value.Substring(1);
    }

    private static readonly StringBuilder _sb = new StringBuilder(1024);
    private static string InternalGenerateProperty(PropertyMember[] members, bool initReadOnly, bool canReactive = false)
    {
        _sb.Length = 0;
        var sb = _sb;
        foreach (var member in members)
        {
            if (canReactive)
            {
                sb.Append("\tprivate ");
                sb.Append(member.TypeName);
                sb.Append(" _");
                sb.Append(ToLowerFirst(member.Name));
                sb.Append(";\n");
            }

            if (member.DefaultValue is not null)
            {
                sb.Append("\t[global::System.ComponentModel.DefaultValue(");
                sb.Append(member.DefaultValue);
                sb.Append(")]\n");
            }

            if (member.SerializedName is not null)
            {
                sb.Append("\t[global::System.Text.Json.Serialization.JsonPropertyName(\"");
                sb.Append(member.SerializedName);
                sb.Append("\")]\n");
            }

            sb.Append("\tpublic ");
            sb.Append(member.TypeName);
            if (member.IsNulable)
                sb.Append('?');
            sb.Append(' ');
            sb.Append(member.Name);

            if (canReactive)
            {
                var nameAsLower = ToLowerFirst(member.Name);
                sb.Append(@" {
        get => _");
                sb.Append(nameAsLower);
                sb.Append(';');

                if (!member.IsReadOnly || initReadOnly)
                {
                    sb.Append("\n#if REACTIVE");
                    sb.Append("\n\t\t");
                    sb.Append(!member.IsReadOnly ? "set" : "init");
                    sb.Append(" => this.RaiseAndSetIfChanged(ref _");
                    sb.Append(nameAsLower);
                    sb.Append(", value);");
                    sb.Append("\n#else");
                    sb.Append("\n\t\t");
                    sb.Append(!member.IsReadOnly ? "set" : "init");
                    sb.Append(" => _");
                    sb.Append(nameAsLower);
                    sb.Append(" = value;");
                    sb.Append("\n#endif");
                }
                sb.Append("\n\t}");
            }
            else
            {
                sb.Append(" { get; ");
                if (!member.IsReadOnly)
                    sb.Append("set; }");
                else if (initReadOnly)
                    sb.Append("init; }");
                else
                    sb.Append("}");
            }
            sb.Append("\n\n");
        }

        if (members.Length > 0)
            sb.Length -= 2;

        return sb.ToString();
    }

    public static string GenerateProperty(PropertyEnumInfo enumInfo)
    {
        var members = InternalGenerateProperty(enumInfo.Members, false);

        return @$"#nullable enable

namespace {enumInfo.Namespace};

public interface {enumInfo.InterfaceName} : global::{enumInfo.BaseType}
{{
{members}
}}";
    }

    public static string GenerateDescImpl(PropertyEnumInfo enumInfo)
    {
        var members = InternalGenerateProperty(enumInfo.Members, true, true);

        return @$"#nullable enable

#if REACTIVE
using ReactiveUI;
#endif

namespace {enumInfo.Namespace}.Impl;

[global::RustyDTO.Supports.PropertyImplInfo<global::{enumInfo.Options.EnumName}, global::{enumInfo.Namespace}.{enumInfo.InterfaceName}>({enumInfo.Options.Index + 1})]
public class {enumInfo.Prefix}{enumInfo.Name}Impl : 
#if REACTIVE
    ReactiveUI.ReactiveObject,
#endif
    global::{enumInfo.Namespace}.{enumInfo.InterfaceName}
{{
    public {enumInfo.Prefix}{enumInfo.Name}Impl(int index)
    {{
        Index = index;
    }}

    public int Index {{ get; }}

{members}
}}";
    }

    public static string GenerateMutableImpl(PropertyEnumInfo enumInfo)
    {
        var members = InternalGenerateProperty(enumInfo.Members, true);

        return @$"#nullable enable

namespace {enumInfo.Namespace}.Impl;

public class {enumInfo.Prefix}{enumInfo.Name}Impl : global::{enumInfo.Namespace}.{enumInfo.InterfaceName}
{{
{members}
}}";
    }

    public static string GenerateEntityDependencies(DependencyLink[] links)
    {
        return $@"#nullable enable

namespace RustyDTO;

public static partial class EntityDependencies
{{
    private static IReadOnlyDictionary<Type, int> _propertyTypes = new Dictionary<Type, int>
    {{
{string.Join(",\n", links.Select(link => $@"      {{ typeof(global::{link.TypeName}), {link.Value-1} }}"))}
    }};

    private static IReadOnlyDictionary<Enum, Type> _enumToType = new Dictionary<Enum, Type>
    {{
{string.Join(",\n", links.Select(link => $@"      {{ (global::{link.EnumName}){link.Value}, typeof(global::{link.TypeName}) }}"))}
    }};

    public static global::System.Type EnumToType(global::System.Enum @enum)
    {{
        return _enumToType[@enum];
    }}
}}";
    }

    internal static void GenerateJsonDefaultResolver(StringBuilder sb, string nameSpace, IReadOnlyCollection<PropertyEnumInfo> classes)
    {
        sb.Clear();

        sb.Append(@"#nullable enable

using System;
using System.Text.Json;
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

public class RustyPropertyJsonSerializer
{
    private static RustyPropertyJsonSerializer _instance;
    public static RustyPropertyJsonSerializer Instance => (_instance ??= new RustyPropertyJsonSerializer());

    private Func<JsonElement, IMutableProperty>[] _mutableFromJson;
    private Func<IMutableProperty, JsonElement>[] _mutableToJson;

    private Func<JsonElement, int, IDescProperty>[] _descFromJson;
    private Func<IDescProperty, JsonElement>[] _descToJson;

    private Dictionary<Type, Action<Utf8JsonWriter, object, string?>> _customWriters;
    private JsonSerializerOptions? _jsonSerializerOptions;

    private global::System.IO.MemoryStream _ms = new ();
    private Utf8JsonWriter _jsonWriter;

    private RustyPropertyJsonSerializer()
    {
        _customWriters = new (32);
        _jsonWriter = new (_ms);
");

        GenerateFromTo(sb, "_mutable", "IMutableProperty", PropertyType.Mutable, classes);
        GenerateFromTo(sb, "_desc", "IDescProperty", PropertyType.Desc, classes);

        sb.Append(@"
    }

    public void SetJsonSerializerOptions(JsonSerializerOptions options)
    {
        _jsonSerializerOptions = options;
    }

    public void SetCustomWriter<T>(Action<Utf8JsonWriter, object, string?> action)
    {
        _customWriters[typeof(T)] = action;
    }

    public void SetMutableResolver(int type, Func<JsonElement, IMutableProperty>? fromJson, Func<IMutableProperty, JsonElement>? toJson)
    {
        if(fromJson is not null)
            _mutableFromJson[type] = fromJson;
        if(toJson is not null)
            _mutableToJson[type] = toJson;
    }

    public void SetDescResolver(int type, Func<JsonElement, int, IDescProperty>? fromJson, Func<IDescProperty, JsonElement>? toJson)
    {
        if(fromJson is not null)
            _descFromJson[type] = fromJson;
        if(toJson is not null)
            _descToJson[type] = toJson;
    }

    public JsonElement ToJson(int type, IMutableProperty property)
    {
        return _mutableToJson[type](property);
    }

    public JsonElement ToJson(int type, IDescProperty property)
    {
        return _descToJson[type](property);
    }

    public IMutableProperty MutableFromJson(int type, JsonElement json)
    {
        return _mutableFromJson[type](json);
    }

    public IDescProperty DescFromJson(int type, JsonElement json, int index)
    {
        return _descFromJson[type](json, index);
    }
}");
    }

    private static void GenerateFromTo(StringBuilder sb, string propertyPrefix, string targetType, PropertyType type, IReadOnlyCollection<PropertyEnumInfo> classes)
    {
        sb.Append("\n\t\t");

        var from = propertyPrefix + "FromJson";
        sb.Append(from);
        sb.Append(" = new Func<JsonElement, ");
        
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
            AppendFromJson(sb, from, enumInfo);
        }

        sb.Append("\n\t\t");

        var to = propertyPrefix + "ToJson";
        sb.Append(to);
        sb.Append(" = new Func<global::RustyDTO.Interfaces.");
        sb.Append(targetType);
        sb.Append(", JsonElement>[RustyDTO.EntityDependencies.");
        sb.Append(countProperty);
        sb.Append("];\n");

        foreach (var enumInfo in classes.Where(info => info.HelpType == type))
        {
            if(enumInfo.Options.IsSkipImpl)
                continue;
            
            AppendToJson(sb, to, enumInfo);
        }

        if (count > 0)
            sb.Length --;

        sb.Append("\n");
    }

    private static void AppendFromJson(StringBuilder sb, string from, PropertyEnumInfo info)
    {
        sb.Append("\t\t");
        sb.Append(from);
        sb.Append('[');
        sb.Append(info.Options.Index);
        sb.Append("] = (json");

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

                sb.Append("json.");
                if (prop.SerializedName is null)
                    sb.Append(JsonToT(type));
                else
                {
                    sb.Append("GetProperty(\"");
                    sb.Append(prop.SerializedName);
                    sb.Append("\").");
                    sb.Append(JsonToT(type));
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

                    sb.Append("json.GetProperty(\"");
                    sb.Append(member.SerializedName ?? member.Name);
                    sb.Append("\").");
                    sb.Append(JsonToT(type));
                    sb.Append(",\n");
                }

                sb.Length -= 2;
                break;
        }

        sb.Append("\n\t\t};\n");
    }

    private static void AppendToJson(StringBuilder sb, string property, PropertyEnumInfo info)
    {
        sb.Append("\t\t");
        sb.Append(property);
        sb.Append('[');
        sb.Append(info.Options.Index);
        sb.Append(@"] = propertyRaw => {
            var property = ");
        sb.Append('(');
        sb.Append(info.InterfaceName);
        sb.Append(')');
        sb.Append(@"propertyRaw;
            _ms.Position = 0;
");

        switch (info.Members.Length)
        {
            case 0:
                sb.Append("\t\t\t_jsonWriter.WriteStartObject();\n\t\t\t_jsonWriter.WriteEndObject();\n");
                break;
            case 1:
                var prop = info.Members[0];
                TToJson(sb, prop, "property", prop.SerializedName);
                break;
            default:
                sb.Append("\t\t\t_jsonWriter.WriteStartObject();\n");
                foreach (var member in info.Members)
                    TToJson(sb, member, "property", member.SerializedName ?? member.Name);
                sb.Append("\t\t\t_jsonWriter.WriteEndObject();\n");
                break;
        }

        sb.Append("\t\t\tvar reader = new Utf8JsonReader(new ReadOnlySpan<byte>(_ms.GetBuffer(), 0, (int)_ms.Position));\n");
        sb.Append("\t\t\treturn JsonElement.ParseValue(ref reader);\n");
        sb.Append("\t\t};\n");
    }

    private static void TToJson(StringBuilder sb, PropertyMember propertyMember, string property, string? asProperty = null)
    {
        var type = propertyMember.TypeName;
        if (propertyMember.Kind is TypeKind.Enum)
            type = "int";

        string prefix;

        switch (type)
        {
            case "int":
            case "long":
            case "byte":
            case "double":
            case "global::System.Int32":
            case "global::System.Int64":
            case "global::System.Byte":
            case "global::System.Double":
                prefix = "WriteNumber";
                break;
            case "bool":
            case "global::System.Boolean":
                prefix = "WriteBoolean";
                break;
            case "string":
            case "global::System.String":
                prefix = "WriteString";
                break;
            default:
                sb.Append("\t\t\t_customWriters[typeof(");
                sb.Append(propertyMember.TypeName);
                sb.Append(")].Invoke(_jsonWriter, ");

                sb.Append(property);
                sb.Append('.');

                sb.Append(propertyMember.Name);
                sb.Append(", ");
                if (asProperty is not null)
                {
                    sb.Append('\"');
                    sb.Append(asProperty);
                    sb.Append('\"');
                }
                else
                    sb.Append("null");

                sb.Append(");\n");
                return;
        }

        sb.Append("\t\t\t_jsonWriter.");
        sb.Append(prefix);

        if (asProperty is null)
            sb.Append("Value(");
        else
        {
            sb.Append("(\"");
            sb.Append(asProperty);
            sb.Append("\", ");
        }

        if (propertyMember.Kind is TypeKind.Enum)
            sb.Append("(int)");

        sb.Append(property);
        sb.Append('.');
        sb.Append(propertyMember.Name);
        sb.Append(");\n");
    }

    private static string JsonToT(string typeName)
    {
        return typeName switch
        {
            "int" or "global::System.Int32" => "GetInt32()",
            "long" or "global::System.Int64" => "GetInt64()",
            "byte" or "global::System.Byte" => "GetByte()",
            "bool" or "global::System.Boolean" => "GetBoolean()",
            "string" or "global::System.String" => "GetString()",
            "double" or "global::System.Double" => "GetDouble()",
            _ => $"Deserialize<{typeName}>(_jsonSerializerOptions)"
        };
    }

    private static bool CanResolveMembers(PropertyMember[] propertyMembers)
    {
        return propertyMembers.All(member => !member.IsReadOnly || 
                                             member.Kind is TypeKind.Struct || 
                                             member.DefaultValue is not null || 
                                             member.IsNulable);
    }
        
    public static void GenerateSimpleMutablePropertyResolver(StringBuilder sb, string nameSpace,
        IReadOnlyCollection<PropertyEnumInfo> classes)
    {
        sb.Clear();

        sb.Append(@"#nullable enable

using System;
using System.Text.Json;

namespace ");
        sb.Append(nameSpace);
        sb.Append(@";

public class SimpleMutablePropertyResolver : global::RustyDTO.Interfaces.IMutablePropertyResolver
{
    private int _count;
    private Func<global::RustyDTO.Interfaces.IMutableProperty>[] _defaultFactory;  

    public bool HasResolve(int type) => type < _count;
    public bool HasDefaultResolve(int type) => _defaultFactory[type] is not null;

    public void SetDefaultResolver(int type, Func<global::RustyDTO.Interfaces.IMutableProperty> resolver)
    {
        _defaultFactory[type] = resolver;
    }

    public SimpleMutablePropertyResolver()
    {
        _count = RustyDTO.EntityDependencies.MutablePropertiesCount;
        _defaultFactory = new Func<global::RustyDTO.Interfaces.IMutableProperty>[_count];");

        foreach (var info in classes.Where(info => info.HelpType is PropertyType.Mutable))
        {
            if(info.Options.IsSkipImpl || info.Options.IsSkipResolver)
                continue;

            var canDefault = CanResolveMembers(info.Members);
            if(!canDefault)
                continue;
            
            sb.Append("\n\t\t_defaultFactory[");
            sb.Append(info.Options.Index);
            sb.Append("] = static () => new global::");
            sb.Append(info.Namespace);
            sb.Append(".Impl.");
            sb.Append(info.Prefix);
            sb.Append(info.Name);
            sb.Append("Impl {");

            FillDefaultResolverMembers(sb, info.Members);

            sb.Append("\n\t\t};");
        }

        sb.Append(@"
    }

    public global::RustyDTO.Interfaces.IMutableProperty Resolve(int type)
    {
        return _defaultFactory[type].Invoke();
    }

    public global::RustyDTO.Interfaces.IMutableProperty Resolve(int type, JsonElement dataElement)
    {
        return global::RustyDTO.CodeGen.Impl.RustyPropertyJsonSerializer.Instance.MutableFromJson(type, dataElement);
    }
}");
    }

    public static void GenerateSimpleDescPropertyResolver(StringBuilder sb, string nameSpace,
        IReadOnlyCollection<PropertyEnumInfo> classes)
    {
        sb.Clear();

        sb.Append(@"#nullable enable

using System;
using System.Text.Json;

namespace ");
        sb.Append(nameSpace);
        sb.Append(@";

public class SimpleDescPropertyResolver : global::RustyDTO.Interfaces.IDescPropertyResolver
{
    private int _count;
    private Func<int, global::RustyDTO.Interfaces.IDescProperty>[] _defaultFactory;  

    public bool HasResolve(int type) => type < _count;
    public bool HasDefaultResolve(int type) => _defaultFactory[type] is not null;

    public void SetDefaultResolver(int type, Func<int, global::RustyDTO.Interfaces.IDescProperty> resolver)
    {
        _defaultFactory[type] = resolver;
    }

    public SimpleDescPropertyResolver()
    {
        _count = RustyDTO.EntityDependencies.DescPropertiesCount;
        _defaultFactory = new Func<int, global::RustyDTO.Interfaces.IDescProperty>[_count];");

        foreach (var info in classes.Where(info => info.HelpType is PropertyType.Desc))
        {
            if(info.Options.IsSkipImpl || info.Options.IsSkipResolver)
                continue;
            
            var canDefault = CanResolveMembers(info.Members);
            if(!canDefault)
                continue;
            
            sb.Append("\n\t\t_defaultFactory[");
            sb.Append(info.Options.Index);
            sb.Append("] = static index => new global::");
            sb.Append(info.Namespace);
            sb.Append(".Impl.");
            sb.Append(info.Prefix);
            sb.Append(info.Name);
            sb.Append("Impl(index) {");

            FillDefaultResolverMembers(sb, info.Members);

            sb.Append("\n\t\t};");
        }

        sb.Append(@"
    }

    public global::RustyDTO.Interfaces.IDescProperty Resolve(int index, int type)
    {
        return _defaultFactory[type].Invoke(index);
    }

    public global::RustyDTO.Interfaces.IDescProperty Resolve(int index, int type, JsonElement dataElement)
    {
        return global::RustyDTO.CodeGen.Impl.RustyPropertyJsonSerializer.Instance.DescFromJson(type, dataElement, index);
    }
}");
    }

    private static void FillDefaultResolverMembers(StringBuilder sb, PropertyMember[] infoMembers)
    {
        foreach (var member in infoMembers)
        {   
            sb.Append("\n\t\t\t");
            sb.Append(member.Name);
            sb.Append(" = ");
            sb.Append(ValueWithTypeResolver(member.TypeName, member.DefaultValue ?? "default"));
            sb.Append(',');
        }

        if (infoMembers.Length > 0)
            sb.Length--;
    }

    private static string ValueWithTypeResolver(string type, string value)
    {
        return (type, value) switch
        {
            ("global::RustyDTO.Interfaces.IRustyEntity", "-2147483648") => "null",
            ("global::RustyDTO.Interfaces.IRustyEntity", "default") => "null",
            _ => value
        };
    }
}
