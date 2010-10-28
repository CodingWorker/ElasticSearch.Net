﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ElasticSearch.DSL
{
	public class Fuzzy : IQuery
	{
		public string Field { get; private set; }
		public string Value { get; private set; }
		public double Boost { get; private set; }
		public double SimilarityScore { get; private set; }
		public int PrefixLength { get; private set; }

		public Fuzzy(string field, string value) : this(field, value, 1.0, 0.5, 0) { }

		public Fuzzy(string field, string value, double boost) : this(field, value, boost, 0.5, 0) { }
		
		public Fuzzy(string field, string value, double boost, double similarityScore) : this(field, value, boost, similarityScore, 0) { }
		
		public Fuzzy(string field, string value, double boost, double similarityScore, int prefixLength)
		{
			this.Value = value;
			this.Field = field;
			this.Boost = boost;
			this.SimilarityScore = similarityScore;
			this.PrefixLength = prefixLength;
		}
		public Fuzzy(Field field) : this(field, 0.5, 0) { } 
		
		public Fuzzy(Field field, double similarityScore, int prefixLength)
		{
			field.ThrowIfNull("field");
		
			this.Field = field.Name;
			this.Value = field.Value;
			if (field.Boost.HasValue)
				this.Boost = field.Boost.Value;
			this.SimilarityScore = similarityScore;
			this.PrefixLength = prefixLength;
		}


	}
}
