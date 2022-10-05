namespace System.Runtime.CompilerServices.Generators;

internal static class SourceGenerationHelper
{
    public const string AttributeNamespace = "RustyEngine.CodeGen";
    public const string AttributeName = "MutableResolverFactoryAttribute";

    public const string AttributeFullName = $"{AttributeNamespace}.{AttributeName}";

    public const string MutablePropertyBase = "RustyDTO.Interfaces.IMutableProperty";

    public const string AttributeSource = @$"#nullable enable

namespace {AttributeNamespace};

[global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = false)]
internal sealed class {AttributeName} : global::System.Attribute
{{
    public {AttributeName}()
    {{
    }}
}}";
}