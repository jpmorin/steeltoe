// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Retry;
using Steeltoe.Common.Transaction;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.RabbitMQ.Batch;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Listener;
using Steeltoe.Messaging.RabbitMQ.Listener.Adapters;
using SimpleMessageConverter = Steeltoe.Messaging.RabbitMQ.Support.Converter.SimpleMessageConverter;

namespace Steeltoe.Messaging.RabbitMQ.Configuration;

public abstract class AbstractRabbitListenerContainerFactory<TContainer> : IRabbitListenerContainerFactory<TContainer>
    where TContainer : AbstractMessageListenerContainer
{
    private readonly IOptionsMonitor<RabbitOptions> _optionsMonitor;
    protected readonly ILogger Logger;
    protected readonly ILoggerFactory LoggerFactory;

    private ISmartMessageConverter _messageConverter;

    protected internal RabbitOptions Options
    {
        get
        {
            if (_optionsMonitor != null)
            {
                return _optionsMonitor.CurrentValue;
            }

            return null;
        }
    }

    public IApplicationContext ApplicationContext { get; set; }

    public IConnectionFactory ConnectionFactory { get; set; }

    public IErrorHandler ErrorHandler { get; set; }

    public ISmartMessageConverter MessageConverter
    {
        get
        {
            _messageConverter ??= ApplicationContext?.GetService<ISmartMessageConverter>() ??
                new SimpleMessageConverter(LoggerFactory?.CreateLogger<SimpleMessageConverter>());

            return _messageConverter;
        }
        set => _messageConverter = value;
    }

    public AcknowledgeMode? AcknowledgeMode { get; set; }

    public bool? IsChannelTransacted { get; set; }

    public IPlatformTransactionManager TransactionManager { get; set; }

    public int? PrefetchCount { get; set; }

    public bool? DefaultRequeueRejected { get; set; }

    public int? RecoveryInterval { get; set; }

    public IBackOff RecoveryBackOff { get; set; }

    public bool? MissingQueuesFatal { get; set; }

    public bool? MismatchedQueuesFatal { get; set; }

    public IConsumerTagStrategy ConsumerTagStrategy { get; set; }

    public int? IdleEventInterval { get; set; }

    public int? FailedDeclarationRetryInterval { get; set; }

    public bool? AutoStartup { get; set; }

    public int Phase { get; set; }

    public List<IMessagePostProcessor> AfterReceivePostProcessors { get; set; }

    public List<IMessagePostProcessor> BeforeSendReplyPostProcessors { get; set; }

    public RetryTemplate RetryTemplate { get; set; }

    public IRecoveryCallback ReplyRecoveryCallback { get; set; }

    public Action<TContainer> ContainerCustomizer { get; set; }

    public bool BatchListener { get; set; }

    public IBatchingStrategy BatchingStrategy { get; set; }

    public bool? DeBatchingEnabled { get; set; }

    public virtual string ServiceName { get; set; }

    public bool PossibleAuthenticationFailureFatal { get; set; } = true;

    protected AbstractRabbitListenerContainerFactory(IApplicationContext applicationContext, ILoggerFactory loggerFactory = null)
    {
        ApplicationContext = applicationContext;
        LoggerFactory = loggerFactory;
    }

    protected AbstractRabbitListenerContainerFactory(IApplicationContext applicationContext, IConnectionFactory connectionFactory,
        ILoggerFactory loggerFactory = null)
    {
        ApplicationContext = applicationContext;
        LoggerFactory = loggerFactory;
        ConnectionFactory = connectionFactory;
    }

    protected AbstractRabbitListenerContainerFactory(IApplicationContext applicationContext, IOptionsMonitor<RabbitOptions> optionsMonitor,
        IConnectionFactory connectionFactory, ILoggerFactory loggerFactory = null)
    {
        ApplicationContext = applicationContext;
        LoggerFactory = loggerFactory;
        ConnectionFactory = connectionFactory;
        _optionsMonitor = optionsMonitor;
    }

    public void SetAfterReceivePostProcessors(params IMessagePostProcessor[] postProcessors)
    {
        ArgumentGuard.NotNull(postProcessors);
        ArgumentGuard.ElementsNotNull(postProcessors);

        AfterReceivePostProcessors = new List<IMessagePostProcessor>(postProcessors);
    }

    public void SetBeforeSendReplyPostProcessors(params IMessagePostProcessor[] postProcessors)
    {
        ArgumentGuard.NotNull(postProcessors);
        ArgumentGuard.ElementsNotNull(postProcessors);

        BeforeSendReplyPostProcessors = new List<IMessagePostProcessor>(postProcessors);
    }

    public TContainer CreateListenerContainer(IRabbitListenerEndpoint endpoint)
    {
        TContainer instance = CreateContainerInstance();

        instance.ConnectionFactory = ConnectionFactory;
        instance.ErrorHandler = ErrorHandler;
        instance.PossibleAuthenticationFailureFatal = PossibleAuthenticationFailureFatal;

        if (MessageConverter != null && endpoint != null)
        {
            endpoint.MessageConverter = MessageConverter;
        }

        if (AcknowledgeMode.HasValue)
        {
            instance.AcknowledgeMode = AcknowledgeMode.Value;
        }

        if (IsChannelTransacted.HasValue)
        {
            instance.IsChannelTransacted = IsChannelTransacted.Value;
        }

        if (ApplicationContext != null)
        {
            instance.ApplicationContext = ApplicationContext;
        }

        if (TransactionManager != null)
        {
            instance.TransactionManager = TransactionManager;
        }

        if (PrefetchCount.HasValue)
        {
            instance.PrefetchCount = PrefetchCount.Value;
        }

        if (DefaultRequeueRejected.HasValue)
        {
            instance.DefaultRequeueRejected = DefaultRequeueRejected.Value;
        }

        if (RecoveryBackOff != null)
        {
            instance.RecoveryBackOff = RecoveryBackOff;
        }

        if (MismatchedQueuesFatal.HasValue)
        {
            instance.MismatchedQueuesFatal = MismatchedQueuesFatal.Value;
        }

        if (MissingQueuesFatal.HasValue)
        {
            instance.MissingQueuesFatal = MissingQueuesFatal.Value;
        }

        if (ConsumerTagStrategy != null)
        {
            instance.ConsumerTagStrategy = ConsumerTagStrategy;
        }

        if (IdleEventInterval.HasValue)
        {
            instance.IdleEventInterval = IdleEventInterval.Value;
        }

        if (FailedDeclarationRetryInterval.HasValue)
        {
            instance.FailedDeclarationRetryInterval = FailedDeclarationRetryInterval.Value;
        }

        if (AutoStartup.HasValue)
        {
            instance.IsAutoStartup = AutoStartup.Value;
        }

        instance.Phase = Phase;

        if (AfterReceivePostProcessors != null)
        {
            instance.SetAfterReceivePostProcessors(AfterReceivePostProcessors.ToArray());
        }

        if (DeBatchingEnabled.HasValue)
        {
            instance.IsDeBatchingEnabled = DeBatchingEnabled.Value;
        }

        if (BatchListener && DeBatchingEnabled.HasValue)
        {
            // turn off container debatching by default for batch listeners
            instance.IsDeBatchingEnabled = false;
        }

        if (endpoint != null)
        {
            if (endpoint.AutoStartup.HasValue)
            {
                instance.IsAutoStartup = endpoint.AutoStartup.Value;
            }

            if (endpoint.AckMode.HasValue)
            {
                instance.AcknowledgeMode = endpoint.AckMode.Value;
            }

            if (BatchingStrategy != null)
            {
                endpoint.BatchingStrategy = BatchingStrategy;
            }

            instance.ListenerId = endpoint.Id;
            endpoint.BatchListener = BatchListener;
            endpoint.SetupListenerContainer(instance);
        }

        if (instance.MessageListener is AbstractMessageListenerAdapter adapterListener)
        {
            if (BeforeSendReplyPostProcessors != null)
            {
                adapterListener.SetBeforeSendReplyPostProcessors(BeforeSendReplyPostProcessors.ToArray());
            }

            if (RetryTemplate != null)
            {
                adapterListener.RetryTemplate = RetryTemplate;

                if (ReplyRecoveryCallback != null)
                {
                    adapterListener.RecoveryCallback = ReplyRecoveryCallback;
                }
            }

            if (DefaultRequeueRejected.HasValue)
            {
                adapterListener.DefaultRequeueRejected = DefaultRequeueRejected.Value;
            }

            if (endpoint != null && endpoint.ReplyPostProcessor != null)
            {
                adapterListener.ReplyPostProcessor = endpoint.ReplyPostProcessor;
            }
        }

        InitializeContainer(instance, endpoint);

        ContainerCustomizer?.Invoke(instance);

        return instance;
    }

    IMessageListenerContainer IRabbitListenerContainerFactory.CreateListenerContainer(IRabbitListenerEndpoint endpoint)
    {
        return CreateListenerContainer(endpoint);
    }

    protected abstract TContainer CreateContainerInstance();

    protected virtual void InitializeContainer(TContainer instance, IRabbitListenerEndpoint endpoint)
    {
    }
}
