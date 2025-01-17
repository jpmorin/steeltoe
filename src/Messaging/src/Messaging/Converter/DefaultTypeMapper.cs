// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Runtime.Loader;

namespace Steeltoe.Messaging.Converter;

public class DefaultTypeMapper : AbstractTypeMapper, ITypeMapper
{
    public TypePrecedence Precedence { get; set; } = TypePrecedence.Inferred;

    public Type DefaultType { get; set; } = typeof(object);

    public Type ToType(IMessageHeaders headers)
    {
        Type inferredType = GetInferredType(headers);

        if (inferredType != null && !inferredType.IsAbstract && !inferredType.IsInterface)
        {
            return inferredType;
        }

        string typeIdHeader = RetrieveHeaderAsString(headers, ClassIdFieldName);

        if (typeIdHeader != null)
        {
            return FromTypeHeader(headers, typeIdHeader);
        }

        if (HasInferredTypeHeader(headers))
        {
            return FromInferredTypeHeader(headers);
        }

        return DefaultType;
    }

    public void FromType(Type type, IMessageHeaders headers)
    {
        AddHeader(headers, ClassIdFieldName, type);

        if (IsContainerType(type))
        {
            AddHeader(headers, ContentClassIdFieldName, GetContentType(type));
        }

        Type keyType = GetKeyType(type);

        if (keyType != null)
        {
            AddHeader(headers, KeyClassIdFieldName, keyType);
        }
    }

    public Type GetInferredType(IMessageHeaders headers)
    {
        if (HasInferredTypeHeader(headers) && Precedence == TypePrecedence.Inferred)
        {
            return FromInferredTypeHeader(headers);
        }

        return null;
    }

    private Type FromTypeHeader(IMessageHeaders headers, string typeIdHeader)
    {
        Type classType = GetClassIdType(typeIdHeader);

        if (!IsContainerType(classType) || classType.IsArray)
        {
            return classType;
        }

        Type contentClassType = GetClassIdType(RetrieveHeader(headers, ContentClassIdFieldName));

        if (!HasKeyType(classType))
        {
            return ConstructCollectionType(classType, contentClassType);
        }

        Type keyClassType = GetClassIdType(RetrieveHeader(headers, KeyClassIdFieldName));
        return ConstructDictionaryType(classType, keyClassType, contentClassType);
    }

    private bool HasKeyType(Type classType)
    {
        if (typeof(Dictionary<,>) == classType)
        {
            return true;
        }

        return false;
    }

    private Type ConstructDictionaryType(Type classType, Type keyClassType, Type contentClassType)
    {
        return classType.MakeGenericType(keyClassType, contentClassType);
    }

    private Type ConstructCollectionType(Type classType, Type contentClassType)
    {
        return classType.MakeGenericType(contentClassType);
    }

    private Type GetClassIdType(string classId)
    {
        if (IdClassMapping.ContainsKey(classId))
        {
            return IdClassMapping[classId];
        }

        try
        {
            return Type.GetType(classId, true);
        }
        catch (Exception)
        {
            foreach (Assembly assembly in AssemblyLoadContext.Default.Assemblies)
            {
                Type result = assembly.GetType(classId, false);

                if (result != null)
                {
                    return result;
                }
            }

            throw new MessageConversionException($"failed to resolve class name. Class not found [{classId}]");
        }
    }
}
