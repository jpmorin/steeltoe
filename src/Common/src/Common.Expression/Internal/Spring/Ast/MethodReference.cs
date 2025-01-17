// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Reflection.Emit;
using Steeltoe.Common.Expression.Internal.Spring.Support;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast;

public class MethodReference : SpelNode
{
    private readonly bool _nullSafe;

    private TypeDescriptor _originalPrimitiveExitTypeDescriptor;

    private volatile CachedMethodExecutor _cachedExecutor;

    public string Name { get; }

    public MethodReference(bool nullSafe, string methodName, int startPos, int endPos, params SpelNode[] arguments)
        : base(startPos, endPos, arguments)
    {
        Name = methodName;
        _nullSafe = nullSafe;
    }

    public override ITypedValue GetValueInternal(ExpressionState state)
    {
        IEvaluationContext evaluationContext = state.EvaluationContext;
        object value = state.GetActiveContextObject().Value;
        Type targetType = state.GetActiveContextObject().TypeDescriptor;
        object[] arguments = GetArguments(state);
        ITypedValue result = GetValueInternal(evaluationContext, value, targetType, arguments);
        UpdateExitTypeDescriptor(result.Value);
        return result;
    }

    public override string ToStringAst()
    {
        var sj = new List<string>();

        for (int i = 0; i < ChildCount; i++)
        {
            sj.Add(GetChild(i).ToStringAst());
        }

        return $"{Name}({string.Join(",", sj)})";
    }

    public override bool IsCompilable()
    {
        CachedMethodExecutor executorToCheck = _cachedExecutor;

        if (executorToCheck == null || executorToCheck.HasProxyTarget || executorToCheck.Get() is not ReflectiveMethodExecutor)
        {
            return false;
        }

        foreach (SpelNode child in children)
        {
            if (!child.IsCompilable())
            {
                return false;
            }
        }

        var executor = (ReflectiveMethodExecutor)executorToCheck.Get();

        if (executor.DidArgumentConversionOccur)
        {
            return false;
        }

        Type type = executor.Method.DeclaringType;

        if (!ReflectionHelper.IsPublic(type) && executor.GetPublicDeclaringClass() == null)
        {
            return false;
        }

        return true;
    }

    public override void GenerateCode(ILGenerator gen, CodeFlow cf)
    {
        MethodInfo method = GetTargetMethodAndType(out Type classType);

        if (method.IsStatic)
        {
            GenerateStaticMethodCode(gen, cf, method);
        }
        else
        {
            GenerateInstanceMethodCode(gen, cf, method, classType);
        }

        cf.PushDescriptor(exitTypeDescriptor);
    }

    protected internal override IValueRef GetValueRef(ExpressionState state)
    {
        object[] arguments = GetArguments(state);

        if (state.GetActiveContextObject().Value == null)
        {
            ThrowIfNotNullSafe(GetArgumentTypes(arguments));
            return NullValueRef.Instance;
        }

        return new MethodValueRef(this, state, arguments);
    }

    protected internal TypeDescriptor ComputeExitDescriptor(object result, Type propertyReturnType)
    {
        if (propertyReturnType.IsValueType)
        {
            return CodeFlow.ToDescriptor(propertyReturnType);
        }

        return CodeFlow.ToDescriptorFromObject(result);
    }

    private void GenerateStaticMethodCode(ILGenerator gen, CodeFlow cf, MethodInfo method)
    {
        TypeDescriptor stackDescriptor = cf.LastDescriptor();
        Label? skipIfNullTarget = null;

        if (_nullSafe)
        {
            skipIfNullTarget = GenerateNullCheckCode(gen);
        }

        if (stackDescriptor != null)
        {
            // Something on the stack when nothing is needed
            gen.Emit(OpCodes.Pop);
        }

        GenerateCodeForArguments(gen, cf, method, children);
        gen.Emit(OpCodes.Call, method);

        if (_originalPrimitiveExitTypeDescriptor != null)
        {
            // The output of the accessor will be a primitive but from the block above it might be null,
            // so to have a 'common stack' element at skipIfNull target we need to box the primitive
            CodeFlow.InsertBoxIfNecessary(gen, _originalPrimitiveExitTypeDescriptor);
        }

        if (skipIfNullTarget.HasValue)
        {
            gen.MarkLabel(skipIfNullTarget.Value);
        }
    }

