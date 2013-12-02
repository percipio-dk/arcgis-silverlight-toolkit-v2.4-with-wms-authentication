// (c) Copyright ESRI.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Windows;
using ESRI.ArcGIS.Client.Symbols;
using System.Collections.Generic;

namespace ESRI.ArcGIS.Client.Toolkit.DataSources
{
	/// <summary>
	/// GeoRSS Layer.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Only <a href="http://www.georss.org/simple">GeoRSS-simple</a> feeds are supported.
	/// Geometries are returned in Geographic WGS84. If you are displaying the feed
	/// on top of a map in a different projection, they must be reprojected manually 
	/// when the graphics collection gets features added.
	/// </para>
	/// <para>
	/// The graphic will not have a symbol associated with them. You should specify
	/// a renderer on this layer, or manually assign symbols to the graphics when
	/// the graphics collection gets features added.
	/// </para>
	/// <para>
	/// Recent earthquake's greater than M2.5 with map tips:<br/>
	/// <code Lang="XAML">
	/// &lt;esri:GeoRssLayer Source=&quot;http://earthquake.usgs.gov/earthquakes/catalogs/1day-M2.5.xml&quot; &gt;
	///	  &lt;esri:GeoRssLayer.Renderer&gt;
	///	    &lt;esri:SimpleRenderer Brush=&quot;Red&quot; /&gt;
	///	  &lt;/esri:GeoRssLayer.Renderer&gt;
	///	  &lt;esri:GeoRssLayer.MapTip&gt;
	///	    &lt;Border Padding=&quot;5&quot; Background=&quot;White&quot; esri:GraphicsLayer.MapTipHideDelay=&quot;0:0:0.5&quot;&gt;
	///	      &lt;StackPanel&gt;
	///	        &lt;TextBlock Text=&quot;{Binding [Title]}&quot; FontWeight=&quot;Bold&quot; FontSize=&quot;12&quot; /&gt;
	///	        &lt;TextBlock Text=&quot;{Binding [Summary]}&quot; FontSize=&quot;10&quot; /&gt;
	///	        &lt;HyperlinkButton Content=&quot;Link&quot; NavigateUri=&quot;{Binding [Link]}&quot; Opacity=&quot;.5&quot; FontSize=&quot;10&quot; TargetName=&quot;_blank&quot; /&gt;
	///	      &lt;/StackPanel&gt;
	///	    &lt;/Border&gt;
	///	  &lt;/esri:GeoRssLayer.MapTip&gt;
	/// &lt;/esri:GeoRssLayer&gt;
	/// </code>
	/// </para>
	/// <para>
	/// If you require a proxy, simply prefix the layer URI with a proxy prefix:<br/>
	/// <code Lang="XAML">
	/// &lt;esri:GeoRssLayer Source=&quot;../proxy.ashx?url=http://earthquake.usgs.gov/earthquakes/catalogs/1day-M2.5.xml&quot; /&gt;
	/// </code>
	/// </para>
	/// <para>
	/// The following attributes will be associated with each graphic:
    /// </para>
    /// <list type="bullet">
	/// 	<item>Title (<see cref="String"/>)</item>
	/// 	<item>Summary (<see cref="String"/>)</item> 
	/// 	<item>PublishDate (<see cref="DateTime"/>)</item>
	/// 	<item>Id (<see cref="String"/>)</item>
	/// 	<item>Link (<see cref="System.Uri"/>)</item>
	/// 	<item>FeedItem (<see cref="System.ServiceModel.Syndication.SyndicationItem"/>)</item>
	/// </list>
    /// <para>
	/// Optionally, if the item is using any of the simple-georss extensions,
	/// these will also be included:
    /// </para>
	/// <list type="bullet">
	///		<item>elev (<see cref="double"/>)</item>
	/// 	<item>floor (<see cref="System.Int32"/>)</item>
	/// 	<item>radius (<see cref="double"/>)</item>
	/// 	<item>featuretypetag (<see cref="string"/>)</item> 
	/// 	<item>relationshiptag (<see cref="string"/>)</item>
	/// 	<item>featurename (<see cref="string"/>)</item>
	/// </list>
    /// <para>
	/// The Graphic's <see cref="ESRI.ArcGIS.Client.Graphic.TimeExtent"/> property 
	/// will be set to a time instance matching the PublishDate.
    /// </para>
	/// </remarks>
	public sealed class GeoRssLayer : GraphicsLayer
    {
		GeoRssLoader loader;

        #region Constructor:
		/// <summary>
		/// Initializes a new instance of the <see cref="GeoRssLayer"/> class.
		/// </summary>
        public GeoRssLayer() : base()
        {
			loader = new GeoRssLoader();
			loader.LoadCompleted += loader_LoadCompleted;
			loader.LoadFailed += loader_LoadFailed;
        }

		private void loader_LoadFailed(object sender, GeoRssLoader.RssLoadFailedEventArgs e)
		{
			this.InitializationFailure = e.ex;
			if (!IsInitialized)
				base.Initialize();
		}

		private void loader_LoadCompleted(object sender, GeoRssLoader.RssLoadedEventArgs e)
		{
			this.Graphics = new GraphicCollection(e.Graphics);
			// GeoRSS-Simple requires geometries in WGS84 hence; setting layer Spatial Reference to 4326:
			this.SpatialReference = new Geometry.SpatialReference(4326);
			if(!IsInitialized)
				base.Initialize();
		}
        #endregion

        #region Overriden Methods:

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
            Update();
        }

		/// <summary>
		/// Called when the GraphicsSource property changes.
		/// </summary>
		/// <param name="oldValue">Old value of the GraphicsSource property.</param>
		/// <param name="newValue">New value of the GraphicsSource property.</param>
		/// <exception cref="InvalidOperationException">Thrown when <see cref="GraphicsLayer.GraphicsSource"/>property is changed on a <see cref="GeoRssLayer"/>.</exception>
		protected override void OnGraphicsSourceChanged(IEnumerable<Graphic> oldValue, IEnumerable<Graphic> newValue)
		{
			throw new InvalidOperationException(Properties.Resources.GraphicsLayer_GraphicsSourceCannotBeSetOnLayer);
		}

        #endregion

        #region Dependency Properties:

#if !SILVERLIGHT
        private System.Net.ICredentials credentials;
        /// <summary>
        /// Gets or sets the network credentials that are sent to the host and used to authenticate the request.
        /// </summary>
        /// <value>The credentials used for authentication.</value>
        public System.Net.ICredentials Credentials
        {
            get { return credentials; }
            set
            {
                if (credentials != value)
                {
                    credentials = value;
                    OnPropertyChanged("Credentials");
                }
            }
        }
#endif

		/// <summary>
		/// Gets or sets the URI for the RSS feed.
		/// </summary>
		public Uri Source
        {
			get { return ((Uri)GetValue(SourceProperty)); }
			set { SetValue(SourceProperty, value); }
        }

		/// <summary>
		/// Identifies the <see cref="Source"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty SourceProperty =
			DependencyProperty.Register("Source", typeof(Uri), typeof(GeoRssLayer), null);
        
        #endregion

		/// <summary>
		/// Reloads the RSS feed from the endpoint.
		/// </summary>
		public void Update()
		{
			if (Source != null)
			{
#if !SILVERLIGHT
				loader.LoadRss(Source, Credentials);
#else
                loader.LoadRss(Source);
#endif
			}
		}
	}
}
