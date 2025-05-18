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

		private ComboBox _comboBoxField;
		private ComboBox _comboBoxOperator;
		private TextBox _txtValue;
		private CheckBox _checkBoxPlaceholder;


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
			_comboBoxField = new ComboBox { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
			_comboBoxOperator = new ComboBox { Width = 90, DropDownStyle = ComboBoxStyle.DropDownList };
			_txtValue = new TextBox { Width = 160 };
			_checkBoxPlaceholder = new CheckBox { Text = "Placeholder" };

			foreach (var op in new[] { "eq", "ne", "contains", "startswith", "endswith", "ge", "le" })
			{
				_comboBoxOperator.Items.Add(op);
			}

			_comboBoxOperator.SelectedIndex = 0;

			_comboBoxField.SelectedIndexChanged += (_, __) => OnFilterChanged();
			_comboBoxOperator.SelectedIndexChanged += (_, __) => OnFilterChanged();
			_txtValue.TextChanged += (_, __) => OnFilterChanged();
			_checkBoxPlaceholder.CheckedChanged += (_, __) => OnFilterChanged();

			Controls.AddRange(new Control[] { _comboBoxField, _comboBoxOperator, _txtValue, _checkBoxPlaceholder });

			// Populate _cbField in runtime after metadata is loaded
		}

		// ============================================================================
		public void LoadCondition(FilterCondition cond)
		{
			_comboBoxField.Text = cond.Attribute;
			_comboBoxOperator.Text = cond.Operator;
			_txtValue.Text = cond.IsPlaceholder ? cond.Placeholder : cond.Value;
			_checkBoxPlaceholder.Checked = cond.IsPlaceholder;
		}

		// ============================================================================
		public void SetAttributes(
			IEnumerable<TableAttribute> attributes)
		{
			_comboBoxField.BeginUpdate();
			_comboBoxField.Items.Clear();

			if (attributes != null)
			{
				foreach (var a in attributes)
				{
					_comboBoxField.Items.Add(a.LogicalName);
				}
			}
			_comboBoxField.EndUpdate();
		}

		// ============================================================================
		public FilterCondition ToModel()
		{
			return new FilterCondition
			{
				Attribute = _comboBoxField.Text,
				Operator = _comboBoxOperator.Text,
				Value = _checkBoxPlaceholder.Checked ? null : _txtValue.Text,
				Placeholder = _checkBoxPlaceholder.Checked ? _txtValue.Text : null,
				ValueType = "string"
			};
		}

		// ============================================================================
		protected virtual void OnFilterChanged()
			=> FilterChanged?.Invoke(this, EventArgs.Empty);
	}
}