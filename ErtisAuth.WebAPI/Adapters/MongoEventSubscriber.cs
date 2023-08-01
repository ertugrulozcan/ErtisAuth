using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using MongoDB.Driver.Core.Events;
using Newtonsoft.Json;

namespace ErtisAuth.WebAPI.Adapters;

public class MongoEventSubscriber : IEventSubscriber
{
    #region Services

    private readonly IEventSubscriber _subscriber;

    #endregion
    
    #region Constructors

    /// <summary>
    /// Constructor
    /// </summary>
    public MongoEventSubscriber()
    {
        this._subscriber = new ReflectionEventSubscriber(this);
    }

    #endregion
    
    #region Methods
    
    public bool TryGetEventHandler<TEvent>(out Action<TEvent> handler)
    {
        return this._subscriber.TryGetEventHandler(out handler);
    }
    
    #endregion

    #region Connection Handlers
    
    public void Handle(ConnectionReceivedMessageEvent e)
    {
        MongoLogger.Current.Log(e.GetType().Name, LogType.Information);
    }

    public void Handle(ConnectionReceivingMessageEvent e)
    {
        MongoLogger.Current.Log(e.GetType().Name, LogType.Information);
    }

    public void Handle(ConnectionReceivingMessageFailedEvent e)
    {
        MongoLogger.Current.Log(e.GetType().Name, LogType.Error);
    }

    public void Handle(ConnectionSendingMessagesEvent e)
    {
        MongoLogger.Current.Log(e.GetType().Name, LogType.Information);
    }

    public void Handle(ConnectionSendingMessagesFailedEvent e)
    {
        MongoLogger.Current.Log(e.GetType().Name, LogType.Error);
    }

    public void Handle(ConnectionSentMessagesEvent e)
    {
        MongoLogger.Current.Log(e.GetType().Name, LogType.Information);
    }

    public void Handle(ConnectionClosedEvent e)
    {
        MongoLogger.Current.Log(e.GetType().Name, LogType.Error);
    }

    public void Handle(ConnectionClosingEvent e)
    {
        MongoLogger.Current.Log(e.GetType().Name, LogType.Warning);
    }

    public void Handle(ConnectionCreatedEvent e)
    {
        MongoLogger.Current.Log(e.GetType().Name, LogType.Information);
    }

    public void Handle(ConnectionFailedEvent e)
    {
        MongoLogger.Current.Log(e.GetType().Name, LogType.Error);
    }

    public void Handle(ConnectionOpenedEvent e)
    {
        MongoLogger.Current.Log(e.GetType().Name, LogType.Information);
    }

    public void Handle(ConnectionOpeningEvent e)
    {
        MongoLogger.Current.Log(e.GetType().Name, LogType.Information);
    }

    public void Handle(ConnectionOpeningFailedEvent e)
    {
        MongoLogger.Current.Log(e.GetType().Name, LogType.Error);
    }

    #endregion

    #region Connection Pool Handlers

    public void Handle(ConnectionPoolAddedConnectionEvent e)
    {
        MongoLogger.Current.Log(e.GetType().Name, LogType.Information);
    }

    public void Handle(ConnectionPoolAddingConnectionEvent e)
    {
        MongoLogger.Current.Log(e.GetType().Name, LogType.Information);
    }

    public void Handle(ConnectionPoolCheckedInConnectionEvent e)
    {
        MongoLogger.Current.Log(e.GetType().Name, LogType.Information);
    }

    public void Handle(ConnectionPoolCheckedOutConnectionEvent e)
    {
        MongoLogger.Current.Log(e.GetType().Name, LogType.Information);
    }

    public void Handle(ConnectionPoolCheckingInConnectionEvent e)
    {
        MongoLogger.Current.Log(e.GetType().Name, LogType.Information);
    }

    public void Handle(ConnectionPoolCheckingOutConnectionEvent e)
    {
        MongoLogger.Current.Log(e.GetType().Name, LogType.Information);
    }

    public void Handle(ConnectionPoolCheckingOutConnectionFailedEvent e)
    {
        MongoLogger.Current.Log(e.GetType().Name, LogType.Error);
    }

    public void Handle(ConnectionPoolClearedEvent e)
    {
        MongoLogger.Current.Log(e.GetType().Name, LogType.Information);
    }

    public void Handle(ConnectionPoolClearingEvent e)
    {
        MongoLogger.Current.Log(e.GetType().Name, LogType.Information);
    }

    public void Handle(ConnectionPoolClosedEvent e)
    {
        MongoLogger.Current.Log(e.GetType().Name, LogType.Error);
    }

    public void Handle(ConnectionPoolClosingEvent e)
    {
        MongoLogger.Current.Log(e.GetType().Name, LogType.Warning);
    }

    public void Handle(ConnectionPoolOpenedEvent e)
    {
        MongoLogger.Current.Log(e.GetType().Name, LogType.Information);
    }

