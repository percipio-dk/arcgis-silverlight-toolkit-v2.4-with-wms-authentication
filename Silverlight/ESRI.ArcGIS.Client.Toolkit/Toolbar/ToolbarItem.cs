// (c) Copyright ESRI.
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
    /// <b>Note: This API is now obsolete.</b> This class is used with the Toolbar, which is deprecated.
    /// </para>
    /// <para>
    /// The item used to represent a toolbar component
    /// </para>
    /// </summary>
    [Obsolete("This class is used with the Toolbar, which is deprecated.")]
    public class ToolbarItem : DependencyObject
    {
        #region Dependency Properties

        /// <summary>
        /// <para>
        /// <b>Note: This API is now obsolete.</b> This class is used with the Toolbar, which is deprecated.
        /// </para>
        /// <para>
        /// Gets or sets the text.
        /// </para>
        /// </summary>
        /// <value>The text.</value>
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        /// <summary>
        /// <para>
        /// <b>Note: This API is now obsolete.</b> This class is used with the Toolbar, which is deprecated.
        /// </para>
        /// <para>
        /// Identifies the <see cref="Text"/> dependency property.
        /// </para>
        /// </summary>
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(ToolbarItem), null);

        /// <summary>
        /// <para>
        /// <b>Note: This API is now obsolete.</b> This class is used with the Toolbar, which is deprecated.
        /// </para>
        /// <para>
        /// Gets or sets the content.
        /// </para>
        /// </summary>
        /// <value>The content.</value>
        public FrameworkElement Content
        {
            get { return (FrameworkElement)GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }

        /// <summary>
        /// <para>
        /// <b>Note: This API is now obsolete.</b> This class is used with the Toolbar, which is deprecated.
        /// </para>
        /// <para>
        /// Identifies the <see cref="Content"/> dependency property.
        /// </para>
        /// </summary>
        public static readonly DependencyProperty ContentProperty =
            DependencyProperty.Register("Content", typeof(FrameworkElement), typeof(ToolbarItem), null);

        /// <summary>
        /// <para>
        /// <b>Note: This API is now obsolete.</b> This class is used with the Toolbar, which is deprecated.
        /// </para>
        /// <para>
        /// Gets or sets the tag.
        /// </para>
        /// </summary>
        /// <value>The tag.</value>
        public object Tag
        {
            get { return (object)GetValue(TagProperty); }
            set { SetValue(TagProperty, value); }
        }

        /// <summary>
        /// <para>
        /// <b>Note: This API is now obsolete.</b> This class is used with the Toolbar, which is deprecated.
        /// </para>
        /// <para>
        /// Identifies the <see cref="Tag"/> dependency property.
        /// </para>
        /// </summary>
        public static readonly DependencyProperty TagProperty =
            DependencyProperty.Register("Tag", typeof(object), typeof(ToolbarItem), null);
        #endregion
    }
}
