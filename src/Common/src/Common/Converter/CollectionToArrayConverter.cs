// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace Steeltoe.Common.Converter;

public class CollectionToArrayConverter : AbstractGenericConditionalConverter
{
    private readonly IConversionService _conversionService;

    public CollectionToArrayConverter(IConversionService conversionService)
        : base(GetConvertiblePairs())
    {
        _conversionService = conversionService;
    }

    public override bool Matches(Type sourceType, Type targetType)
    {
        if (!targetType.IsArray || sourceType.IsArray)
        {
            return false;
        }

        return ConversionUtils.CanConvertElements(ConversionUtils.GetElementType(sourceType), ConversionUtils.GetElementType(targetType), _conversionService);
    }

    public override object Convert(object source, Type sourceType, Type targetType)
    {
        if (source == null)
        {
            return null;
        }

        Type targetElementType = ConversionUtils.GetElementType(targetType);

        if (targetElementType == null)
        {
            throw new InvalidOperationException("No target element type");
        }

        var enumerable = source as IEnumerable;
        int len = ConversionUtils.Count(enumerable);

        var array = Array.CreateInstance(targetElementType, len);
        int i = 0;

        foreach (object sourceElement in enumerable)
        {
            object targetElement = _conversionService.Convert(sourceElement, sourceElement.GetType(), targetElementType);
            array.SetValue(targetElement, i++);
        }

        return array;
    }

    private static ISet<(Type SourceType, Type TargetType)> GetConvertiblePairs()
    {
        return new HashSet<(Type SourceType, Type TargetType)>
        {
            (typeof(ICollection), typeof(object[])),
            (typeof(ISet<>), typeof(object[]))
        };
    }
}
