namespace Tusk.Domain.Php;

public readonly record struct PhpVersion
{
    public string Value { get; }

    public PhpVersion(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("PHP version cannot be null or empty.", nameof(value));
        }

        Value = value.Trim();
    }

    public override string ToString() => Value;

    public static bool TryParse(string input, out PhpVersion version)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            version = default;
            return false;
        }

        version = new PhpVersion(input.Trim());
        return true;
    }
}
