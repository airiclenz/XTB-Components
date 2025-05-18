using System.Collections.Generic;
using Newtonsoft.Json;


// ============================================================================
// ============================================================================
namespace Com.AiricLenz.XTB.Components.Filter.Schema
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	[JsonObject(ItemTypeNameHandling = TypeNameHandling.Auto)]
	public class TableFilter
	{
		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public FilterGroup RootFilter { get; set; } = new FilterGroup();

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		// Optional: Define mappings from placeholder names to environment-specific GUIDs
		public Dictionary<string, Dictionary<string, string>> PlaceholderMappings { get; set; }
			= new Dictionary<string, Dictionary<string, string>>();
	}


	// ================================================================================
	// ================================================================================
	public interface IFilterElement { }



	// ================================================================================
	// ================================================================================
	public class FilterGroup : IFilterElement
	{
		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		private string _logicalOperatior = "AND";

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public string LogicalOperator
		{
			get => _logicalOperatior;
			set
			{
				var tempValue = value.ToUpper();

				if (tempValue == "AND" ||
					tempValue == "OR")
				{
					_logicalOperatior = tempValue;
				}
			}
		}

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public List<IFilterElement> Elements { get; set; } = new List<IFilterElement>();
	}

	// ================================================================================
	// ================================================================================
	public class FilterCondition : IFilterElement
	{

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public string Attribute { get; set; } = string.Empty;
		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public string Operator { get; set; } = "eq";
		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		// Only used if Placeholder is null
		public string Value { get; set; }
		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		// Optional: used instead of hard-coded Value
		public string Placeholder { get; set; }
		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		// string, guid, datetime, etc.
		public string ValueType { get; set; } = "string";
		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public bool IsPlaceholder => !string.IsNullOrEmpty(Placeholder);
	}

}

