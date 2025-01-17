// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Steeltoe.Security.Authentication.CloudFoundry.Test;

public class TestResponse : IHttpResponseFeature
{
    public Stream Body
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public bool HasStarted => throw new NotImplementedException();

    public IHeaderDictionary Headers
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public string ReasonPhrase
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public int StatusCode
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public void OnCompleted(Func<object, Task> callback, object state)
    {
    }

    public void OnStarting(Func<object, Task> callback, object state)
    {
    }
}
