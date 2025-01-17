// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;

namespace Steeltoe.Messaging.Converter;

public class DefaultContentTypeResolver : IContentTypeResolver
{
    public MimeType DefaultMimeType { get; set; }

    public MimeType Resolve(IMessageHeaders headers)
    {
        if (headers == null || !headers.ContainsKey(MessageHeaders.ContentType))
        {
            return DefaultMimeType;
        }

        object value = headers[MessageHeaders.ContentType];

        if (value == null)
        {
            return null;
        }

        if (value is MimeType mimeType)
        {
            return mimeType;
        }

        if (value is string stringValue)
        {
            return MimeType.ToMimeType(stringValue);
        }

        throw new ArgumentException($"Unknown type for contentType header value: {value.GetType()}", nameof(headers));
    }

    public override string ToString()
    {
        return $"DefaultContentTypeResolver[defaultMimeType={DefaultMimeType}]";
    }
}
