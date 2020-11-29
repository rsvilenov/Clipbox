using System;
using System.Collections.Generic;

namespace Clipbox.Core.Diagnostics
{
    [Serializable]
    public class AggregateReport
    {
        public AggregateReport(Version productVersion)
            : this()
        {
            ProductMajorVersion = productVersion.Major;
            ProductMinorVersion = productVersion.Minor;
            BuildNumber = productVersion.Build;
            Is64BitVersion = Environment.Is64BitProcess;
        }

        public AggregateReport()
        {
            ComponentErrors = new List<ComponentError>();
        }

        public void AddComponentError(Component component, string error)
        {
            ComponentErrors.Add(new ComponentError
            {
                FailedComponent = component,
                Error = error
            });
        }

        public DiagnosticsReport MainProcessReport { get; set; }

        public DiagnosticsReport GuardProcessReport { get; set; }

        public DiagnosticsReport SearchProcessReport { get; set; }

        public int ProductMajorVersion { get; set; }

        public int ProductMinorVersion { get; set; }

        public int BuildNumber { get; set; }

        public bool Is64BitVersion { get; set; }

        public List<ComponentError> ComponentErrors { get; set; }
    }


    [Serializable]
    public class ComponentError
    {
        public Component FailedComponent { get; set; }

        public string Error { get; set; }
    }

    public enum Component
    {
        MainProcess,
        Guard,
        SearchProcess,
        SearchIndex,
        Deskband,
        DeskbandOwner,
        DeskbandControl,
        Service
    }
}
