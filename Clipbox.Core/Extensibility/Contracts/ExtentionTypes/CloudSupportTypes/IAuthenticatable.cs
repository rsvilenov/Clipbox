using Clipbox.Core.Extensibility.EventArgsClasses;
using System;

namespace Clipbox.Infrastructure.CloudSupport
{
    /// <summary>
    /// This interface is for cloud service auth APIs, which are async only.
    /// It allows us to use the Auth api without the need to await it.
    /// </summary>
    public interface IAuthenticatable
    {
        void InitiateAuthentication();

        void ChangeLogin();

        bool IsAuthenticated { get; }

        event EventHandler<AuthenticationStageEventArgs> AuthenticationStageChanged;
    }
}
