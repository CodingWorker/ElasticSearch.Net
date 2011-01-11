﻿using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;
using System.Web;
using ElasticSearch.Client.Mapping;
using ElasticSearch.Client.Transport;
using ElasticSearch.DSL;
using ElasticSearch.Mapping;
using ElasticSearch.Thrift;
using ElasticSearch.Utils;
using Newtonsoft.Json.Linq;

namespace ElasticSearch.Client
{
	/// <summary>
	/// ElasticSearchClient
	/// </summary>
	public class ElasticSearchClient
	{
		private static readonly ElasticSearchClient _client = new ElasticSearchClient();
		private LogWrapper _logger = LogWrapper.GetLogger();

		private ElasticSearchClient()
		{
		}

		public static ElasticSearchClient Instance
		{
			get { return _client; }
		}

		public OperateResult Index(string index, IndexItem indexItem)
		{
			return Index(index, indexItem.IndexType, indexItem.IndexKey, indexItem.ToJson());
		}

		public OperateResult Index(string index, string type, string indexKey, string jsonData)
		{
			Contract.Ensures(!string.IsNullOrEmpty(index));
			Contract.Ensures(!string.IsNullOrEmpty(type));
			Contract.Ensures(!string.IsNullOrEmpty(jsonData));
			Contract.Ensures(!string.IsNullOrEmpty(indexKey));

			var url = "/{0}/{1}/{2}/".F(index.ToLower(), type, indexKey);
			RestResponse result = RestProvider.Instance.Post(url, jsonData);
			return GetOperationResult(result);
		}

		public OperateResult Bulk(IList<BulkObject> bulkObjects)
		{
			Contract.Ensures(bulkObjects != null);
			Contract.Ensures(bulkObjects.Count > 0);

			const string url = "/_bulk";
			string jsonData = bulkObjects.GetJson();
			RestResponse result = RestProvider.Instance.Post(url, jsonData);
			var result1= GetOperationResult(result);
			result1.Success = result.Status == Thrift.Status.OK;
			return result1;
		}

		public Document Get(string index, string type, string indexKey)
		{
			Contract.Ensures(!string.IsNullOrEmpty(index));
			Contract.Ensures(!string.IsNullOrEmpty(type));
			Contract.Ensures(!string.IsNullOrEmpty(indexKey));

			string url = "/{0}/{1}/{2}".F(index.ToLower(), type, indexKey);
			RestResponse result = RestProvider.Instance.Get(url);

			if (result.Body != null)
			{
				var document = new Document();
				document.JsonString = result.GetBody();
				try
				{
					var hitResult = JsonSerializer.Get<Hits>(result.GetBody());
					document.Hits = hitResult;
				}
				catch (System.Exception e)
				{
					_logger.Error(e);
				}

				return document;
			}
			return null;
		}

		public OperateResult Delete(string indexName, string indexType, string[] objectKeys)
		{
			Contract.Ensures(!string.IsNullOrEmpty(indexName));
			Contract.Ensures(!string.IsNullOrEmpty(indexType));
			Contract.Ensures(objectKeys != null);
			Contract.Ensures(objectKeys.Length > 0);

			string url = "/_bulk";
			var stringBuilder = new StringBuilder(objectKeys.Length);
			foreach (string variable in objectKeys)
			{
				stringBuilder.AppendLine(
					"{{ \"delete\" : {{ \"_index\" : \"{0}\", \"_type\" : \"{1}\", \"_id\" : \"{2}\" }} }}".F(indexName.ToLower(),
																											  indexType, variable));
			}
			string jsonData = stringBuilder.ToString();
			RestResponse result = RestProvider.Instance.Post(url, jsonData);
			var result1= GetOperationResult(result);
			result1.Success = result.Status == Thrift.Status.OK;
			return result1;
		}

		#region search

		public SearchResult Search(string index, string[] type, string queryString, int size)
		{
			return Search(index, type, queryString, 0, size);
		}

		public SearchResult Search(string index, string[] type, string queryString, int from, int size)
		{
			Contract.Ensures(!string.IsNullOrEmpty(index));
			Contract.Ensures(!string.IsNullOrEmpty(queryString));
			Contract.Ensures(type != null);
			Contract.Ensures(type.Length > 0);
			Contract.Ensures(from >= 0);
			Contract.Ensures(size > 0);

			queryString = HttpUtility.UrlEncode(queryString.Trim());

			string url = "/{0}/{1}/_search?q={2}&from={3}&size={4}".F(index.ToLower(), string.Join(",", type), queryString, from,
																	  size);
			RestResponse result = RestProvider.Instance.Get(url);
			var hitResult = new SearchResult(result.GetBody());
			return hitResult;
		}

