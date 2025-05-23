﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Com.AiricLenz.XTB.Plugin.Helpers;
using Newtonsoft.Json;



// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.XTB.Components
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	[Serializable]
	public partial class SortableCheckList : Control
	{

		private ToolTip _toolTip;
		private string _currentTooltipText;
		private Timer _toolTipTimer;
		private Point _lastMousePosition;
		private bool _isToolTipVisible = false;
		private bool _showToolTips = true;

		private List<SortableCheckItem> _items = new List<SortableCheckItem>();
		private List<ColumnDefinition> _columns = new List<ColumnDefinition>();
		private List<int> _visible = new List<int>();

		private int _itemHeight = 20;
		private float _textHeight;
		private float _borderThickness = 1f;
		private Color _borderColor = SystemColors.ControlDark;
		private int _scrollOffset = 0;
		private int _checkBoxSize = 18;
		private int _checkBoxRadius = 5;
		private int _checkBoxMargin = 3;
		private int _selectedIndex = -1;
		private int _dragBurgerSize = 14;
		private float _dragBurgerLineThickness = 1.5f;
		private bool _isShowScrollBar = true;
		private bool _isShowOnlyCheckedItems = false;
		private bool _isSortable = true;
		private bool _isCheckable = true;
		private bool _isBoldWhenCheck = true;
		private Image _noDataImage = null;
		private int _dynamicColumnSpace = 0;
		private SortableCheckListFilter _filter = null;

		private Color _colorOff = Color.FromArgb(150, 150, 150);
		private Color _colorOn = Color.MediumSlateBlue;

		private const string MeasureText = "QWypg/#_Ág";
		private int? _dragStartIndex = null;
		private int _currentDropIndex = -1;
		private int _hoveringAboveDragBurgerIndex = -1;
		private int _hoveringAboveCheckBoxIndex = -1;



		// ============================================================================
		public SortableCheckList()
		{
			InitializeComponent();
			SuspendLayout();

			SetStyle(
				ControlStyles.Selectable |
				ControlStyles.UserPaint |
				ControlStyles.ResizeRedraw |
				ControlStyles.OptimizedDoubleBuffer,
				true);

			TabStop = true;
			DoubleBuffered = true;
			BackColor = SystemColors.Window;

			// Initialize the tooltip
			_toolTip = new ToolTip
			{
				// effectlively disable automatic timing
				AutoPopDelay = 999999999,
				InitialDelay = 999999999,
				ReshowDelay = 999999999,
				AutomaticDelay = 999999999,
				//ShowAlways = true // Always show tooltips
			};

			// Timer to control tooltip visibility
			_toolTipTimer = new Timer { Interval = 750 };
			_toolTipTimer.Tick += ShowTooltipTimer_Tick;

			// initialize with one initial columns
			_columns.Add(
				new ColumnDefinition
				{
					Header = "Title",
					PropertyName = string.Empty,
					Width = "100px"
				});

			AdjustItemHeight();
			ResumeLayout();
			Invalidate();
		}

		// ##################################################
		// ##################################################

		#region Properties


		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public List<SortableCheckItem> Items
		{
			get => _items;
			set
			{
				_items = value ?? new List<SortableCheckItem>();
				RecalculateColumnWidths();
				ApplyFilter();     // sets IsFilteredOut flags
				Invalidate();
				SyncJsonProperties();
			}
		}

		/*
		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public List<SortableCheckItem> FilteredItems
    		=> _items.Where(it => !it.IsFilteredOut).ToList();
		*/

		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		[Browsable(true)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public List<ColumnDefinition> Columns
		{
			get => _columns;
			set
			{
				_columns = value ?? new List<ColumnDefinition>();

				RecalculateColumnWidths();
				OnSortingColumnChanged();
				Invalidate();
				SyncJsonProperties();
			}
		}

		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		[Browsable(false)]
		[EditorBrowsable(EditorBrowsableState.Always)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
		[Category("Data")]
		[DisplayName("ColumnDefinitions")]
		public string ColumnsJson
		{
			get => JsonConvert.SerializeObject(_columns, Formatting.None);
			set
			{
				_columns = string.IsNullOrEmpty(value) ? new List<ColumnDefinition>() :
						   JsonConvert.DeserializeObject<List<ColumnDefinition>>(value) ?? new List<ColumnDefinition>();
			}
		}

		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public SortableCheckListFilter Filter
		{
			get => _filter;
			set
			{
				_filter = value;
				ApplyFilter();
				Invalidate();
			}
		}

		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		private void SyncJsonProperties()
		{
			ColumnsJson = JsonConvert.SerializeObject(_columns, Formatting.None);
		}

		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// Return the index of the currently selected row / item; If no row / item is selected, the return value is -1
		/// </summary>
		public int SelectedIndex
		{
			get
			{
				return _selectedIndex;
			}
		}

		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// Return the currently selected row / item; If no row / item is selected, the return value is null
		/// </summary>
		public object SelectedItem
		{
			get
			{
				if (_selectedIndex != -1)
				{
					return _items[_selectedIndex].ItemObject;
				}

				return null;
			}
		}


		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// Returns a list of all checked items
		/// </summary>
		public List<SortableCheckItem> CheckedItems
		{
			get
			{
				return _items.Where(item => item.IsChecked).ToList();
			}
		}


		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// Returns a list of all checked items
		/// </summary>
		public List<object> CheckedObject
		{
			get
			{
				return _items.Where(item => item.IsChecked)
							 .Select(item => item.ItemObject)
							 .ToList();
			}
		}



		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public int ItemHeigth
		{
			get
			{
				return _itemHeight;
			}
			set
			{
				_itemHeight = value;
				AdjustItemHeight();
				Invalidate();
			}
		}


		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public int CheckBoxSize
		{
			get
			{
				return _checkBoxSize;
			}
			set
			{
				_checkBoxSize = value;

				if (_checkBoxSize < 10)
				{
					_checkBoxSize = 10;
				}

				AdjustItemHeight();
				RecalculateColumnWidths();
				Invalidate();
			}
		}

		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public int CheckBoxRadius
		{
			get
			{
				return _checkBoxRadius;
			}
			set
			{
				_checkBoxRadius = value;

				if (_checkBoxRadius > _checkBoxSize)
				{
					_checkBoxRadius = _checkBoxSize;
				}

				RecalculateColumnWidths();
				Invalidate();
			}
		}

		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public int CheckBoxMargin
		{
			get
			{
				return _checkBoxMargin;
			}
			set
			{
				_checkBoxMargin = value;

				if (_checkBoxMargin < 1)
				{
					_checkBoxMargin = 1;
				}

				if (_checkBoxMargin > (_checkBoxSize / 2f) - 1)
				{
					_checkBoxMargin = (int) (_checkBoxSize / 2f) - 1;
				}

				RecalculateColumnWidths();
				Invalidate();
			}
		}


		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public override Font Font
		{
			get => base.Font;
			set
			{
				base.Font = value;

				RecalculateColumnWidths();
				AdjustItemHeight();
				Invalidate();
			}
		}


		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public Color BorderColor
		{
			get
			{
				return _borderColor;
			}
			set
			{
				_borderColor = value;
				Invalidate();
			}
		}

		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public float BorderThickness
		{
			get
			{
				return _borderThickness;
			}
			set
			{
				_borderThickness = value;
				Invalidate();
			}
		}

		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public Color ColorChecked
		{
			get
			{
				return _colorOn;
			}
			set
			{
				_colorOn = value;
				Invalidate();
			}
		}

		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public Color ColorUnchecked
		{
			get
			{
				return _colorOff;
			}
			set
			{
				_colorOff = value;
				Invalidate();
			}
		}


		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public int DragBurgerSize
		{
			get
			{
				return _dragBurgerSize;
			}
			set
			{
				_dragBurgerSize = value;

				RecalculateColumnWidths();
				Invalidate();
			}
		}

		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public float DragBurgerLineThickness
		{
			get
			{
				return _dragBurgerLineThickness;
			}
			set
			{
				_dragBurgerLineThickness = value;
				Invalidate();
			}
		}

		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public bool ShowScrollBar
		{
			get
			{
				return _isShowScrollBar;
			}
			set
			{
				_isShowScrollBar = value;

				RecalculateColumnWidths();
				Invalidate();
			}
		}


		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public bool IsCheckable
		{
			get
			{
				return _isCheckable;
			}
			set
			{
				_isCheckable = value;

				RecalculateColumnWidths();
				Invalidate();
			}
		}


		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public bool IsSortable
		{
			get
			{
				return _isSortable;
			}
			set
			{
				_isSortable = value;

				RecalculateColumnWidths();
				Invalidate();
			}
		}

		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public bool IsBoldWhenChecked
		{
			get
			{
				return _isBoldWhenCheck;
			}
			set
			{
				_isBoldWhenCheck = value;

				Invalidate();
			}
		}

		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public bool ShowOnlyCheckedItems
		{
			get
			{
				return _isShowOnlyCheckedItems;
			}
			set
			{
				_isShowOnlyCheckedItems = value;

				ApplyFilter();
				Invalidate();
			}
		}



		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public new Image BackgroundImage
		{
			get
			{
				return _noDataImage;
			}
			set
			{
				_noDataImage = value;
				Invalidate();
			}
		}


		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public bool ShowTooltips
		{
			get
			{
				return _showToolTips;
			}
			set
			{
				_showToolTips = value;
				_toolTip.Active = value;
			}
		}




		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		[Browsable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public new string Text
		{
			get
			{
				return base.Text;
			}
			set
			{
				base.Text = value;
			}
		}


		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		[Browsable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public int SortingColumnIndex
		{
			get
			{
				int index = 0;

				for (int i = 0; i < _columns.Count; i++)
				{
					if (_columns[i].Enabled)
					{
						if (_columns[i].IsSortingColumn)
						{
							return index;
						}
						index++;
					}
				}

				return -1;
			}

			set
			{
				if (value == -1)
				{
					ResetSortingColumnToNone();
					OnSortingColumnChanged();
					Invalidate();
					return;
				}

				int index = 0;

				for (int i = 0; i < _columns.Count; i++)
				{
					if (_columns[i].Enabled)
					{
						if (index == value &&
							_columns[i].IsSortable)
						{
							ResetSortingColumnToNone();
							_columns[i].IsSortingColumn = true;

							Sort();
							OnSortingColumnChanged();
							Invalidate();
							return;
						}

						index++;
					}
				}
			}
		}

		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		[Browsable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public SortOrder SortingColumnOrder
		{
			get
			{
				int sortingColumnIndex = SortingColumnIndex;

				if (sortingColumnIndex == -1)
				{
					return SortOrder.None;
				}

				int index = 0;

				for (int i = 0; i < _columns.Count; i++)
				{
					if (_columns[i].Enabled)
					{
						if (index == sortingColumnIndex)
						{
							return _columns[i].SortOrder;
						}

						index++;
					}
				}

				return SortOrder.None;
			}

			set
			{
				if (value == SortOrder.None)
				{
					return;
				}

				int sortingColumnIndex = SortingColumnIndex;
				int index = 0;

				for (int i = 0; i < _columns.Count; i++)
				{
					if (_columns[i].Enabled)
					{
						if (index == sortingColumnIndex &&
							_columns[i].IsSortable)
						{
							_columns[i].SortOrder = value;

							Sort();
							OnSortingColumnChanged();
							Invalidate();
							return;
						}

						index++;
					}
				}

			}
		}

		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// Returns a list of ItemObjects cast to the specified type T.
		/// </summary>
		/// <typeparam name="T">The type to cast each ItemObject to.</typeparam>
		public List<T> GetItemObjects<T>()
		{
			return _items
				.Select(it => it.ItemObject)
				.OfType<T>()
				.ToList();
		}



		#endregion

		// ##################################################
		// ##################################################

		#region Events

		/// <summary>
		/// Triggers when a new row was selected or all have been de-selected.
		/// </summary>
		public event EventHandler SelectedIndexChanged;

		/// <summary>
		/// Triggers whenever an item is checked or unchecked.
		/// </summary>
		public event EventHandler<ItemEventArgs> ItemChecked;

		/// <summary>
		/// Triggers if the order of the items has be changed.
		/// </summary>
		public event EventHandler ItemOrderChanged;

		/// <summary>
		/// Triggers if the sorting column changed
		/// </summary>
		public event EventHandler SortingColumnChanged;



		// ============================================================================
		protected override void OnPaint(
			PaintEventArgs e)
		{
			base.OnPaint(e);
			Graphics g = e.Graphics;

			g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
			g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

			//int visibleCount = GetVisibleItemCount();

			Brush brushCheckedRow =
				new SolidBrush(
					Color.FromArgb(10, ColorHelper.MixColors(_colorOn, 0.7, Color.Black)));

			var checkerRadius = _checkBoxRadius * 0.5f;
			var marginTopText = ((_itemHeight - _textHeight) / 2f);
			var marginTopBurger = (_itemHeight - _dragBurgerSize) / 2f;
			var marginTopCheckBox = (_itemHeight - _checkBoxSize) / 2f;

			// -------------------------------------
			// paint all headers
			var leftMargin = 10 + (_isCheckable ? _checkBoxSize + 10 : 0);
			var spaceAvailableForColumns = GetSpaceForColumns(leftMargin);

			var colPosX = leftMargin;

			g.FillRectangle(
				new SolidBrush(Color.FromArgb(255, 60, 60, 60)),
				new Rectangle(0, 0, this.Width, _itemHeight));

			if (_visible.Count > 0)
			{
				foreach (var column in _columns)
				{
					if (column.Enabled == false)
					{
						continue;
					}

					var colWidth = column.GetWithInPixels(_dynamicColumnSpace);
					var headerString = column.Header;

					if (column.IsSortingColumn)
					{
						if (column.SortOrder == SortOrder.Ascending)
						{
							headerString += " ▲"; //" ↑";
						}
						else
						{
							headerString += " ▼"; //" ↓";
						}
					}

					g.DrawString(
						headerString,
						new Font(Font, FontStyle.Bold),
						Brushes.White,
						colPosX,
						marginTopText + 1);

					colPosX += colWidth;
				}
			}



			// -------------------------------------
			// paint all items
			int rowsPerPage = (Height - _itemHeight) / _itemHeight;
			int startV = _scrollOffset / _itemHeight;
			int endV = Math.Min(_visible.Count, startV + rowsPerPage);

			if (_items.Count > 0)
			{


				for (int v = startV; v < endV; v++)
				{
					// logical index in _items
					int i = _visible[v];

					// add one item height for the header
					int yPosition = Math.Max(
						_itemHeight,
						((v + 1) * _itemHeight) - _scrollOffset);

					var item = _items[i];
					var isChecked = item.IsChecked && _isCheckable;
					var isSelected = i == _selectedIndex;

					var brushRow = isSelected ? new SolidBrush(Color.FromArgb(210, SystemColors.Highlight)) : (isChecked ? brushCheckedRow : Brushes.White);
					var brushText = isSelected ? new SolidBrush(SystemColors.HighlightText) : new SolidBrush(ForeColor);
					var hoveringThisCheckBox = _hoveringAboveCheckBoxIndex == i;

					var checkBoxFillColor =
						isChecked ?
						ColorHelper.MixColors(_colorOn, (isSelected ? 0.2 : 0), Color.White) :
						_colorOff;

					var penCheckBoxFrame =
						isSelected ?
						new Pen(Color.FromArgb(210, Color.Black), hoveringThisCheckBox ? 2f : 1.5f) :
						new Pen(Color.FromArgb(120, 0, 0, 0), isChecked ? (hoveringThisCheckBox ? 2f : 1.5f) : (hoveringThisCheckBox ? 2f : 1f));

					// paint the row
					var penRowFrame = new Pen(Color.FromArgb(50, Color.LightGray));
					g.FillRectangle(brushRow, new Rectangle(0, yPosition, this.Width, _itemHeight));
					g.DrawRectangle(penRowFrame, new Rectangle(0, yPosition, this.Width, _itemHeight));


					// write the text
					colPosX = leftMargin;

					if (_items.Count > 0)
					{
						foreach (var column in _columns)
						{
							if (column.Enabled == false)
							{
								continue;
							}

							var colWidth = column.GetWithInPixels(_dynamicColumnSpace);

							if (!string.IsNullOrWhiteSpace(column.PropertyName))
							{
								var propertyObject = GetPropertyValue(item.ItemObject, column.PropertyName);

								if (propertyObject is Bitmap)
								{
									var propertyBitmap = propertyObject as Bitmap;
									var imageHeight = Math.Min(_itemHeight - 2, propertyBitmap.Height);
									var ratio = imageHeight / (float) propertyBitmap.Height;
									var imageWidth = propertyBitmap.Width * ratio;
									var marginTop = (_itemHeight - imageHeight) / 2;


									g.DrawImage(
										propertyBitmap,
										colPosX + column.MarginLeft,
										yPosition + marginTopText,
										imageWidth,
										imageHeight);
								}
								else
								{
									var propertyString = propertyObject?.ToString();

									g.DrawString(
										propertyString,
										(isChecked && _isBoldWhenCheck) || isSelected ? new Font(Font, FontStyle.Bold) : Font,
										brushText,
										new RectangleF(
											colPosX,
											yPosition + marginTopText + 1,
											colWidth,
											_textHeight));
								}
							}


							colPosX += colWidth;
						}
					}

					// Draw the checkbox background
					if (_isCheckable)
					{
						LinearGradientBrush lgb = new LinearGradientBrush(
							new Point(10, yPosition + (int) marginTopCheckBox),
							new Point(10 + _checkBoxSize, yPosition + (int) marginTopCheckBox + _checkBoxSize),
							ColorHelper.MixColors(checkBoxFillColor, 0.11, Color.Black),
							Color.FromArgb(255, checkBoxFillColor));

						DrawRoundedRectangle(
							g,
							new Rectangle(10, yPosition + (int) marginTopCheckBox, _checkBoxSize, _checkBoxSize),
							_checkBoxRadius,
							penCheckBoxFrame,
							lgb);

						// Draw the ckecker if this item is checked
						if (isChecked)
						{
							DrawRoundedRectangle(
								g,
								new Rectangle(
									10 + _checkBoxMargin,
									yPosition + (int) marginTopCheckBox + _checkBoxMargin,
									_checkBoxSize - (2 * _checkBoxMargin),
									_checkBoxSize - (2 * _checkBoxMargin)),
								checkerRadius,
								null,
								Brushes.White);
						}
					}

					// Draw drag-burger
					if (_isSortable)
					{
						var brushBurgerLines =
							new SolidBrush(
								Color.FromArgb(
									_hoveringAboveDragBurgerIndex == i ? 100 : 40,
									Color.Black));

						// the lines
						g.FillRectangle(
							brushBurgerLines,
							new RectangleF(
								Width - (_isShowScrollBar ? 15f : 8f) - (_dragBurgerSize * 2f),
								yPosition + marginTopBurger,
								_dragBurgerSize * 2f,
								_dragBurgerLineThickness));

						g.FillRectangle(
							brushBurgerLines,
							new RectangleF(
								Width - (_isShowScrollBar ? 15f : 8f) - (_dragBurgerSize * 2f),
								yPosition + (_itemHeight / 2f) - (_dragBurgerLineThickness / 2f),
								_dragBurgerSize * 2f,
								_dragBurgerLineThickness));

						g.FillRectangle(
							brushBurgerLines,
							new RectangleF(
								Width - (_isShowScrollBar ? 15f : 8f) - (_dragBurgerSize * 2f),
								yPosition + marginTopBurger + _dragBurgerSize - _dragBurgerLineThickness,
								_dragBurgerSize * 2f,
								_dragBurgerLineThickness));


						// paint a drag-n-drop indicator
						if (i == _currentDropIndex)
						{
							var brushFill = new SolidBrush(Color.FromArgb(50, Color.OrangeRed));
							g.FillRectangle(brushFill, 0, yPosition + 1, Width, _itemHeight - 2);
						}
					}
				}
			}


			// paint the scroll bar
			if (_isShowScrollBar)
			{
				int totalItemsHeight = (_visible.Count + 1) * _itemHeight;
				int clientHeight = this.ClientRectangle.Height - 7;

				// Calculate the length and position of the scrollbar
				float scrollBarRatio = clientHeight / (float) totalItemsHeight;

				int scrollBarHeight =
					scrollBarRatio > 1 ?
					clientHeight :
					(int) (clientHeight * scrollBarRatio);

				int scrollBarPos = 3 + (int) ((float) _scrollOffset / totalItemsHeight * clientHeight);

				// white background
				DrawRoundedRectangle(
					g,
					new Rectangle(Width - 11, scrollBarPos - 3, 10, scrollBarHeight + 6),
					4f,
					null,
					new SolidBrush(Color.FromArgb(230, BackColor)));

				// actual scrollbar
				DrawRoundedRectangle(
					g,
					new Rectangle(Width - 8, scrollBarPos, 4, scrollBarHeight),
					4f,
					null,
					new SolidBrush(Color.FromArgb(100, Color.Gray)));
			}


			// -------------------------------------
			// paint the border
			var borderPen = new Pen(_borderColor, _borderThickness);
			g.DrawRectangle(borderPen, 0, 0, Width - _borderThickness, Height - _borderThickness);

		}

		// ============================================================================
		private void ShowTooltipTimer_Tick(
			object sender,
			EventArgs e)
		{
			_toolTipTimer.Stop();


			if (_isToolTipVisible)
			{
				return;
			}


			_toolTip.Show(
				_currentTooltipText,
				this,
				new Point(
					_lastMousePosition.X + 15,
					_lastMousePosition.Y - 3));

			_isToolTipVisible = true;
		}


		// ============================================================================
		protected virtual void OnSelectedIndexChanged()
		{
			SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
		}

		// ============================================================================
		protected virtual void OnItemChecked(SortableCheckItem item = null)
		{
			if (item != null)
			{
				var eventArgs = new ItemEventArgs(item);
				ItemChecked?.Invoke(this, eventArgs);
			}
			else
			{
				ItemChecked?.Invoke(this, ItemEventArgs.Empty);
			}
		}

		// ============================================================================
		protected virtual void OnItemOrderChanged()
		{
			ItemOrderChanged?.Invoke(this, EventArgs.Empty);
		}

		// ============================================================================
		protected virtual void OnSortingColumnChanged()
		{
			SortingColumnChanged?.Invoke(this, EventArgs.Empty);
		}


		// ============================================================================
		protected override void OnMouseLeave(
			EventArgs e)
		{
			base.OnLeave(e);

			_hoveringAboveCheckBoxIndex = -1;
			_hoveringAboveDragBurgerIndex = -1;

			_toolTip.Active = false;
			_toolTip.Hide(this);
			_toolTipTimer.Stop();
		}


		// ============================================================================
		protected override void OnMouseEnter(
			EventArgs e)
		{
			base.OnEnter(e);

			_toolTip.Active = _showToolTips;
		}


		// ============================================================================
		protected override void OnMouseWheel(
			MouseEventArgs e)
		{
			base.OnMouseWheel(e);

			_scrollOffset -= e.Delta / 120 * _itemHeight;

			ClampScrollOffset();
			Invalidate();
		}


		// ============================================================================
		protected override void OnResize(
			EventArgs e)
		{
			base.OnResize(e);
			ClampScrollOffset();
			EnsureItemVisible(_selectedIndex);
			RecalculateColumnWidths();
			Invalidate();
		}

		// ============================================================================
		protected override void OnMouseDown(
			MouseEventArgs e)
		{
			base.OnMouseDown(e);
			Focus();
			HandleMousePointerPosition(e.Location, true);
			Invalidate();
		}


		// ============================================================================
		protected override void OnMouseUp(
			MouseEventArgs e)
		{
			base.OnMouseUp(e);

			if (_isSortable &&
				_dragStartIndex.HasValue &&
				_currentDropIndex != -1 &&
				_dragStartIndex.Value != _currentDropIndex)
			{
				// Reorder the list
				var draggedItem = _items[_dragStartIndex.Value];
				var draggedMetaItem = _items[_dragStartIndex.Value];

				_items.RemoveAt(_dragStartIndex.Value);
				_items.Insert(_currentDropIndex, draggedItem);

				// Reset drag state
				_dragStartIndex = null;
				_currentDropIndex = -1;

				// update all sorting indexes
				int index = 0;
				foreach (var item in _items)
				{
					item.SortingIndex = index++;
				}

				ResetSortingColumnToNone();

				// Trigger events
				OnItemOrderChanged();
				OnSelectedIndexChanged();
				OnSortingColumnChanged();
			}
			else
			{
				_dragStartIndex = null;
				_currentDropIndex = -1;
			}

			ApplyFilter();
			Invalidate();
		}

		// ============================================================================
		protected override void OnMouseMove(
			MouseEventArgs e)
		{
			base.OnMouseMove(e);

			HandleMousePointerPosition(e.Location);

			if (_isSortable &&
				_dragStartIndex.HasValue)
			{
				int newIndex = GetItemAtPoint(e.Location);

				if (newIndex != -1 && newIndex != _currentDropIndex)
				{
					_currentDropIndex = newIndex;
					Invalidate();
				}
			}
		}


		// ============================================================================
		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			switch (keyData)
			{
				case Keys.Up:
					SelectPreviousItem();
					return true; // Key press handled
				case Keys.Down:
					SelectNextItem();
					return true; // Key press handled
				case Keys.Space:
					if (_isCheckable)
					{
						ToggleSelectedItem();
					}
					break;
			}
			return base.ProcessCmdKey(ref msg, keyData);
		}


		#endregion

		// ##################################################
		// ##################################################

		#region Custom Logic


		// ============================================================================
		public override void Refresh()
		{
			base.Refresh();

			ApplyFilter();
			ClampScrollOffset();
			Invalidate();
		}



		// ============================================================================
		private void RecalculateColumnWidths()
		{
			var fixedWidthUsed = 0;
			var percentSum = 0;

			foreach (var column in _columns)
			{
				if (column.Enabled == false)
				{
					continue;
				}

				if (column.IsFixedWidth)
				{
					fixedWidthUsed += column.GetWithInPixels();
				}
				else
				{
					percentSum += column.GetWithInPixels(100);
				}
			}

			_dynamicColumnSpace = GetSpaceForColumns() - fixedWidthUsed;
		}


		// ============================================================================
		private int GetSpaceForColumns(
			int leftMargin = -1)
		{
			if (leftMargin == -1)
			{
				leftMargin = 10 + (_isCheckable ? _checkBoxSize + 10 : 0);
			}

			return
				this.Width -
				leftMargin -
				(_isSortable ? (_dragBurgerSize * 2) + 6 : 0) -
				(_isShowScrollBar ? 8 : 0);
		}


		// ============================================================================
		private void ClampScrollOffset()
		{
			_scrollOffset =
				Math.Max(
					0,
					Math.Min(
						_scrollOffset,
						Math.Max(
							0,
							(GetVisibleItemCount() * _itemHeight) - (Height - _itemHeight))));

			_scrollOffset = RoundUpToNextMultiple(_scrollOffset, _itemHeight);
		}


		// ============================================================================
		int RoundUpToNextMultiple(int number, int stepSize)
		{
			return ((number + stepSize - 1) / stepSize) * stepSize;
		}


		// ============================================================================
		private int GetItemAtPoint(
			Point location)
		{
			// Calculate which item is at the given location
			for (int v = 0; v < _visible.Count; v++)
			{
				if (GetItemBounds(v).Contains(location))
					return _visible[v];   // return **logical** index
			}
			return -1;
		}

		// ============================================================================
		private Rectangle GetItemBounds(
			int visibleIndex)
		{
			return new Rectangle(
				0,
				(visibleIndex * _itemHeight) + _itemHeight - _scrollOffset,
				Width,
				_itemHeight);
		}

		// ============================================================================
		public void SetItemChecked(
			int index,
			bool state)
		{
			if (index >= 0 &&
				index < _items.Count)
			{
				_items[index].IsChecked = state;
				ApplyFilter();
			}
		}

		// ============================================================================
		public void CheckAllItems()
		{
			foreach (var item in _items)
			{
				item.IsChecked = true;
			}

			ApplyFilter();
			OnItemChecked();
			Invalidate();
		}

		// ============================================================================
		public void UnCheckAllItems()
		{
			foreach (var item in _items)
			{
				item.IsChecked = false;
			}

			ApplyFilter();
			OnItemChecked();
			Invalidate();
		}

		// ============================================================================
		public void InvertCheckOfAllItems()
		{
			foreach (var item in _items)
			{
				item.IsChecked = !item.IsChecked;
			}

			ApplyFilter();
			OnItemChecked();
			Invalidate();
		}

		// ============================================================================
		private void SelectPreviousItem()
		{
			int v = LogicalToVisible(_selectedIndex);

			if (v > 0)
			{
				_selectedIndex = _visible[v - 1];
			}

			EnsureItemVisible(_selectedIndex);
			OnSelectedIndexChanged();
			Invalidate();
		}

		// ============================================================================
		private void SelectNextItem()
		{
			int v = LogicalToVisible(_selectedIndex);

			if (v < _visible.Count - 1)
			{
				_selectedIndex = _visible[v + 1];
			}

			EnsureItemVisible(_selectedIndex);
			OnSelectedIndexChanged();
			Invalidate();
		}

		// ============================================================================
		private void ToggleSelectedItem()
		{
			if (_selectedIndex == -1)
			{
				return;
			}

			_items[_selectedIndex].Toggle();
			OnItemChecked(_items[_selectedIndex]);
			Invalidate();

		}

		// ============================================================================
		public new void Invalidate()
		{
			CheckIfBackgroundImageNeedsShowing();

			base.Invalidate();
		}

		// ============================================================================
		public void DeselectAll()
		{
			_selectedIndex = -1;
			Invalidate();
		}


		// ============================================================================
		public void EnsureItemVisible(
			int logicalIndex)
		{
			int v = LogicalToVisible(logicalIndex);
			if (v == -1)
			{
				return;
			}

			int top = (v * _itemHeight) + _itemHeight;
			int bot = top + _itemHeight;

			if (top < _scrollOffset)
				_scrollOffset = top;
			else if (bot > _scrollOffset + Height)
				_scrollOffset = bot - Height + _itemHeight;

			Invalidate();
		}

		// ============================================================================
		private void CheckIfBackgroundImageNeedsShowing()
		{
			var noData =
				_items == null ||
				GetVisibleItemCount() == 0;

			var supposedBackground = noData ? _noDataImage : null;

			if (supposedBackground != base.BackgroundImage)
			{
				base.BackgroundImage = supposedBackground;
				Invalidate();
			}
		}

		// ============================================================================
		private void DrawRoundedRectangle(
			Graphics g,
			Rectangle bounds,
			float cornerRadius,
			Pen drawPen,
			Brush fillBrush)
		{
			using (GraphicsPath path = new GraphicsPath())
			{
				path.AddArc(bounds.Left, bounds.Top, cornerRadius, cornerRadius, 180, 90);
				path.AddArc(bounds.Right - cornerRadius, bounds.Top, cornerRadius, cornerRadius, 270, 90);
				path.AddArc(bounds.Right - cornerRadius, bounds.Bottom - cornerRadius, cornerRadius, cornerRadius, 0, 90);
				path.AddArc(bounds.Left, bounds.Bottom - cornerRadius, cornerRadius, cornerRadius, 90, 90);
				path.CloseFigure();

				if (fillBrush != null)
				{
					g.FillPath(fillBrush, path);
				}

				if (drawPen != null)
				{
					g.DrawPath(drawPen, path);
				}
			}
		}

		// ============================================================================
		private void AdjustItemHeight()
		{
			using (Graphics g = CreateGraphics())
			{
				SizeF textSize = g.MeasureString(MeasureText, Font);
				_textHeight = textSize.Height;
				_itemHeight = _textHeight > _itemHeight ? (int) _textHeight : _itemHeight;
				_itemHeight = _checkBoxSize + 2 > _itemHeight ? _checkBoxSize + 2 : _itemHeight;
			}
		}


		// ============================================================================
		private void UpdateToolTipText(
			Point pointerLocation,
			int yIndex)
		{
			var newTooltipText = "This is some very useful information alright!";


			// -------------------------------------
			// go through all columns
			var leftMargin = 10 + (_isCheckable ? _checkBoxSize + 10 : 0);
			var spaceAvailableForColumns = GetSpaceForColumns(leftMargin);
			var colPosX = leftMargin;

			if (_items.Count > 0)
			{
				foreach (var column in _columns)
				{
					if (column.Enabled == false)
					{
						continue;
					}

					var colWidth = column.GetWithInPixels(_dynamicColumnSpace);

					if (pointerLocation.X > colPosX &&
						pointerLocation.X <= colPosX + colWidth)
					{
						newTooltipText = column.TooltipText;
						break;
					}

					colPosX += colWidth;
				}
			}


			// Update tooltip only if the text changes
			if (_currentTooltipText != newTooltipText)
			{
				_toolTipTimer.Start();
				_currentTooltipText = newTooltipText;

				_toolTip.SetToolTip(this, _currentTooltipText);
				//_toolTip.Active = true;
			}
		}


		// ============================================================================
		private void HandleColumnsResorting(
			Point pointerLocation)
		{
			if (pointerLocation.Y > _itemHeight)
			{
				return;
			}

			// -------------------------------------
			// go through all columns
			var leftMargin = 10 + (_isCheckable ? _checkBoxSize + 10 : 0);
			var spaceAvailableForColumns = GetSpaceForColumns(leftMargin);
			var colPosX = leftMargin;

			if (_items.Count > 0)
			{
				foreach (var column in _columns)
				{
					if (column.Enabled == false)
					{
						continue;
					}

					var colWidth = column.GetWithInPixels(_dynamicColumnSpace);

					// diod we click into a columns header?
					if (pointerLocation.X > colPosX &&
						pointerLocation.X <= colPosX + colWidth)
					{
						if (!column.IsSortable)
						{
							return;
						}

						var wasSortingColumnAlready = column.IsSortingColumn;

						// reset which coluns is sorted right now
						ResetSortingColumnToNone();
						column.IsSortingColumn = true;

						if (wasSortingColumnAlready)
						{
							if (column.SortOrder == SortOrder.Ascending)
							{
								column.SortOrder = SortOrder.Descending;
							}
							else if (column.SortOrder == SortOrder.Descending)
							{
								column.SortOrder = SortOrder.Ascending;
							}
							else
							{
								column.SortOrder = SortOrder.Ascending;
							}
						}

						OnSortingColumnChanged();

						Sort();
						return;
					}

					colPosX += colWidth;
				}
			}

		}


		// ============================================================================
		public bool Sort()
		{
			foreach (var column in _columns)
			{
				if (!column.IsSortingColumn)
				{
					continue;
				}

				try
				{
					if (column.SortOrder == SortOrder.Ascending)
					{
						// Perform sorting ascending
						_items = _items.OrderBy(
							item => GetPropertyValue(
								item.ItemObject,
								column.PropertyName).ToString()).ToList();
					}
					else
					{
						// Perform sorting descending
						_items = _items.OrderByDescending(
							item => GetPropertyValue(
								item.ItemObject,
								column.PropertyName).ToString()).ToList();
					}
				}
				catch (Exception) { return false; }

				// update all sorting indexes
				int index = 0;
				foreach (var item in _items)
				{
					item.SortingIndex = index++;
				}

				ApplyFilter();
				OnItemOrderChanged();
				Invalidate();
				break;
			}

			return true;
		}


		// ============================================================================
		private object GetPropertyValue(
			object obj,
			string propertyName)
		{
			if (obj == null)
			{
				return null;
			}

			PropertyInfo prop = obj.GetType().GetProperty(propertyName);
			return prop?.GetValue(obj);
		}


		// ============================================================================
		private void ResetSortingColumnToNone()
		{
			foreach (var column in _columns)
			{
				column.IsSortingColumn = false;
			}
		}


		// ============================================================================
		private void HandleMousePointerPosition(
			Point pointerLocation,
			bool isClick = false)
		{
			// hide the current toolTip
			if (_lastMousePosition != pointerLocation)
			{
				_toolTip.Hide(this);
				_isToolTipVisible = false;

				// restart the toolTip timer
				_toolTipTimer.Stop();
				_toolTipTimer.Start();
				_lastMousePosition = pointerLocation;
			}

			// do some initialization first
			var _hoverCheckBoxIndexOld = _hoveringAboveCheckBoxIndex;
			var _hoverDragBurgerIndexOld = _hoveringAboveDragBurgerIndex;

			_hoveringAboveCheckBoxIndex = -1;
			_hoveringAboveDragBurgerIndex = -1;

			var mouseOverRowWithIndex = -1;

			int rowsPerPage = (Height - _itemHeight) / _itemHeight;
			int startV = _scrollOffset / _itemHeight;
			int endV = Math.Min(_visible.Count, startV + rowsPerPage);

			// get row index of mouse position
			for (int v = startV; v < endV; v++)
			{
				int i = _visible[v];

				int yPosition = (v * _itemHeight) + _itemHeight - _scrollOffset;

				var checkBoxBoundsRow =
					new Rectangle(0, yPosition, Width, _itemHeight);

				if (checkBoxBoundsRow.Contains(pointerLocation))
				{
					mouseOverRowWithIndex = i;
					break;
				}
			}

			// not over a data row?
			if (_selectedIndex == -1)
			{
				// we are above the header
				if (pointerLocation.Y < _itemHeight)
				{
					_currentTooltipText = "This is the header with titles for the different columns below.";

					if (!isClick)
					{
						return;
					}

					HandleColumnsResorting(
						pointerLocation);
				}
				else
				{
					_currentTooltipText = string.Empty;
				}
			}

			// check check-boxes
			if (_isCheckable &&
				mouseOverRowWithIndex != -1)
			{
				int v = LogicalToVisible(mouseOverRowWithIndex);
				int yPosition = (v * _itemHeight) + _itemHeight - _scrollOffset;

				var checkBoxBoundsCheckBox =
					new Rectangle(10, yPosition, _checkBoxSize, _checkBoxSize);

				if (checkBoxBoundsCheckBox.Contains(pointerLocation))
				{
					if (isClick)
					{
						_items[mouseOverRowWithIndex].Toggle();
						OnItemChecked(_items[mouseOverRowWithIndex]);
					}
					else
					{
						_hoveringAboveCheckBoxIndex = mouseOverRowWithIndex;
						if (_hoveringAboveCheckBoxIndex != _hoverCheckBoxIndexOld)
						{
							Invalidate();
						}
					}

					_currentTooltipText = "Check this row for adding it to the process execution.";

					// Leave - we are done here...
					return;
				}
			}


			// check drag-burger
			if (_isSortable &&
				mouseOverRowWithIndex != -1)
			{
				int v = LogicalToVisible(mouseOverRowWithIndex);
				int yPosition = (v * _itemHeight) + _itemHeight - _scrollOffset;

				var checkBoxBoundsCheckBox =
					new RectangleF(
						Width - 15f - (_dragBurgerSize * 2f),
						yPosition,
						_dragBurgerSize * 2f,
						_itemHeight);

				if (checkBoxBoundsCheckBox.Contains(pointerLocation))
				{
					if (isClick)
					{
						_dragStartIndex = mouseOverRowWithIndex;
					}
					else
					{
						_hoveringAboveDragBurgerIndex = mouseOverRowWithIndex;
						if (_hoveringAboveDragBurgerIndex != _hoverDragBurgerIndexOld)
						{
							Invalidate();
						}
					}

					_currentTooltipText = "You can re-sort the rows here. This will have an effect on the execution order.";

					// Leave - we are done here...
					return;
				}

			}

			// Not a click?
			// this is only executed over actual data (not check-ball, drag ha,burger, ...)
			if (!isClick)
			{
				if (_hoveringAboveCheckBoxIndex != _hoverCheckBoxIndexOld ||
					_hoveringAboveDragBurgerIndex != _hoverDragBurgerIndexOld)
				{
					Invalidate();
				}

				if (mouseOverRowWithIndex != -1)
				{
					UpdateToolTipText(
						pointerLocation,
						mouseOverRowWithIndex);
				}

				return;
			}


			// ..we have a click.
			// check if the selected index would change:
			if (_selectedIndex != mouseOverRowWithIndex)
			{
				_selectedIndex = mouseOverRowWithIndex;

				OnSelectedIndexChanged();
				ApplyFilter();

			}

		}

		// ============================================================================
		// Update ApplyFilter to AND-join _isShowOnlyCheckedItems with existing filter
		private bool ApplyFilter()
		{
			if (_items == null)
			{
				return false;
			}


			object prevSelection =
				_selectedIndex >= 0 &&
				_selectedIndex < _items.Count
					? _items[_selectedIndex].ItemObject
					: null;

			// 1) reset flags
			foreach (var it in _items)
			{
				it.IsFilteredOut = false;
			}

			// 2) mark hidden items
			bool hasText =
				_filter != null &&
				!string.IsNullOrWhiteSpace(_filter.FilterOnProperty) &&
				!string.IsNullOrWhiteSpace(_filter.FilterString);

			if (hasText || _isShowOnlyCheckedItems)
			{
				string needle = _filter?.FilterString ?? string.Empty;
				Func<string, bool> pred = _ => true;

				if (hasText)
				{
					switch (_filter.ConditionOperator)
					{
						case ConditionOperator.Equals:
							pred = s => string.Equals(
										   s, needle, StringComparison.OrdinalIgnoreCase);
							break;
						case ConditionOperator.Contains:
							pred = s => s?.IndexOf(
										   needle, StringComparison.OrdinalIgnoreCase) >= 0;
							break;
						case ConditionOperator.StartsWith:
							pred = s => s?.StartsWith(
										   needle, StringComparison.OrdinalIgnoreCase) == true;
							break;
						case ConditionOperator.EndsWith:
							pred = s => s?.EndsWith(
										   needle, StringComparison.OrdinalIgnoreCase) == true;
							break;
					}
				}

				foreach (var it in _items)
				{
					bool textOk = !hasText ||
								  pred((GetPropertyValue(it.ItemObject,
										  _filter.FilterOnProperty)?.ToString()) ?? string.Empty);
					bool checkedOk = !_isShowOnlyCheckedItems || it.IsChecked;

					it.IsFilteredOut = !(textOk && checkedOk);
				}
			}

			// 3) refresh the visible-row map (no duplicates, minimal memory)
			_visible = _items
					   .Select((it, idx) => new { it, idx })
					   .Where(x => !x.it.IsFilteredOut)
					   .Select(x => x.idx)
					   .ToList();

			// 4) keep selection if still visible
			_selectedIndex = prevSelection == null
				? -1
				: _items
					.FindIndex(it => Equals(it.ItemObject, prevSelection) &&
									 !it.IsFilteredOut);

			ClampScrollOffset();
			return true;
		}


		// ============================================================================
		private int GetVisibleItemCount()
		{
			return _visible.Count;
		}

		// ============================================================================
		private int LogicalToVisible(int logical)
		{
			return _visible.IndexOf(logical);
		}

		// ============================================================================
		private int VisibleToLogical(int visible)
		{
			return (
				visible >= 0 &&
				visible < _visible.Count)
					? _visible[visible]
					: -1;
		}


		// ============================================================================
		/// <summary>
		/// Helper method to update selection after filtering
		/// </summary>
		/// <param name="prevSelectedItem"></param>
		private void UpdateSelectionAfterFilter(
			object prevSelectedItem)
		{
			bool prevSelectedHasValue = prevSelectedItem != null;

			if (prevSelectedItem == null)
			{
				_selectedIndex = -1;
				return;
			}

			// Find the index of the previous item in the new filtered list
			int newIndex =
			_items
				.Where(it => !it.IsFilteredOut)
				.ToList()
				.FindIndex(it => Equals(it.ItemObject, prevSelectedItem));

			_selectedIndex = newIndex;

			if ((prevSelectedHasValue &&
				_selectedIndex == -1))
			{
				OnSelectedIndexChanged();
			}
		}

		#endregion

	}

	// ##################################################
	// ##################################################

	#region Supporting Classes

	// ============================================================================
	// ============================================================================
	// ============================================================================
	[Serializable]
	[TypeConverter(typeof(ColumnDefinitionConverter))]
	public sealed class SortableCheckItem :
		INotifyPropertyChanged,
		IComparable<SortableCheckItem>
	{
		private bool _isChecked;
		private bool _isFilteredOut;
		private int _sortingIndex;
		private string _title;

		private object _itemObject;     // the wrapped item (any type)
		private string _linkedProperty; // null → no external link
		private bool _localChecked;                 // fallback storage



		// ============================================================================
		public SortableCheckItem(
			object itemObject,
			string linkedPropertyName = null)
		{
			if (itemObject == null)
			{
				throw new ArgumentNullException(nameof(itemObject));
			}

			_itemObject = itemObject;
			_isChecked = false;
			_title = itemObject.ToString();
			_linkedProperty = linkedPropertyName;
			_isFilteredOut = false;

			// If the wrapped item notifies, attach a listener (only when we have a link).
			var notifier = itemObject as INotifyPropertyChanged;
			if (notifier != null && _linkedProperty != null)
				notifier.PropertyChanged += HandleItemObjectChanged;
		}

		// ============================================================================
		public SortableCheckItem(
			object item,
			int sortingIndex)
		{
			_itemObject = item;
			_sortingIndex = sortingIndex;
			_isChecked = false;
			_title = item.ToString();
			_isFilteredOut = false;
		}


		// ............................................................................
		public event PropertyChangedEventHandler PropertyChanged;


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public string Title
		{
			get
			{
				return _title;
			}
			set
			{
				_title = value;
				OnPropertyChanged(nameof(Title));
			}
		}

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public object ItemObject
		{
			get
			{
				return _itemObject;
			}
			set
			{
				_itemObject = value;
				OnPropertyChanged(nameof(ItemObject));
			}
		}

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public bool IsChecked
		{
			get
			{
				if (_linkedProperty == null)
				{
					return _localChecked;
				}

				var linkedPorperty = _itemObject.GetType().GetProperty(_linkedProperty);

				if (linkedPorperty == null ||
					linkedPorperty.PropertyType != typeof(bool))
				{

					throw new InvalidOperationException(
						"Linked property not found or not Boolean.");
				}

				return (bool) linkedPorperty.GetValue(_itemObject, null);
			}

			set
			{
				if (value == IsChecked)
				{
					return;
				}

				if (_linkedProperty == null)
				{
					_localChecked = value;
				}
				else
				{
					var pi = _itemObject.GetType().GetProperty(_linkedProperty);

					if (pi == null ||
						pi.PropertyType != typeof(bool))
					{
						throw new InvalidOperationException(
							"Linked property not found or not Boolean.");
					}

					pi.SetValue(_itemObject, value, null);
				}

				OnPropertyChanged(nameof(IsChecked));
			}
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public int SortingIndex
		{
			get
			{
				return _sortingIndex;
			}
			set
			{
				_sortingIndex = value;
				OnPropertyChanged(nameof(SortingIndex));
			}
		}

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public bool IsFilteredOut
		{
			get => _isFilteredOut;
			set
			{
				if (_isFilteredOut == value)
				{
					return;
				}

				_isFilteredOut = value;
				OnPropertyChanged(nameof(IsFilteredOut));
			}
		}



		// ============================================================================
		public int CompareTo(
			SortableCheckItem other)
		{
			return this.SortingIndex.CompareTo(other.SortingIndex);
		}

		// ============================================================================
		public bool Toggle()
		{
			IsChecked = !IsChecked;
			return IsChecked;
		}

		// ============================================================================
		private void HandleItemObjectChanged(
			object sender,
			PropertyChangedEventArgs e)
		{
			if (e.PropertyName != _linkedProperty)
			{
				return;
			}

			OnPropertyChanged(e.PropertyName);
		}

		// ============================================================================
		private void OnPropertyChanged(
			string propertyName)
		{
			var handler = PropertyChanged;

			if (handler != null)
			{
				handler(
					this,
					new PropertyChangedEventArgs(propertyName));
			}
		}

	}


	// ============================================================================
	// ============================================================================
	// ============================================================================
	[Serializable]
	public class ColumnDefinition
	{
		private string _widthString = "100px";
		private int _widthNumber = 100;
		private bool _enabled = true;

		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public bool Enabled
		{
			get
			{
				return _enabled;
			}

			set
			{
				_enabled = value;
			}
		}


		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// Accepts string definitions of the with in a few formats:
		/// ###% or ###px or ### --> ###px
		/// </summary>
		public string Width
		{
			get
			{
				return _widthString;
			}
			set
			{
				if (TryParseWidthString(
					value,
					out string widthString,
					out int widthNumber))
				{
					_widthString = widthString;
					_widthNumber = widthNumber;
				}
			}
		}

		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// This poroperty allows for aligning  images if the property value is a bitmap.
		/// This is ignored for text values.
		/// </summary>
		public int MarginLeft
		{
			get; set;
		} = 0;

		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public string TooltipText
		{
			get; set;
		} = string.Empty;

		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public string Header
		{
			get; set;
		} = string.Empty;

		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public string PropertyName
		{
			get; set;
		} = string.Empty;


		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public bool IsFixedWidth
		{
			get
			{
				return !_widthString.EndsWith("%");
			}
		}


		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public bool IsSortable
		{
			get; set;
		} = false;


		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public SortOrder SortOrder
		{
			get; set;
		} = SortOrder.Ascending;


		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public bool IsSortingColumn
		{
			get; set;
		} = false;



		// ============================================================================
		public ColumnDefinition()
		{
			// notting...
		}


		// ============================================================================
		public override string ToString()
		{
			return string.IsNullOrWhiteSpace(Header) ? base.ToString() : Header;
		}

		// ============================================================================
		public int GetWithInPixels(
			int clientWith = 0)
		{
			if (_widthString.EndsWith("px"))
			{
				return _widthNumber;
			}

			else if (_widthString.EndsWith("%"))
			{
				return (int) ((_widthNumber / 100f) * clientWith);
			}

			return _widthNumber;
		}




		// ============================================================================
		private bool TryParseWidthString(
			string widthString,
			out string resultString,
			out int resultNumber)
		{
			widthString = widthString.Trim().ToLower();
			widthString = widthString.Replace(" ", "");
			widthString = widthString.ToLower();

			var workString = widthString;

			if (workString.EndsWith("px"))
			{
				workString = workString.Remove(workString.Length - 2, 2);
			}

			else if (workString.EndsWith("%"))
			{
				workString = workString.Remove(workString.Length - 1, 1);
			}

			var isSuccess =
				int.TryParse(workString, out int widthNumber);

			if (isSuccess)
			{
				resultString = widthString;
				resultNumber = widthNumber;
			}
			else
			{
				resultString = string.Empty;
				resultNumber = -1;
			}

			return isSuccess;
		}

	}

	// ============================================================================
	// ============================================================================
	// ============================================================================
	internal sealed class ColumnDefinitionConverter : ExpandableObjectConverter
	{
		// ============================================================================
		public override bool CanConvertTo(
			ITypeDescriptorContext context,
			Type destinationType) =>
			destinationType == typeof(string) || base.CanConvertTo(context, destinationType);


		// ============================================================================
		public override object ConvertTo(
			ITypeDescriptorContext context,
			CultureInfo culture,
			object value,
			Type destinationType)
		{
			if (destinationType == typeof(string) &&
				value is ColumnDefinition col)
			{
				// Use the header text Visual Studio designers should display.
				return string.IsNullOrWhiteSpace(col.Header)
						? col.PropertyName
						: col.Header;
			}

			return base.ConvertTo(context, culture, value, destinationType);
		}
	}



	// ============================================================================
	// ============================================================================
	// ============================================================================
	public class CollectionEditor<T> : CollectionEditor
	{
		public CollectionEditor(Type type) : base(type) { }

		protected override Type CreateCollectionItemType() => typeof(T);
	}


	// ============================================================================
	// ============================================================================
	// ============================================================================
	public class SortableCheckListFilter
	{
		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public string FilterOnProperty
		{
			get; set;
		}

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public string FilterString
		{
			get; set;
		}

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public ConditionOperator ConditionOperator
		{
			get; set;
		}


		// ============================================================================
		public SortableCheckListFilter()
		{
			FilterOnProperty = null;
			FilterString = string.Empty;
			ConditionOperator = ConditionOperator.Equals;
		}

		// ============================================================================
		public SortableCheckListFilter(
			string filterOnProperty,
			string filterString,
			ConditionOperator condition = ConditionOperator.Contains)
		{
			FilterOnProperty = filterOnProperty;
			FilterString = filterString;
			ConditionOperator = condition;
		}
	}


	// ============================================================================
	// ============================================================================
	// ============================================================================
	public enum ConditionOperator
	{
		Equals,
		Contains,
		StartsWith,
		EndsWith
	}


	// ============================================================================
	// ============================================================================
	// ============================================================================
	public class ItemEventArgs : EventArgs
	{

		public new static readonly ItemEventArgs Empty = new ItemEventArgs(null);

		// ============================================================================
		public SortableCheckItem Item
		{
			get;
		}

		// ============================================================================
		public ItemEventArgs(SortableCheckItem item)
		{
			Item = item;
		}
	}


	#endregion
}
