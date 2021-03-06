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
  /// <b>Note: This API is now obsolete.</b> This class is used with the Toolbar, which is deprecated.
  /// </para>
  /// <para>
  /// Handler for the Toolbar Index Changed 
  /// </para>
  /// </summary>
    [Obsolete("This class is used with the Toolbar, which is deprecated.")]
    public delegate void ToolbarIndexChangedHandler(object sender, SelectedToolbarItemArgs e);

    /// <summary>
    /// <para>
    /// <b>Note: This API is now obsolete.</b> This class is used with the Toolbar, which is deprecated.
    /// </para>
    /// <para>
    /// Handler for the Toolbar item Mouse enter 
    /// </para>
    /// </summary>
    [Obsolete("This class is used with the Toolbar, which is deprecated.")]
    public delegate void ToolbarItemMouseEnter(object sender, SelectedToolbarItemArgs e);

    /// <summary>
    /// <para>
    /// <b>Note: This API is now obsolete.</b> This class is used with the Toolbar, which is deprecated.
    /// </para>
    /// <para>
    /// Handler for the Toolbar item Mouse Leave 
    /// </para>
    /// </summary>
    [Obsolete("This class is used with the Toolbar, which is deprecated.")]
    public delegate void ToolbarItemMouseLeave(object sender, SelectedToolbarItemArgs e);

    /// <summary>
    /// <para>
    /// <b>Note: This API is now obsolete.</b> This class is used with the Toolbar, which is deprecated.
    /// </para>
    /// <para>
    /// Used with the Toolbar to pass along Event arguments used with the selected toolbar item. 
    /// </para>
    /// </summary>
    [Obsolete("This class is used with the Toolbar, which is deprecated.")]
    public class SelectedToolbarItemArgs : EventArgs
    {
        private readonly ToolbarItem _cmi;
        private readonly int _index;

        /// <summary>
        /// <para>
        /// <b>Note: This API is now obsolete.</b> This class is used with the Toolbar, which is deprecated.
        /// </para>
        /// <para>
        /// Initializes a new instance of the <see cref="SelectedToolbarItemArgs"/> class. 
        /// </para>
        /// </summary>
        /// <param name="tbarItem">The tbar item.</param>
        /// <param name="tbarIndex">Index of the tbar.</param>
        public SelectedToolbarItemArgs(ToolbarItem tbarItem, int tbarIndex)
        {
            _cmi = tbarItem;
            _index = tbarIndex;
        }

        /// <summary>
        /// <para>
        /// <b>Note: This API is now obsolete.</b> This class is used with the Toolbar, which is deprecated.
        /// </para>
        /// <para>
        /// Gets the selected <see cref="ToolbarItem"/>. 
        /// </para>
        /// </summary>
        /// <value>The <see cref="ToolbarItem"/>.</value>
        public ToolbarItem Item
        {
            get { return _cmi; }
        }

        /// <summary>
        /// <para>
        /// <b>Note: This API is now obsolete.</b> This class is used with the Toolbar, which is deprecated.
        /// </para>
        /// <para>
        /// Gets the index. 
        /// </para>
        /// </summary>
        /// <value>The index of the selected <see cref="ToolbarItem"/>.</value>
        public int Index
        {
            get { return _index;  }   
        }

    }
}