		/// <summary>
		/// 搜索index下所有type
		/// </summary>
		/// <param name="index"></param>
		/// <param name="queryString"></param>
		/// <param name="from"></param>
		/// <param name="size"></param>
		/// <returns></returns>
		public SearchResult Search(string index, string queryString, int from, int size)
		{
			Contract.Ensures(!string.IsNullOrEmpty(index));
			Contract.Ensures(!string.IsNullOrEmpty(queryString));
			Contract.Ensures(from >= 0);
			Contract.Ensures(size > 0);

			queryString = HttpUtility.UrlEncode(queryString.Trim());
			string url = "/{0}/_search?q={1}&from={2}&size={3}".F(index.ToLower(), queryString, from, size);
			RestResponse result = RestProvider.Instance.Get(url);

			var hitResult = new SearchResult(result.GetBody());
			return hitResult;
		}

		public SearchResult Search(string index, string type, string queryString)
		{
			return Search(index, new[] { type }, queryString, 10);
		}

		public SearchResult Search(string index, string type, string queryString, int from, int size)
		{
			return Search(index, new[] { type }, queryString, from, size);
		}

		public SearchResult Search(string index, string type, string queryString, string sortString, int from, int size)
		{
			return Search(index, new[] { type }, queryString, sortString, from, size);
		}

		public SearchResult Search(string index, string[] type, string queryString, string sortString, int from, int size)
		{
			return Search(index, type, queryString, sortString, null, from, size);
		}

		public SearchResult Search(string index, string[] type, string queryString, string sortString, string[] fields,
									 int from, int size)
		{
			Contract.Ensures(!string.IsNullOrEmpty(index));
			Contract.Ensures(type != null);
			Contract.Ensures(type.Length > 0);
			Contract.Ensures(!string.IsNullOrEmpty(queryString));

			queryString = HttpUtility.UrlEncode(queryString.Trim());
			string url = "/{0}/{1}/_search?q={2}&from={3}&size={4}".F(index.ToLower(), string.Join(",", type), queryString, from,
																	  size);

			if (!string.IsNullOrEmpty(sortString))
			{
				url += "&sort=" + sortString;
			}

			if (fields != null && fields.Length > 0)
			{
				url += "&fields=" + string.Join(",", fields);
			}

			RestResponse result = RestProvider.Instance.Get(url);

			var hitResult = new SearchResult(result.GetBody());
			return hitResult;
		}

		public List<string> SearchIds(string index, string type, string queryString, string sortString, int from, int size)
		{
			return SearchIds(index, new[] { type }, queryString, sortString, from, size);
		}

		public List<string> SearchIds(string index, string[] type, string queryString, string sortString, int from, int size)
		{
			Contract.Ensures(!string.IsNullOrEmpty(index));
			Contract.Ensures(type != null);
			Contract.Ensures(type.Length > 0);
			Contract.Ensures(!string.IsNullOrEmpty(queryString));

			queryString = HttpUtility.UrlEncode(queryString.Trim());
			string url = "/{0}/{1}/_search?q={2}&fields=_id&from={3}&size={4}".F(index.ToLower(), string.Join(",", type),
																				 queryString, from, size);

			if (!string.IsNullOrEmpty(sortString))
			{
				url += "&sort=" + sortString;
			}

			RestResponse result = RestProvider.Instance.Get(url);

			var hitResult = new SearchResult(result.GetBody());
			return hitResult.GetHitIds();
		}


		public SearchResult Search(string index, string type, string queryString, int size)
		{
			return Search(index, new[] { type }, queryString, size);
		}

		public SearchResult SearchByDSL(string index, string[] type, string queryString, int from, int size)
		{
			Contract.Ensures(!string.IsNullOrEmpty(index));
			Contract.Ensures(type != null);
			Contract.Ensures(type.Length > 0);
			Contract.Ensures(!string.IsNullOrEmpty(queryString));
			Contract.Ensures(from >= 0);
			Contract.Ensures(size > 0);

			var query = new QueryString(queryString);

			var elasticQuery = new ElasticQuery(from, size);
			elasticQuery.AddQuery(query);

			string jsonstr = JsonSerializer.Get(elasticQuery);

			string url = "/{0}/{1}/_search".F(index.ToLower(), string.Join(",", type));
			RestResponse result = RestProvider.Instance.Post(url, jsonstr);
			var hitResult = new SearchResult(result.GetBody());
			return hitResult;
		}

