namespace Tavenem.Blazor.IndexedDB;

internal readonly record struct BatchOptions(
    bool Reset = false,
    int? Skip = null,
    int? Take = null,
    string? TypeDiscriminator = null,
    string? TypeDiscriminatorValue = null);
