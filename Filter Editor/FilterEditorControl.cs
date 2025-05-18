using System;
using System.Collections.Generic;
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
	public partial class FilterEditorControl : UserControl
    {
        public event EventHandler FilterChanged;
		
		private TableFilter _filter = new TableFilter();
		private List<TableAttribute> _attributes = new List<TableAttribute>();


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::  
		[Browsable(false)]
		public List<TableAttribute> Attributes
		{
			get => _attributes;
			set
			{
				_attributes = value ?? new List<TableAttribute>();
				ApplyAttributesToRoot();
			}
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::  
		[Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public TableFilter Filter
        {
            get => BuildFilterFromUI();
            set
            {
                _filter = value ?? new TableFilter();
                RenderFilter(_filter);
                OnFilterChanged();
            }
        }


        // ============================================================================  
        public FilterEditorControl()
        {
            InitializeComponent();
            DoubleBuffered = true;
            //Dock = DockStyle.Fill;

            RenderFilter(_filter);
        }

        // ============================================================================  
        private void InitializeComponent()
        {
            var panelRoot = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(5)
            };

            Controls.Add(panelRoot);

            // add root filter group  
            var filterGroupControl = new FilterGroupControl();
            filterGroupControl.SetAttributes(_attributes);
			filterGroupControl.FilterChanged += (_, __) => OnFilterChanged();
			
            panelRoot.Controls.Add(filterGroupControl);
        }


		// ============================================================================  
		private void RenderFilter(
            TableFilter tableFilter)
        {
            if (!(Controls[0] is Panel panelRoot &&
                panelRoot.Controls.Count != 0 &&
                panelRoot.Controls[0] is FilterGroupControl rootGroup))
            {
                return;
            }

            rootGroup.LoadGroup(
				tableFilter.RootFilter);
        }

		// ============================================================================
		    private void ApplyAttributesToRoot()
        {
            if (Controls.Count == 0)
            {
                return;
            }

            if (!(Controls[0] is Panel panelRoot) ||
                panelRoot.Controls.Count == 0)
            {
                return;
            }

            if (panelRoot.Controls[0] is FilterGroupControl rootGrp)
            {
                rootGrp.SetAttributes(_attributes);
            }
        }

		// ============================================================================  
		private TableFilter BuildFilterFromUI()
        {
            if (!(Controls[0] is Panel panelRoot &&
                panelRoot.Controls.Count != 0 &&
                panelRoot.Controls[0] is FilterGroupControl rootGroup))
            {
                return _filter;
            }

            _filter.RootFilter = rootGroup.ToModel();
            return _filter;
        }

        // ============================================================================  
        protected virtual void OnFilterChanged()
            => FilterChanged?.Invoke(this, EventArgs.Empty);
    }
}