		#endregion

		#region admin

		public OperateResult Refresh(params string[] index)
		{
			string indexs = string.Empty;
			if (index.Length > 0)
			{
				indexs = "/" + string.Join(",", index);
			}
			string url = indexs.ToLower() + "/_refresh";

			RestResponse result = RestProvider.Instance.Get(url);
			return GetOperationResult(result);
		}

		public OperateResult Flush(params string[] index)
		{
			string indexs = string.Empty;
			if (index.Length > 0)
			{
				indexs = "/" + string.Join(",", index);
			}
			string url = indexs.ToLower() + "/_flush";

			RestResponse result = RestProvider.Instance.Get(url);
			return GetOperationResult(result);
		}

		public OperateResult Optimize(params string[] index)
		{
			string indexs = string.Empty;
			if (index.Length > 0)
			{
				indexs = "/" + string.Join(",", index);
			}
			string url = indexs.ToLower() + "/_optimize";

			RestResponse result = RestProvider.Instance.Get(url);
			return GetOperationResult(result);
		}

		public string Status(params string[] index)
		{
			string indexs = string.Empty;
			if (index.Length > 0)
			{
				indexs = "/" + string.Join(",", index);
			}
			string url = indexs.ToLower() + "/_status";

			return RestProvider.Instance.Get(url).GetBody();
		}

		#endregion

		#region mapping

		public OperateResult PutMapping(string index, TypeSetting typeSetting)
		{
			Contract.Ensures(!string.IsNullOrEmpty(index));
			Contract.Ensures(typeSetting != null);
			var url = "/" + index + "/_mapping";

			var mappings = new Dictionary<string, TypeSetting>();
			mappings.Add(typeSetting.Type, typeSetting);

			var data = JsonSerializer.Get(mappings);

			var response = RestProvider.Instance.Put(url, data);

			if (response != null)
			{
				try
				{
					if (response.Status == Thrift.Status.INTERNAL_SERVER_ERROR||response.Status==Thrift.Status.BAD_REQUEST)
					{
						//auto create index
						CreateIndex(index, new IndexSetting(5, 1));
						//try again
						response = RestProvider.Instance.Put(url, data);

						return GetOperationResult(response);
					}
				}
				catch (System.Exception e)
				{
					_logger.Error(e);
				}

			}
			return GetOperationResult(response);
		}

		#endregion

		#region indexAdmin

		public OperateResult CreateIndex(string index)
		{
			return CreateIndex(index, new IndexSetting(5, 1));
		}

		public OperateResult CreateIndex(string index, IndexSetting indexSetting)
		{
			Contract.Ensures(!string.IsNullOrEmpty(index));
			Contract.Ensures(indexSetting != null);

			string url = "/" + index.ToLower() + "/";

			string json = JsonSerializer.Get(indexSetting);
			json = "{    index : " + json + " }";

			RestResponse result = RestProvider.Instance.Post(url, json);
			return GetOperationResult(result);
		}

		public OperateResult ModifyIndex(string index, IndexSetting indexSetting)
		{
			Contract.Ensures(!string.IsNullOrEmpty(index));
			Contract.Ensures(indexSetting != null);

			string url = "/" + index.ToLower() + "/_settings";

			string json = JsonSerializer.Get(indexSetting);
			json = "{    index : " + json + " }";

			RestResponse result = RestProvider.Instance.Put(url, json);
			return GetOperationResult(result);
		}

		public OperateResult DeleteIndex(string index)
		{
			Contract.Ensures(!string.IsNullOrEmpty(index));

			string url = "/{0}".F(index.ToLower());

			RestResponse result = RestProvider.Instance.Delete(url);

			return GetOperationResult(result);
		}

		public OperateResult CreateTemplate(string templateName, TemplateSetting template)
		{
			Contract.Ensures(template != null);

			string url = "/_template/{0}".F(templateName);

			string json = JsonSerializer.Get(template);
			RestResponse result = RestProvider.Instance.Post(url, json);

			return GetOperationResult(result);
		}

