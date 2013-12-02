// (c) Copyright ESRI.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using System.Windows;

namespace ESRI.ArcGIS.Client.Toolkit.DataSources
{
    /// <summary>OpenStreetMap tiled layer.  Note, use of the OpenStreetMapLayer in your map application requires attribution.  Please read
    /// the <a href="http://www.openstreetmap.org">usage guidelines</a> for using OpenStreetMap tile layers in your application.</summary>
    /// <remarks>
    /// 	<para>To use an OpenStreetMap tile layer in your map, add the OpenStreetMapLayer, select a style, and add attribution. </para>
    /// 	<code language="XAML">
    /// &lt;esri:Map x:Name="MyMap"&gt;
    ///     &lt;esri:OpenStreetMapLayer Style="Mapnik" /&gt;
    /// &lt;/esri:Map&gt;<br/>&lt;esri:Attribution Layers="{Binding ElementName=MyMap, Path=Layers}" /&gt;
    /// </code>
    /// 	<para>When including the OpenStreetMapLayer in your map application, you must also include attribution.  For the latest information, please read the <a href="http://www.openstreetmap.org">usage guidelines</a> for using OpenStreetMap tile layers in your application.</para>
    /// 	<para>OpenStreetMap is released under the Create Commons "Attribution-Share Alike 2.0 Generic" license.</para>
    /// </remarks>
	public class OpenStreetMapLayer : TiledMapServiceLayer, IAttribution
#if WINDOWS_PHONE
		, ITileCache
#endif
	{
		/// <summary>Available subdomains for tiles.</summary>
		private static string[] subDomains = { "a", "b", "c" };
		/// <summary>Base URL used in GetTileUrl.</summary>
		private static string[] baseUrl =
		{
			"http://{0}.tile.openstreetmap.org/{1}/{2}/{3}.png",
			"http://{0}.tah.openstreetmap.org/Tiles/tile/{1}/{2}/{3}.png",
			"http://{0}.tile.opencyclemap.org/cycle/{1}/{2}/{3}.png",
			"http://{0}.tile.cloudmade.com/fd093e52f0965d46bb1c6c6281022199/3/256/{1}/{2}/{3}.png",
		};
		/// <summary>Simple constant used for full extent and tile origin specific to this projection.</summary>
		private const double cornerCoordinate = 20037508.3427892;
		/// <summary>ESRI Spatial Reference ID for Web Mercator.</summary>
		private const int WKID = 102100;

		/// <summary>
		/// Initializes a new instance of the <see cref="OpenStreetMapLayer"/> class.
		/// </summary>
		public OpenStreetMapLayer()
		{
			Style = MapStyle.Mapnik;
			this.SpatialReference = new SpatialReference(WKID);
		}

		/// <summary>
		/// Initializes the <see cref="OpenStreetMapLayer"/> class.
		/// </summary>
		static OpenStreetMapLayer()
		{
			CreateAttributionTemplate();
		}

		/// <summary>
		/// Initializes the resource.
		/// </summary>
		/// <remarks>
		/// 	<para>Override this method if your resource requires asyncronous requests to initialize,
		/// and call the base method when initialization is completed.</para>
		/// 	<para>Upon completion of initialization, check the <see cref="ESRI.ArcGIS.Client.Layer.InitializationFailure"/> for any possible errors.</para>
		/// </remarks>
		/// <seealso cref="ESRI.ArcGIS.Client.Layer.Initialized"/>
		/// <seealso cref="ESRI.ArcGIS.Client.Layer.InitializationFailure"/>
		public override void Initialize()
		{
			//Full extent fo the layer
			this.FullExtent = new ESRI.ArcGIS.Client.Geometry.Envelope(-cornerCoordinate,-cornerCoordinate,cornerCoordinate,cornerCoordinate)
			{
				SpatialReference = new SpatialReference(WKID)
			};
			//This layer's spatial reference
			//Set up tile information. Each tile is 256x256px, 19 levels.
			this.TileInfo = new TileInfo()
			{
				Height = 256,
				Width = 256,
				Origin = new MapPoint(-cornerCoordinate, cornerCoordinate) { SpatialReference = new ESRI.ArcGIS.Client.Geometry.SpatialReference(WKID) },
				Lods = new Lod[19]
			};
			//Set the resolutions for each level. Each level is half the resolution of the previous one.
			double resolution = cornerCoordinate * 2 / 256;
			for (int i = 0; i < TileInfo.Lods.Length; i++)
			{
				TileInfo.Lods[i] = new Lod() { Resolution = resolution };
				resolution /= 2;
			}
			//Call base initialize to raise the initialization event
			base.Initialize();
		}
		
		/// <summary>
		/// Returns a url to the specified tile
		/// </summary>
		/// <param name="level">Layer level</param>
		/// <param name="row">Tile row</param>
		/// <param name="col">Tile column</param>
		/// <returns>URL to the tile image</returns>
		public override string GetTileUrl(int level, int row, int col)
		{
			// Select a subdomain based on level/row/column so that it will always
			// be the same for a specific tile. Multiple subdomains allows the user
			// to load more tiles simultanously. To take advantage of the browser cache
			// the following expression also makes sure that a specific tile will always 
			// hit the same subdomain.
			string subdomain = subDomains[(level + col + row) % subDomains.Length];
			return string.Format(baseUrl[(int)Style], subdomain, level, col, row);
		}

		/// <summary>
		/// Gets or sets the map style.
		/// </summary>
		public MapStyle Style
		{
			get { return (MapStyle)GetValue(StyleProperty); }
			set { SetValue(StyleProperty, value); }
		}

		/// <summary>
		/// Identifies the <see cref="Style"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty StyleProperty =
			DependencyProperty.Register("Style", typeof(MapStyle), typeof(OpenStreetMapLayer), new PropertyMetadata(MapStyle.Mapnik, OnStylePropertyChanged));

		private static void OnStylePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			OpenStreetMapLayer obj = (OpenStreetMapLayer)d;
			if (obj.IsInitialized)
				obj.Refresh();
		}

