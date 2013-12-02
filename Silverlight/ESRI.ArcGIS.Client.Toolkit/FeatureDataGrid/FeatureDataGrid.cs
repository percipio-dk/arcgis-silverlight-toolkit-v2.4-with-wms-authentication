// (c) Copyright ESRI.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Toolkit.Utilities;
using System.ComponentModel;
using ESRI.ArcGIS.Client.FeatureService;
using System.Collections.ObjectModel;
using System.Reflection;
#if SILVERLIGHT
using System.Windows.Browser;
using System.ComponentModel.DataAnnotations;
#else
using System.Web;
using System.Collections.Specialized;
using System.Windows.Media;
#if NET35
using Microsoft.Windows.Controls;
using Microsoft.Windows.Controls.Primitives;
#endif
#endif

namespace ESRI.ArcGIS.Client.Toolkit
{

	/// <summary>
	/// Feature DataGrid Control.
	/// </summary>
	[TemplatePart(Name = "MoveFirstButton", Type = typeof(ButtonBase))]
	[TemplatePart(Name = "MovePreviousButton", Type = typeof(ButtonBase))]
	[TemplatePart(Name = "CurrentRecordNumberTextBox", Type = typeof(TextBox))]
	[TemplatePart(Name = "MoveNextButton", Type = typeof(ButtonBase))]
	[TemplatePart(Name = "MoveLastButton", Type = typeof(ButtonBase))]
	[TemplatePart(Name = "NumberOfRecordsTextBlock", Type = typeof(TextBlock))]
	[TemplatePart(Name = "PopupMenu", Type = typeof(Popup))]
	[TemplatePart(Name = "OptionsButton", Type = typeof(ButtonBase))]
	[TemplatePart(Name = "ClearSelectionMenuButton", Type = typeof(ButtonBase))]
	[TemplatePart(Name = "SwitchSelectionMenuButton", Type = typeof(ButtonBase))]
	[TemplatePart(Name = "SelectAllMenuButton", Type = typeof(ButtonBase))]
	[TemplatePart(Name = "ZoomToSelectionMenuButton", Type = typeof(ButtonBase))]
	[TemplatePart(Name = "DeleteSelectedRowsMenuButton", Type = typeof(ButtonBase))]
	[TemplatePart(Name = "SubmitChangesMenuButton", Type = typeof(ButtonBase))]
	[TemplatePart(Name = "AutoChangeMapExtentCheckBox", Type = typeof(ToggleButton))]
	public partial class FeatureDataGrid : DataGrid
	{
		#region Private Properties

		#region Constants
		// The Minimum Extent Ratio for the Map:
		private const double EXPAND_EXTENT_RATIO = .05;
		#endregion
		private string recordsText;						// Text to be shown for the NumberOfRecordsTextBlock

		#region Status Bar Variables
		// Variables for record navigator components:
		private ButtonBase moveFirstButton = null;
		private ButtonBase movePreviousButton = null;
		private TextBox currentRecordNumberTextBox = null;
		private ButtonBase moveNextButton = null;
		private ButtonBase moveLastButton = null;

		// Variable for the text block showing number of selected rows from total number of records:
		private TextBlock numberOfRecordsTextBlock = null;

		// Variables for the items in the Options menu:
		private Popup popupMenu = null;
		private ButtonBase optionsButton = null;
		private ButtonBase clearSelectionMenuButton = null;
		private ButtonBase switchSelectionMenuButton = null;
		private ButtonBase selectAllMenuButton = null;
		private ButtonBase zoomToSelectionMenuButton = null;
		private ButtonBase deleteSelectedRowsMenuButton = null;
		private ButtonBase submitChangesMenuButton = null;
		private ToggleButton autoChangeMapExtentCheckBox = null;
		#endregion


		#endregion Private Properties

		#region Overrides
		/// <summary>
		/// When overridden in a derived class, is invoked whenever application 
		/// code or internal processes (such as a rebuilding layout pass) call 
		/// ApplyTemplate. In simplest terms, this means the method is called 
		/// just before a UI element displays in an application.
		/// </summary>
		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			// get template items
			moveFirstButton = GetTemplateChild("MoveFirstButton") as ButtonBase;
			movePreviousButton = GetTemplateChild("MovePreviousButton") as ButtonBase;
			currentRecordNumberTextBox = GetTemplateChild("CurrentRecordNumberTextBox") as TextBox;
			moveNextButton = GetTemplateChild("MoveNextButton") as ButtonBase;
			moveLastButton = GetTemplateChild("MoveLastButton") as ButtonBase;
			numberOfRecordsTextBlock = GetTemplateChild("NumberOfRecordsTextBlock") as TextBlock;
			optionsButton = GetTemplateChild("OptionsButton") as ButtonBase;
			popupMenu = GetTemplateChild("PopupMenu") as Popup;
			clearSelectionMenuButton = GetTemplateChild("ClearSelectionMenuButton") as ButtonBase;
			switchSelectionMenuButton = GetTemplateChild("SwitchSelectionMenuButton") as ButtonBase;
			selectAllMenuButton = GetTemplateChild("SelectAllMenuButton") as ButtonBase;
			zoomToSelectionMenuButton = GetTemplateChild("ZoomToSelectionMenuButton") as ButtonBase;
			deleteSelectedRowsMenuButton = GetTemplateChild("DeleteSelectedRowsMenuButton") as ButtonBase;
			submitChangesMenuButton = GetTemplateChild("SubmitChangesMenuButton") as ButtonBase;
			autoChangeMapExtentCheckBox = GetTemplateChild("AutoChangeMapExtentCheckBox") as ToggleButton;

#if SILVERLIGHT
			rowsPresenter = GetTemplateChild("RowsPresenter") as DataGridRowsPresenter;
#endif
			// Add event handlers for status bar
			InitializeFeatureDataGrid();
		}

		/// <summary>
		/// Raises the <see cref="E:System.Windows.Controls.DataGrid.AutoGeneratingColumn"/> event.
		/// </summary>
		/// <param name="e">The event data.</param>
		protected override void OnAutoGeneratingColumn(DataGridAutoGeneratingColumnEventArgs e)
		{
			base.AutoGenerateColumns = true;
			base.OnAutoGeneratingColumn(e);
			string mappedKey = GetMappedKey(e);
			if (featureLayer != null && featureLayer.LayerInfo != null)			
			{
				// Don't show object id field.
				if (featureLayer.LayerInfo.ObjectIdField == mappedKey)
				{
					e.Cancel = true;
					return;
				}
				
				// Only create columns for out fields.
				Tasks.OutFields outFields = featureLayer.OutFields;
				if (outFields == null || (!outFields.Contains("*") && !outFields.Contains(mappedKey)))
				{
					e.Cancel = true; return;
				}

				// Find field info for the current column
				Field field = featureLayer.LayerInfo.Fields.FirstOrDefault(x => x.Name == mappedKey);
				if (field != null)
				{
					if (FieldDomainUtils.IsDynamicDomain(field, featureLayer.LayerInfo))
					{
						// dynamic coded value domains
						DynamicCodedValueColumn(e);
					}
					else if (field.Name == featureLayer.LayerInfo.TypeIdField)
					{
						// type id field
						TypeIDColumn(e);
					}
					else if (field.Domain is CodedValueDomain)
					{
						// coded value domains
						CodedValueColumn(e);
					}
					else if (field.Type == Field.FieldType.Date)
					{
						// date time fields
						DateTimeColumn(e);
					}
#if !SILVERLIGHT					
					else
					{
						// all others
						TextColumn(e);
					}
#endif
					// Attached Field info to the column for easy reference
					e.Column.SetValue(FieldColumnProperty, field);
#if SILVERLIGHT
					SetFieldAliasMapping(e.Column, e.PropertyName, field.Alias);
#endif
				}
			}
#if !SILVERLIGHT
			else
			{				
				TextColumn(e); // Not a feature layer
			}
#endif
#if SILVERLIGHT			
			e.Column.Header = objectType.GetDisplayName(e.PropertyName);			
#else
			e.Column.Header = AliasLookup(e.PropertyName);
#endif
			e.Column.CanUserSort = true;
			e.Column.CanUserReorder = e.Column.CanUserReorder;
			e.Column.CanUserResize = e.Column.CanUserResize;
			e.Column.SortMemberPath = e.PropertyName;
		}		

