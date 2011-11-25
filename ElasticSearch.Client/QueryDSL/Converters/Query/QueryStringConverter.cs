using System;
using System.Text;
using Newtonsoft.Json;

namespace ElasticSearch.Client.QueryDSL
{
    internal class QueryStringConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            QueryStringQuery term = (QueryStringQuery)value;
            if (term != null)
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.Append("{query_string:{ ");
                stringBuilder.Append("\"default_field\":\"" + term.DefaultField + "\" ");
                stringBuilder.Append(",\"query\":\"" + term.Query + "\" ");
                stringBuilder.Append(",\"default_operator\":\"" + term.DefaultOperator + "\" ");
                stringBuilder.Append(",\"analyzer\":\"" + term.Analyzer + "\" ");
                stringBuilder.Append("}}");

                //TODO ��ɸ������+ �����ж�

                writer.WriteRawValue(stringBuilder.ToString());
            }
		
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(QueryStringQuery).IsAssignableFrom(objectType);
        }
    }
}