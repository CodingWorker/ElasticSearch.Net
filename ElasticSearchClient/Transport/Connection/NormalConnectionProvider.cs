﻿using System;
using System.Collections.Generic;
using System.Net.Sockets;
using ElasticSearch.Client.Exception;
using Thrift.Transport;

namespace ElasticSearch.Client
{
	internal class NormalConnectionProvider : ConnectionProvider
	{
		private Random _random = new Random();

		/// <summary>
		/// 
		/// </summary>
		/// <param name="builder"></param>
		public NormalConnectionProvider(ConnectionBuilder builder)
			: base(builder)
		{
			if (builder.Servers.Count > 1 && builder.Timeout == 0)
				throw new ElasticSearchException("You must specify a timeout when using multiple servers.");

			Timeout = builder.Timeout;
			ActiveServers = builder.Servers;
			TSocketSetting = builder.TSocketSetting;
		}

		/// <summary>
		/// 
		/// </summary>
		public int Timeout { get; private set; }

		public TSocketSettings TSocketSetting { get; private set; }
		/// <summary>
		/// 
		/// </summary>
		public IList<Server> ActiveServers { get; private set; }

		/// <summary>
		/// Gets if there are any more connections left to try.
		/// </summary>
		public bool HasNext
		{
			get { return ActiveServers.Count > 0; }
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public override IConnection Open()
		{
			IConnection conn = null;

			while (HasNext)
			{
				try
				{
					conn = CreateConnection();
					conn.Open();
					break;
				}
				catch (SocketException exc)
				{
					using (TimedLock.Lock(ActiveServers))
					{
						Close(conn);
						ActiveServers.Remove(conn.Server);
						conn = null;
					}
				}
			}

			if (conn == null)
				throw new ElasticSearchException("No connection could be made because all servers have failed.");

			return conn;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public override IConnection CreateConnection()
		{
			if (ActiveServers.Count == 0)
				return null;

			var server = Builder.Servers[_random.Next(ActiveServers.Count)];
			var conn = new Connection(server, TSocketSetting);
			return conn;
		}
	}
}