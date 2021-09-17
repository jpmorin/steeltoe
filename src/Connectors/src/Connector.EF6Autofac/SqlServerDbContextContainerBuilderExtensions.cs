﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using Autofac.Builder;
using Microsoft.Extensions.Configuration;
using Steeltoe.CloudFoundry.Connector.Relational;
using Steeltoe.CloudFoundry.Connector.Services;
using Steeltoe.CloudFoundry.Connector.SqlServer;
using Steeltoe.Common.HealthChecks;
using System;
using System.Data;

namespace Steeltoe.CloudFoundry.Connector.EF6Autofac
{
    public static class SqlServerDbContextContainerBuilderExtensions
    {
        /// <summary>
        /// Add your SqlServer-based DbContext to the ContainerBuilder
        /// </summary>
        /// <typeparam name="TContext">Your DbContext</typeparam>
        /// <param name="container">Autofac <see cref="ContainerBuilder" /></param>
        /// <param name="config">Your app config</param>
        /// <param name="serviceName">Name of service instance</param>
        /// <returns><see cref="IRegistrationBuilder{TLimit, TActivatorData, TRegistrationStyle}"/></returns>
        [Obsolete("Use RegisterSqlServerDbContext instead")]
        public static IRegistrationBuilder<object, SimpleActivatorData, SingleRegistrationStyle> RegisterDbContext<TContext>(this ContainerBuilder container, IConfiguration config, string serviceName = null)
            => container.RegisterSqlServerDbContext<TContext>(config, serviceName);

        /// <summary>
        /// Add your SqlServer-based DbContext to the ContainerBuilder
        /// </summary>
        /// <typeparam name="TContext">Your DbContext</typeparam>
        /// <param name="container">Autofac <see cref="ContainerBuilder" /></param>
        /// <param name="config">Your app config</param>
        /// <param name="serviceName">Name of service instance</param>
        /// <returns><see cref="IRegistrationBuilder{TLimit, TActivatorData, TRegistrationStyle}"/></returns>
        public static IRegistrationBuilder<object, SimpleActivatorData, SingleRegistrationStyle> RegisterSqlServerDbContext<TContext>(this ContainerBuilder container, IConfiguration config, string serviceName = null)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var info = serviceName == null
                ? config.GetSingletonServiceInfo<SqlServerServiceInfo>()
                : config.GetRequiredServiceInfo<SqlServerServiceInfo>(serviceName);

            var sqlServerConfig = new SqlServerProviderConnectorOptions(config);
            var factory = new SqlServerProviderConnectorFactory(info, sqlServerConfig, typeof(TContext));

            var healthFactory = new SqlServerProviderConnectorFactory(info, sqlServerConfig, SqlServerTypeLocator.SqlConnection);
            container.Register(c => new RelationalHealthContributor((IDbConnection)healthFactory.Create(null))).As<IHealthContributor>();

            return container.Register(c => factory.Create(null)).As<TContext>();
        }
    }
}