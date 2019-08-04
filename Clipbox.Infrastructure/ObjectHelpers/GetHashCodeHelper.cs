using System.Collections.Generic;

namespace Clipbox.Infrastructure.ObjectHelpers
{
    public struct GetHashCodeHelper
    {
        // https://stackoverflow.com/questions/263400/what-is-the-best-algorithm-for-an-overridden-system-object-gethashcode
        // a slightly modified version of Şafak Gür's implementation from the thread above

        private readonly int _value;

        public GetHashCodeHelper(int value) => _value = value;

        public static GetHashCodeHelper Start { get; } = new GetHashCodeHelper(17);

        public static implicit operator int(GetHashCodeHelper hash) => hash._value;

        public GetHashCodeHelper Append<T>(T obj)
        {
            var h = EqualityComparer<T>.Default.GetHashCode(obj);
            return unchecked(new GetHashCodeHelper((_value * 31) + h));
        }

        public override int GetHashCode() => _value;
    }
}
