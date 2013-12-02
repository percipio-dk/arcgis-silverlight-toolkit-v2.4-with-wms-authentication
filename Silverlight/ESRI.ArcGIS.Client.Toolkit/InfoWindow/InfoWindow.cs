// (c) Copyright ESRI.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using ESRI.ArcGIS.Client.Geometry;

namespace ESRI.ArcGIS.Client.Toolkit
{
	/// <summary>
	/// Creates an instance of the InfoWindow that positions itself on top of the map
	/// </summary>
	[TemplatePart(Name = "BorderPath", Type = typeof(Path))]
	[TemplateVisualState(Name = "Show", GroupName = "CommonStates")]
	[TemplateVisualState(Name = "Hide", GroupName = "CommonStates")]
	public class InfoWindow : ContentControl
	{
		#region Private fields
		private const double ArrowHeight =
#if WINDOWS_PHONE
			20;
#else
			10;
#endif
		private TranslateTransform translate;
		private Path borderPath;
		private bool isDesignMode = false;
		private PathGeometry borderGeometry;
		private bool realizeGeometryScheduled;
		private Size lastSize;
		private double lastArrowSize = double.NaN;
		private double lastCornerRadius = double.NaN;
		#endregion

		/// <summary>
		/// Initializes a new instance of the <see cref="InfoWindow"/> class.
		/// </summary>
		public InfoWindow()
		{
#if SILVERLIGHT
			DefaultStyleKey = typeof(InfoWindow);
#endif
			isDesignMode = System.ComponentModel.DesignerProperties.GetIsInDesignMode(this);//Note: returns true in Blend but not VS
			Loaded += InfoWindow_Loaded;
		}

		private void InfoWindow_Loaded(object sender, RoutedEventArgs e)
		{
			CheckPosition();
		}

		/// <summary>
		/// Static initialization for the <see cref="InfoWindow"/> control.
		/// </summary>
		static InfoWindow()
		{
#if !SILVERLIGHT
			DefaultStyleKeyProperty.OverrideMetadata(typeof(InfoWindow), new FrameworkPropertyMetadata(typeof(InfoWindow)));
#endif
		}
		
		/// <summary>
		/// Provides the behavior for the Arrange pass of Silverlight layout.
		/// Classes can override this method to define their own Arrange pass 
		/// behavior.
		/// </summary>
		/// <param name="finalSize">The final area within the parent that this
		/// object should use to arrange itself and its children.</param>
		/// <returns>
		/// The actual size that is used after the element is arranged in layout.
		/// </returns>
		protected override Size ArrangeOverride(Size finalSize)
		{
			var size = base.ArrangeOverride(finalSize);
			if (!realizeGeometryScheduled && BuildBorderPath(size, ArrowHeight, CornerRadius))
			{
				realizeGeometryScheduled = true;
				LayoutUpdated += OnLayoutUpdated;
			}

			if (Map == null && Anchor == null && !isDesignMode) return size;
			CheckPosition();
			if (!isDesignMode)
			{
				Point p2 = Map.MapToScreen(Anchor, true);
				Point p = Map.TransformToVisual(this.Parent as UIElement).Transform(p2);
				translate.X = p.X - size.Width  * .5;
				translate.Y = p.Y - size.Height - ArrowHeight - CornerRadius;
				RenderTransformOrigin = new Point(0.5, (size.Height + CornerRadius + ArrowHeight) / size.Height);
			}
			return size;
		}

		private void OnLayoutUpdated(object sender, EventArgs e)
		{
			realizeGeometryScheduled = false;
			LayoutUpdated -= OnLayoutUpdated;
			borderPath.Data = borderGeometry;
			borderPath.Margin = new Thickness(0, 0, -CornerRadius * 2 - StrokeThickness, -CornerRadius * 2 - ArrowHeight - StrokeThickness);
		}