		public Dictionary<string, TemplateSetting> GetTemplate(string templateName)
		{
			Contract.Ensures(!string.IsNullOrEmpty(templateName));

			string url = "/_template/{0}".F(templateName);

			RestResponse result = RestProvider.Instance.Get(url);

			if (result.Body != null)
			{
				var document = new Document();
				document.JsonString = result.GetBody();
				try
				{
					return JsonSerializer.Get<Dictionary<string, TemplateSetting>>(result.GetBody());
				}
				catch (System.Exception e)
				{
					_logger.Error(e);
				}
			}
			return null;
		}

		public OperateResult DeleteTemplate(string templateName)
		{
			Contract.Ensures(!string.IsNullOrEmpty(templateName));
			Contract.Ensures(templateName != null);

			string url = "/_template/{0}".F(templateName);
			RestResponse result = RestProvider.Instance.Delete(url);
			
			return GetOperationResult(result);
		}

		#endregion

		#region delete

		public OperateResult Delete(string index, string type, string indexKey)
		{
			Contract.Ensures(!string.IsNullOrEmpty(index));
			Contract.Ensures(!string.IsNullOrEmpty(type));
			Contract.Ensures(!string.IsNullOrEmpty(indexKey));


			string url = "/{0}/{1}/{2}/".F(index.ToLower(), type, indexKey);
			RestResponse result = RestProvider.Instance.Delete(url);
			return GetOperationResult(result);
		}

		/// <summary>
		/// DeleteByQuery
		/// </summary>
		/// <param name="index"></param>
		/// <param name="type"></param>
		/// <param name="queryString">user:kimchy</param>
		/// <example>DeleteByQueryString('multenant','resume','user:kimchy');</example>
		public OperateResult DeleteByQueryString(string index, string type, string queryString)
		{
			Contract.Ensures(!string.IsNullOrEmpty(index));
			Contract.Ensures(!string.IsNullOrEmpty(type));
			Contract.Ensures(!string.IsNullOrEmpty(queryString));

			queryString = HttpUtility.UrlEncode(queryString.Trim());
			string url = "/{0}/{1}/_query?q={2}".F(index.ToLower(), type, queryString);
			RestResponse result = RestProvider.Instance.Delete(url);

			return GetOperationResult(result);
		}

		private OperateResult GetOperationResult(RestResponse result)
		{
			string jsonString = result.GetBody();
			if (jsonString != null)
			{
				var hitResult = JsonSerializer.Get<OperateResult>(jsonString);
				hitResult.JsonString = jsonString;
				return hitResult;
			}
			return null;
		}

		public OperateResult DeleteByQueryString(string index, string[] type, string queryString)
		{
			Contract.Ensures(!string.IsNullOrEmpty(index));
			Contract.Ensures(type != null);
			Contract.Ensures(type.Length > 0);
			Contract.Ensures(!string.IsNullOrEmpty(queryString));

			queryString = HttpUtility.UrlEncode(queryString.Trim());
			string url = "/{0}/{1}/_query?q={2}".F(index.ToLower(), string.Join(",", type), queryString);
			RestResponse result = RestProvider.Instance.Delete(url);
			string jsonString = result.GetBody();
			if (jsonString != null)
			{
				var hitResult = JsonSerializer.Get<OperateResult>(jsonString);
				hitResult.JsonString = jsonString;
				return hitResult;
			}
			return null;
		}

		public OperateResult DeleteByQueryString(string[] index, string[] type, string queryString)
		{
			Contract.Ensures(index != null);
			Contract.Ensures(type != null);
			Contract.Ensures(index.Length > 0);
			Contract.Ensures(type.Length > 0);
			Contract.Ensures(!string.IsNullOrEmpty(queryString));

			queryString = HttpUtility.UrlEncode(queryString.Trim());
			string url = "/{0}/{1}/_query?q=".F(string.Join(",", index).ToLower(), string.Join(",", type), queryString);
			RestResponse result = RestProvider.Instance.Delete(url);
			string jsonString = result.GetBody();
			if (jsonString != null)
			{
				var hitResult = JsonSerializer.Get<OperateResult>(jsonString);
				hitResult.JsonString = jsonString;
				return hitResult;
			}
			return null;
		}

