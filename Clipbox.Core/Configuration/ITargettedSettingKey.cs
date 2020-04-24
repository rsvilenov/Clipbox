using System;

namespace Clipbox.Core.Configuration
{
    public interface ITargettedSettingKey
    {
        Type ConsumerType { get; }
    }
}