    private void GenerateInstanceMethodCode(ILGenerator gen, CodeFlow cf, MethodInfo targetMethod, Type targetType)
    {
        TypeDescriptor stackDescriptor = cf.LastDescriptor();

        if (stackDescriptor == null)
        {
            // Nothing on the stack but something is needed
            CodeFlow.LoadTarget(gen);
            stackDescriptor = TypeDescriptor.Object;
        }

        Label? skipIfNullTarget = null;

        if (_nullSafe)
        {
            skipIfNullTarget = GenerateNullCheckCode(gen);
        }

        if (targetType.IsValueType)
        {
            if (stackDescriptor.IsBoxed || stackDescriptor.IsReferenceType)
            {
                gen.Emit(OpCodes.Unbox_Any, targetType);
            }

            LocalBuilder local = gen.DeclareLocal(targetType);
            gen.Emit(OpCodes.Stloc, local);
            gen.Emit(OpCodes.Ldloca, local);
        }
        else
        {
            if (stackDescriptor.Value != targetType)
            {
                CodeFlow.InsertCastClass(gen, new TypeDescriptor(targetType));
            }
        }

        GenerateCodeForArguments(gen, cf, targetMethod, children);
        gen.Emit(targetType.IsValueType ? OpCodes.Call : OpCodes.Callvirt, targetMethod);

        if (_originalPrimitiveExitTypeDescriptor != null)
        {
            // The output of the accessor will be a primitive but from the block above it might be null,
            // so to have a 'common stack' element at skipIfNull target we need to box the primitive
            CodeFlow.InsertBoxIfNecessary(gen, _originalPrimitiveExitTypeDescriptor);
        }

        if (skipIfNullTarget.HasValue)
        {
            gen.MarkLabel(skipIfNullTarget.Value);
        }
    }

    private Label GenerateNullCheckCode(ILGenerator gen)
    {
        Label skipIfNullTarget = gen.DefineLabel();
        Label continueTarget = gen.DefineLabel();
        gen.Emit(OpCodes.Dup);
        gen.Emit(OpCodes.Ldnull);
        gen.Emit(OpCodes.Cgt_Un);
        gen.Emit(OpCodes.Brtrue, continueTarget);

        // cast null on stack to result type
        CodeFlow.InsertCastClass(gen, exitTypeDescriptor);
        gen.Emit(OpCodes.Br, skipIfNullTarget);
        gen.MarkLabel(continueTarget);
        return skipIfNullTarget;
    }

    private ITypedValue GetValueInternal(IEvaluationContext evaluationContext, object value, Type targetType, object[] arguments)
    {
        List<Type> argumentTypes = GetArgumentTypes(arguments);

        if (value == null)
        {
            ThrowIfNotNullSafe(argumentTypes);
            return TypedValue.Null;
        }

        IMethodExecutor executorToUse = GetCachedExecutor(evaluationContext, value, targetType, argumentTypes);

        if (executorToUse != null)
        {
            try
            {
                return executorToUse.Execute(evaluationContext, value, arguments);
            }
            catch (AccessException ex)
            {
                // Two reasons this can occur:
                // 1. the method invoked actually threw a real exception
                // 2. the method invoked was not passed the arguments it expected and
                //    has become 'stale'

                // In the first case we should not retry, in the second case we should see
                // if there is a better suited method.

                // To determine the situation, the AccessException will contain a cause.
                // If the cause is an InvocationTargetException, a user exception was
                // thrown inside the method. Otherwise the method could not be invoked.
                ThrowSimpleExceptionIfPossible(value, ex);

                // At this point we know it wasn't a user problem so worth a retry if a
                // better candidate can be found.
                _cachedExecutor = null;
            }
        }

        // either there was no accessor or it no longer existed
        executorToUse = FindAccessorForMethod(argumentTypes, value, evaluationContext);
        _cachedExecutor = new CachedMethodExecutor(executorToUse, value as Type, targetType, argumentTypes);

        try
        {
            return executorToUse.Execute(evaluationContext, value, arguments);
        }
        catch (AccessException ex)
        {
            // Same unwrapping exception handling as above in above catch block
            ThrowSimpleExceptionIfPossible(value, ex);
            throw new SpelEvaluationException(StartPosition, ex, SpelMessage.ExceptionDuringMethodInvocation, Name, value.GetType().FullName, ex.Message);
        }
    }