		public OperateResult DeleteByQueryString(string index, string queryString)
		{
			Contract.Ensures(!string.IsNullOrEmpty(index));
			Contract.Ensures(!string.IsNullOrEmpty(queryString));

			queryString = HttpUtility.UrlEncode(queryString.Trim());
			string url = "/{0}/_query?q=".F(index.ToLower(), queryString);
			RestResponse result = RestProvider.Instance.Delete(url);
			string jsonString = result.GetBody();
			if (jsonString != null)
			{
				var hitResult = JsonSerializer.Get<OperateResult>(jsonString);
				hitResult.JsonString = jsonString;
				return hitResult;
			}
			return null;
		}

		public OperateResult DeleteByQueryString(string[] index, string queryString)
		{
			Contract.Ensures(index != null);
			Contract.Ensures(index.Length > 0);
			Contract.Ensures(!string.IsNullOrEmpty(queryString));

			queryString = HttpUtility.UrlEncode(queryString.Trim());
			string url = "/{0}/_query?q={1}".F(string.Join(",", index).ToLower(), queryString);
			RestResponse result = RestProvider.Instance.Delete(url);
			string jsonString = result.GetBody();
			if (jsonString != null)
			{
				var hitResult = JsonSerializer.Get<OperateResult>(jsonString);
				hitResult.JsonString = jsonString;
				return hitResult;
			}
			return null;
		}

		/// <summary>
		///be careful,influence all indicies
		/// </summary>
		/// <param name="queryString"></param>
		internal OperateResult DeleteByQueryString(string queryString)
		{
			Contract.Ensures(!string.IsNullOrEmpty(queryString));

			queryString = HttpUtility.UrlEncode(queryString.Trim());
			string url = "/_all/_query?q={0}".F(queryString);
			RestResponse result = RestProvider.Instance.Delete(url);
			string jsonString = result.GetBody();
			if (jsonString != null)
			{
				var hitResult = JsonSerializer.Get<OperateResult>(jsonString);
				hitResult.JsonString = jsonString;
				return hitResult;
			}
			return null;
		}

		#endregion

		#region count

		public int Count(string index, string[] type, string queryString)
		{
			Contract.Ensures(!string.IsNullOrEmpty(index));
			Contract.Ensures(!string.IsNullOrEmpty(queryString));
			Contract.Ensures(type != null);
			Contract.Ensures(type.Length > 0);

			queryString = HttpUtility.UrlEncode(queryString.Trim());
			string url = "/{0}/{1}/_count?q={2}".F(index.ToLower(), string.Join(",", type), queryString);
			RestResponse result = RestProvider.Instance.Get(url);

			string restr = result.GetBody();

			if (!string.IsNullOrEmpty(restr))
			{
				try
				{
					JObject o = JObject.Parse(restr);
					var count = (int)o["count"];

					return count;
				}
				catch (System.Exception e)
				{
					_logger.Error(e);
				}
			}
			return 0;
		}

		public int Count(string index, string type, string queryString)
		{
			return Count(index, new[] { type }, queryString);
		}

		public int Count(string index, string queryString)
		{
			Contract.Ensures(!string.IsNullOrEmpty(index));
			Contract.Ensures(!string.IsNullOrEmpty(queryString));

			queryString = HttpUtility.UrlEncode(queryString.Trim());
			string url = "/{0}/_count?q={1}".F(index.ToLower(), queryString);
			RestResponse result = RestProvider.Instance.Get(url);

			string restr = result.GetBody();

			if (!string.IsNullOrEmpty(restr))
			{
				try
				{
					JObject o = JObject.Parse(restr);
					var count = (int)o["count"];

					return count;
				}
				catch (System.Exception e)
				{
					_logger.Error(e);
				}
			}
			return 0;
		}

		public int Count(string queryString)
		{
			Contract.Ensures(!string.IsNullOrEmpty(queryString));

			queryString = HttpUtility.UrlEncode(queryString.Trim());
			string url = "/_count?q={0}".F(queryString);
			RestResponse result = RestProvider.Instance.Get(url);

			string restr = result.GetBody();

			if (!string.IsNullOrEmpty(restr))
			{
				try
				{
					JObject o = JObject.Parse(restr);
					var count = (int)o["count"];

					return count;
				}
				catch (System.Exception e)
				{
					_logger.Error(e);
				}
			}
			return 0;
		}

		#endregion
	}
}