// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace Steeltoe.Common.Converter;

public class NumberToCharacterConverter : AbstractGenericConverter
{
    public NumberToCharacterConverter()
        : base(GetConvertiblePairs())
    {
    }

    public override object Convert(object source, Type sourceType, Type targetType)
    {
        return System.Convert.ToChar(source, CultureInfo.InvariantCulture);
    }

    private static ISet<(Type SourceType, Type TargetType)> GetConvertiblePairs()
    {
        return new HashSet<(Type SourceType, Type TargetType)>
        {
            (typeof(int), typeof(char)),
            (typeof(uint), typeof(char)),
            (typeof(ulong), typeof(char)),
            (typeof(long), typeof(char)),
            (typeof(short), typeof(char)),
            (typeof(ushort), typeof(char))
        };
    }
}
