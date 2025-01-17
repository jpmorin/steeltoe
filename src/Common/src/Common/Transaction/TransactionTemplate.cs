// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;

namespace Steeltoe.Common.Transaction;

public class TransactionTemplate : DefaultTransactionDefinition
{
    private readonly ILogger _logger;

    public IPlatformTransactionManager TransactionManager { get; set; }

    public TransactionTemplate(ILogger logger = null)
    {
        _logger = logger;
    }

    public TransactionTemplate(IPlatformTransactionManager transactionManager, ILogger logger = null)
    {
        _logger = logger;
        TransactionManager = transactionManager;
    }

    public TransactionTemplate(IPlatformTransactionManager transactionManager, ITransactionDefinition transactionDefinition, ILogger logger = null)
        : base(transactionDefinition)
    {
        _logger = logger;
        TransactionManager = transactionManager;
    }

    public void Execute(Action<ITransactionStatus> action)
    {
        Execute<object>(s =>
        {
            action(s);
            return null;
        });
    }

    public T Execute<T>(Func<ITransactionStatus, T> action)
    {
        if (TransactionManager == null)
        {
            throw new InvalidOperationException("No PlatformTransactionManager set");
        }

        ITransactionStatus status = TransactionManager.GetTransaction(this);
        T result;

        try
        {
            result = action(status);
        }
        catch (Exception ex)
        {
            // Transactional code threw application exception -> rollback
            RollbackOnException(status, ex);
            throw;
        }

        TransactionManager.Commit(status);
        return result;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        return base.Equals(obj) && (obj is not TransactionTemplate otherTemplate || TransactionManager == otherTemplate.TransactionManager);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    private void RollbackOnException(ITransactionStatus status, Exception ex)
    {
        if (TransactionManager == null)
        {
            throw new InvalidOperationException("No PlatformTransactionManager set");
        }

        _logger?.LogDebug(ex, "Initiating transaction rollback on application exception");

        try
        {
            TransactionManager.Rollback(status);
        }
        catch (TransactionSystemException ex2)
        {
            _logger?.LogError(ex, "Application exception overridden by rollback exception");
            ex2.InitApplicationException(ex);
            throw;
        }
        catch (Exception ex2)
        {
            _logger?.LogError(ex2, "Application exception overridden by rollback exception: {original}", ex);
            throw;
        }
    }
}
