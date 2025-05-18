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


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::  
		public List<TableAttribute> Attributes { get; set; } = new List<TableAttribute> ();


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
            filterGroupControl.FilterChanged += (_, __) => OnFilterChanged();
            panelRoot.Controls.Add(filterGroupControl);
        }

        // ============================================================================  
        private void RenderFilter(
            TableFilter tableFilter)
        {
            if (!(Controls[0] is Panel pnlRoot &&
                pnlRoot.Controls.Count != 0 &&
                pnlRoot.Controls[0] is FilterGroupControl rootGroup))
            {
                return;
            }

            rootGroup.LoadGroup(
				tableFilter.RootFilter);
        }

        // ============================================================================  
        private TableFilter BuildFilterFromUI()
        {
            if (!(Controls[0] is Panel pnlRoot &&
                pnlRoot.Controls.Count != 0 &&
                pnlRoot.Controls[0] is FilterGroupControl rootGroup))
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
