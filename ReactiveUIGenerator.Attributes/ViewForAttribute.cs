namespace ReactiveUIGenerator;

[global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = false)]
public sealed class ViewForAttribute<T> : global::System.Attribute
{
    public global::System.Type ViewModelType { get; }

    public ViewForAttribute()
    {
        ViewModelType = typeof(T);
    }
}
