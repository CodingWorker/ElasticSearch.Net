﻿namespace ElasticSearch.Client
{
	internal interface IConnectionProvider
	{
		ConnectionBuilder Builder { get; }

		IConnection CreateConnection();

		IConnection Open();

		bool Close(IConnection connection);
	}
}