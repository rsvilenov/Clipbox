using Clipbox.Core.ClipboardContent.Enums;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Infrastructure.UI;

namespace Clipbox.Views.FilterBand
{
    public class SourceMachineInfo : BindableObject, IMachineInfo
    {
        private string _machineName;
        public string MachineName
        {
            get { return _machineName; }
            set { SetField(ref _machineName, value); }
        }

        private string _machineUniqueId;
        public string MachineUniqueId
        {
            get { return _machineUniqueId; }
            set { SetField(ref _machineUniqueId, value); }
        }

        private MachineType _machineType;
        public MachineType MachineType
        {
            get { return _machineType; }
            set { SetField(ref _machineType, value); }
        }

        private string _userName;
        public string UserName
        {
            get { return _userName; }
            set { SetField(ref _userName, value); }
        }
    }
}