    private void ThrowIfNotNullSafe(IList<Type> argumentTypes)
    {
        if (!_nullSafe)
        {
            throw new SpelEvaluationException(StartPosition, SpelMessage.MethodCallOnNullObjectNotAllowed,
                FormatHelper.FormatMethodForMessage(Name, argumentTypes));
        }
    }

    private void ThrowSimpleExceptionIfPossible(object value, AccessException ex)
    {
        if (ex.InnerException is TargetInvocationException)
        {
            Exception rootCause = ex.InnerException.InnerException;

            if (rootCause is SystemException exception)
            {
                throw exception;
            }

            throw new ExpressionInvocationTargetException(StartPosition,
                $"A problem occurred when trying to execute method '{Name}' on object of type [{value.GetType().FullName}]", rootCause);
        }
    }

    private object[] GetArguments(ExpressionState state)
    {
        object[] arguments = new object[ChildCount];

        for (int i = 0; i < arguments.Length; i++)
        {
            // Make the root object the active context again for evaluating the parameter expressions
            try
            {
                state.PushActiveContextObject(state.GetScopeRootContextObject());
                arguments[i] = children[i].GetValueInternal(state).Value;
            }
            finally
            {
                state.PopActiveContextObject();
            }
        }

        return arguments;
    }

    private List<Type> GetArgumentTypes(params object[] arguments)
    {
        var descriptors = new List<Type>(arguments.Length);

        foreach (object argument in arguments)
        {
            descriptors.Add(argument?.GetType());
        }

        return descriptors;
    }

    private IMethodExecutor GetCachedExecutor(IEvaluationContext evaluationContext, object value, Type targetType, IList<Type> argumentTypes)
    {
        List<IMethodResolver> methodResolvers = evaluationContext.MethodResolvers;

        if (methodResolvers.Count != 1 || methodResolvers[0] is not ReflectiveMethodResolver)
        {
            // Not a default ReflectiveMethodResolver - don't know whether caching is valid
            return null;
        }

        CachedMethodExecutor executorToCheck = _cachedExecutor;

        if (executorToCheck != null && executorToCheck.IsSuitable(value, targetType, argumentTypes))
        {
            return executorToCheck.Get();
        }

        _cachedExecutor = null;
        return null;
    }

    private IMethodExecutor FindAccessorForMethod(List<Type> argumentTypes, object targetObject, IEvaluationContext evaluationContext)
    {
        AccessException accessException = null;
        List<IMethodResolver> methodResolvers = evaluationContext.MethodResolvers;

        foreach (IMethodResolver methodResolver in methodResolvers)
        {
            try
            {
                IMethodExecutor methodExecutor = methodResolver.Resolve(evaluationContext, targetObject, Name, argumentTypes);

                if (methodExecutor != null)
                {
                    return methodExecutor;
                }
            }
            catch (AccessException ex)
            {
                accessException = ex;
                break;
            }
        }

        string method = FormatHelper.FormatMethodForMessage(Name, argumentTypes);
        string className = FormatHelper.FormatClassNameForMessage(targetObject as Type ?? targetObject.GetType());

        if (accessException != null)
        {
            throw new SpelEvaluationException(StartPosition, accessException, SpelMessage.ProblemLocatingMethod, method, className);
        }

        throw new SpelEvaluationException(StartPosition, SpelMessage.MethodNotFound, method, className);
    }

