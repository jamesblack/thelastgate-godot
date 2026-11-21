using GdUnit4;
using TheLastGate.Planner;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Planner;

[TestSuite]
public sealed class PlannerChoicesTests
{
    private TempFolder folder = null!;

    private string FilePath => Path.Combine(folder.FolderPath, PlannerChoicesStore.FileName);

    [BeforeTest]
    public void MakeAFolder() => folder = new TempFolder();

    [AfterTest]
    public void DeleteTheFolder() => folder.Dispose();

    [TestCase]
    public void AMissingFile_HasNoChoices()
    {
        var loaded = PlannerChoicesStore.Load(FilePath, 1234);

        AssertThat(loaded.Weapon).IsNull();
        AssertThat(loaded.Offhand).IsNull();
    }

    [TestCase]
    public void SavedChoices_LoadBack()
    {
        AssertThat(PlannerChoicesStore.Save(FilePath, 1234, Choices(CombatWeapon.Axe, CombatOffhand.Shield))).IsTrue();

        var loaded = PlannerChoicesStore.Load(FilePath, 1234);

        AssertThat(loaded.Weapon).IsEqual(CombatWeapon.Axe);
        AssertThat(loaded.Offhand).IsEqual(CombatOffhand.Shield);
    }

    [TestCase]
    public void Characters_AreKeptApart()
    {
        PlannerChoicesStore.Save(FilePath, 11, Choices(CombatWeapon.Dagger, CombatOffhand.DualWield));
        PlannerChoicesStore.Save(FilePath, 22, Choices(CombatWeapon.Staff, CombatOffhand.None));
        PlannerChoicesStore.Save(FilePath, 11, Choices(CombatWeapon.Sword, CombatOffhand.Shield));

        var first = PlannerChoicesStore.Load(FilePath, 11);
        var second = PlannerChoicesStore.Load(FilePath, 22);

        AssertThat(first.Weapon).IsEqual(CombatWeapon.Sword);
        AssertThat(first.Offhand).IsEqual(CombatOffhand.Shield);
        AssertThat(second.Weapon).IsEqual(CombatWeapon.Staff);
        AssertThat(second.Offhand).IsEqual(CombatOffhand.None);
        AssertThat(PlannerChoicesStore.Load(FilePath, 33).Weapon).IsNull();
    }

    [TestCase]
    public void OutOfRangeValues_AreIgnored()
    {
        WriteWords(1, 1, 1234, (uint)Enum.GetValues<CombatWeapon>().Length, unchecked((uint)-5));

        var loaded = PlannerChoicesStore.Load(FilePath, 1234);

        AssertThat(loaded.Weapon).IsNull();
        AssertThat(loaded.Offhand).IsNull();
    }

    [TestCase]
    public void ADamagedFile_IsReplaced()
    {
        File.WriteAllText(FilePath, "not a planner file");
        AssertThat(PlannerChoicesStore.Load(FilePath, 1234).Weapon).IsNull();

        AssertThat(PlannerChoicesStore.Save(FilePath, 1234, Choices(CombatWeapon.Spear, CombatOffhand.None))).IsTrue();

        var loaded = PlannerChoicesStore.Load(FilePath, 1234);
        AssertThat(loaded.Weapon).IsEqual(CombatWeapon.Spear);
        AssertThat(loaded.Offhand).IsEqual(CombatOffhand.None);
    }

    [TestCase]
    public void ACharacterNotCreatedYet_IsNotSaved()
    {
        AssertThat(PlannerChoicesStore.Save(FilePath, 0, Choices(CombatWeapon.Axe, CombatOffhand.None))).IsFalse();
        AssertThat(PlannerChoicesStore.Load(FilePath, 0).Weapon).IsNull();
    }

    [TestCase]
    public void SpellsAndCards_RoundTrip()
    {
        var saved = Choices(CombatWeapon.Sword, CombatOffhand.Shield);
        saved.SpellToggles = 0x5;
        saved.TarotCards = (1ul << 22) | (1ul << 43);

        AssertThat(PlannerChoicesStore.Save(FilePath, 7, saved)).IsTrue();
        var loaded = PlannerChoicesStore.Load(FilePath, 7);

        AssertThat(loaded.Weapon).IsEqual(CombatWeapon.Sword);
        AssertThat(loaded.Offhand).IsEqual(CombatOffhand.Shield);
        AssertThat(loaded.SpellToggles).IsEqual(0x5u);
        AssertThat(loaded.TarotCards).IsEqual(saved.TarotCards);
    }

