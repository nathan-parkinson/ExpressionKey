# ExpressionKey

[![NuGet Version and Downloads count](https://buildstats.info/nuget/expressionkey)](https://www.nuget.org/packages/ExpressionKey/)

ExpressionKey is a library to match up objects - usually from a database - based on defined keys and relationships.  

Ideal for use with micro orms like dapper.

Sample usage:
```c#
public class Builder : KeyBuilder
{
    public Builder()
    {
        AddKey<Parent, int>(x => x.Id);
        AddKey<Child, int>(x => x.Id);
        AddRelationship<Parent, Parent>(p => p.NextLevel, (p1, p2) => p1.ParentId == p2.Id);
        AddRelationship<Parent, IList<Child>, Child>(p => p.Children, (p, c) => p.Id == c.ParentId);
        AddRelationship<Child, Parent>(c => c.Parent, (c, p) => c.ParentId == p.Id);
    }
}

public class Parent
{
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public Parent NextLevel { get; set; }
    public IList<Child> Children { get; set; } = new List<Child>();
}

public class Child
{
    public int Id { get; set; }
    public int ParentId { get; set; }
    public Parent Parent { get; set; }
}

var parentEntities = Enumerable.Range(1, 100).Select(x => new Parent
{
    Id = x,
    ParentId = x < 2 ? default(int?) : x - 1
}).ToList();

var childEntities = Enumerable.Range(1, 1000).Select(x => new Child
{
    Id = x,
    ParentId = (int)Math.Ceiling(x / 10.0f)
}).ToList();


var builder = new Builder();
var pool = builder.CreateEntityPool();
pool.AddEntities(parentEntities);
pool.AddEntities(childEntities);

var matchedAndConsolidatedEntities = pool.GetEntities<Parent>(parentEntities);
```

Improved documentation to follow.
