using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Com.AiricLenz.XTB.Components.Filter.Schema;

// ============================================================================
// ============================================================================
namespace Com.AiricLenz.XTB.Components
{

	// ============================================================================
	// ============================================================================
	public partial class FilterGroupControl : UserControl
	{
		public event EventHandler FilterChanged;

		// keep reference to children ► easier recursion
		private FlowLayoutPanel _flowLayoutPanel;


		// ============================================================================
		public FilterGroupControl()
		{
			InitializeComponent();
			DoubleBuffered = true;
			Dock = DockStyle.Top;
			Padding = new Padding(5);
		}

		// ============================================================================
		private void InitializeComponent()
		{
			Height = 32; // will grow with children

			var comboBoxOperator = new ComboBox
			{
				Width = 60,
				DropDownStyle = ComboBoxStyle.DropDownList,
				Items = { "AND", "OR" },
				SelectedIndex = 0
			};

			comboBoxOperator.SelectedIndexChanged += (sender, e) => OnFilterChanged();
			Controls.Add(comboBoxOperator);

			var buttonAddCondition = new Button { Text = "+ Condition", Width = 60 };
            
			buttonAddCondition.Click += (btnSender, btnE) =>
            {
                var c = new FilterConditionControl();
                c.FilterChanged += (controlSender, controlE) => OnFilterChanged();
                _flowLayoutPanel.Controls.Add(c);
                OnFilterChanged();
            };

			var buttonAddGroup = new Button { Text = "+ Group", Width = 60 };
			buttonAddGroup.Click += (btnSender, btnE) =>
            {
                var filterGroup = new FilterGroupControl();
                filterGroup.FilterChanged += (controlSender, controlE) => OnFilterChanged();
                _flowLayoutPanel.Controls.Add(filterGroup);
                OnFilterChanged();
            };

			Controls.Add(buttonAddGroup);

			_flowLayoutPanel = new FlowLayoutPanel
			{
				FlowDirection = FlowDirection.TopDown,
				AutoSize = true,
				Dock = DockStyle.Bottom,
				Padding = new Padding(20, 5, 5, 5)
			};

			Controls.Add(_flowLayoutPanel);
		}

		// ============================================================================
		public void LoadGroup(
			Filter.Schema.FilterGroup filterGroup)
		{
			if (Controls[0] is ComboBox comboBox)
			{
				comboBox.SelectedItem = filterGroup.LogicalOperator.ToUpperInvariant();
			}

			_flowLayoutPanel.Controls.Clear();

			if (filterGroup.Elements == null)
			{
				return;
			}

			foreach (var element in filterGroup.Elements)
			{
				if (element is FilterCondition condition)
				{
					var control = new FilterConditionControl();
					control.LoadCondition(condition);
					control.FilterChanged += (sender, e) => OnFilterChanged();
					_flowLayoutPanel.Controls.Add(control);
				}
				else if (element is Filter.Schema.FilterGroup subGroup)
				{
					var control = new FilterGroupControl();
					control.LoadGroup(subGroup);
					control.FilterChanged += (sender, e) => OnFilterChanged();
					_flowLayoutPanel.Controls.Add(control);
				}
			}
		}

		// ============================================================================
		public FilterGroup ToModel()
		{
			var filterGroup = new FilterGroup
			{
				LogicalOperator = (Controls[0] as ComboBox)?.SelectedItem?.ToString()?.ToLower() ?? "and"
			};

			foreach (Control child in _flowLayoutPanel.Controls)
			{
				if (child is FilterConditionControl condCtl)
				{
					filterGroup.Elements.Add(condCtl.ToModel());
				}
				else if (child is FilterGroupControl grpCtl)
				{
					filterGroup.Elements.Add(grpCtl.ToModel());
				}
			}
			return filterGroup;
		}

		// ============================================================================
		protected virtual void OnFilterChanged()
			=> FilterChanged?.Invoke(this, EventArgs.Empty);
	}
}