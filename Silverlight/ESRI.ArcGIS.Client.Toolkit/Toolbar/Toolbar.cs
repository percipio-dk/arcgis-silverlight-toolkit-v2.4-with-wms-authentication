﻿// (c) Copyright ESRI.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace ESRI.ArcGIS.Client.Toolkit
{
    /// <summary>
    /// <para>
    /// <b>Note: This API is now obsolete.</b> Please use a Panel or other container control, such as a StackPanel or Grid.
    /// </para>
    /// <para>
    /// A Widget used to represent a Toolbar.  This control has been deprecated to encourage effective use of container 
    /// controls in the core platform which are designed to layout framework elements in an application.  
    /// For example, if a panel of tools is necessary in an application, use the StackPanel to organize Buttons 
    /// or other controls that initiate actions.     
    /// </para>
    /// </summary>
    [Obsolete("Please use a Panel or other container control, such as a StackPanel or Grid.")]	
	public class Toolbar : Control
    {
        // Magnification 0 - 1
        // DisplayCaptions = true/false

        /// <summary>
        /// <para>
        /// <b>Note: This API is now obsolete.</b> Please use a Panel or other container control, such as a StackPanel or Grid.
        /// </para>
        /// <para>
        /// Occurs when [toolbar index changed].
        /// </para>
        /// </summary>
        public event ToolbarIndexChangedHandler ToolbarIndexChanged;

        /// <summary>
        /// <para>
        /// <b>Note: This API is now obsolete.</b> Please use a Panel or other container control, such as a StackPanel or Grid.
        /// </para>
        /// <para>
        /// Occurs when [toolbar item clicked].
        /// </para>
        /// </summary>
        public event ToolbarIndexChangedHandler ToolbarItemClicked;

        /// <summary>
        /// <para>
        /// <b>Note: This API is now obsolete.</b> Please use a Panel or other container control, such as a StackPanel or Grid.
        /// </para>
        /// <para>
        /// Occurs when [toolbar item mouse enter].
        /// </para>
        /// </summary>
        public event ToolbarItemMouseEnter ToolbarItemMouseEnter;

        /// <summary>
        /// <para>
        /// <b>Note: This API is now obsolete.</b> Please use a Panel or other container control, such as a StackPanel or Grid.
        /// </para>
        /// <para>
        /// Occurs when [toolbar item mouse leave].
        /// </para>
        /// </summary>
        public event ToolbarItemMouseLeave ToolbarItemMouseLeave;

        /// <summary>
        /// <para>
        /// <b>Note: This API is now obsolete.</b> Please use a Panel or other container control, such as a StackPanel or Grid.
        /// </para>
        /// <para>
        /// An enumeration used to select the type of click effect
        /// </para>
        /// </summary>
        public enum ClickEffect 
        {
            /// <summary>
            /// <para>
            /// <b>Note: This API is now obsolete.</b> Please use a Panel or other container control, such as a StackPanel or Grid.
            /// </para>
            /// <para>
            /// Specifify the None enumeration when no click effect is wanted
            /// </para>
            /// </summary>
            None,

            /// <summary>
            /// <para>
            /// <b>Note: This API is now obsolete.</b> Please use a Panel or other container control, such as a StackPanel or Grid.
            /// </para>
            /// <para>
            /// Use this enumeration to specify a bounce click effect
            /// </para>
            /// </summary>
            Bounce 
        };

        private const string ROOT_ELEMENT = "RootElement";
        private const double LARGE_ICON_SIZE = 0.70; //1
        private const double MEDIUM_ICON_SIZE = 0.65; //0.75;
        private const double SMALL_ICON_SIZE = 0.60; //0.65;
        private const double NORMAL_ICON_SIZE = 0.55;

        private Panel m_rootElement;

        /// <summary>
        /// <para>
        /// <b>Note: This API is now obsolete.</b> Please use a Panel or other container control, such as a StackPanel or Grid.
        /// </para>
        /// <para>
        /// Initializes a new instance of the <see cref="Toolbar"/> class.
        /// </para>
        /// </summary>
        public Toolbar()
        {
#if SILVERLIGHT
            this.DefaultStyleKey = typeof(Toolbar);
#endif
			this.Items = new ToolbarItemCollection();
        }
  /// <summary>
  /// <para>
  /// <b>Note: This API is now obsolete.</b> Please use a Panel or other container control, such as a StackPanel or Grid.
  /// </para>
  /// <para>
  /// Static initialization for the <see cref="Toolbar"/> control.
  /// </para>
  /// </summary>
		static Toolbar()
		{
#if !SILVERLIGHT
			DefaultStyleKeyProperty.OverrideMetadata(typeof(Toolbar),
				new FrameworkPropertyMetadata(typeof(Toolbar)));
#endif
		}

        /// <summary>
        /// <para>
        /// <b>Note: This API is now obsolete.</b> Please use a Panel or other container control, such as a StackPanel or Grid.
        /// </para>
        /// <para>
        /// When overridden in a derived class, is invoked whenever application code or internal processes (such as a rebuilding layout pass) call <see cref="M:System.Windows.Controls.Control.ApplyTemplate"/>.
        /// </para>
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            m_rootElement = this.GetTemplateChild(ROOT_ELEMENT) as Panel;

            if(m_rootElement == null) 
                return;

            m_rootElement.MouseLeave += new MouseEventHandler(m_rootElement_MouseLeave);
            
            for(int i = 0; i < this.Items.Count; ++i)
            {
                ToolbarItem cmi = this.Items[i];
                FrameworkElement contentElement = cmi.Content;
				if (contentElement != null)
				{
					contentElement.Height = this.MaxItemHeight * NORMAL_ICON_SIZE;
					contentElement.Width = this.MaxItemWidth * NORMAL_ICON_SIZE;
					contentElement.Tag = i;
					contentElement.RenderTransform = BuildTransformGroup();
					contentElement.VerticalAlignment = VerticalAlignment.Bottom;
					contentElement.MouseEnter += new MouseEventHandler(contentElement_MouseEnter);
					contentElement.MouseLeave += new MouseEventHandler(contentElement_MouseLeave);
					contentElement.MouseLeftButtonDown += new MouseButtonEventHandler(contentElement_MouseLeftButtonDown);

					m_rootElement.Children.Add(contentElement);
				}
            }
        }

        void contentElement_MouseLeave(object sender, MouseEventArgs e)
        {
            if (ToolbarItemMouseLeave != null)
            {
                FrameworkElement selectedItem = (FrameworkElement)sender;
                int index = (int)selectedItem.Tag;
                SelectedToolbarItemArgs tbarArgs = new
                    SelectedToolbarItemArgs(this.Items[index], index);
                ToolbarItemMouseLeave(this, tbarArgs);
            }
        }

        private TransformGroup BuildTransformGroup()
        {
            TransformGroup tg = new TransformGroup();
            TranslateTransform tt = new TranslateTransform();
            tt.Y = 0;
            tg.Children.Add(tt);
            return tg;
        }

        private void contentElement_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement selectedItem = (FrameworkElement)sender;
            
            if(this.ToolbarItemClickEffect == ClickEffect.Bounce)
                ApplyBounceEffect(selectedItem);

            if(ToolbarItemClicked != null)
            {
                int index = (int)selectedItem.Tag;
                SelectedToolbarItemArgs tbarArgs = new 
                    SelectedToolbarItemArgs(this.Items[index], index);
                ToolbarItemClicked(this, tbarArgs);
            }
            
        }

        private void m_rootElement_MouseLeave(object sender, MouseEventArgs e)
        {
            AdjustSizes(-1);
        }


        private void contentElement_MouseEnter(object sender, MouseEventArgs e)
        {
            FrameworkElement r = sender as FrameworkElement;
            int index = (int) r.Tag;
            AdjustSizes(index);
            SelectedToolbarItemArgs args = new SelectedToolbarItemArgs(this.Items[index], index);
            
            if(ToolbarIndexChanged != null)
                ToolbarIndexChanged(this, args);

            if (ToolbarItemMouseEnter != null)
                ToolbarItemMouseEnter(this, args);
            
        }

        private void AdjustSizes(int index)
        {
            for (int i = 0; i < this.Items.Count; ++i )
            {
                if(index == -1)
                {
                    ApplyResizeEffect(this.Items[i].Content, NORMAL_ICON_SIZE, this.MaxItemWidth, this.MaxItemHeight);
                    continue;
                }

                if (i == index)
                    ApplyResizeEffect(this.Items[i].Content, LARGE_ICON_SIZE, this.MaxItemWidth, this.MaxItemHeight);
                else if (i == index - 1 || i == index + 1)
                    ApplyResizeEffect(this.Items[i].Content, MEDIUM_ICON_SIZE, this.MaxItemWidth, this.MaxItemHeight);
                else if (i == index - 2 || i == index + 2)
                    ApplyResizeEffect(this.Items[i].Content, SMALL_ICON_SIZE, this.MaxItemWidth, this.MaxItemHeight);
                else
                    ApplyResizeEffect(this.Items[i].Content, NORMAL_ICON_SIZE, this.MaxItemWidth, this.MaxItemHeight);
            }
        }

        private void ApplyResizeEffect(FrameworkElement element, double factor, double width, double height)
        {
            TimeSpan speed = TimeSpan.FromMilliseconds(100);
            DoubleAnimation daWidth = new DoubleAnimation { To = factor*width, Duration = new Duration(speed) };
            DoubleAnimation daHeight = new DoubleAnimation { To = factor*height, Duration = new Duration(speed) };
            Storyboard sb = new Storyboard();
			daHeight.SetValue(Storyboard.TargetPropertyProperty, new PropertyPath("Height"));
			daWidth.SetValue(Storyboard.TargetPropertyProperty, new PropertyPath("Width"));
			sb.Children.Add(daWidth);
			sb.Children.Add(daHeight);
			Storyboard.SetTarget(daWidth, element);
            Storyboard.SetTarget(daHeight, element);
            sb.Begin();
        }

        private void ApplyBounceEffect(FrameworkElement e)
        {
            var da = new DoubleAnimationUsingKeyFrames();
            var k1 = new SplineDoubleKeyFrame
            {
                KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(100)),
                Value = this.MaxItemHeight * 0.30
            };
            var k2 = new SplineDoubleKeyFrame
            {
                KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(200)),
                Value = 0
            };
            var k3 = new SplineDoubleKeyFrame
            {
                KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(300)),
                Value = this.MaxItemHeight * 0.10
            };
            var k4 = new SplineDoubleKeyFrame
            {
                KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(350)),
                Value = 0
            };
            da.KeyFrames.Add(k1);
            da.KeyFrames.Add(k2);
            da.KeyFrames.Add(k3);
            da.KeyFrames.Add(k4);

            Storyboard sb = new Storyboard();
            Storyboard.SetTarget(da, e);
            Storyboard.SetTargetProperty(da,
                new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(TranslateTransform.Y)"));
            sb.Children.Add(da);
            sb.Begin();
        }

        #region MaxItemHeight Property

        /// <summary>
        /// <para>
        /// <b>Note: This API is now obsolete.</b> Please use a Panel or other container control, such as a StackPanel or Grid.
        /// </para>
        /// <para>
        /// Identifies the MaxItemHeight dependency property
        /// </para>
        /// </summary>
        public static readonly DependencyProperty MaxItemHeightProperty = DependencyProperty.Register(
            "MaxItemHeight",
            typeof(double),
            typeof(Toolbar),
            null);

        /// <summary>
        /// <para>
        /// <b>Note: This API is now obsolete.</b> Please use a Panel or other container control, such as a StackPanel or Grid.
        /// </para>
        /// <para>
        /// Gets or sets the maximum height of the <see cref="ToolbarItem"/> item.
        /// </para>
        /// </summary>
        /// <value>The height of the max item.</value>
        public double MaxItemHeight
        {
            get
            {
                return (double)GetValue(MaxItemHeightProperty);
            }
            set { SetValue(MaxItemHeightProperty, value); }
        }
        #endregion

        #region MaxItemWidth Property

        /// <summary>
        /// <para>
        /// <b>Note: This API is now obsolete.</b> Please use a Panel or other container control, such as a StackPanel or Grid.
        /// </para>
        /// <para>
        /// Identifies the MaxItemWidth dependency property
        /// </para>
        /// </summary>
        public static readonly DependencyProperty MaxItemWidthProperty = DependencyProperty.Register(
            "MaxItemWidth",
            typeof(double),
            typeof(Toolbar),
            null);

        /// <summary>
        /// <para>
        /// <b>Note: This API is now obsolete.</b> Please use a Panel or other container control, such as a StackPanel or Grid.
        /// </para>
        /// <para>
        /// Gets or sets the maximum width of the <see cref="ToolbarItem"/>.
        /// </para>
        /// </summary>
        /// <value>The width of the max item.</value>
        public double MaxItemWidth
        {
            get
            {
                return (double)GetValue(MaxItemWidthProperty);
            }
            set { SetValue(MaxItemWidthProperty, value); }
        }
        #endregion

        #region Items Property

        /// <summary>
        /// <para>
        /// <b>Note: This API is now obsolete.</b> Please use a Panel or other container control, such as a StackPanel or Grid.
        /// </para>
        /// <para>
        /// Identifies the Items dependency property
        /// </para>
        /// </summary>
        public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(
            "Items",
            typeof(ToolbarItemCollection),
            typeof(Toolbar),
            null);

        /// <summary>
        /// <para>
        /// <b>Note: This API is now obsolete.</b> Please use a Panel or other container control, such as a StackPanel or Grid.
        /// </para>
        /// <para>
        /// Gets or sets the <see cref="ToolbarItemCollection"/>.
        /// </para>
        /// </summary>
        /// <value>The items.</value>
        public ToolbarItemCollection Items
        {
            get 
            {
                return GetValue(ItemsProperty) as ToolbarItemCollection;  
            }
            set { SetValue(ItemsProperty, value as ToolbarItemCollection); }
        }
        #endregion

        #region ToolbarItemClickEffect Property

        /// <summary>
        /// <para>
        /// <b>Note: This API is now obsolete.</b> Please use a Panel or other container control, such as a StackPanel or Grid.
        /// </para>
        /// <para>
        /// Identifies the ToolbarItemClieckEffect dependency property
        /// </para>
        /// </summary>
        public static readonly DependencyProperty ToolbarItemClickEffectProperty = DependencyProperty.Register(
            "ToolbarItemClickEffect",
            typeof(Toolbar.ClickEffect),
            typeof(Toolbar),
            null);

        /// <summary>
        /// <para>
        /// <b>Note: This API is now obsolete.</b> Please use a Panel or other container control, such as a StackPanel or Grid.
        /// </para>
        /// <para>
        /// Gets or sets the toolbar item click effect.
        /// </para>
        /// </summary>
        /// <value>The toolbar item click effect.</value>
        public ClickEffect ToolbarItemClickEffect
        {
            get { return (ClickEffect)GetValue(ToolbarItemClickEffectProperty); }
            set { SetValue(ToolbarItemClickEffectProperty, value); }
        }
        #endregion


    }
}