		private bool BuildBorderPath(Size size, double arrowSize, double cornerRadius)
		{
			if (lastSize == size && lastArrowSize == arrowSize && lastCornerRadius == cornerRadius)
				return false;
			lastSize = size;
			lastArrowSize = arrowSize;
			lastCornerRadius = cornerRadius;
			double bottomY = size.Height - ArrowHeight;
			PathGeometry pg = new PathGeometry();
			PathFigure p = new PathFigure();
			p.StartPoint = new Point(0, -cornerRadius);
			p.Segments.Add(new LineSegment() { Point = new Point(size.Width, -cornerRadius) }); //Top line
			p.Segments.Add(new ArcSegment()
			{
				Size = new Size(cornerRadius, cornerRadius),
				SweepDirection = System.Windows.Media.SweepDirection.Clockwise,
				Point = new Point(size.Width + cornerRadius, 0)
			}); //UR

			p.Segments.Add(new LineSegment() { Point = new Point(size.Width + cornerRadius, size.Height) }); //Right side
			p.Segments.Add(new ArcSegment()
			{
				Size = new Size(cornerRadius, cornerRadius),
				SweepDirection = System.Windows.Media.SweepDirection.Clockwise,
				Point = new Point(size.Width, size.Height + cornerRadius)
			}); //LR

			//Miter
			p.Segments.Add(new LineSegment() { Point = new Point(size.Width * .5 + arrowSize * .5, size.Height + cornerRadius) }); //Bottom line, Right of miter
			p.Segments.Add(new LineSegment() { Point = new Point(size.Width * .5, size.Height + cornerRadius + arrowSize) }); //Right side of Miter down
			p.Segments.Add(new LineSegment() { Point = new Point(size.Width * .5 - arrowSize * .5, size.Height + cornerRadius) }); //Left side of Miter up

			p.Segments.Add(new LineSegment() { Point = new Point(0, size.Height + cornerRadius) }); //Bottom line, left of miter
			p.Segments.Add(new ArcSegment()
			{
				Size = new Size(cornerRadius, cornerRadius),
				SweepDirection = System.Windows.Media.SweepDirection.Clockwise,
				Point = new Point(-cornerRadius, size.Height)
			}); //LL

			p.Segments.Add(new LineSegment() { Point = new Point(-cornerRadius, 0) }); //Left side
			p.Segments.Add(new ArcSegment()
			{
				Size = new Size(cornerRadius, cornerRadius),
				SweepDirection = System.Windows.Media.SweepDirection.Clockwise,
				Point = new Point(0, -cornerRadius)
			}); //UR

			pg.Figures.Add(p);
			borderGeometry = pg;
			return true;
		}

		/// <summary>
		/// When overridden in a derived class, is invoked whenever application 
		/// code or internal processes (such as a rebuilding layout pass) call 
		/// <see cref="M:System.Windows.Controls.Control.ApplyTemplate"/>. In 
		/// simplest terms, this means the method is called just before a UI 
		/// element displays in an application. For more information, see Remarks.
		/// </summary>
		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			borderPath = GetTemplateChild("BorderPath") as Path;
			RenderTransform = translate = new TranslateTransform();
			InvalidateArrange();
			ChangeVisualState(false);
		}

		private void ChangeVisualState(bool useTransitions)
		{
			InvalidateMeasure();
			if (IsOpen && (Anchor != null && Map != null || isDesignMode))
				GoToState(useTransitions, "Show");
			else
				GoToState(useTransitions, "Hide");
		}

		private bool GoToState(bool useTransitions, string stateName)
		{
			return VisualStateManager.GoToState(this, stateName, useTransitions);
		}

		#region Properties

		/// <summary>
		/// Gets or sets a value indicating whether the InfoWindow is open.
		/// </summary>
		/// <value><c>true</c> if this instance is open; otherwise, <c>false</c>.</value>
		public bool IsOpen
		{
			get { return (bool)GetValue(IsOpenProperty); }
			set { SetValue(IsOpenProperty, value); }
		}

		/// <summary>
		/// Identifies the <see cref="IsOpen"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty IsOpenProperty =
			DependencyProperty.Register("IsOpen", typeof(bool), typeof(InfoWindow), new PropertyMetadata(false, OnIsOpenPropertyChanged));

