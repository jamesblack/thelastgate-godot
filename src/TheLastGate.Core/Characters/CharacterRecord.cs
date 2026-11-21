using System;
using System.IO;

namespace TheLastGate.Characters;

// A .moa character file, the same format the current client saves: the login key, then the
// settings block with the character's name, description and a flag saying they still have to be
// sent to the server.
public sealed class CharacterRecord
{
    public const int SettingsSize = 584;
    public const int Size = LoginKey.Size + SettingsSize;
    public const string NewAccountName = "New Account";

    private const int CharacterNameLength = 80;
    private const int DescriptionOffset = 160;
    private const int DescriptionLength = 160;
    private const int ChangedOffset = 320;

    private readonly byte[] bytes = new byte[Size];

    private CharacterRecord()
    {
    }

    public LoginKey Key
    {
        get => LoginKey.Parse(bytes);
        set => value.Write(bytes);
    }

    public string CharacterName
    {
        get => NulTerminatedText.Read(SettingsField(0, CharacterNameLength));
        set => NulTerminatedText.Write(SettingsField(0, CharacterNameLength), value);
    }

    public string Description
    {
        get => NulTerminatedText.Read(SettingsField(DescriptionOffset, DescriptionLength));
        set => NulTerminatedText.Write(SettingsField(DescriptionOffset, DescriptionLength), value);
    }

    public bool Changed
    {
        get => bytes[LoginKey.Size + ChangedOffset] != 0;
        set => bytes[LoginKey.Size + ChangedOffset] = (byte)(value ? 1 : 0);
    }

    public bool SettingsRead => throw new NotImplementedException();

    public Span<byte> SettingsBlock => throw new NotImplementedException();

    public CharacterSettings Settings() => CharacterSettings.Parse(bytes.AsSpan(LoginKey.Size));

    public void Store(CharacterSettings settings) => settings.Write(bytes.AsSpan(LoginKey.Size));

    public static CharacterRecord CreateNew()
    {
        var record = new CharacterRecord();
        record.StartNewAccount();
        record.Store(new CharacterSettings());
        return record;
    }

    // A file that isn't exactly one record long keeps only its key and gets default settings,
    // as the current client does.
    public static CharacterRecord Read(string filePath)
    {
        var fileBytes = File.ReadAllBytes(filePath);
        if (fileBytes.Length < LoginKey.Size)
        {
            throw new InvalidDataException($"{Path.GetFileName(filePath)} is too short to be a character file.");
        }

        var record = new CharacterRecord();
        if (fileBytes.Length == Size)
        {
            fileBytes.CopyTo(record.bytes, 0);
        }
        else
        {
            fileBytes.AsSpan(0, LoginKey.Size).CopyTo(record.bytes);
            record.Store(new CharacterSettings());
        }

        return record;
    }

    public void Write(string filePath)
    {
        var folder = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(folder))
        {
            Directory.CreateDirectory(folder);
        }

        File.WriteAllBytes(filePath, bytes);
    }

    // Forgets the account so the next login creates a new character, keeping the description
    // and settings.
    public void StartNewAccount()
    {
        Key = new LoginKey(0, 0, 0, NewAccountName, 0);
        CharacterName = "";
        Changed = true;
    }

    private Span<byte> SettingsField(int offset, int length) => bytes.AsSpan(LoginKey.Size + offset, length);
}