    [TestCase]
    public void AFormatOneFile_KeepsWeaponAndOffhand()
    {
        WriteFormatOneFile();

        var loaded = PlannerChoicesStore.Load(FilePath, 7);

        AssertThat(loaded.Weapon).IsEqual(CombatWeapon.Sword);
        AssertThat(loaded.Offhand).IsEqual(CombatOffhand.Shield);
        AssertThat(loaded.SpellToggles).IsEqual(0u);
        AssertThat(loaded.TarotCards).IsEqual(0ul);
    }

    [TestCase]
    public void SavingOverAFormatOneFile_KeepsItsCharacters()
    {
        WriteFormatOneFile();

        AssertThat(PlannerChoicesStore.Save(FilePath, 9, Choices(CombatWeapon.Axe, CombatOffhand.None))).IsTrue();

        var loaded = PlannerChoicesStore.Load(FilePath, 7);
        AssertThat(loaded.Weapon).IsEqual(CombatWeapon.Sword);
        AssertThat(loaded.Offhand).IsEqual(CombatOffhand.Shield);
        AssertThat(PlannerChoicesStore.Load(FilePath, 9).Weapon).IsEqual(CombatWeapon.Axe);
    }

    [TestCase]
    public void Corruptors_RoundTrip()
    {
        var saved = Choices(CombatWeapon.Sword, CombatOffhand.Shield);
        saved.CorruptorTicks = 0x5;
        saved.CorruptorCounts[0] = 2;
        saved.CorruptorCounts[1] = 4;
        saved.CorruptorCounts[2] = 7;
        saved.CorruptorCounts[TreeEffects.CorruptorCount - 1] = 20;

        AssertThat(PlannerChoicesStore.Save(FilePath, 7, saved)).IsTrue();
        var loaded = PlannerChoicesStore.Load(FilePath, 7);

        AssertThat(loaded.CorruptorTicks).IsEqual(0x5u);
        AssertThat(loaded.CorruptorCounts[0]).IsEqual(2);
        AssertThat(loaded.CorruptorCounts[1]).IsEqual(4);
        AssertThat(loaded.CorruptorCounts[2]).IsEqual(7);
        AssertThat(loaded.CorruptorCounts[3]).IsEqual(0);
        AssertThat(loaded.CorruptorCounts[TreeEffects.CorruptorCount - 1]).IsEqual(20);
    }

    [TestCase]
    public void AFormatTwoFile_LoadsWithoutCorruptors()
    {
        WriteWords(2, 1, 7, (uint)CombatWeapon.Sword, (uint)CombatOffhand.Shield, 0x5, 0x400000, 0x800);

        var loaded = PlannerChoicesStore.Load(FilePath, 7);

        AssertThat(loaded.Weapon).IsEqual(CombatWeapon.Sword);
        AssertThat(loaded.Offhand).IsEqual(CombatOffhand.Shield);
        AssertThat(loaded.SpellToggles).IsEqual(0x5u);
        AssertThat(loaded.TarotCards).IsEqual((0x800ul << 32) | 0x400000);
        AssertThat(loaded.CorruptorTicks).IsEqual(0u);
        AssertThat(loaded.CorruptorCounts.Distinct().ToArray()).ContainsExactly(0);
    }

    [TestCase]
    public void CorruptorCounts_AreKeptBetweenOneTickedAnd20()
    {
        var saved = Choices(CombatWeapon.Sword, CombatOffhand.Shield);
        saved.CorruptorTicks = 0x7;
        saved.CorruptorCounts[0] = 0;
        saved.CorruptorCounts[1] = 21;
        saved.CorruptorCounts[2] = 5;
        saved.CorruptorCounts[3] = 30;

        AssertThat(PlannerChoicesStore.Save(FilePath, 7, saved)).IsTrue();
        var loaded = PlannerChoicesStore.Load(FilePath, 7);

        AssertThat(loaded.CorruptorCounts.Take(4)).ContainsExactly(1, 20, 5, 20);
    }

