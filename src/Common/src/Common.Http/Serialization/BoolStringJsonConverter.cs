// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Steeltoe.Common.Http.Serialization;

public class BoolStringJsonConverter : JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType == JsonTokenType.False || reader.TokenType == JsonTokenType.True ? reader.GetBoolean() : bool.Parse(reader.GetString());
    }

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
    {
#pragma warning disable S4040 // Strings should be normalized to uppercase
        writer.WriteStringValue(value.ToString(CultureInfo.InvariantCulture).ToLowerInvariant());
#pragma warning restore S4040 // Strings should be normalized to uppercase
    }
}
