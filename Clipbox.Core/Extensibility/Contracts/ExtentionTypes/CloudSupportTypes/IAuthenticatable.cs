using System;
using System.IO;

namespace Clipbox.Infrastructure.CloudSupport
{
    /// <summary>
    /// This interface is for cloud service auth APIs, which are async only.
    /// It allows us to use the Auth api without the need to await it.
    /// </summary>
    public interface IAuthenticatable
    {
        void InitiateAuthentication(object loginInfo);

        bool IsAuthenticated { get; }

        event EventHandler Authenticated;

        event EventHandler<ErrorEventArgs> AuthenticationFailed;
    }
}