    [TestCase]
    public void Items_RoundTrip()
    {
        var saved = Choices(CombatWeapon.Sword, CombatOffhand.Shield);
        saved.EnchantTicks = 0x41;
        saved.EnchantCounts[0] = 2;
        saved.EnchantCounts[6] = 5;
        saved.GearTicks = 0x100001;

        AssertThat(PlannerChoicesStore.Save(FilePath, 7, saved)).IsTrue();
        var loaded = PlannerChoicesStore.Load(FilePath, 7);

        AssertThat(loaded.EnchantTicks).IsEqual(0x41u);
        AssertThat(loaded.EnchantCounts[0]).IsEqual(2);
        AssertThat(loaded.EnchantCounts[6]).IsEqual(5);
        AssertThat(loaded.EnchantCounts[1]).IsEqual(0);
        AssertThat(loaded.GearTicks).IsEqual(0x100001u);
    }

    [TestCase]
    public void AFormatThreeFile_LoadsWithoutItems()
    {
        WriteWords(
            3,
            1,
            7,
            (uint)CombatWeapon.Sword,
            (uint)CombatOffhand.Shield,
            0x5,
            0x400000,
            0x800,
            0x3,
            0x0402,
            0,
            0,
            0,
            0,
            0,
            0);

        var loaded = PlannerChoicesStore.Load(FilePath, 7);

        AssertThat(loaded.Weapon).IsEqual(CombatWeapon.Sword);
        AssertThat(loaded.SpellToggles).IsEqual(0x5u);
        AssertThat(loaded.CorruptorTicks).IsEqual(0x3u);
        AssertThat(loaded.CorruptorCounts[0]).IsEqual(2);
        AssertThat(loaded.CorruptorCounts[1]).IsEqual(4);
        AssertThat(loaded.EnchantTicks).IsEqual(0u);
        AssertThat(loaded.GearTicks).IsEqual(0u);
        AssertThat(loaded.EnchantCounts.Distinct().ToArray()).ContainsExactly(0);
    }

    [TestCase]
    public void EnchantCounts_AreKeptBetweenOneTickedAnd20()
    {
        var saved = Choices(CombatWeapon.Sword, CombatOffhand.Shield);
        saved.EnchantTicks = 0x7;
        saved.EnchantCounts[0] = 0;
        saved.EnchantCounts[1] = 21;
        saved.EnchantCounts[2] = 5;
        saved.EnchantCounts[3] = 30;

        AssertThat(PlannerChoicesStore.Save(FilePath, 7, saved)).IsTrue();
        var loaded = PlannerChoicesStore.Load(FilePath, 7);

        AssertThat(loaded.EnchantCounts.Take(4)).ContainsExactly(1, 20, 5, 20);
    }

    [TestCase]
    public void AFullFile_DropsTheOldestCharacter()
    {
        for (uint character = 1; character <= 256; character++)
        {
            PlannerChoicesStore.Save(FilePath, character, Choices(CombatWeapon.Axe, CombatOffhand.None));
        }

        PlannerChoicesStore.Save(FilePath, 1000, Choices(CombatWeapon.Staff, CombatOffhand.None));

        AssertThat(PlannerChoicesStore.Load(FilePath, 1).Weapon).IsNull();
        AssertThat(PlannerChoicesStore.Load(FilePath, 2).Weapon).IsEqual(CombatWeapon.Axe);
        AssertThat(PlannerChoicesStore.Load(FilePath, 1000).Weapon).IsEqual(CombatWeapon.Staff);
    }

    [TestCase]
    public void TheFile_IsTheCurrentClientsLayout()
    {
        PlannerChoicesStore.Save(FilePath, 7180, Choices(CombatWeapon.Axe, CombatOffhand.None));

        var bytes = File.ReadAllBytes(FilePath);

        AssertThat(bytes.Length).IsEqual(88);
        AssertThat(Words(bytes).Take(5)).ContainsExactly(4u, 1u, 7180u, (uint)CombatWeapon.Axe, 0u);
    }

    private static PlannerChoices Choices(CombatWeapon weapon, CombatOffhand offhand) =>
        new() { Weapon = weapon, Offhand = offhand };

    private static IEnumerable<uint> Words(byte[] bytes) =>
        Enumerable.Range(0, bytes.Length / 4).Select(index => BitConverter.ToUInt32(bytes, index * 4));

    private void WriteFormatOneFile() => WriteWords(1, 1, 7, (uint)CombatWeapon.Sword, (uint)CombatOffhand.Shield);

    private void WriteWords(params uint[] words) =>
        File.WriteAllBytes(FilePath, words.SelectMany(BitConverter.GetBytes).ToArray());
}
