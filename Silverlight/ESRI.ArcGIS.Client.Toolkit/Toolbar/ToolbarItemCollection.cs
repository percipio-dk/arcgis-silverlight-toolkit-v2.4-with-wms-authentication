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
using System.Collections.ObjectModel;

namespace ESRI.ArcGIS.Client.Toolkit
{
    /// <summary>
    /// <para>
    /// <b>Note: This API is now obsolete.</b> This class is used with the Toolbar, which is deprecated.
    /// </para>
    /// <para>
    /// An observable <seealso cref="System.Collections.ObjectModel.ObservableCollection&lt;T&gt;"/> Collection of Toolbaritems
    /// </para>
    /// </summary>
    [Obsolete("This class is used with the Toolbar, which is deprecated.")]
    public class ToolbarItemCollection : ObservableCollection<ToolbarItem>
    {

    }
}
