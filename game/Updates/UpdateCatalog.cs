using System;
using System.Collections.Generic;
using System.Linq;

namespace TheLastGate.Updates;

public sealed class UpdateCatalog
{
    public const int RollbackChoiceLimit = 10;

    private readonly Version current;
    private readonly AvailableRelease[] newestFirst;

    public UpdateCatalog(Version current, IEnumerable<AvailableRelease> releases)
    {
        this.current = current;
        newestFirst = releases
            .DistinctBy(release => release.Version)
            .OrderByDescending(release => release.Version)
            .ToArray();
    }

    public AvailableRelease? Newest => NewerThanCurrent.FirstOrDefault();

    public IReadOnlyList<AvailableRelease> NewerThanCurrent =>
        newestFirst.Where(release => release.Version > current).ToArray();

    public IReadOnlyList<AvailableRelease> NotesToShow =>
        NewerThanCurrent is { Count: > 0 } newer
            ? newer
            : newestFirst.Where(release => release.Version == current).ToArray();

    public IReadOnlyList<AvailableRelease> RollbackChoices =>
        newestFirst.Where(release => release.Version < current).Take(RollbackChoiceLimit).ToArray();

    public IReadOnlyList<AvailableRelease> UndoneBy(Version target) =>
        newestFirst.Where(release => release.Version > target && release.Version <= current).ToArray();
}
