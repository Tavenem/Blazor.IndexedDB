﻿namespace Tavenem.Blazor.IndexedDB;

internal class IndexedDbStoreInfo
{
    public string? DatabaseName { get; set; }
    public string? StoreName { get; set; }
    public int? Version { get; set; }
    public string[]? StoreNames { get; set; }
    public string? KeyPath { get; set; }
}
