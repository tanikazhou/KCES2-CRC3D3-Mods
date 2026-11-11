using System.Reflection;
using BepInEx.Configuration;

namespace CRC3D3.KeyHints.Discovery;

/// <summary>Fields of one key hint tag class, found by name.</summary>
/// <remarks>
/// <para>Each plugin compiles its own copy of tag class, so each copy is its own type. Fields are looked up once per type.</para>
/// <para>Missing field = default value (older tag class). Field with other type = whole tag class rejected.</para>
/// </remarks>
internal sealed class TagFields
{
    private static readonly Dictionary<Type, TagFields> _found = [];
    private static readonly Dictionary<Type, string> _errors = [];

    private TagFields(Type type)
    {
        Label = Field(type, "Label");
        Order = Field(type, "Order");
        Context = Field(type, "Context");
        Active = Field(type, "Active");
        Failed = Field(type, "Failed");
        Key = Field(type, "Key");
        Changed = Field(type, "Changed");
    }

    /// <summary>Gets row text field, or <see langword="null"/>.</summary>
    public FieldInfo Label { get; }

    /// <summary>Gets sort key field, or <see langword="null"/>.</summary>
    public FieldInfo Order { get; }

    /// <summary>Gets context list field, or <see langword="null"/>.</summary>
    public FieldInfo Context { get; }

    /// <summary>Gets plugin state field, or <see langword="null"/>.</summary>
    public FieldInfo Active { get; }

    /// <summary>Gets failed feature field, or <see langword="null"/>.</summary>
    public FieldInfo Failed { get; }

    /// <summary>Gets fixed key field, or <see langword="null"/>.</summary>
    public FieldInfo Key { get; }

    /// <summary>Gets change signal field, or <see langword="null"/>.</summary>
    public FieldInfo Changed { get; }

    /// <summary>Gets fields of tag class.</summary>
    /// <param name="tagType">Tag class.</param>
    /// <param name="error">Field and type that do not match, or <see langword="null"/>.</param>
    /// <returns>Fields, or <see langword="null"/> when one field has wrong type.</returns>
    /// <remarks>Main thread only.</remarks>
    public static TagFields For(Type tagType, out string error)
    {
        if (_found.TryGetValue(tagType, out TagFields fields))
        {
            error = null;
            return fields;
        }

        if (_errors.TryGetValue(tagType, out error))
        {
            return null;
        }

        error = WrongType(tagType, "Label", typeof(string)) ?? WrongType(tagType, "Order", typeof(int))
            ?? WrongType(tagType, "Context", typeof(string)) ?? WrongType(tagType, "Active", typeof(bool))
            ?? WrongType(tagType, "Failed", typeof(bool)) ?? WrongType(tagType, "Key", typeof(KeyboardShortcut?))
            ?? WrongType(tagType, "Changed", typeof(Action));
        if (error != null)
        {
            _errors[tagType] = error;
            return null;
        }

        fields = new TagFields(tagType);
        _found[tagType] = fields;
        return fields;
    }

    private static FieldInfo Field(Type type, string name) => type.GetField(name, BindingFlags.Instance | BindingFlags.Public);

    private static string WrongType(Type type, string name, Type expected)
    {
        FieldInfo f = Field(type, name);
        return f == null || f.FieldType == expected ? null : $"field {name} is {f.FieldType.Name}, expected {expected.Name}";
    }
}
