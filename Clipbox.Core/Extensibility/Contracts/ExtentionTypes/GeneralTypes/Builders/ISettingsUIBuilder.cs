namespace Clipbox.Core.Extensibility.Contracts.ExtentionTypes.GeneralTypes.Builders
{
    public interface ISettingsUIBuilder
    {
        ISettingsUIBuilder Add(string settingName, ISettingUIPart uiPart);
    }

}
