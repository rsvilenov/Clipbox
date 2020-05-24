using System;

namespace Clipbox.Extensions.BuiltIn.TextFormats.Enums
{
    [Flags]
    public enum PasswordMatchOptions
    {
        First,
        Second = 1,
        Third = 2,
        Fourth = 4
    }
}
