﻿using JasperFx.Core;
using JasperFx.Core.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Weasel.Core.Migrations;
using Wolverine.Configuration;
using Wolverine.Postgresql.Transport;

namespace Wolverine.Postgresql;

public static class PostgresqlConfigurationExtensions
{
    /// <summary>
    ///     Register Postgresql backed message persistence to a known connection string
    /// </summary>
    /// <param name="options"></param>
    /// <param name="connectionString"></param>
    /// <param name="schema"></param>
    public static void PersistMessagesWithPostgresql(this WolverineOptions options, string connectionString,
        string? schema = null)
    {
        options.Include<PostgresqlBackedPersistence>(o =>
        {
            o.Settings.ConnectionString = connectionString;
            if (schema.IsNotEmpty())
            {
                o.Settings.SchemaName = schema;
            }
            
            o.Settings.ScheduledJobLockId = $"{schema ?? "public"}:scheduled-jobs".GetDeterministicHashCode();
        });
    }
    
    /// <summary>
    /// Register PostgreSQL backed message persistence *and* the PostgreSQL messaging transport
    /// </summary>
    /// <param name="options"></param>
    /// <param name="connectionString"></param>
    /// <param name="schema"></param>
    /// <returns></returns>
    public static PostgresqlPersistenceExpression UsePostgresqlPersistenceAndTransport(this WolverineOptions options,
        string connectionString,
        string? schema = null)
    {
        var extension = new PostgresqlBackedPersistence();
        extension.Settings.ConnectionString = connectionString;

        if (schema.IsNotEmpty())
        {
            extension.Settings.SchemaName = schema;
        }
        else
        {
            schema = "dbo";
                
        }

        options.Services.AddTransient<IDatabase, PostgresqlTransportDatabase>();

        extension.Settings.ScheduledJobLockId = $"{schema}:scheduled-jobs".GetDeterministicHashCode();
        options.Include(extension);
        
        options.Include<PostgresqlBackedPersistence>(x =>
        {
            x.Settings.ConnectionString = connectionString;

            if (schema.IsNotEmpty())
            {
                x.Settings.SchemaName = schema;
            }
            else
            {
                schema = "dbo";
                
            }

            x.Settings.ScheduledJobLockId = $"{schema}:scheduled-jobs".GetDeterministicHashCode();
        });

        var transport = new PostgresqlTransport(extension.Settings);
        options.Transports.Add(transport);

        return new PostgresqlPersistenceExpression(transport, options);
    }
    
        /// <summary>
    ///     Quick access to the Postgresql Transport within this application.
    ///     This is for advanced usage
    /// </summary>
    /// <param name="endpoints"></param>
    /// <returns></returns>
    internal static PostgresqlTransport PostgresqlTransport(this WolverineOptions endpoints)
    {
        var transports = endpoints.As<WolverineOptions>().Transports;

        try
        {
            return transports.OfType<PostgresqlTransport>().Single();
        }
        catch (Exception)
        {
            throw new InvalidOperationException("The Sql Server transport is not registered in this system");
        }
    }

    /// <summary>
    /// Listen for incoming messages at the designated Sql Server queue by name
    /// </summary>
    /// <param name="endpoints"></param>
    /// <param name="queueName"></param>
    /// <returns></returns>
    public static PostgresqlListenerConfiguration ListenToPostgresqlQueue(this WolverineOptions endpoints, string queueName)
    {
        var transport = endpoints.PostgresqlTransport();
        var corrected = transport.MaybeCorrectName(queueName);
        var queue = transport.Queues[corrected];
        queue.EndpointName = queueName;
        queue.IsListener = true;

        return new PostgresqlListenerConfiguration(queue);
    }

    /// <summary>
    ///     Publish matching messages straight to a Sql Server queue using the queue name
    /// </summary>
    /// <param name="publishing"></param>
    /// <param name="queueName"></param>
    /// <returns></returns>
    public static PostgresqlSubscriberConfiguration ToPostgresqlQueue(this IPublishToExpression publishing,
        string queueName)
    {
        var transports = publishing.As<PublishingExpression>().Parent.Transports;
        var transport = transports.OfType<PostgresqlTransport>().Single();

        var corrected = transport.MaybeCorrectName(queueName);
        var queue = transport.Queues[corrected];
        queue.EndpointName = queueName;

        // This is necessary unfortunately to hook up the subscription rules
        publishing.To(queue.Uri);

        return new PostgresqlSubscriberConfiguration(queue);
    }
    
}