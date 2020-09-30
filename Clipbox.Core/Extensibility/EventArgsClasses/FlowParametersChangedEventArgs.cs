namespace Clipbox.Core.Extensibility.EventArgsClasses
{
    public class FlowParametersChangedEventArgs
    {
        public FlowParametersChangedEventArgs(bool areParametersSetToInitialState)
        {
            AreParametersSetToInitialState = areParametersSetToInitialState;
        }

        public bool AreParametersSetToInitialState { get; }
    }
}
