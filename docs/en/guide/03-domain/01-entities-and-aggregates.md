# Entities and aggregates

## Choosing a base class

An entity is an object whose identity is determined by its ID.
D3Fragments provides the following three base classes for different purposes.

| Base class | ID | Creation time, update time, version | Raises domain events | Use |
|---|:-:|:-:|:-:|---|
| `AggregateRoot` | ✓ | ✓ | ✓ | Aggregate roots |
| `Entity` | ✓ | ✓ | – | Child entities inside an aggregate |
| `EntityBase` | ✓ | – | – | Entities that do not record times or versions |

Each of these classes fixes the ID type to `UuidV7`.
To use a different ID type, derive from `AggregateRoot<TId>`, `Entity<TId>`, or `EntityBase<TId>` and pass the ID to the constructor.

```csharp
public sealed class Book : AggregateRoot
{
    private readonly List<Reservation> _reservations = [];

    private Book(Isbn isbn, BookTitle title, string author, BookStatus status)
    {
        Isbn = isbn;
        Title = title;
        Author = author;
        Status = status;
    }

    public Isbn Isbn { get; private set; }

    public BookTitle Title { get; private set; }

    public string Author { get; private set; }

    public BookStatus Status { get; private set; }

    public IReadOnlyCollection<Reservation> Reservations => _reservations;

    // ...
}
```

Change the state of an entity only through its methods (operations).
The analyzers warn if an entity has public setters ([DFR0003](../../analyzers/DFR0003.md)) or exposes mutable collections ([DFR0009](../../analyzers/DFR0009.md)).

## IDs and identity

Two entities with the same ID and the same runtime type are considered equal (`Equals` and `==`).
If you pass a default value (such as `UuidV7.Empty`) as the ID, the constructor throws `ArgumentException`.
As a result, an entity without a determined ID cannot exist.

`UuidV7` is a struct that represents a UUID (version 7) that sorts in time order.
Base classes with a fixed ID type, such as `AggregateRoot`, generate the ID with `UuidV7.NewUuidV7()` when the instance is created.
Because the ID is determined before the entity is saved to the database, you can use the ID as soon as you create the aggregate.

```csharp
var book = Book.Create("9780321125217", "Domain-Driven Design", "Eric Evans");
var id = book.Id; // Already generated before saving
```

`UuidV7` supports conversion to and from strings (`Parse`, `TryParse`, `ToString`) and to and from JSON.
It can also be used directly as an ASP.NET Core route parameter.

## Creation time, update time, and version

`Entity` and `AggregateRoot` record the following properties automatically.

| Property | Description |
|---|---|
| `CreatedAt` | The system time (UTC) when the instance was created |
| `UpdatedAt` | The system time (UTC) when an update was last saved |
| `Version` | The version number used for optimistic concurrency control. It starts at 0 and increases by 1 each time an update is saved |

These are audit fields.
Keep times that have business meaning (such as the loan date) in separate properties.

`D3DbContext` updates the update time and the version number when it saves changes.
If only a child entity is changed, it is also treated as an update of the aggregate root.
As a result, conflicts can be detected even when different parts of an aggregate are changed at the same time (see [EF Core integration](../05-efcore/01-dbcontext.md)).

## Aggregates and repositories

An aggregate is a group of entities that is handled together as a unit of consistency.
From outside the aggregate, the entities inside it are manipulated only through the aggregate root.

Define a repository interface for each aggregate root, and do not create repositories for child entities.
By deriving from `IRepository<TAggregate>`, the following operations are already defined.

| Method | Description |
|---|---|
| `FindAsync(id, cancellationToken)` | Gets the aggregate with the specified ID. Returns `null` if not found |
| `Add(aggregate)` | Adds an aggregate |
| `Delete(token)` | Deletes an aggregate (using `DeletionToken<T>`, described below) |
| `CountAsync(cancellationToken)` | Gets the number of aggregates |
| `ExistsAsync(id, cancellationToken)` | Determines whether the aggregate exists without loading it |

```csharp
public interface IBookRepository : IRepository<Book>
{
    Task<Pager<Book>> ListAsync(int pageNo, int pageSize, BookStatus? bookStatus = null, CancellationToken cancellationToken = default);

    Task<List<Book>> FindExpiredHoldsAsync(DateOnly today, CancellationToken cancellationToken = default);
}
```

Repositories have no update or save methods.
The Unit of Work tracks changes to retrieved aggregates and saves them all together when the Command completes.

Do not define methods that retrieve or update only part of an aggregate.
They can break the consistency of the aggregate, so the analyzers warn about them ([DFR0004](../../analyzers/DFR0004.md), [DFR0005](../../analyzers/DFR0005.md)).

## Deleting aggregates and DeletionToken

Whether an aggregate can be deleted should be decided by the aggregate itself.
In D3Fragments, a repository's `Delete` takes a `DeletionToken<T>` instead of an ID.
A `DeletionToken<T>` can only be issued by the aggregate root's `IssueDeletionToken`.
Therefore, an aggregate cannot be deleted without passing its deletion rules.

```csharp
public sealed class Book : AggregateRoot
{
    // ...

    public DeletionToken<Book> IssueDeletionToken()
    {
        if (Status != BookStatus.Available)
        {
            throw new DomainException(ErrorType.InvalidState, "A book that is not on the shelf cannot be removed.");
        }

        if (Reservations.Any())
        {
            throw new DomainException(ErrorType.InvalidState, "A book with reservations cannot be removed.");
        }

        return base.IssueDeletionToken(this);
    }
}
```

```csharp
// Usage in a handler
var book = await books.FindAsync(request.BookId, cancellationToken)
    ?? throw new EntityNotFoundException(nameof(Book));
books.Delete(book.IssueDeletionToken());
```

## Related APIs

- @D3Fragments.AggregateRoot
- @D3Fragments.Entity
- @D3Fragments.EntityBase
- @D3Fragments.UuidV7
- <xref:D3Fragments.IRepository`1>
- <xref:D3Fragments.DeletionToken`1>