    public void Handle(ConnectionPoolOpeningEvent e)
    {
        MongoLogger.Current.Log(e.GetType().Name, LogType.Information);
    }

    public void Handle(ConnectionPoolRemovedConnectionEvent e)
    {
        MongoLogger.Current.Log(e.GetType().Name, LogType.Error);
    }

    public void Handle(ConnectionPoolRemovingConnectionEvent e)
    {
        MongoLogger.Current.Log(e.GetType().Name, LogType.Warning);
    }

    #endregion
    
    #region Cluster Handlers

    public void Handle(ClusterAddedServerEvent e)
    {
        MongoLogger.Current.Log(e.GetType().Name, LogType.Information);
    }

    public void Handle(ClusterAddingServerEvent e)
    {
        MongoLogger.Current.Log(e.GetType().Name, LogType.Information);
    }

    public void Handle(ClusterClosedEvent e)
    {
        MongoLogger.Current.Log(e.GetType().Name, LogType.Error);
    }

    public void Handle(ClusterClosingEvent e)
    {
        MongoLogger.Current.Log(e.GetType().Name, LogType.Warning);
    }

    public void Handle(ClusterDescriptionChangedEvent e)
    {
        MongoLogger.Current.Log(e.GetType().Name, LogType.Information);
    }

    public void Handle(ClusterOpenedEvent e)
    {
        MongoLogger.Current.Log(e.GetType().Name, LogType.Information);
    }

    public void Handle(ClusterOpeningEvent e)
    {
        MongoLogger.Current.Log(e.GetType().Name, LogType.Information);
    }

    public void Handle(ClusterRemovedServerEvent e)
    {
        MongoLogger.Current.Log(e.GetType().Name, LogType.Warning);
    }

    public void Handle(ClusterRemovingServerEvent e)
    {
        MongoLogger.Current.Log(e.GetType().Name, LogType.Warning);
    }

    public void Handle(ClusterSelectedServerEvent e)
    {
        MongoLogger.Current.Log(e.GetType().Name, LogType.Information);
    }

    public void Handle(ClusterSelectingServerEvent e)
    {
        MongoLogger.Current.Log(e.GetType().Name, LogType.Information);
    }

    public void Handle(ClusterSelectingServerFailedEvent e)
    {
        MongoLogger.Current.Log(e.GetType().Name, LogType.Error);
    }

    #endregion
    
    #region Server Handlers

    public void Handle(ServerClosedEvent e)
    {
        MongoLogger.Current.Log(e.GetType().Name, LogType.Error);
    }

    public void Handle(ServerClosingEvent e)
    {
        MongoLogger.Current.Log(e.GetType().Name, LogType.Warning);
    }

    public void Handle(ServerDescriptionChangedEvent e)
    {
        MongoLogger.Current.Log(e.GetType().Name, LogType.Information);
    }

    public void Handle(ServerHeartbeatFailedEvent e)
    {
        MongoLogger.Current.Log(e.GetType().Name, LogType.Error);
    }

    public void Handle(ServerOpenedEvent e)
    {
        MongoLogger.Current.Log(e.GetType().Name, LogType.Information);
    }

    public void Handle(ServerOpeningEvent e)
    {
        MongoLogger.Current.Log(e.GetType().Name, LogType.Information);
    }

    #endregion

    #region Command Handlers

    public void Handle(CommandFailedEvent e)
    {
        MongoLogger.Current.Log(e.GetType().Name, LogType.Error);
    }

    #endregion
    
    #region Other Handlers

    public void Handle(SdamInformationEvent e)
    {
        MongoLogger.Current.Log(e.GetType().Name, LogType.Information);
    }

    #endregion
}

internal class MongoLogger
{
    private const bool ENABLE = false; 
    
    #region Fields

    private static MongoLogger _self;

    #endregion

    #region Properties
    
    public static MongoLogger Current => _self ??= new MongoLogger();

    private List<MongoLog> LogList { get; }
    
    public IReadOnlyCollection<MongoLog> Logs { get; }

    #endregion

    #region Constructors

    /// <summary>
    /// Constructor
    /// </summary>
    private MongoLogger()
    {
        this.LogList = new List<MongoLog>();
        this.Logs = new ReadOnlyCollection<MongoLog>(this.LogList);
    }

    #endregion

    #region Methods

    [SuppressMessage("ReSharper", "HeuristicUnreachableCode")]
    public void Log(string message, LogType logType)
    {
        if (ENABLE)
#pragma warning disable CS0162
        {
            this.LogList.Add(new MongoLog
            {
                LogTime = DateTime.Now,
                Message = message,
                Type = logType
            });   
        }
#pragma warning restore CS0162
    }

    #endregion
}

internal class MongoLog
{
    #region Properties

    [JsonProperty("logTime")]
    public DateTime LogTime { get; init; }
    
    [JsonProperty("message")]
    public string Message { get; init; }
    
    [JsonProperty("type")]
    public LogType Type { get; init; }

    #endregion
}

internal enum LogType
{
    Information,
    Warning,
    Error
}