		/// <summary>
		/// Raises the <see cref="E:System.Windows.Controls.DataGrid.SelectionChanged"/> event.
		/// </summary>
		/// <param name="e">The event data.</param>
		protected override void OnSelectionChanged(SelectionChangedEventArgs e)
		{
#if SILVERLIGHT
			// PagedCollectionView and CollectionViewSource automatically select the first row in the collection. This happens 
			// only the first time that we instantiate a new instance of them therefore, we need to remove this unwanted selection:
			if (isCreatingItemsSource)
			{
				isCreatingItemsSource = false;
				// We just need to take care of the first item in SelectedItems such that if its corresponding graphic 
				// has NOT been selected in the associated layer it's an unwanted selection caused by creation of a new 
				// instance of PagedCollectionView or CollectionViewSource, hence it should be removed from SelectedItems:
				if (SelectedItems != null && SelectedItems.Count > 0)
				{
					object item = SelectedItems[0];
					Graphic graphicSibling = DataSourceCreator.GetGraphicSibling(item);
					if (graphicSibling != null && !GraphicsLayer.SelectedGraphics.Contains(graphicSibling))
						SelectedItems.Remove(item);
				}
				ShowNumberOfRecords();
				return;
			}
			base.OnSelectionChanged(e);

			if (ItemsSource == null || (ItemsSource.GetEnumerator() != null && !ItemsSource.GetEnumerator().MoveNext()))
				return;

			if (GraphicsLayer != null && Graphics != null)
			{
				isSelectionChangedFromFeatureDataGrid = true;
				SelectGraphics(e.AddedItems, true);
				SelectGraphics(e.RemovedItems, false);
				isSelectionChangedFromFeatureDataGrid = false;				

				ShowNumberOfRecords();
				SetCurrentRecordNumberTextBox();
			}
#else
			if (_collection != null)
			{				
				foreach (object o in e.AddedItems)
					if (o is Graphic)
						(o as Graphic).Selected = true;

				foreach (object o in e.RemovedItems)
					if (o is Graphic && !_selected.Contains(o as Graphic)) // only remove items that are not being preserved.
						(o as Graphic).Selected = false;

				if (_selected.Count > 0)
					_selected.Clear();									
			}
			base.OnSelectionChanged(e);			
#endif
		}

		/// <summary>
		/// When a DataGridRow scrolls into view this method is invoked. A binding
		/// relationship is established between the DataGridRow.IsSelected property
		/// and the Graphic.Selected property.
		/// </summary>		
		protected override void OnLoadingRow(DataGridRowEventArgs e)
		{
#if !SILVERLIGHT
			Graphic graphic = e.Row.DataContext as Graphic;
			if (graphic != null)
				e.Row.IsSelected = graphic.Selected;
#endif
			e.Row.MouseLeftButtonUp += new MouseButtonEventHandler(DataGridRow_MouseLeftButtonUp);
			base.OnLoadingRow(e);
		}

		/// <summary>
		/// Unloads the row when it scrolls out of view. 
		/// </summary>
		/// <param name="e">The data for the event.</param>
		protected override void OnUnloadingRow(DataGridRowEventArgs e)
		{
			e.Row.MouseLeftButtonDown -= new MouseButtonEventHandler(DataGridRow_MouseLeftButtonUp);
			base.OnUnloadingRow(e);
		}

		/// <summary>
		/// When a DataGridRow is clicked and AutoZoomToSelection is enabled a 
		/// zoom to the selected graphics will be performed.
		/// </summary>		
		private void DataGridRow_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (autoChangeMapExtentCheckBox != null && autoChangeMapExtentCheckBox.IsChecked.Value)
				ZoomToSelection();
		}

		/// <summary>
		/// Checks for FeatureLayer restrictions and cancels the edit if FeatureLayer, 
		/// EditorTracking or Field indicate that and edit is not allowed.
		/// </summary>		
		/// <param name="e">The event data.</param>
		protected override void OnBeginningEdit(DataGridBeginningEditEventArgs e)
		{
#if SILVERLIGHT
			bool isFeatureReadOnly;
			if (featureLayer != null)
			{
				isFeatureReadOnly = featureLayer.IsReadOnly;				
			}
			else
				isFeatureReadOnly = false;

			if (!isFeatureReadOnly)
			{
				// Verify whether cell's associated attribute is editable:
				Field associatedField = e.Column.GetValue(FieldColumnProperty) as Field;
				object rowDataContext = e.Row.DataContext;
				if (rowDataContext != null && associatedField != null)
				{
					System.Reflection.PropertyInfo propertyInfo = rowDataContext.GetType().GetProperty(associatedField.Name);
					if (propertyInfo != null)
					{
						object[] customAttributes = propertyInfo.GetCustomAttributes(typeof(EditableAttribute), true);
						if (customAttributes != null && customAttributes.Length > 0)
						{
							EditableAttribute editableAttribute = customAttributes[0] as EditableAttribute;
							if (editableAttribute != null && !editableAttribute.AllowEdit)
							{
								e.Cancel = true;    // Cell (attribute) is readonly
								return;
							}
						}
					}
				}

				// The attribute is editable:
				base.OnBeginningEdit(e);
			}
			else
				e.Cancel = true;
#else
			if (featureLayer != null)
			{
				// Check layer to see if it is read-only.
				e.Cancel = featureLayer.IsReadOnly;
				if (e.Cancel)
					return;				

				// Check (Attribute/Column) to see if it is read-only.
				Field field = e.Column.GetValue(FieldColumnProperty) as Field;
				e.Cancel = !field.Editable;
				if (e.Cancel)
					return;
			}
			base.OnBeginningEdit(e);
#endif
		}
		#endregion

		#region Dependency Properties
		/// <summary>
		/// Gets or sets the map containing the <see cref="GraphicsLayer"/>.
		/// </summary>
		/// <remarks>Used for adjusting the extent when requested by user.</remarks>
		/// <value>The map.</value>
		public Map Map
		{
			get { return (Map)GetValue(MapProperty); }
			set { SetValue(MapProperty, value); }
		}
		/// <summary>
		/// Identifies the <see cref="Map"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty MapProperty =
			DependencyProperty.Register("Map", typeof(Map), typeof(FeatureDataGrid), null);

		/// <summary>
		/// The graphics layer bound to the <see cref="FeatureDataGrid"/>.
		/// </summary>
		public GraphicsLayer GraphicsLayer
		{
			get { return (GraphicsLayer)GetValue(GraphicsLayerProperty); }
			set { SetValue(GraphicsLayerProperty, value); }
		}
		/// <summary>
		/// Identifies the <see cref="GraphicsLayer"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty GraphicsLayerProperty =
			DependencyProperty.Register("GraphicsLayer", typeof(GraphicsLayer), typeof(FeatureDataGrid), new PropertyMetadata(null, OnGraphicsLayerPropertyChanged));


		/// <summary>
		/// Collection of graphics in the associated layer that have been filtered by a spatial/attribute query. 
		/// </summary>
		/// <value>The filter source.</value>
		/// <remarks>Is set then FeatureDataGrid will be populated by this collection.</remarks>
		public IEnumerable<Graphic> FilterSource
		{
			get { return (IEnumerable<Graphic>)GetValue(FilterSourceProperty); }
			set { SetValue(FilterSourceProperty, value); }
		}
		/// <summary>
		/// Identifies the <see cref="FilterSource"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty FilterSourceProperty =
			DependencyProperty.Register("FilterSource", typeof(IEnumerable<Graphic>), typeof(FeatureDataGrid), new PropertyMetadata(OnFilterSourcePropertyChanged));

		#endregion

		#region Attached Properties
		/// <summary>
		/// Used to Attached FeatureLayer.LayerInfo.Fields information to a DataGridColumn for easy reference 
		/// useful to validate column and cell data such as (DataType, ReadOnly, FieldName, Nullable)
		/// </summary>
		private static readonly DependencyProperty FieldColumnProperty =
			DependencyProperty.RegisterAttached("FieldColumn", typeof(Field), typeof(DataGridColumn), null);
		#endregion Attached Properties

		#region Properties
		/// <summary>
		/// Gets the selected graphics.
		/// </summary>
		/// <value>The selected graphics.</value>
		public IList<Graphic> SelectedGraphics
		{
			get
			{
				if (GraphicsLayer == null)
					return null;
				if (FilterSource == null)
					return GraphicsLayer.SelectedGraphics.ToList();
				return GraphicsLayer.SelectedGraphics.Intersect(FilterSource).ToList();
			}
		}
		#endregion

		#region Methods
  /// <summary>
  /// <para>
  /// <b>Note: This API is now obsolete.</b> This method call is no longer necessary to refresh a row.
  /// </para>
  /// <para>
  /// Updates a data row corresponds to the given graphic object. 
  /// </para>
  /// </summary>
		/// <param name="graphic">The graphic.</param>
