﻿namespace ElasticSearch.DSL
{
	public class InclusiveRange : IRange
	{
		public string Field { get; private set; }
		public int? From { get; private set; }
		public int? To { get; private set; }
		public double Boost { get; private set; }
		public bool Exclusive { get; private set; }
		
		public InclusiveRange(string field, int? from, int? to) : this(field, from, to, null) { }

		private InclusiveRange(string field, int? from, int? to, double? boost)
		{
			this.Field = field;
			this.To = to;
			this.From = from;
			if (boost.HasValue);
				this.Boost = boost.Value;
			this.Exclusive = true;
		}
	}
}