    private void UpdateExitTypeDescriptor(object result)
    {
        CachedMethodExecutor executorToCheck = _cachedExecutor;

        if (executorToCheck != null && executorToCheck.Get() is ReflectiveMethodExecutor executor)
        {
            MethodInfo method = executor.Method;
            TypeDescriptor descriptor = ComputeExitDescriptor(result, method.ReturnType);

            if (_nullSafe && CodeFlow.IsValueType(descriptor))
            {
                _originalPrimitiveExitTypeDescriptor = descriptor;
                exitTypeDescriptor = CodeFlow.ToBoxedDescriptor(descriptor);
            }
            else
            {
                exitTypeDescriptor = descriptor;
            }
        }
    }

    private MethodInfo GetTargetMethodAndType(out Type targetType)
    {
        var methodExecutor = (ReflectiveMethodExecutor)_cachedExecutor?.Get();

        if (methodExecutor == null)
        {
            throw new InvalidOperationException($"No applicable cached executor found: {_cachedExecutor}");
        }

        MethodInfo method = methodExecutor.Method;
        targetType = GetMethodTargetType(method, methodExecutor);
        return method;
    }

    private Type GetMethodTargetType(MethodInfo method, ReflectiveMethodExecutor methodExecutor)
    {
        if (ReflectionHelper.IsPublic(method.DeclaringType))
        {
            return method.DeclaringType;
        }

        return methodExecutor.GetPublicDeclaringClass();
    }

    private sealed class MethodValueRef : IValueRef
    {
        private readonly IEvaluationContext _evaluationContext;
        private readonly object _value;
        private readonly Type _targetType;
        private readonly object[] _arguments;
        private readonly MethodReference _methodReference;

        public bool IsWritable => false;

        public MethodValueRef(MethodReference methodReference, ExpressionState state, object[] arguments)
        {
            _methodReference = methodReference;
            _evaluationContext = state.EvaluationContext;
            _value = state.GetActiveContextObject().Value;
            _targetType = state.GetActiveContextObject().TypeDescriptor;
            _arguments = arguments;
        }

        public ITypedValue GetValue()
        {
            ITypedValue result = _methodReference.GetValueInternal(_evaluationContext, _value, _targetType, _arguments);
            _methodReference.UpdateExitTypeDescriptor(result.Value);
            return result;
        }

        public void SetValue(object newValue)
        {
            throw new InvalidOperationException();
        }
    }

    private sealed class CachedMethodExecutor
    {
        private readonly IMethodExecutor _methodExecutor;
        private readonly Type _staticClass;
        private readonly Type _targetType;
        private readonly List<Type> _argumentTypes;

        public bool HasProxyTarget => false;

        public CachedMethodExecutor(IMethodExecutor methodExecutor, Type staticClass, Type targetType, List<Type> argumentTypes)
        {
            _methodExecutor = methodExecutor;
            _staticClass = staticClass;
            _targetType = targetType;
            _argumentTypes = argumentTypes;
        }

        public bool IsSuitable(object value, Type targetType, IList<Type> argumentTypes)
        {
            return (_staticClass == null || _staticClass.Equals(value)) && _targetType == targetType && AreEqual(_argumentTypes, argumentTypes);
        }

        public IMethodExecutor Get()
        {
            return _methodExecutor;
        }

        private static bool AreEqual(IList<Type> list1, IList<Type> list2)
        {
            if (ReferenceEquals(list1, list2))
            {
                return true;
            }

            return list1 is not null && list1.SequenceEqual(list2);
        }
    }
}
