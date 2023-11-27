using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Tavenem.Blazor.IndexedDB.Sample;

public static class Extensions
{
    public static IJsonTypeInfoResolver WithModifier(this IJsonTypeInfoResolver resolver, Action<JsonTypeInfo> modifier)
        => new ModifierResolver(resolver, modifier);

    private sealed class ModifierResolver(IJsonTypeInfoResolver source, Action<JsonTypeInfo> modifier) : IJsonTypeInfoResolver
    {
        public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            var typeInfo = source.GetTypeInfo(type, options);
            if (typeInfo is not null)
            {
                modifier(typeInfo);
            }

            return typeInfo;
        }
    }
}