		private static void OnIsOpenPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			InfoWindow obj = (InfoWindow)d;
			obj.ChangeVisualState(true);
		}

		/// <summary>
		/// Gets or sets the map.
		/// </summary>
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
			DependencyProperty.Register("Map", typeof(Map), typeof(InfoWindow), new PropertyMetadata(OnMapPropertyChanged));

		private static void OnMapPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			InfoWindow obj = (InfoWindow)d;
			Map newValue = (Map)e.NewValue;
			Map oldValue = (Map)e.OldValue;
			if (oldValue != null)
			{
				oldValue.ExtentChanging -= obj.map_ExtentChanging;
				oldValue.ExtentChanged -= obj.map_ExtentChanging;
			}
			if (newValue != null)
			{
				newValue.ExtentChanging += obj.map_ExtentChanging;
				newValue.ExtentChanged += obj.map_ExtentChanging;
			}
			obj.CheckPosition();
			obj.InvalidateArrange();
		}

		/// <summary>
		/// Gets or sets the anchor point.
		/// </summary>
		/// <value>The anchor point.</value>
		public MapPoint Anchor
		{
			get { return (MapPoint)GetValue(AnchorProperty); }
			set { SetValue(AnchorProperty, value); }
		}

		/// <summary>
		/// Identifies the <see cref="Anchor"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty AnchorProperty =
			DependencyProperty.Register("Anchor", typeof(MapPoint), typeof(InfoWindow), new PropertyMetadata(OnAnchorPropertyChanged));

		private static void OnAnchorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			InfoWindow obj = (InfoWindow)d;
			obj.CheckPosition();
			obj.InvalidateArrange();
		}

		/// <summary>
		/// Gets or sets the corner radius.
		/// </summary>
		/// <value>The corner radius.</value>
		public double CornerRadius
		{
			get { return (double)GetValue(CornerRadiusProperty); }
			set { SetValue(CornerRadiusProperty, value); }
		}

		/// <summary>
		/// Identifies the <see cref="CornerRadius"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty CornerRadiusProperty =
			DependencyProperty.Register("CornerRadius", typeof(double), typeof(InfoWindow), new PropertyMetadata(5d, OnCornerRadiusPropertyChanged));

		private static void OnCornerRadiusPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (((double)e.NewValue) < 0)
				throw new ArgumentOutOfRangeException("CornerRadius");
			(d as InfoWindow).InvalidateArrange();
		}

		/// <summary>
		/// Gets or sets the stroke thickness.
		/// </summary>
		/// <value>The stroke thickness.</value>
		public double StrokeThickness
		{
			get { return (double)GetValue(StrokeThicknessProperty); }
			set { SetValue(StrokeThicknessProperty, value); }
		}

		/// <summary>
		/// Identifies the <see cref="StrokeThickness"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty StrokeThicknessProperty =
			DependencyProperty.Register("StrokeThickness", typeof(double), typeof(InfoWindow), new PropertyMetadata(2d));

		#endregion

		private void map_ExtentChanging(object sender, ExtentEventArgs e)
		{
			CheckPosition();
			InvalidateArrange();
		}

		private void CheckPosition()
		{
			if (Map != null && Anchor != null && this.Parent != null && Map.Extent != null)
			{
				if (!Map.WrapAroundIsActive)
				{
					if (Map.Extent.Intersects(Anchor.Extent))
					{
				this.Visibility = System.Windows.Visibility.Visible;
						return;
					}
					else Visibility = System.Windows.Visibility.Collapsed;
				}
				else //Wraparound mode. Perform intersection test on normalized geometries
				{
					var ext = Geometry.Geometry.NormalizeCentralMeridian(Map.Extent);
					var pnt = Geometry.Geometry.NormalizeCentralMeridian(Anchor);
					if (ext is Envelope)
					{
						if ((ext as Envelope).Intersects(pnt.Extent))
						{
							this.Visibility = System.Windows.Visibility.Visible;
							return;
						}
						else Visibility = System.Windows.Visibility.Collapsed;
					}
					else if (ext is ESRI.ArcGIS.Client.Geometry.Polygon) //We will get back a polygon with multiple rings if the extent crosses the anti-meridian
					{
						//Check if any of the rings intersects the point
						foreach (var ring in (ext as ESRI.ArcGIS.Client.Geometry.Polygon).Rings)
						{
							var poly = new ESRI.ArcGIS.Client.Geometry.Polygon();
							poly.Rings.Add(ring);
							if (poly.Extent.Intersects(pnt.Extent))
							{
								this.Visibility = System.Windows.Visibility.Visible;
								return;
							}
							
						}
						Visibility = System.Windows.Visibility.Collapsed;
					}
				}
			}
			else if (!isDesignMode)
				this.Visibility = System.Windows.Visibility.Collapsed;
		}
	}
}