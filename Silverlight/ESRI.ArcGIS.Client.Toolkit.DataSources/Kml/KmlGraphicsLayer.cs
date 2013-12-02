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

namespace ESRI.ArcGIS.Client.Toolkit.DataSources.Kml
{
	/// <summary>
	/// KML GraphicsLayer subclass for allowing legend based on the styles
	/// </summary>
	internal class KmlGraphicsLayer : GraphicsLayer
	{
		#region ILegendSupport Members

		private LayerLegendInfo _legendInfo; // legend based on the style. 
		internal LayerLegendInfo LegendInfo
		{
			private get { return _legendInfo; }
			set
			{
				if (_legendInfo != value)
				{
					_legendInfo = value;
					OnLegendChanged();
				}
			}
		}

		/// <summary>
		/// Queries for the legend infos of the layer.
		/// </summary>
		/// <remarks>
		/// The returned result is encapsulated in a <see cref="LayerLegendInfo" /> object.
		/// </remarks>
		/// <param name="callback">The method to call on completion.</param>
		/// <param name="errorCallback">The method to call in the event of an error.</param>
		public override void QueryLegendInfos(Action<LayerLegendInfo> callback, Action<Exception> errorCallback)
		{
			if (callback == null)
				return;

			// If a renderer has been set, use it for the legend
			if (Renderer != null)
				base.QueryLegendInfos(callback, errorCallback);
			else
				callback(LegendInfo);
		}

		#endregion
	}
}