		#region IAttribution Members
		private const string template = @"<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
			<TextBlock Text=""Map data © OpenStreetMap contributors, CC-BY-SA"" TextWrapping=""Wrap""/></DataTemplate>";

		private static DataTemplate _attributionTemplate;
		private static void CreateAttributionTemplate()
		{
#if SILVERLIGHT
			_attributionTemplate = System.Windows.Markup.XamlReader.Load(template) as DataTemplate;
#else
			using (System.IO.MemoryStream stream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(template)))
			{
				_attributionTemplate = System.Windows.Markup.XamlReader.Load(stream) as DataTemplate;
			}
#endif
		}

		/// <summary>
		/// Gets the attribution template of the layer.
		/// </summary>
		/// <value>The attribution template.</value>
		public DataTemplate AttributionTemplate
		{
			get { return _attributionTemplate; }
		}

		#endregion

		/// <summary>
		/// MapStyle
		/// </summary>
		public enum MapStyle : int
		{
			/// <summary>
			/// Mapnik
			/// </summary>
			Mapnik = 0,
			/// <summary>
			/// Osmarender
			/// </summary>
			Osmarender = 1,
			/// <summary>
			/// Cycle Map
			/// </summary>
			CycleMap = 2,
			/// <summary>
			/// No Name
			/// </summary>
			NoName = 3
		}
#if WINDOWS_PHONE
		#region ITileCache Members

		bool ITileCache.PersistCacheAcrossSessions
		{
			get { return true; }
		}

		string ITileCache.CacheUid
		{
			get
			{
				return string.Format("ESRI.ArcGIS.Client.Toolkit.DataSources.OpenStreetMapLayer_{0}", this.Style);
			}
		}

		#endregion
#endif
	}
}
