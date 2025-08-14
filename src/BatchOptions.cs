namespace Tavenem.Blazor.IndexedDB;

internal readonly record struct BatchOptions(
    int? Skip = null,
    int? Take = null,
    string? TypeDiscriminator = null,
    string? TypeDiscriminatorValue = null,
    string? ContinuationKey = null);
