using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace D3Fragments.EFCore;

// 保存時に、変更された集約のUpdatedAt・Versionを更新する。D3DbContextとD3SaveChangesInterceptorが共通で使う
internal static class AggregateUpdateTracker
{
    public static void MarkModifiedEntitiesAsUpdated(DbContext context)
    {
        // Entries() は変更検出を行うため、プロパティ変更・子エンティティの追加削除がこの時点で状態に反映される
        var entries = context.ChangeTracker.Entries().ToList();
        var updated = new HashSet<IUpdateTrackable>(ReferenceEqualityComparer.Instance);
        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Modified && entry.Entity is IUpdateTrackable trackable)
            {
                updated.Add(trackable);
            }

            // 子エンティティの変更は、集約単位で競合を検知するため集約ルートの更新として扱う
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted) || IsAggregateRoot(entry.Entity.GetType()))
            {
                continue;
            }

            // 別の集約へ移された子（外部キーが変わった子）は、移動元と移動先の両方の集約の変更として扱う
            EntityEntry?[] parents = entry.State == EntityState.Modified
                ? [FindParent(entry, entries), FindParent(entry, entries, useOriginalValues: true)]
                : [FindParent(entry, entries)];
            foreach (var parent in parents)
            {
                if (FindAggregateRoot(parent, entries) is { State: EntityState.Unchanged or EntityState.Modified, Entity: IUpdateTrackable root })
                {
                    updated.Add(root);
                }
            }
        }

        // 集約ルート自身と子エンティティの両方が変更されていても、更新は1回だけ行う
        foreach (var trackable in updated)
        {
            trackable.MarkUpdated();
        }
    }

    // parent から親をたどり、最初に見つかった集約ルートを返す
    private static EntityEntry? FindAggregateRoot(EntityEntry? parent, List<EntityEntry> entries)
    {
        var visited = new HashSet<EntityEntry>();
        for (var current = parent; current is not null && visited.Add(current); current = FindParent(current, entries))
        {
            if (IsAggregateRoot(current.Entity.GetType()))
            {
                return current;
            }
        }
        return null;
    }

    // 親側に子へのナビゲーションがある外部キー（または所有関係）を集約の構成とみなし、親のエントリを探す。
    // 他の集約をIDのみで参照する外部キー（親側にナビゲーションがない）はたどらない
    // ponytail: 追跡中の全エントリを線形探索する。1回の保存で扱うエンティティ数が多くなり性能が問題になれば、キーで索引化する
    // useOriginalValues が true の場合は、変更前の外部キーの値で親を探す（別の集約へ移される前の親）
    private static EntityEntry? FindParent(EntityEntry entry, List<EntityEntry> entries, bool useOriginalValues = false)
    {
        foreach (var foreignKey in entry.Metadata.GetForeignKeys().Where(x => x.IsOwnership || x.PrincipalToDependent is not null))
        {
            // 削除された子や、コレクションから外されて外部キーがnullになった子は、変更前の値で親を特定する
            var keyValues = foreignKey.Properties.Select(p => useOriginalValues || entry.State == EntityState.Deleted ? entry.OriginalValues[p] : entry.CurrentValues[p] ?? entry.OriginalValues[p]).ToArray();
            if (keyValues.Any(x => x is null))
            {
                continue;
            }

            var parent = entries.FirstOrDefault(candidate =>
                foreignKey.PrincipalEntityType.IsAssignableFrom(candidate.Metadata)
                && foreignKey.PrincipalKey.Properties.Select(p => candidate.CurrentValues[p]).SequenceEqual(keyValues));
            if (parent is not null)
            {
                return parent;
            }
        }
        return null;
    }

    private static bool IsAggregateRoot(Type type)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(AggregateRoot<>))
            {
                return true;
            }
        }
        return false;
    }
}
