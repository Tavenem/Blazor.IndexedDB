using Tavenem.Blazor.IndexedDB.Sample.Entities;

namespace Tavenem.Blazor.IndexedDB.Sample.Models;

public class Person 
    : EntityBase
{
    public string Name { get; set; }
    public string Surname { get; set; }
    public string Phone { get; set; }
}