#if !SILVERLIGHT
		[Obsolete("This method call is no longer necessary to refresh a row.")]
		[EditorBrowsable(EditorBrowsableState.Never)]
#endif
		public void RefreshRow(Graphic graphic)
		{
#if SILVERLIGHT
			int idx = GetGraphicIndexInGraphicsCollection(graphic);
			IList gridRows = ItemsSource.AsList();
			if (idx > -1 && idx < gridRows.Count)
			{
				try
				{
					// In Silverlight refreshing a row corresponding to a graphic causes to lose current selection.
					// Preserving index of currently selected items in the FeatureDataGrid:
					int selCount = SelectedItems.Count;
					int[] selIndexes = new int[selCount];
					for (int i = 0; i < selCount; i++)
						selIndexes[i] = GetRowIndexInRowsCollection(SelectedItems[i]);
					// Unsubscribe from PagedCollectionView CollectionChanged event to perform a manual source 
					// collection update:
					(ItemsSource as PagedCollectionView).CollectionChanged -= PagedCollectionView_CollectionChanged;
					graphic.RefreshRow((ItemsSource as ICollectionView).SourceCollection, idx, objectType);
					gridRows = ItemsSource.AsList();	// Refresh needed as a row in the ItemsSource has changed
					// Subscribing back to the PagedCollectionView CollectionChanged event handler:
					(ItemsSource as PagedCollectionView).CollectionChanged += PagedCollectionView_CollectionChanged;
					// Restoring the selection stored before updating the row (Silverlight only):
					SelectedItems.Clear();
					for (int i = 0; i < selCount; i++)
						SelectedItems.Add(gridRows[selIndexes[i]]);
				}
				catch (Exception ex)
				{
					throw new ArgumentException(string.Format(Properties.Resources.FeatureDataGrid_RowUpdateFailed, idx.ToString()), ex);
				}
			}
#endif
		}

		/// <summary>
		/// Scrolls the <see cref="FeatureDataGrid"/> vertically to display the row
		/// for the specified <see cref="Graphic"/> and scrolls the <see cref="FeatureDataGrid"/>
		/// horizontally to display the specified column.
		/// </summary>
		/// <param name="graphic">The graphic.</param>
		/// <param name="column">The column.</param>
		public void ScrollIntoView(Graphic graphic, DataGridColumn column)
		{
#if SILVERLIGHT
			int idx = GetGraphicIndexInGraphicsCollection(graphic);
			if (idx > -1)
			{
				if (GraphicsLayer != null && Graphics != null && Graphics.Contains(graphic))
					ScrollIntoView((ItemsSource as ICollectionView).SourceCollection.AsList()[idx], column);
			}
#else
			int idx = (_collection != null) ? _collection.IndexOf(graphic) : -1;
			if (idx > -1)
				base.ScrollIntoView(graphic, column);
#endif
		}
		#endregion

		#region Private Methods
		/// <summary>
		/// Initializes events for the <see cref="FeatureDataGrid"/> and all the identified 
		/// variables in its control template.
		/// </summary>
		private void InitializeFeatureDataGrid()
		{
			// Selected row highlights entire row
			SelectionMode = DataGridSelectionMode.Extended;
			// Remove existing handlers
			RemoveStatusBarEventHandlers();
			// Add new handlers			
			AddStatusBarEventsHandlers();
			// Configure selected graphics text and total graphics text
			if (numberOfRecordsTextBlock != null)
			{
#if !SILVERLIGHT
				BindToTotalGraphicsCount();		// Adds binding to GraphicsLayer.Graphics.Count
				BindToSelectedGraphicsCount();	// Adds binding to GraphicsLayer.SelectedGraphics.Count
#endif
				if (numberOfRecordsTextBlock.Text == "Records ({0} out of {1} Selected)") //Equivalent to default template string - replace with resource string
					recordsText = Properties.Resources.FeatureDataGrid_SelectedAndTotalRows;
				else //Preserve string in overridden template
					recordsText = numberOfRecordsTextBlock.Text;
				var graphic = SelectedGraphics == null ? null : SelectedGraphics.FirstOrDefault();
#if SILVERLIGHT
				SetNumberOfRecordsTextBlock(0, 0);
				if (graphic != null)
				{
					currentRecordNumber = GetRowIndexInItemsSource(graphic);
					ValidateCurrentRecordNumber();
					SetCurrentRecordNumberTextBox();
					SelectCurrentRecord();
				}
#else
				UpdateRecordsText(); // Update the status bar text
				if (graphic != null)
				{					
					var index = _collection == null ? -1 : _collection.IndexOf(graphic);					
					MoveRecord(index);
					UpdateCurrentRecordTextBox();
				}				
#endif
			}
			SetSubmitButtonVisibility();
			SetSubmitButtonEnableState();
			SetDeleteSelectedRowsMenuButtonEnableState();
		}
