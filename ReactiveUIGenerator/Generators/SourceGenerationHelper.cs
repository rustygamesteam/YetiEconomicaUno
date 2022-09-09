using H.Generators.Extensions;
using ReactiveUIGenerator.Models;

namespace ReactiveUIGenerator.Generators;

internal class SourceGenerationHelper
{
    public static string GenerateViewFor(ClassData @class, ViewForData property)
    {
        return @$"#nullable enable

namespace {@class.Namespace}
{{
    {@class.Modifiers.Trim()}partial class {@class.Name} : global::ReactiveUI.IViewFor<global::{property.ViewModelType}>
    {{
        public static readonly global::Microsoft.UI.Xaml.DependencyProperty ViewModelProperty = global::Microsoft.UI.Xaml.DependencyProperty.Register(
                name: ""ViewModel"",
                propertyType: typeof(global::{property.ViewModelType}),
                ownerType: typeof({@class.Name}),
                typeMetadata: global::Microsoft.UI.Xaml.PropertyMetadata.Create(defaultValue: null));

        public {property.ViewModelType}? ViewModel
        {{
            get => (global::{property.ViewModelType}?) GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }}

        object? global::ReactiveUI.IViewFor.ViewModel
        {{
            get => ViewModel;
            set => ViewModel = (global::{property.ViewModelType}?)value;
        }}
    }}
}}".RemoveBlankLinesWhereOnlyWhitespaces();
    }


    public const string Attribute = @"namespace ReactiveUIGenerator;

[global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = false)]
public sealed class ViewForAttribute<T> : global::System.Attribute
    where T : global::ReactiveUI.IReactiveObject
{
    public global::System.Type ViewModelType { get; }

    public ViewForAttribute()
    {
        ViewModelType = typeof(T);
    }
}

[global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = false)]
public sealed class ViewForAttribute : global::System.Attribute
{
    public global::System.Type ViewModelType { get; }

    public ViewForAttribute(global::System.Type type)
    {
        ViewModelType = type;
    }
}";

}
