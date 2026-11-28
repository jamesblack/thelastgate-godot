using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TheLastGate.Characters;

namespace TheLastGate.Hud.Depot;

public sealed class DepotDirectory
{
    private readonly IReadOnlyList<DepotCharacter> characters;
    private readonly DepotLayoutKeeper keeper;

    public DepotDirectory(DepotLayoutKeeper keeper, Season season, IReadOnlyList<DepotCharacter> characters)
    {
        this.keeper = keeper;
        this.characters = characters;
        Layout = keeper.Layouts.For(season);
        Layout.Sync(characters);
    }

    public DepotLayout Layout { get; }

    public string NameOf(int characterNumber) =>
        characters.Where(character => character.Number == characterNumber)
            .Select(character => character.Name)
            .FirstOrDefault()
        ?? characterNumber.ToString(CultureInfo.InvariantCulture);

    public void Save() => keeper.Save();
}