#if !SILVERLIGHT
		/// <summary>
		/// Creates a DataGridColumn that uses a TexBlock for Display and Editing.
		/// </summary>
		private void TextColumn(DataGridAutoGeneratingColumnEventArgs e)
		{
			string maxLength = "";
			bool Nullable = true;
			string FieldType = "{x:Null}";
			string RangeDomainValidationXAML = string.Empty;
			if (featureLayer != null)
			{
				Field field = featureLayer.LayerInfo.Fields.FirstOrDefault(x => x.Name == GetMappedKey(e));
				maxLength = field.Length > 0 ? "MaxLength=\"" + field.Length.ToString() + "\"" : "";
				Nullable = field.Nullable;
				FieldType = field.Type.ToString();
				RangeDomainValidationXAML = GetRangeDomainValidation(field);
			}
			DataGridTemplateColumn templateColumn = new DataGridTemplateColumn();
			// Creating CellTemplate:
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			string propName = GetBindingPropertyName(e.PropertyName);
			sb.Append("<DataTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" ");
			sb.Append("xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">");
			sb.AppendFormat("<TextBlock Text=\"{{Binding {0}}}\" VerticalAlignment=\"Center\" Margin=\"3\" />", propName);
			sb.Append("</DataTemplate>");

			System.IO.MemoryStream templateStream = new System.IO.MemoryStream(System.Text.UTF8Encoding.Default.GetBytes(sb.ToString()));
			templateColumn.CellTemplate = System.Windows.Markup.XamlReader.Load(templateStream) as DataTemplate;

			// Creating CellEditingTemplate:
			sb = new System.Text.StringBuilder();
			sb.Append("<DataTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" ");
			sb.Append("xmlns:local=\"clr-namespace:ESRI.ArcGIS.Client.Toolkit;assembly=ESRI.ArcGIS.Client.Toolkit\" ");
			sb.Append("xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">");
			sb.Append("<Grid>");
			sb.Append("<Grid.Resources>");
			sb.AppendFormat("<local:StringToPrimitiveTypeConverter x:Key=\"converter\" FieldType=\"{0}\" />", FieldType);
			sb.Append("</Grid.Resources>");			
			sb.AppendFormat("<TextBox {0} >",maxLength);
			sb.Append("<TextBox.Text>");
			sb.AppendFormat("<Binding Path=\"{0}\" Mode=\"TwoWay\" Converter=\"{{StaticResource converter}}\" >", propName);
			sb.Append("<Binding.ValidationRules>");
			sb.AppendFormat("<local:FeatureValidationRule ValidationStep=\"ConvertedProposedValue\" FieldType=\"{0}\" Nullable=\"{1}\" />", FieldType, Nullable);
			if (!string.IsNullOrEmpty(RangeDomainValidationXAML))
				sb.Append(RangeDomainValidationXAML);
			sb.Append("</Binding.ValidationRules>");
			sb.Append("</Binding>");
			sb.Append("</TextBox.Text>");
			sb.Append("</TextBox>");
			sb.Append("</Grid>");
			sb.Append("</DataTemplate>");

			templateStream = new System.IO.MemoryStream(System.Text.UTF8Encoding.Default.GetBytes(sb.ToString()));
			templateColumn.CellEditingTemplate = System.Windows.Markup.XamlReader.Load(templateStream) as DataTemplate;

			e.Column = templateColumn;
			return;
		}
#endif
		/// <summary>
		/// Creates a DataGridColumn that works with TypeID Field of a FeatureLayer.
		/// </summary>
		private void TypeIDColumn(DataGridAutoGeneratingColumnEventArgs e)
		{
			Field field = featureLayer.LayerInfo.Fields.FirstOrDefault(x => x.Name == GetMappedKey(e));
			CodedValueDomainColumn column = new CodedValueDomainColumn();
			column.CodedValueSources = FieldDomainUtils.BuildTypeIDCodedValueSource(field, featureLayer.LayerInfo);
			column.Field = e.PropertyName;
			e.Column = column;
		}
		/// <summary>
		/// Creates a DataGridColumn that works with CodedValue Domain types of a FeatureLayer.
		/// </summary>
		private void CodedValueColumn(DataGridAutoGeneratingColumnEventArgs e)
		{
			Field field = featureLayer.LayerInfo.Fields.FirstOrDefault(x => x.Name == GetMappedKey(e));
			CodedValueDomainColumn column = new CodedValueDomainColumn();
			column.CodedValueSources = FieldDomainUtils.BuildCodedValueSource(field);
			column.Field = e.PropertyName;
			e.Column = column;
		}
		/// <summary>
		/// Creates a DataGridColumn that works with Dynamic Coded Value Domains 
		/// (aka Sub Domains) types of a FeatureLayer.
		/// </summary>		
		private void DynamicCodedValueColumn(DataGridAutoGeneratingColumnEventArgs e)
		{
			Field field = featureLayer.LayerInfo.Fields.FirstOrDefault(x => x.Name == GetMappedKey(e));
			DynamicCodedValueSource dynamicCodedValueSource = FieldDomainUtils.BuildDynamicCodedValueSource(field, featureLayer.LayerInfo);
			DynamicCodedValueDomainColumn column = new DynamicCodedValueDomainColumn();
			column.DynamicCodedValueSource = dynamicCodedValueSource;
			column.LookupField = featureLayer.LayerInfo.TypeIdField;
			column.FieldInfo = field;
			column.Field = e.PropertyName;
			e.Column = column;
		}
		/// <summary>
		/// Creates a DataGridColumn that works with DataTime
		/// </summary>		
		private void DateTimeColumn(DataGridAutoGeneratingColumnEventArgs e)
		{
			string DatePickerStart = GetDatePickerStart();
			string DatePickerEnd = GetDatePickerEnd();

#if !SILVERLIGHT
			string RangeDomainValidationXAML = string.Empty;
			if (featureLayer != null)
			{
				Field field = featureLayer.LayerInfo.Fields.FirstOrDefault(x => x.Name == GetMappedKey(e));				
				RangeDomainValidationXAML = GetRangeDomainValidation(field);
			}
#endif

			DataGridTemplateColumn templateColumn = new DataGridTemplateColumn();
			// Creating CellTemplate:
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			sb.Append("<DataTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" ");
			sb.Append("xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" ");
			sb.Append("xmlns:local=\"clr-namespace:ESRI.ArcGIS.Client.Toolkit.Utilities;assembly=ESRI.ArcGIS.Client.Toolkit\">");
			sb.Append("<Grid>");
			sb.Append("<Grid.Resources>");
			sb.Append("<local:DateTimeConverter x:Key=\"dateTimeConverter\" />");
			sb.Append("</Grid.Resources>");
			string propName = GetBindingPropertyName(e.PropertyName);
			sb.AppendFormat("<TextBlock Text=\"{{Binding {0}, Converter={{StaticResource dateTimeConverter}}}}\" VerticalAlignment=\"Center\" Margin=\"3\" />", propName);
			sb.Append("</Grid>");
			sb.Append("</DataTemplate>");

#if SILVERLIGHT
			templateColumn.CellTemplate = (DataTemplate)System.Windows.Markup.XamlReader.Load(sb.ToString());
#else
			System.IO.MemoryStream templateStream = new System.IO.MemoryStream(System.Text.UTF8Encoding.Default.GetBytes(sb.ToString()));
			templateColumn.CellTemplate = System.Windows.Markup.XamlReader.Load(templateStream) as DataTemplate;
#endif
			// Creating CellEditingTemplate:
			sb = new System.Text.StringBuilder();
			sb.Append("<DataTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" ");
			sb.Append("xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" ");
			sb.Append("xmlns:local=\"clr-namespace:ESRI.ArcGIS.Client.Toolkit;assembly=ESRI.ArcGIS.Client.Toolkit\" ");
#if SILVERLIGHT
			sb.Append("xmlns:datePicker=\"clr-namespace:System.Windows.Controls;assembly=System.Windows.Controls\" >");
#else
#if NET35
			sb.Append("xmlns:datePicker=\"http://schemas.microsoft.com/wpf/2008/toolkit\" >");
#else
			sb.Append(">");
#endif
#endif
			//sb.AppendFormat("DatePicker SelectedDate=\"{{Binding {0}, Mode=TwoWay}}\" IsDropDownOpen=\"False\" />", propName);			
			sb.AppendFormat("{0}DatePicker IsDropDownOpen=\"False\" >",DatePickerStart);
			sb.AppendFormat("{0}DatePicker.SelectedDate>", DatePickerStart);
			sb.AppendFormat("<Binding Path=\"{0}\" Mode=\"TwoWay\" >", propName);
#if !SILVERLIGHT
			sb.Append("<Binding.ValidationRules>");			
			if (!string.IsNullOrEmpty(RangeDomainValidationXAML))
				sb.Append(RangeDomainValidationXAML);
			sb.Append("</Binding.ValidationRules>");
#endif
			sb.Append("</Binding>");
			sb.AppendFormat("{0}DatePicker.SelectedDate>", DatePickerEnd);
			sb.AppendFormat("{0}DatePicker>", DatePickerEnd);
			sb.Append("</DataTemplate>");

#if SILVERLIGHT
			templateColumn.CellEditingTemplate = (DataTemplate)System.Windows.Markup.XamlReader.Load(sb.ToString());
#else
			templateStream = new System.IO.MemoryStream(System.Text.UTF8Encoding.Default.GetBytes(sb.ToString()));
			templateColumn.CellEditingTemplate = System.Windows.Markup.XamlReader.Load(templateStream) as DataTemplate;
#endif

			e.Column = templateColumn;
		}

		/// <summary>
		/// Get the starting XAML for a DatePicker control
		/// </summary>
		private string GetDatePickerStart()
		{

#if  NET35 || SILVERLIGHT
			return "<datePicker:";
#else
			return "<";
#endif
		}
		/// <summary>
		/// Gets the ending XAML for DataPicker control.
		/// </summary>		
		private string GetDatePickerEnd()
		{
#if  NET35 || SILVERLIGHT
			return "</datePicker:";
#else
			return "</";
#endif
		}

		/// <summary>
		/// Create a binding string used for template columns.
		/// </summary>				
		private static string GetBindingPropertyName(string PropertyName)
		{
			return string.Format(
#if SILVERLIGHT
				"{0}"				
#else
				"Attributes[{0}]"
#endif
				, PropertyName);
		}

		/// <summary>
		/// Used to get the property name 
		/// </summary>		
		private string GetMappedKey(DataGridAutoGeneratingColumnEventArgs e)
		{
#if SILVERLIGHT
			return e.PropertyName.MappedKey(); // reflection safe property name.
#else
			return e.PropertyName;
#endif
		}

		/// <summary>
		/// Adds event handlers to status bar controls.
		/// </summary>
		private void AddStatusBarEventsHandlers()
		{
			// Move to First Record
			if (moveFirstButton != null)
				moveFirstButton.Click += MoveFirstButton_Click;
			// Move to Previous Record
			if (movePreviousButton != null)
				movePreviousButton.Click += MovePreviousButton_Click;
			// Current Record Text Box
			if (currentRecordNumberTextBox != null)
				currentRecordNumberTextBox.KeyDown += CurrentRecordNumberTextBox_KeyDown;
			// Move to Next Record
			if (moveNextButton != null)
				moveNextButton.Click += MoveNextButton_Click;
			// Move to Last Record
			if (moveLastButton != null)
				moveLastButton.Click += MoveLastButton_Click;
			// Options Popup button
			if (optionsButton != null)
				optionsButton.Click += OptionsButton_Click;
			// Popup context 
			if (popupMenu != null && popupMenu.Child != null)
				popupMenu.Child.MouseLeave += PopupChild_MouseLeave;
			// Clear Record Selection
			if (clearSelectionMenuButton != null)
				clearSelectionMenuButton.Click += ClearSelectionMenuButton_Click;
			// Invert Record Selection
			if (switchSelectionMenuButton != null)
				switchSelectionMenuButton.Click += SwitchSelectionMenuButton_Click;
			// Select All Records
			if (selectAllMenuButton != null)
				selectAllMenuButton.Click += SelectAllMenuButton_Click;
			// Zoom to Record on Map
			if (zoomToSelectionMenuButton != null)
				zoomToSelectionMenuButton.Click += ZoomToSelectionMenuButton_Click;
			// Delete Selected Records
			if (deleteSelectedRowsMenuButton != null)
				deleteSelectedRowsMenuButton.Click += DeleteSelectedRowsMenuButton_Click;
			// Submit Changes back to the Feature Server
			if (submitChangesMenuButton != null)
				submitChangesMenuButton.Click += SubmitChangesMenuButton_Click;
		}

		/// <summary>
		/// Removes event handlers from status bar controls.
		/// </summary> 
		private void RemoveStatusBarEventHandlers()
		{
			// Move to First Record
			if (moveFirstButton != null)
				moveFirstButton.Click -= MoveFirstButton_Click;
			// Move to Previous Record
			if (movePreviousButton != null)
				movePreviousButton.Click -= MovePreviousButton_Click;
			// Current Record Text Box
			if (currentRecordNumberTextBox != null)
				currentRecordNumberTextBox.KeyDown -= CurrentRecordNumberTextBox_KeyDown;
			// Move to Next Record
			if (moveNextButton != null)
				moveNextButton.Click -= MoveNextButton_Click;
			// Move to Last Record
			if (moveLastButton != null)
				moveLastButton.Click -= MoveLastButton_Click;
			// Options Popup button
			if (optionsButton != null)
				optionsButton.Click -= OptionsButton_Click;
			// Popup context 
			if (popupMenu != null && popupMenu.Child != null)
				popupMenu.Child.MouseLeave -= PopupChild_MouseLeave;
			// Clear Record Selection
			if (clearSelectionMenuButton != null)
				clearSelectionMenuButton.Click -= ClearSelectionMenuButton_Click;
			// Invert Record Selection
			if (switchSelectionMenuButton != null)
				switchSelectionMenuButton.Click -= SwitchSelectionMenuButton_Click;
			// Select All Records
			if (selectAllMenuButton != null)
				selectAllMenuButton.Click -= SelectAllMenuButton_Click;
			// Zoom to Record on Map
			if (zoomToSelectionMenuButton != null)
				zoomToSelectionMenuButton.Click -= ZoomToSelectionMenuButton_Click;
			// Delete Selected Records
			if (deleteSelectedRowsMenuButton != null)
				deleteSelectedRowsMenuButton.Click -= DeleteSelectedRowsMenuButton_Click;
			// Submit Changes back to the Feature Server
			if (submitChangesMenuButton != null)
				submitChangesMenuButton.Click -= SubmitChangesMenuButton_Click;
		}
		/// <summary>
		/// Moves to the first.
		/// </summary>
		private void MoveFirst()
		{
#if SILVERLIGHT
			currentRecordNumber = 0;
			SelectCurrentRecord();
#else			
			MoveRecord(0);
#endif
		}
		/// <summary>
		/// Moves to the previous.
		/// </summary>
		private void MovePrevious()
		{
#if SILVERLIGHT
			int idx = GetRowIndexInRowsCollection(CurrentItem);
			if (idx > 0)
				currentRecordNumber = idx - 1;
			SelectCurrentRecord();
#else
			if (_collection != null)
				MoveRecord(_collection.CurrentPosition - 1);
#endif
		}
		/// <summary>
		/// Moves to the next.
		/// </summary>
		private void MoveNext()
		{
#if SILVERLIGHT
			int idx = GetRowIndexInRowsCollection(CurrentItem);
			if (idx < recordsCount - 1)
			{
				if (idx == 0 && SelectedItem != CurrentItem)
					currentRecordNumber = 0;
				else
					currentRecordNumber = idx + 1;
			}
			SelectCurrentRecord();
#else
			if (_collection != null)
				MoveRecord(_collection.CurrentPosition + 1);
#endif
		}
		/// <summary>
		/// Moves to the last.
		/// </summary>
		private void MoveLast()
		{
#if SILVERLIGHT
			currentRecordNumber = recordsCount - 1;
			SelectCurrentRecord();
#else
			if (_collection != null)
				MoveRecord(_collection.Count - 1);
#endif
		}
		/// <summary>
		/// Clears selection.
		/// </summary>
		private void ClearSelection()
		{
#if SILVERLIGHT
			if (ItemsSource == null || SelectedGraphicCount == 0)
				return;

			if (SelectedItems != null)
				SelectedItems.Clear();
#else
			if (FilterSource != null && FilterSource.Count() > 0)
				FilterSource.ForEach(x => x.UnSelect());
			else
			{
				foreach (Graphic g in _collection)
					g.UnSelect();
			}
#endif
		}
		/// <summary>
		/// Calculates the MBR containing all selected graphics and changes the map extent 
		/// to the calculated extent.
		/// </summary>
		private void ZoomToSelection()
		{
			if (SelectedGraphicCount == 0)
				return;

			if (Map != null && SelectedGraphics != null)
			{
				Envelope newMapExtent = null;
				foreach (Graphic graphic in SelectedGraphics)
				{
					if (graphic.Geometry != null && graphic.Geometry.Extent != null)
						newMapExtent = graphic.Geometry.Extent.Union(newMapExtent);
				}
				if (newMapExtent != null)
				{
					if (newMapExtent.Width > 0 || newMapExtent.Height > 0)
					{
						newMapExtent = new Envelope(newMapExtent.XMin - newMapExtent.Width * EXPAND_EXTENT_RATIO, newMapExtent.YMin - newMapExtent.Height * EXPAND_EXTENT_RATIO,
							newMapExtent.XMax + newMapExtent.Width * EXPAND_EXTENT_RATIO, newMapExtent.YMax + newMapExtent.Height * EXPAND_EXTENT_RATIO);
						Map.ZoomTo(newMapExtent);
					}
					else
						Map.PanTo(newMapExtent);
				}
			}
		}
		/// <summary>
		/// Switches the current selection.
		/// </summary>
		private void SwitchSelection()
		{
#if SILVERLIGHT
			if (ItemsSource != null)
			{
				if (recordsCount == SelectedGraphicCount)
					ClearSelection();
				else if (SelectedGraphicCount == 0)
					SelectAll();
				else
				{
					foreach (object gridRow in ItemsSource)
					{
						if (SelectedItems.Contains(gridRow))
							SelectedItems.Remove(gridRow);
						else
							SelectedItems.Add(gridRow);
					}
				}
			}
#else
			foreach (Graphic g in _collection)
			{
				if (SelectedItems.Contains(g))
					SelectedItems.Remove(g);
				else
					SelectedItems.Add(g);
			}
#endif
		}
		/// <summary>
		/// Deletes selected <see cref="FeatureDataGrid"/> row(s) and removes 
		/// related graphic(s) from <see cref="FeatureDataGrid"/>'s graphics layer.
		/// </summary>
		private void DeleteSelectedRows()
		{
#if SILVERLIGHT
			List<object> currentSelectedItems = new List<object>();
			currentSelectedItems.AddRange(SelectedItems.OfType<object>());
			while (currentSelectedItems.Count > 0)
			{
				Graphic graphic = DataSourceCreator.GetGraphicSibling(currentSelectedItems[0]);
				if (graphic != null && GraphicsLayer != null && GraphicsLayer.Graphics != null)
				{					
					int idxToRemove = GraphicsLayer.Graphics.IndexOf(graphic);
					if (idxToRemove > -1)
						GraphicsLayer.Graphics.RemoveAt(idxToRemove);					
				}
				currentSelectedItems.RemoveAt(0);
			}
			ShowNumberOfRecords();
			SelectCurrentRecord();
			SetCurrentRecordNumberTextBox();
#else
			if (_collection != null && GraphicsLayer != null && GraphicsLayer.Graphics != null)
			{
				List<Graphic> graphics = new List<Graphic>();
				foreach (Graphic g in _collection)
				{
					if (g.Selected && !graphics.Contains(g))
						graphics.Add(g);
				}
				foreach (Graphic g in graphics)
				{
					if (GraphicsLayer.Graphics.Contains(g))
					{						
						GraphicsLayer.Graphics.Remove(g);
					}
				}
			}
#endif
		}
		/// <summary>
		/// Submits changes back to the feature layer.
		/// </summary>
		private void SubmitChanges()
		{
			this.CommitEdit(DataGridEditingUnit.Row, true);
			if (featureLayer != null)
				featureLayer.SaveEdits();
		}
		/// <summary>
		/// Sets the state of the submit button enable state.
		/// </summary>
		private void SetSubmitButtonEnableState()
		{
			if (submitChangesMenuButton != null)
			{
				if (featureLayer != null)
				{
					submitChangesMenuButton.Visibility = System.Windows.Visibility.Visible;
					if (featureLayer.HasEdits)
						submitChangesMenuButton.IsEnabled = true;
					else
						submitChangesMenuButton.IsEnabled = false;
				}
				else
					submitChangesMenuButton.IsEnabled = false;
			}
		}
		/// <summary>
		/// Sets the submit button visibility.
		/// </summary>
		private void SetSubmitButtonVisibility()
		{
			if (submitChangesMenuButton != null)
			{
				if (featureLayer != null)
					submitChangesMenuButton.Visibility = Visibility.Visible;
				else
					submitChangesMenuButton.Visibility = Visibility.Collapsed;
			}
		}
		/// <summary>
		/// Determines the default enabled state for Deleting Rows.
		/// </summary>
		private void SetDeleteSelectedRowsMenuButtonEnableState()
		{
			if (deleteSelectedRowsMenuButton != null)
			{
				bool CanDelete = false;
#if !SILVERLIGHT
				// if data grid allows delete then continue
				CanDelete = CanUserDeleteRows;
				if (CanDelete)
				{
#endif
					// feature layer may have delete restrictions
					if (featureLayer != null)
						CanDelete = !featureLayer.IsReadOnly;
					else if (GraphicsLayer != null && GraphicsLayer.Graphics != null)
						CanDelete = true;
					else
						CanDelete = false;
#if !SILVERLIGHT
				}
#endif
				deleteSelectedRowsMenuButton.IsEnabled = CanDelete;
			}
		}
		#endregion PrivateMethods

		#region Navigation
		/// <summary>
		/// Handles the Click event of the MoveFirstButton control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
		private void MoveFirstButton_Click(object sender, RoutedEventArgs e)
		{
			MoveFirst();
		}
		/// <summary>
		/// Handles the Click event of the MovePreviousButton control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
		private void MovePreviousButton_Click(object sender, RoutedEventArgs e)
		{
			MovePrevious();
		}
		/// <summary>
		/// Handles the Click event of the MoveNextButton control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
		private void MoveNextButton_Click(object sender, RoutedEventArgs e)
		{
			MoveNext();
		}
		/// <summary>
		/// Handles the Click event of the MoveLastButton control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
		private void MoveLastButton_Click(object sender, RoutedEventArgs e)
		{
			MoveLast();
		}
		/// <summary>
		/// Finds and selects the grid row in the index specified in the textbox.
		/// </summary>		
		private void CurrentRecordNumberTextBox_KeyDown(object sender, KeyEventArgs e)
		{
#if SILVERLIGHT
			TextBox textBox = sender as TextBox;
			if (textBox != null)
			{
				if (e.Key == Key.Enter)
				{
					int convertedNo = int.Parse(textBox.Text);
					currentRecordNumber = convertedNo - 1;
					ValidateCurrentRecordNumber();
					SetCurrentRecordNumberTextBox();

					SelectCurrentRecord();
				}
				else if ((e.PlatformKeyCode >= 48 && e.PlatformKeyCode <= 57) ||
						 (e.PlatformKeyCode >= 96 && e.PlatformKeyCode <= 105))
				{
					if (textBox.SelectedText.Length > 0)
						textBox.Text = textBox.Text.Remove(textBox.SelectionStart, textBox.SelectedText.Length);
					textBox.Text += e.Key.ToString().Substring(e.Key.ToString().Length - 1);
					textBox.SelectionStart = textBox.Text.Length;
				}
				e.Handled = true;
			}
#else
			// if enter key is pressed move to the location in the text box
			if (e.Key == Key.Enter)
			{
				int position = int.Parse(currentRecordNumberTextBox.Text) - 1;
				MoveRecord(position);
				e.Handled = true;
			}
			else if (e.Key != Key.D0 && e.Key != Key.D1 && e.Key != Key.D2 && e.Key != Key.D3 &&
					e.Key != Key.D4 && e.Key != Key.D5 && e.Key != Key.D6 && e.Key != Key.D7 &&
					e.Key != Key.D8 && e.Key != Key.D9 && e.Key != Key.NumPad0 &&
					e.Key != Key.NumPad1 && e.Key != Key.NumPad2 && e.Key != Key.NumPad3 &&
					e.Key != Key.NumPad4 && e.Key != Key.NumPad5 && e.Key != Key.NumPad6 &&
					e.Key != Key.NumPad7 && e.Key != Key.NumPad8 && e.Key != Key.NumPad9)
			{
				// only allow numeric entries to pass to the textbox
				e.Handled = true;
			}
#endif
		}
		#endregion Navigation

		#region Options Menu
		/// <summary>
		/// Handles the Click event of the OptionsButton control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
		private void OptionsButton_Click(object sender, RoutedEventArgs e)
		{
			if (popupMenu != null && popupMenu.Child is FrameworkElement)
			{
				double opacity = popupMenu.Opacity;
				popupMenu.Opacity = 0;
				popupMenu.IsOpen = true;
				Dispatcher.BeginInvoke((Action)delegate()
				{
					popupMenu.VerticalOffset = -(popupMenu.Child as FrameworkElement).ActualHeight
#if SILVERLIGHT                    
                    + optionsButton.ActualHeight
#endif
;
					popupMenu.Opacity = opacity;
				});
			}
		}

		/// <summary>
		/// Handles the MouseLeave event of the PopupChild control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.Input.MouseEventArgs"/> instance containing the event data.</param>
		private void PopupChild_MouseLeave(object sender, MouseEventArgs e)
		{
			popupMenu.IsOpen = false;
		}

		/// <summary>
		/// Handles the Click event of the ClearSelectionMenuButton control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
		private void ClearSelectionMenuButton_Click(object sender, RoutedEventArgs e)
		{
			ClearSelection();
			if (popupMenu != null)
				popupMenu.IsOpen = false;
		}

		/// <summary>
		/// Handles the Click event of the SwitchSelectionMenuButton control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
		private void SwitchSelectionMenuButton_Click(object sender, RoutedEventArgs e)
		{
			SwitchSelection();
			if (popupMenu != null)
				popupMenu.IsOpen = false;
		}

		/// <summary>
		/// Handles the Click event of the SelectAllMenuButton control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
		private void SelectAllMenuButton_Click(object sender, RoutedEventArgs e)
		{
			this.SelectAll();
			if (popupMenu != null)
				popupMenu.IsOpen = false;
		}

		/// <summary>
		/// Handles the Click event of the ZoomToSelectionMenuButton control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
		private void ZoomToSelectionMenuButton_Click(object sender, RoutedEventArgs e)
		{
			ZoomToSelection();
			if (popupMenu != null)
				popupMenu.IsOpen = false;
		}

		/// <summary>
		/// Handles the Click event of the DeleteSelectedRowsMenuButton control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
		private void DeleteSelectedRowsMenuButton_Click(object sender, RoutedEventArgs e)
		{
			DeleteSelectedRows();
			if (popupMenu != null)
				popupMenu.IsOpen = false;
		}
		/// <summary>
		/// Handles the Click event of the SubmitChangesMenuButton control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
		private void SubmitChangesMenuButton_Click(object sender, RoutedEventArgs e)
		{
			SubmitChanges();
			if (popupMenu != null)
				popupMenu.IsOpen = false;
		}
		#endregion Options Menu
	}

	/// <summary>
	/// CodedValueDomainColumn is used for coded value domains from the feature
	/// layer. CodedValueDomains also are used for FeatureTypes
	/// </summary>
	public sealed class CodedValueDomainColumn : DataGridBoundColumn
	{
		// private
		private CodedValueSourceConverter nameConverter;
		private CodedValueSourceLookupConverter lookupConverter;
		private CodedValueSourceSelectedItemConverter selectedConverter;


		/// <summary>
		/// Gets or sets the coded value sources. Coded values will be used to 
		/// show display text when viewing. Coded values will be used as combo 
		/// box options during editing.
		/// </summary>
		/// <value>The coded value sources.</value>
		public CodedValueSources CodedValueSources { get; set; }
		/// <summary>
		/// Gets or sets the field. The field name of the graphic attribute that 
		/// should be used for this column.
		/// </summary>
		/// <value>The field.</value>
		public string Field { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="CodedValueDomainColumn"/> class.
		/// </summary>
		public CodedValueDomainColumn() :
			base()
		{
			CodedValueSources = new CodedValueSources();
			nameConverter = new CodedValueSourceConverter();
			lookupConverter = new CodedValueSourceLookupConverter();
			selectedConverter = new CodedValueSourceSelectedItemConverter();
		}

		/// <summary>
		/// When overridden in a derived class, gets an editing element that is bound to the column's <see cref="P:System.Windows.Controls.DataGridBoundColumn.Binding"/> property value.
		/// </summary>
		/// <param name="cell">The cell that will contain the generated element.</param>
		/// <param name="dataItem">The data item represented by the row that contains the intended cell.</param>
		/// <returns>
		/// A new editing element that is bound to the column's <see cref="P:System.Windows.Controls.DataGridBoundColumn.Binding"/> property value.
		/// </returns>
		protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem)
		{			
			ComboBox box = new ComboBox()
			{
				Margin = new Thickness(4.0),
				VerticalAlignment = VerticalAlignment.Center,
				VerticalContentAlignment = VerticalAlignment.Center,
				DisplayMemberPath = "DisplayName"
			};
			if (!string.IsNullOrEmpty(Field) && this.CodedValueSources != null)
			{
				// Item Source Binding
				Binding binding = new Binding();
				binding.Mode = BindingMode.OneWay;
				binding.Converter = lookupConverter;
				binding.ConverterParameter = this.CodedValueSources;
				box.SetBinding(ComboBox.ItemsSourceProperty, binding);

				// Selected Item Binding
				selectedConverter.Field = Field;
				Binding selectedBinding = new Binding();
				selectedBinding.Mode = BindingMode.OneWay;
				selectedBinding.Converter = selectedConverter;
				selectedBinding.ConverterParameter = this.CodedValueSources;
				box.SetBinding(ComboBox.SelectedItemProperty, selectedBinding);

				box.SelectionChanged += box_SelectionChanged;
			}
			return box;
		}

		/// <summary>
		/// When overridden in a derived class, gets a read-only element that is bound to the column's <see cref="P:System.Windows.Controls.DataGridBoundColumn.Binding"/> property value.
		/// </summary>
		/// <param name="cell">The cell that will contain the generated element.</param>
		/// <param name="dataItem">The data item represented by the row that contains the intended cell.</param>
		/// <returns>
		/// A new, read-only element that is bound to the column's <see cref="P:System.Windows.Controls.DataGridBoundColumn.Binding"/> property value.
		/// </returns>
		protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
		{
			TextBlock block = new TextBlock()
			{
				Margin = new Thickness(4.0),
				VerticalAlignment = VerticalAlignment.Center			
			};
			if (!string.IsNullOrEmpty(Field) && this.CodedValueSources != null)
			{
				nameConverter.Field = this.Field;
				Binding binding =
#if SILVERLIGHT
				new Binding();
#else
				new Binding("Attributes["+Field+"]");
#endif
				binding.Mode = BindingMode.OneWay;
				binding.Converter = nameConverter;
				binding.ConverterParameter = this.CodedValueSources;
				block.SetBinding(TextBlock.TextProperty, binding);
			}
			return block;
		}

		/// <summary>
		/// When overridden in a derived class, sets cell content as needed for editing.
		/// </summary>
		/// <param name="editingElement">The element that the column displays for a cell in editing mode.</param>
		/// <param name="editingEventArgs">Information about the user gesture that is causing a cell to enter editing mode.</param>
		/// <returns>
		/// Derived classes return the unedited cell value. This implementation returns null in all cases.
		/// </returns>
		protected override object PrepareCellForEdit(FrameworkElement editingElement, RoutedEventArgs editingEventArgs)
		{
			return null;
		}

		void box_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// WPF raises a selection change event even when nothing has changed.
			if (e.AddedItems.Count <= 0) return;

			ComboBox box = sender as ComboBox;
			BindingExpression bindingExpression = box.GetBindingExpression(ComboBox.ItemsSourceProperty);
			object xObject = bindingExpression.DataItem;
			CodedValueSource codedValueSource = e.AddedItems[0] as CodedValueSource;

			if (xObject != null && string.IsNullOrEmpty(Field) != true)
			{
#if SILVERLIGHT
				PropertyInfo property = xObject.GetType().GetProperty(Field);
				if (property != null)
				{
					if (codedValueSource == null)
						DataSourceCreator.SetProperty(null, xObject, property);
					else
						DataSourceCreator.SetProperty(codedValueSource.Code, xObject, property);
				}
#else
				if ((xObject as Graphic).Attributes.ContainsKey(Field))
					FeatureDataGrid.TempAttributes[Field] = (codedValueSource == null) ? null : codedValueSource.Code;
#endif
			}
		}

		private class CodedValueSourceSelectedItemConverter : IValueConverter
		{
			public string Field { get; set; }

			#region IValueConverter Members

			public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
			{
				CodedValueSources codedValueSources = parameter as CodedValueSources;
				if (value != null && string.IsNullOrEmpty(Field) != true && codedValueSources != null)
				{
#if SILVERLIGHT
					PropertyInfo property = value.GetType().GetProperty(Field);
					if (property != null)
					{
						var code = property.GetValue(value, null);
#else
					var code = (value is Graphic) ? (value as Graphic).Attributes[Field] : null;
#endif
					if (code != null)
					{
						CodedValueSource codedValueSource = codedValueSources.FirstOrDefault(x => x.Code != null && x.Code.ToString() == code.ToString());
						return codedValueSource;
					}
#if SILVERLIGHT
					}
#endif
				}
				return null;
			}

			public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
			{
				throw new NotImplementedException();
			}

			#endregion
		}
	}

	/// <summary>
	/// DynamicCodedValueDomainColumn is used for coded value domain types that 
	/// change depending on the value of another column. i.e when the TypeIDField 
	/// value changes a new set of coded values may apply. 
	/// </summary>
	public sealed class DynamicCodedValueDomainColumn : DataGridBoundColumn
	{
		// private
		private DynamicCodedValueSourceConverter nameConverter;
		private DynamicCodedValueSourceLookupConverter lookupConverter;
		private DynamicCodedValueSourceSelectedItemConverter selectedConverter;


		/// <summary>
		/// Gets or sets the dynamic coded value source. a Dictionary that is 
		/// used to lookup the CodedValueSource that should be used based on the 
		/// LookupField value.
		/// </summary>
		/// <value>The dynamic coded value source.</value>
		public DynamicCodedValueSource DynamicCodedValueSource { get; set; }
		/// <summary>
		/// Gets or sets the lookup field. The lookup field is used to determine what 
		/// coded value source to use for this column. i.e. LookupField will usually be
		/// the TypeIDField and when the value of the TypeIDField changes a coded value source
		/// will be looked up from the DynamicCodedValueSource property.
		/// </summary>
		/// <value>The lookup field.</value>
		public string LookupField { get; set; }
		/// <summary>
		/// Gets or sets the field. The field name of the graphic attribute that 
		/// should be used for this column.
		/// </summary>
		/// <value>The field.</value>
		public string Field { get; set; }
		/// <summary>
		/// Gets or sets the field information. 
		/// </summary>
		public Field FieldInfo { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="DynamicCodedValueDomainColumn"/> class.
		/// </summary>
		public DynamicCodedValueDomainColumn()
			: base()
		{
			nameConverter = new DynamicCodedValueSourceConverter();
			lookupConverter = new DynamicCodedValueSourceLookupConverter();
			selectedConverter = new DynamicCodedValueSourceSelectedItemConverter();
		}

		/// <summary>
		/// When overridden in a derived class, gets an editing element that is bound to the column's <see cref="P:System.Windows.Controls.DataGridBoundColumn.Binding"/> property value.
		/// </summary>
		/// <param name="cell">The cell that will contain the generated element.</param>
		/// <param name="dataItem">The data item represented by the row that contains the intended cell.</param>
		/// <returns>
		/// A new editing element that is bound to the column's <see cref="P:System.Windows.Controls.DataGridBoundColumn.Binding"/> property value.
		/// </returns>
		protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem)
		{
			ComboBox box = new ComboBox
			{
				Margin = new Thickness(4.0),
				VerticalAlignment = VerticalAlignment.Center,
				VerticalContentAlignment = VerticalAlignment.Center,
				DisplayMemberPath = "DisplayName"
			};
			if (!string.IsNullOrEmpty(LookupField) && !string.IsNullOrEmpty(Field) && DynamicCodedValueSource != null)
			{
				// Item Source Binding
				lookupConverter.LookupField = this.LookupField;
				lookupConverter.Field = this.FieldInfo;
				Binding binding = new Binding();
				binding.Mode = BindingMode.OneWay;
				binding.Converter = lookupConverter;
				binding.ConverterParameter = DynamicCodedValueSource;
				box.SetBinding(ComboBox.ItemsSourceProperty, binding);

				// Selected Item Binding
				selectedConverter.Field = Field;
				selectedConverter.LookupField = LookupField;
				Binding selectedBinding = new Binding();
				selectedBinding.Mode = BindingMode.OneWay;
				selectedBinding.Converter = selectedConverter;
				selectedBinding.ConverterParameter = this.DynamicCodedValueSource;
				box.SetBinding(ComboBox.SelectedItemProperty, selectedBinding);

				box.SelectionChanged += box_SelectionChanged;
			}
			return box;
		}

		/// <summary>
		/// When overridden in a derived class, gets a read-only element that is bound to the column's <see cref="P:System.Windows.Controls.DataGridBoundColumn.Binding"/> property value.
		/// </summary>
		/// <param name="cell">The cell that will contain the generated element.</param>
		/// <param name="dataItem">The data item represented by the row that contains the intended cell.</param>
		/// <returns>
		/// A new, read-only element that is bound to the column's <see cref="P:System.Windows.Controls.DataGridBoundColumn.Binding"/> property value.
		/// </returns>
		protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
		{
			TextBlock block = new TextBlock
			{
				Margin = new Thickness(4.0),
				VerticalAlignment = VerticalAlignment.Center
			};
			if (!string.IsNullOrEmpty(LookupField) && !string.IsNullOrEmpty(Field) && DynamicCodedValueSource != null)
			{
				nameConverter.LookupField = this.LookupField;
				nameConverter.Field = this.Field;

				Binding binding =
#if SILVERLIGHT
				new Binding();
#else
				new Binding("Attributes["+Field+"]");
#endif
				binding.Mode = BindingMode.OneWay;
				binding.Converter = nameConverter;
				binding.ConverterParameter = 
#if SILVERLIGHT
				DynamicCodedValueSource;
#else
				new Object[] { DynamicCodedValueSource, block };
#endif					
				block.SetBinding(TextBlock.TextProperty, binding);
			}
			return block;
		}

		private void box_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// WPF raises a selection change event even when nothing has changed.
			if (e.AddedItems.Count <= 0) return;

			ComboBox box = sender as ComboBox;
			BindingExpression bindingExpression = box.GetBindingExpression(ComboBox.ItemsSourceProperty);
			object xObject = bindingExpression.DataItem;
			CodedValueSource codedValueSource = e.AddedItems[0] as CodedValueSource;

			if (xObject != null && string.IsNullOrEmpty(Field) != true)
			{
#if SILVERLIGHT
				PropertyInfo property = xObject.GetType().GetProperty(Field);
				if (property != null)
				{
					if (codedValueSource == null)
						DataSourceCreator.SetProperty(null, xObject, property);
					else
						DataSourceCreator.SetProperty(codedValueSource.Code, xObject, property);
				}
#else
				if ((xObject as Graphic).Attributes.ContainsKey(Field))
					FeatureDataGrid.TempAttributes[Field] = (codedValueSource == null) ? null : codedValueSource.Code;
#endif
			}
		}

		/// <summary>
		/// When overridden in a derived class, sets cell content as needed for editing.
		/// </summary>
		/// <param name="editingElement">The element that the column displays for a cell in editing mode.</param>
		/// <param name="editingEventArgs">Information about the user gesture that is causing a cell to enter editing mode.</param>
		/// <returns>
		/// Derived classes return the unedited cell value. This implementation returns null in all cases.
		/// </returns>
		protected override object PrepareCellForEdit(FrameworkElement editingElement, RoutedEventArgs editingEventArgs)
		{
			return null;
		}

		private class DynamicCodedValueSourceSelectedItemConverter : IValueConverter
		{
			public string Field { get; set; }
			public string LookupField { get; set; }

			#region IValueConverter Members

			public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
			{
				DynamicCodedValueSource dynamicCodedValueSource = parameter as DynamicCodedValueSource;
				if (value != null && string.IsNullOrEmpty(Field) != true
					&& string.IsNullOrEmpty(LookupField) != true && dynamicCodedValueSource != null)
				{
#if SILVERLIGHT
					PropertyInfo property = value.GetType().GetProperty(Field);
					if (property == null)
						return null;
					
					var code = property.GetValue(value, null);
#else
					var code = value is Graphic ? (value as Graphic).Attributes[Field] : null;
#endif
					if (code == null)
						return null;
#if SILVERLIGHT
					PropertyInfo lookupProperty = value.GetType().GetProperty(LookupField);
					if(lookupProperty == null) 
						return null;
					
					var key = lookupProperty.GetValue(value,null);
#else
					var key = value is Graphic ? (value as Graphic).Attributes[LookupField] : null;
#endif
					if (key == null)
						return null;

					if (dynamicCodedValueSource.ContainsKey(key))
					{
						CodedValueSources codedValueSources = dynamicCodedValueSource[key];
						if (codedValueSources != null)
						{
							CodedValueSource codedValueSource = codedValueSources.FirstOrDefault(x => x.Code != null && x.Code.ToString() == code.ToString());
							return codedValueSource;
						}
					}
					else if (dynamicCodedValueSource.ContainsKey(key.ToString()))
					{
						CodedValueSources codedValueSources = dynamicCodedValueSource[key.ToString()];
						if (codedValueSources != null)
						{
							CodedValueSource codedValueSource = codedValueSources.FirstOrDefault(x => x.Code != null && x.Code.ToString() == code.ToString());
							return codedValueSource;
						}
					}
				}
				return null;
			}

			public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
			{
				throw new NotImplementedException();
			}

			#endregion
		}
	}
}
