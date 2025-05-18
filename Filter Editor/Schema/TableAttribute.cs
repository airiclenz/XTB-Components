using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

// ============================================================================
// ============================================================================
namespace Com.AiricLenz.XTB.Components.Filter.Schema
{

	// ============================================================================
	// ============================================================================
	public class TableAttribute
	{
		[JsonProperty]
		public string LogicalName { get; set; } = string.Empty;
		[JsonProperty]
		public string DisplayName { get; set; } = string.Empty;
		[JsonProperty]
		public string TypeName { get; set; } = string.Empty;
		[JsonProperty]
		public bool IsChecked { get; set; } = false;


		[JsonIgnore]
		public Bitmap TypeImage { get; set; } = null;

	}
}
