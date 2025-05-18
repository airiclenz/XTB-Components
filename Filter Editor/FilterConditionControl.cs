using System;
using System.Linq;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Com.AiricLenz.XTB.Components.Filter.Schema;
using System.Collections.Generic;

// ============================================================================
// ============================================================================
namespace Com.AiricLenz.XTB.Components.Filter.Schema
{
	// ============================================================================
	// ============================================================================
	public partial class FilterConditionControl : UserControl
	{
		public event EventHandler FilterChanged;

		private ComboBox _cbField;
		private ComboBox _cbOperator;
		private TextBox _txtValue;
		private CheckBox _chkPlaceholder;

		public void SetAttributeList(IEnumerable<TableAttribute> attributes)
		{
			_cbField.BeginUpdate();
			_cbField.Items.Clear();
			if (attributes != null)
			{
				foreach (var a in attributes)
					_cbField.Items.Add(a.LogicalName);
			}
			_cbField.EndUpdate();
		}


		// ============================================================================
		public FilterConditionControl()
		{
			InitializeComponent();
			Dock = DockStyle.Top;
			Height = 28;
		}

		// ============================================================================
		private void InitializeComponent()
		{
			_cbField = new ComboBox { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
			_cbOperator = new ComboBox { Width = 90, DropDownStyle = ComboBoxStyle.DropDownList };
			_txtValue = new TextBox { Width = 160 };
			_chkPlaceholder = new CheckBox { Text = "Placeholder" };

			foreach (var op in new[] { "eq", "ne", "contains", "startswith", "endswith", "ge", "le" })
			{
				_cbOperator.Items.Add(op);
			}

			_cbOperator.SelectedIndex = 0;

			_cbField.SelectedIndexChanged += (_, __) => OnFilterChanged();
			_cbOperator.SelectedIndexChanged += (_, __) => OnFilterChanged();
			_txtValue.TextChanged += (_, __) => OnFilterChanged();
			_chkPlaceholder.CheckedChanged += (_, __) => OnFilterChanged();

			Controls.AddRange(new Control[] { _cbField, _cbOperator, _txtValue, _chkPlaceholder });

			// Populate _cbField in runtime after metadata is loaded
		}

		// ============================================================================
		public void LoadCondition(FilterCondition cond)
		{
			_cbField.Text = cond.Attribute;
			_cbOperator.Text = cond.Operator;
			_txtValue.Text = cond.IsPlaceholder ? cond.Placeholder : cond.Value;
			_chkPlaceholder.Checked = cond.IsPlaceholder;
		}

		// ============================================================================
		public FilterCondition ToModel()
		{
			return new FilterCondition
			{
				Attribute = _cbField.Text,
				Operator = _cbOperator.Text,
				Value = _chkPlaceholder.Checked ? null : _txtValue.Text,
				Placeholder = _chkPlaceholder.Checked ? _txtValue.Text : null,
				ValueType = "string"
			};
		}

		// ============================================================================
		protected virtual void OnFilterChanged()
			=> FilterChanged?.Invoke(this, EventArgs.Empty);
	}
}