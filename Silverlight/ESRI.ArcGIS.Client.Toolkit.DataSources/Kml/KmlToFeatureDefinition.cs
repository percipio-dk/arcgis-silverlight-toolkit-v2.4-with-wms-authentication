// (c) Copyright ESRI.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Xml.Linq;
using ESRI.ArcGIS.Client.Geometry;
#if !SILVERLIGHT
using ESRI.ArcGIS.Client.Toolkit.DataSources.Kml.Zip;
using System.Xml;
#endif

namespace ESRI.ArcGIS.Client.Toolkit.DataSources.Kml
{

	internal class ContainerInfo
	{
		public XElement Element { get; set; }
		public string Name { get; set; }
		public bool Visible { get; set; }
		public string AtomAuthor { get; set; }
		public Uri AtomHref { get; set; }
		public double RefreshInterval { get; set; }
		public string Url { get; set; } // Only for NetworkLinks
		public int FolderId { get; set; } // internal folderId useful for the webmaps
	}

    /// <summary>
    /// Converts a KML document into a FeatureDefinition.
    /// </summary>
    internal class KmlToFeatureDefinition
    {
        #region Private Classes
        /// <summary>
        /// This stores the state of the currently processed KML feature while its style information is
        /// downloaded from an external source.
        /// </summary>
        private class DownloadStyleState
        {
            public string StyleId { get; set; }
            public System.Net.ICredentials Credentials { get; set; }
			public Action<KMLStyle> Callback { get; set; }

			public DownloadStyleState(string styleId, System.Net.ICredentials credentials, Action<KMLStyle> callback)
			{
				StyleId = styleId;
				Credentials = credentials;
				Callback = callback;
			}
		}

		/// <summary>
		/// Helper class to wait for the end of styles downloads
		/// </summary>
		private class WaitHelper
		{
			private int _nbDownloading;
			private ManualResetEvent _waitHandle;

			public void Reset()
			{
				_nbDownloading = 0;
				_waitHandle = null;
			}

			public void AddOne()
			{
				_nbDownloading++;
				if (_waitHandle == null)
					_waitHandle = new ManualResetEvent(false);
				else
					_waitHandle.Reset();
			}

			public void OneDone()
			{
				_nbDownloading--;
				Debug.Assert(_nbDownloading >= 0 && _waitHandle != null);
				if (_nbDownloading == 0)
					_waitHandle.Set();
			}

			/// <summary>
			/// Waits for the styles.
			/// </summary>
			public void Wait()
			{
				if (_waitHandle != null)
					_waitHandle.WaitOne();
			}

		}
		#endregion

        #region Private Members
        private static readonly XNamespace atomNS = "http://www.w3.org/2005/Atom";

        // The output of the conversion process is this object. It will contain the metadata for all
        // graphic features and will later be used to convert that information into elements that are
        // added to a graphics layer. This separation of effort was done to facilitate running the code
        // in this process on a background worker thread while the creation of graphic elements and their
        // associated brushes and UI components to be done on the UI thread.
        internal FeatureDefinition featureDefs;

        /// <summary>
        /// Optional. Gets or sets the URL to a proxy service that brokers Web requests between the Silverlight 
        /// client and a KML file.  Use a proxy service when the KML file is not hosted on a site that provides
        /// a cross domain policy file (clientaccesspolicy.xml or crossdomain.xml).
        /// </summary>
        /// <value>The Proxy URL string.</value>
        private string ProxyUrl { get; set; }

    	private readonly WaitHelper _waitHelper = new WaitHelper();
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="KmlToFeatureDefinition"/> class.
        /// </summary>
        public KmlToFeatureDefinition(string proxyUrl)
        {
            // Instantiate feature definition object that will contain metadata for all supported element
            // types in the KML
            featureDefs = new FeatureDefinition();

            ProxyUrl = proxyUrl;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Takes features in the KML element and converts them into equivalent features
        /// and adds them to the FeatureDefinition.
        /// Only the direct children of the KML element are converted.
        /// </summary>
		/// <param name="context">Context containing the XElement with the KML definition to be converted.</param>
		/// <param name="credentials">The credentials.</param>
		/// <returns></returns>
        public FeatureDefinition Convert(KmlLayerContext context, System.Net.ICredentials credentials = null)
        {
			XElement xElement = context.Element;
			XNamespace kmlNS = xElement.Name.Namespace;
			_waitHelper.Reset();

            // Remove any existing features so only those contained in the input KML element file are stored
            featureDefs.Clear();

			// Process the styles if they are not already known (the styles are shared by all folders/documents, so process them only once)
			if (context.Styles == null)
			{
				featureDefs.styles = new Dictionary<string, KMLStyle>();

				// Find all Style elements that have an ID and can thus be referenced by other styleURLs.
				IEnumerable<XElement> styles = xElement.Descendants().Where(e => e.Name.LocalName == "Style" && (string)e.Attribute("id") != null);
				foreach (XElement style in styles)
				{
					KMLStyle kmlStyle = new KMLStyle();
					GetStyle(style, kmlStyle);
					featureDefs.AddStyle(kmlStyle.StyleId, kmlStyle);
				}

				// Find all StyleMap elements that have an ID and can thus be referenced by other styleURLs.
				IEnumerable<XElement> styleMaps = xElement.Descendants().Where(e => e.Name.LocalName == "StyleMap" && (string)e.Attribute("id") != null);
				foreach (XElement map in styleMaps)
				{
					// A style map may need to download styles in other documents.
					// Need to use asynchronous pattern.
					GetStyleMapAsync(map, null, credentials
					                 , kmlStyle =>
					                   	{
					                   		if (kmlStyle != null)
					                   			featureDefs.AddStyle(kmlStyle.StyleId, kmlStyle);
					                   	});
				}

				// Wait for getting all styles before creating the feature definition
				_waitHelper.Wait();
			}
			else
			{
				foreach (var style in context.Styles)
					featureDefs.AddStyle(style.Key, style.Value);
			}

			// Process the optional NetworkLinkControl
			XElement networkLinkControl = xElement.Element(kmlNS + "NetworkLinkControl");
			if (networkLinkControl != null)
			{
				featureDefs.networkLinkControl = new NetworkLinkControl();
				XElement minRefreshPeriod = networkLinkControl.Element(kmlNS + "minRefreshPeriod");
				if (minRefreshPeriod != null)
					featureDefs.networkLinkControl.MinRefreshPeriod = GetDoubleValue(minRefreshPeriod);
			}

			// Find the containers which will be represented by a sublayer i.e Document/Folder/NetworkLink
			foreach (XElement container in xElement.Elements().Where(element => element.Name.LocalName == "Folder" || element.Name.LocalName == "Document" || element.Name.LocalName == "NetworkLink"))
			{
				ContainerInfo containerInfo = new ContainerInfo
				                         	{
				                         		Element = container,
				                         		Url = null, // only for networklink
				                         		Visible = true,
				                         		AtomAuthor = context.AtomAuthor, // Use parent value by default
				                         		AtomHref = context.AtomHref  // Use parent value by default
				                         	};

				XNamespace kmlContainerNS = container.Name.Namespace;
				if (container.Name.LocalName == "NetworkLink")
				{
					string hrefValue = "";
					string composite = "";
					string layerids = "";

					// Link takes precedence over Url from KML version 2.1 and later:
					XElement url = container.Element(kmlContainerNS + "Link") ?? container.Element(kmlContainerNS + "Url");
					if (url != null)
					{
						XElement href = url.Element(kmlContainerNS + "href");
						if (href != null)
						{
							hrefValue = href.Value;
						}

						// This next section is to parse special elements that only occur when an ArcGIS Server KML 
						// is to be processed.
						XElement view = url.Element(kmlContainerNS + "viewFormat");
						if (view != null)
						{
							int begIdx = view.Value.IndexOf("Composite");
							if (begIdx != -1)
							{
								int endIdx = view.Value.IndexOf("&", begIdx);
								if (endIdx != -1)
									composite = view.Value.Substring(begIdx, endIdx - begIdx);
							}

							begIdx = view.Value.IndexOf("LayerIDs");
							if (begIdx != -1)
							{
								int endIdx = view.Value.IndexOf("&", begIdx);
								if (endIdx != -1)
									layerids = view.Value.Substring(begIdx, endIdx - begIdx);
							}
						}

						// If network link URL is successfully extracted, then add to container list
						if (!String.IsNullOrEmpty(hrefValue))
						{
							// extract refreshInterval
							XElement refreshMode = url.Element(kmlContainerNS + "refreshMode");
							if (refreshMode != null && refreshMode.Value == "onInterval")
							{
								XElement refreshInterval = url.Element(kmlContainerNS + "refreshInterval");
								if (refreshInterval != null)
									containerInfo.RefreshInterval = GetDoubleValue(refreshInterval);
								else
									containerInfo.RefreshInterval = 4; // default value 
							}


							// the following values are for processing specialized ArcGIS Server KML links
							// generated from REST endpoints.
							if (!String.IsNullOrEmpty(composite))
								hrefValue += "?" + composite;

							if (!String.IsNullOrEmpty(layerids))
							{
								if (!String.IsNullOrEmpty(hrefValue))
									hrefValue += "&" + layerids;
								else
									hrefValue += "?" + layerids;
							}
							containerInfo.Url = hrefValue;

						}
						else
							containerInfo = null; // Link without href. Should not happen. Skip it.
					}
					else
						containerInfo = null; // NetworkLink without Link/Url. Should not happen. Skip it.
				}
				else
				{
					// Folder or Document XElement 
					XElement linkElement = container.Elements(atomNS + "link").Where(element => element.HasAttributes).FirstOrDefault();
					if (linkElement != null)
					{
						// Overwrite global default value only upon successful extraction from element
						string tempHref = GetAtomHref(linkElement);
						if (!String.IsNullOrEmpty(tempHref))
							containerInfo.AtomHref = new Uri(tempHref);
					}

					XElement authorElement = container.Element(atomNS + "author");
					if (authorElement != null)
					{
						// Overwrite global default value only upon successful extraction from element
						string tempAuthor = GetAtomAuthor(authorElement);
						if (!String.IsNullOrEmpty(tempAuthor))
							containerInfo.AtomAuthor = tempAuthor;
					}
				}

				if (containerInfo != null)
				{
					XElement visibilityElement = container.Element(kmlContainerNS + "visibility");
					if (visibilityElement != null)
					{
						containerInfo.Visible = GetBooleanValue(visibilityElement);
					}

					XElement nameElement = container.Element(kmlContainerNS + "name");
					if (nameElement != null)
					{
						containerInfo.Name = nameElement.Value;
					}

					if (container.HasAttributes && container.Attribute(KmlLayer.FolderIdAttributeName) != null)
					{
						containerInfo.FolderId = (int)container.Attribute(KmlLayer.FolderIdAttributeName);
					}

					featureDefs.AddContainer(containerInfo);
				}
			}


            // Process all children placemarks or groundoverlays
			foreach (XElement element in xElement.Elements().Where(element => element.Name == kmlNS + "Placemark" || element.Name == kmlNS + "GroundOverlay" ))
            {
                // Establish baseline style if a "styleUrl" setting is present
                XElement styleElement = element.Element(kmlNS + "styleUrl");
				if (styleElement != null)
				{
					// get the style asynchronously and create the feature definition as soon as the style is there
					XElement featureElement = element;
					GetStyleUrlAsync(styleElement.Value, null, credentials, kmlStyle => CreateFeatureDefinition(kmlStyle, featureElement, null, context));
				}
				else
				{
					// Create feature definition synchronously using default KML style, meta data and placemark information
					CreateFeatureDefinition(null, element, null, context);
				}
            }

			// Get the name of the XElement
			XElement nameXElement = xElement.Element(kmlNS + "name");
			if (nameXElement != null && string.IsNullOrEmpty(featureDefs.name))
			{
				featureDefs.name = nameXElement.Value;
			}

			// At this point, some inner styles are possibly on the way to being downloaded and so the feature definitions are not created yet
			// Wait for all downloads to be sure all feature definitions are created before terminating the background worker
        	_waitHelper.Wait();

        	int folderId = 0;
			if (xElement.HasAttributes && xElement.Attribute(KmlLayer.FolderIdAttributeName) != null)
			{
				folderId = (int)xElement.Attribute(KmlLayer.FolderIdAttributeName);
			}

			if (!featureDefs.groundOverlays.Any() && !featureDefs.placemarks.Any() && featureDefs.containers.Count() == 1 && folderId == 0
				&& string.IsNullOrEmpty(featureDefs.containers.First().Url))
			{
				// Avoid useless level when there is no groundoverlay, no placemark and only one folder or document at the root level
				ContainerInfo container = featureDefs.containers.First();
				Dictionary<string, KMLStyle> styles = featureDefs.styles.ToDictionary(style => style.Key, style => style.Value);

				KmlLayerContext childContext = new KmlLayerContext
						                      	{
						                      		Element = container.Element, // The XElement that the KML layer has to process
						                      		Styles = styles,
						                      		Images = context.Images,
						                      		AtomAuthor = container.AtomAuthor,
						                      		AtomHref = container.AtomHref
						                      	};

				featureDefs.hasRootContainer = true;
				return Convert(childContext, credentials);
			}

			return featureDefs;
        }
        #endregion

        #region Private Methods

		/// <summary>
    	/// Downloads KML file containing style, extracts style and creates feature definitions.
    	/// </summary>
    	/// <param name="styleUrl">Style id to locate in file.</param>
    	/// <param name="credentials">The credentials.</param>
    	/// <param name="callback">Callback to execture with the downloaded style</param>
    	/// <returns></returns>
    	private void DownloadStyleAsync(string styleUrl, System.Net.ICredentials credentials, Action<KMLStyle> callback)
		{
			// We can only download KML/KMZ files that are stored remotely, not on the local file system
			if (styleUrl.StartsWith("http://") || styleUrl.StartsWith("https://"))
			{
				// Split style into file URL and style id
				string[] tokens = styleUrl.Split('#');
				if (tokens.Length == 2)
				{
					// Store current state so event handler can resume
					DownloadStyleState state = new DownloadStyleState('#' + tokens[1], credentials, callback);
					WebClient webClient = new WebClient();

					if (credentials != null)
						webClient.Credentials = credentials;

					webClient.OpenReadCompleted += StyleDownloaded;
					webClient.OpenReadAsync(Utilities.PrefixProxy(ProxyUrl, tokens[0]), state);
					_waitHelper.AddOne();
					return;
				}
			}

			// Incorrect styleUrl : execute the callback with a null style
			// Thsi will use the default style to process the placemark.
    		callback(null); 
		}

        /// <summary>
        /// Event handler invoked when an external KML file containing a style definition has been downloaded.
        /// </summary>
        /// <param name="sender">Sending object.</param>
        /// <param name="e">Event arguments including error information and the input stream.</param>
		private void StyleDownloaded(object sender, OpenReadCompletedEventArgs e)
		{
			DownloadStyleState state = (DownloadStyleState)e.UserState;

			if (sender is WebClient)
				((WebClient) sender).OpenReadCompleted -= StyleDownloaded; // free the webclient

			// If there is no error downloading the KML/KMZ file, then load it into an XDocument and find
			// the style id within.
			XDocument xDoc = null;
			ZipFile zipFile = null;
			if (e.Error == null)
			{
				Stream seekableStream = KmlLayer.ConvertToSeekable(e.Result);
				if (seekableStream != null)
				{
					if (KmlLayer.IsStreamCompressed(seekableStream))
					{
						// Feed result into ZIP library
						// Extract KML file embedded within KMZ file
						zipFile = ZipFile.Read(seekableStream);
						xDoc = GetKmzContents(zipFile);
					}
					else
					{
						try
						{
#if SILVERLIGHT
							xDoc = XDocument.Load(seekableStream, LoadOptions.None);
#else
							xDoc = XDocument.Load(System.Xml.XmlReader.Create(seekableStream));
#endif
						}
						catch
						{
							xDoc = null;
						}
					}
				}
			}

			if (xDoc == null)
			{
				// Error while getting the style : execute the callback with a null style
				if (zipFile != null)
				   zipFile.Dispose();
				state.Callback(null);
			}
			else
			{
				// Look for the style in the downloaded document
				// This may need to download other documents recursively. 
				GetStyleUrlAsync(state.StyleId, xDoc, state.Credentials, kmlStyle => StoreZipfileAndCallback(kmlStyle, state.Callback, zipFile));
			}

			_waitHelper.OneDone();
		}

		private static void StoreZipfileAndCallback(KMLStyle kmlStyle, Action<KMLStyle> callback, ZipFile zipFile)
		{
			if (zipFile != null)
			{
				if (kmlStyle.ZipFile == null && !String.IsNullOrEmpty(kmlStyle.IconHref))
				{
					kmlStyle.ZipFile = zipFile;
				}
				else
				{
					zipFile.Dispose();
				}
			}
			callback(kmlStyle);
		}

		/// <summary>
		/// Processes each file in the ZIP stream, storing images in a dictionary and load the KML contents
		/// into an XDocument.
		/// </summary>
		/// <param name="zipFile">Decompressed stream from KMZ.</param>
		/// <returns>XDocument containing KML content from the KMZ source.</returns>
		private static XDocument GetKmzContents(ZipFile zipFile)
		{
			XDocument xDoc = null;

			// Process each file in the archive
			foreach (string filename in zipFile.EntryFileNames)
			{
				// Determine where the last "." character exists in the filename and if is does not appear
				// at all, then skip the file.
				int lastPeriod = filename.LastIndexOf(".");
				if (lastPeriod == -1)
					continue;

#if SILVERLIGHT
				Stream ms = zipFile.GetFileStream(filename);
#else
				MemoryStream ms = new MemoryStream();
				zipFile.Extract(filename, ms);
#endif
				if (ms == null) continue;
				ms.Seek(0, SeekOrigin.Begin);

				switch (filename.Substring(lastPeriod).ToLower())
				{
					case ".kml":
						// Create the XDocument object from the input stream
						try
						{
#if SILVERLIGHT
							xDoc = XDocument.Load(ms, LoadOptions.None);
#else
							xDoc = XDocument.Load(XmlReader.Create(ms));
#endif
						}
						catch
						{
							xDoc = null;
						}
						break;
				}
			}

			return xDoc;
		}

		private void CreateFeatureDefinition(KMLStyle kmlStyle, XElement feature, XElement geometry, KmlLayerContext context)
        {
			if (feature == null)
				return; // should not happen

			XNamespace kmlNS = feature.Name.Namespace;
			if (feature.Name.LocalName == "Placemark")
			{
				// kmlStyle is null when the placemark doesn't reference any shared style (or a shared style that we are not able to download)
				// in this case, use a default style
				if (kmlStyle == null)
					kmlStyle = new KMLStyle();

				// Determine what kind of feature is present in the placemark. If an input geometry is present, then the
				// style has already been determined and this method is being called recursively for each child element
				// of a multi-geometry placemarker.
				XElement geomElement = null;
				if (geometry != null)
				{
					geomElement = geometry;
				}
				else
				{
					geomElement = GetFeatureType(feature);

					// Override any settings from the inline style "Style" node
					XElement styleElement = feature.Element(kmlNS + "Style");
					if (styleElement != null)
					{
						GetStyle(styleElement, kmlStyle);
					}
				}

				PlacemarkDescriptor fd = null;

				if (geomElement != null && geomElement.Name != null)
				{
					switch (geomElement.Name.LocalName)
					{
						case "Point":
							fd = ExtractPoint(kmlStyle, geomElement);
							break;

						case "LineString":
							fd = ExtractPolyLine(kmlStyle, geomElement);
							break;

						case "LinearRing":
							fd = ExtractLinearRing(kmlStyle, geomElement);
							break;

						case "Polygon":
							fd = ExtractPolygon(kmlStyle, geomElement);
							break;

						case "MultiGeometry":
							foreach (XElement item in geomElement.Elements())
							{
								// Use recursion to walk the hierarchy of embedded definitions
								CreateFeatureDefinition(kmlStyle, feature, item, context);
							}
							break;

						case "LatLonBox":
							ExtractFeatureStyleInfo(kmlStyle, feature);
							fd = ExtractLatLonBox(kmlStyle, geomElement);
							break;
					}

					// If a feature definition was created, then assign attributes and add to collection
					if (fd != null)
					{
						if (fd.Geometry != null)
							fd.Geometry.SpatialReference = new SpatialReference(4326);

						XElement descElement = feature.Element(kmlNS + "description");
						if (descElement != null)
							fd.Attributes.Add("description", descElement.Value);

						XElement nameElement = feature.Element(kmlNS + "name");
						if (nameElement != null)
							fd.Attributes.Add("name", nameElement.Value);

						if (atomNS != null)
						{
							// Initialize to parent value
							Uri atomHrefValue = context.AtomHref;

							// If node exists, has attributes, and can be successfully extracted, then extract
							// this value.
							XElement atomHrefElement = feature.Element(atomNS + "link");
							if (atomHrefElement != null && atomHrefElement.HasAttributes)
							{
								string tempHref = GetAtomHref(atomHrefElement);
								if (!String.IsNullOrEmpty(tempHref))
									atomHrefValue = new Uri(tempHref);
							}

							// If a value was extracted or assigned from a parent, then add to attributes
							if (atomHrefValue != null)
								fd.Attributes.Add("atomHref", atomHrefValue);

							// AtomAuthor : Initialize to parent value
							string atomValue = context.AtomAuthor;

							// If node exists, has attributes, and can be successfully extracted, then extract
							// this value.
							XElement atomAuthorElement = feature.Element(atomNS + "author");
							if (atomAuthorElement != null)
							{
								string tempAuthor = GetAtomAuthor(atomAuthorElement);
								if (!String.IsNullOrEmpty(tempAuthor))
									atomValue = tempAuthor;
							}

							// If a value was extracted or assigned from a parent, then add to attributes
							if (!String.IsNullOrEmpty(atomValue))
								fd.Attributes.Add("atomAuthor", atomValue);
						}

						// Extract extended information
						XElement extendedDataElement = feature.Element(kmlNS + "ExtendedData");
						if (extendedDataElement != null)
						{
							List<KmlExtendedData> extendedList = new List<KmlExtendedData>();
							IEnumerable<XElement> dataElements =
								from e in extendedDataElement.Descendants(kmlNS + "Data")
								select e;
							foreach (XElement data in dataElements)
							{
								XAttribute name = data.Attribute("name");
								if (name != null)
								{
									KmlExtendedData listItem = new KmlExtendedData();
									listItem.Name = name.Value;

									foreach (XElement dataChild in data.Descendants())
									{
										if (dataChild.Name == kmlNS + "displayName")
											listItem.DisplayName = dataChild.Value;
										else if (dataChild.Name == kmlNS + "value")
											listItem.Value = dataChild.Value;
									}

									extendedList.Add(listItem);
								}
							}

							if (extendedList.Count > 0)
								fd.Attributes.Add("extendedData", extendedList);
						}

						featureDefs.AddPlacemark(fd);
					}
				}
			}
			else if (feature.Name.LocalName == "GroundOverlay")
			{
				XElement latLonBoxElement = feature.Element(kmlNS + "LatLonBox");

				if (latLonBoxElement != null)
				{
					GroundOverlayDescriptor fd = new GroundOverlayDescriptor();

					fd.Envelope = ExtractEnvelope(latLonBoxElement);

					XElement rotationElement = latLonBoxElement.Element(kmlNS + "rotation");
					if (rotationElement != null)
						fd.Rotation = GetDoubleValue(rotationElement);

					XElement colorElement = feature.Element(kmlNS + "color");
					if (colorElement != null)
						fd.Color = GetColorFromHexString(colorElement.Value);
					else
						fd.Color = System.Windows.Media.Colors.White; // Default = white

					XElement iconElement = feature.Element(kmlNS + "Icon");
					if (iconElement != null)
					{
						XElement href = iconElement.Element(kmlNS + "href");
						if (href != null)
						{
							fd.IconHref = href.Value;
						}
					}

					featureDefs.AddGroundOverlay(fd);
				}
			}
        }

        /// <summary>
        /// Extracts the feature element from the Placemark.
        /// </summary>
        /// <param name="element">Placemark node that may contain a supported feature type node.</param>
        /// <returns>XElement node containing a supported feature type definition.</returns>
        private static XElement GetFeatureType(XElement element)
        {
            string[] featureTypes = { "Point", "LineString", "LinearRing", "Polygon", "MultiGeometry", "LatLonBox" };

        	return element.Elements().FirstOrDefault(e => featureTypes.Contains(e.Name.LocalName));
        }

        private static void ExtractFeatureStyleInfo(KMLStyle kmlStyle, XElement placemark)
        {
        	XNamespace kmlNS = placemark.Name.Namespace;
            XElement colorElement = placemark.Element(kmlNS + "color");
            if (colorElement != null)
                kmlStyle.PolyFillColor = GetColorFromHexString(colorElement.Value);

            XElement iconElement = placemark.Element(kmlNS + "Icon");
            if (iconElement != null)
                kmlStyle.IconHref = iconElement.Value.Trim();
        }

        /// <summary>
        /// Extracts a polygon from the input element and applies style information to the placemark descriptor.
        /// </summary>
        /// <param name="kmlStyle">KML Style information.</param>
        /// <param name="geomElement">Polygon geometry information.</param>
		/// <returns>A PlacemarkDescriptor object representing the feature.</returns>
        private static PlacemarkDescriptor ExtractLatLonBox(KMLStyle kmlStyle, XElement geomElement)
        {
			XNamespace kmlNS = geomElement.Name.Namespace;
			ESRI.ArcGIS.Client.Geometry.Polygon polygon = new Polygon();
            double? north = null, south = null, east = null, west = null;
            double temp;
            XElement boundary;

            // Extract box values
            boundary = geomElement.Element(kmlNS + "north");
            if (boundary != null)
            {
                if (double.TryParse(boundary.Value, System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out temp))
                    north = temp;
            }
            boundary = geomElement.Element(kmlNS + "south");
            if (boundary != null)
            {
                if (double.TryParse(boundary.Value, System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out temp))
                    south = temp;
            }
            boundary = geomElement.Element(kmlNS + "east");
            if (boundary != null)
            {
                if (double.TryParse(boundary.Value, System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out temp))
                    east = temp;
            }
            boundary = geomElement.Element(kmlNS + "west");
            if (boundary != null)
            {
                if (double.TryParse(boundary.Value, System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out temp))
                    west = temp;
            }

            if (north.HasValue && south.HasValue && east.HasValue && west.HasValue)
            {
                ESRI.ArcGIS.Client.Geometry.PointCollection pts = new PointCollection();
                MapPoint mp1 = new MapPoint(west.Value, north.Value);
                pts.Add(mp1);
                MapPoint mp2 = new MapPoint(east.Value, north.Value);
                pts.Add(mp2);
                MapPoint mp3 = new MapPoint(east.Value, south.Value);
                pts.Add(mp3);
                MapPoint mp4 = new MapPoint(west.Value, south.Value);
                pts.Add(mp4);

                polygon.Rings.Add(pts);

                // Create symbol and use style information
                PolygonSymbolDescriptor sym = new PolygonSymbolDescriptor();
                sym.style = kmlStyle;

                // Create feature descriptor from geometry and other information
                return new PlacemarkDescriptor()
                {
                    Geometry = polygon,
                    Symbol = sym
                };
            }

            return null;
        }

		/// <summary>
		/// Extracts an envelope from the input element.
		/// </summary>
		/// <param name="geomElement">LatLonBox geometry information.</param>
		/// <returns>An envelope.</returns>
		private static Envelope ExtractEnvelope(XElement geomElement)
		{
			XNamespace kmlNS = geomElement.Name.Namespace;
			double? north = null, south = null, east = null, west = null;
			double temp;
			XElement boundary;

			// Extract box values
			boundary = geomElement.Element(kmlNS + "north");
			if (boundary != null)
			{
				if (double.TryParse(boundary.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out temp))
					north = temp;
			}
			boundary = geomElement.Element(kmlNS + "south");
			if (boundary != null)
			{
				if (double.TryParse(boundary.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out temp))
					south = temp;
			}
			boundary = geomElement.Element(kmlNS + "east");
			if (boundary != null)
			{
				if (double.TryParse(boundary.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out temp))
					east = temp;
			}
			boundary = geomElement.Element(kmlNS + "west");
			if (boundary != null)
			{
				if (double.TryParse(boundary.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out temp))
					west = temp;
			}

			return north.HasValue && south.HasValue && east.HasValue && west.HasValue
			       	? new Envelope(west.Value, south.Value, east.Value, north.Value)
			       	: null;
		}

        /// <summary>
        /// Extracts a polygon from the input element and applies style information to the placemark descriptor.
        /// </summary>
        /// <param name="kmlStyle">KML Style information.</param>
        /// <param name="geomElement">Polygon geometry information.</param>
        /// <returns>A PlacemarkDescriptor object representing the feature.</returns>
        private static PlacemarkDescriptor ExtractPolygon(KMLStyle kmlStyle, XElement geomElement)
        {
			XNamespace kmlNS = geomElement.Name.Namespace;
			ESRI.ArcGIS.Client.Geometry.Polygon polygon = new Polygon();

            // Extract outer polygon boundary
            XElement boundary;
            boundary = geomElement.Element(kmlNS + "outerBoundaryIs");
            if (boundary != null)
            {
                ESRI.ArcGIS.Client.Geometry.PointCollection pts = ExtractRing(boundary);
                if (pts != null && pts.Count > 0)
                {
                    polygon.Rings.Add(pts);
                }
            }

            // Extract holes (if any)
            IEnumerable<XElement> holes =
                from e in geomElement.Descendants(kmlNS + "innerBoundaryIs")
                select e;
            foreach (XElement hole in holes)
            {
                ESRI.ArcGIS.Client.Geometry.PointCollection pts = ExtractRing(hole);
                if (pts != null && pts.Count > 0)
                {
                    polygon.Rings.Add(pts);
                }
            }
            
            // Create symbol and use style information
            PolygonSymbolDescriptor sym = new PolygonSymbolDescriptor();
            sym.style = kmlStyle;

            if (polygon.Rings.Count > 0)
            {
                // Create feature descriptor from geometry and other information
                return new PlacemarkDescriptor()
                {
                    Geometry = polygon,
                    Symbol = sym
                };
            }

            return null;
        }

        /// <summary>
        /// Extracts a linear ring from the input element and applies style information to the placemark descriptor.
        /// </summary>
        /// <param name="kmlStyle">KML Style information.</param>
        /// <param name="geomElement">Linear ring geometry information.</param>
        /// <returns>A PlacemarkDescriptor object representing the feature.</returns>
        private static PlacemarkDescriptor ExtractLinearRing(KMLStyle kmlStyle, XElement geomElement)
        {
			XNamespace kmlNS = geomElement.Name.Namespace;
			XElement coord = geomElement.Element(kmlNS + "coordinates");
            if (coord != null)
            {
                // Extract coordinates and build geometry
                ESRI.ArcGIS.Client.Geometry.PointCollection pts = ExtractCoordinates(coord);
                if (pts != null && pts.Count > 0)
                {
                    ESRI.ArcGIS.Client.Geometry.Polygon polygon = new Polygon();
                    polygon.Rings.Add(pts);

                    // Create symbol and use style information
                    LineSymbolDescriptor sym = new LineSymbolDescriptor();
                    sym.style = kmlStyle;

                    // Create feature descriptor from geometry and other information
                    return new PlacemarkDescriptor()
                    {
                        Geometry = polygon,
                        Symbol = sym
                    };
                }
            }
            
            return null;
        }

        /// <summary>
        /// Extracts a polyline from the input element and applies style information to the placemark descriptor.
        /// </summary>
        /// <param name="kmlStyle">KML Style information.</param>
        /// <param name="line">Polyline geometry information.</param>
		/// <returns>A PlacemarkDescriptor object representing the feature.</returns>
        private static PlacemarkDescriptor ExtractPolyLine(KMLStyle kmlStyle, XElement line)
        {
			XNamespace kmlNS = line.Name.Namespace;
			XElement coord = line.Element(kmlNS + "coordinates");
            if (coord != null)
            {
                // Extract coordinates and build geometry
                ESRI.ArcGIS.Client.Geometry.PointCollection pts = ExtractCoordinates(coord);
                if (pts != null && pts.Count > 0)
                {
                    ESRI.ArcGIS.Client.Geometry.Polyline polyline = new ESRI.ArcGIS.Client.Geometry.Polyline();
                    polyline.Paths.Add(pts);

                    // Create symbol and use style information
                    LineSymbolDescriptor sym = new LineSymbolDescriptor();
                    sym.style = kmlStyle;

                    // Create feature descriptor from geometry and other information
                    return new PlacemarkDescriptor()
                    {
                        Geometry = polyline,
                        Symbol = sym
                    };
                }
            }

            return null;
        }

        /// <summary>
        /// Extracts a point from the input element and applies style information to the placemark descriptor.
        /// </summary>
        /// <param name="kmlStyle">KML Style information.</param>
        /// <param name="point">Point geometry information.</param>
		/// <returns>A PlacemarkDescriptor object representing the feature.</returns>
        private static PlacemarkDescriptor ExtractPoint(KMLStyle kmlStyle, XElement point)
        {
			XNamespace kmlNS = point.Name.Namespace;
			XElement coord = point.Element(kmlNS + "coordinates");
            if (coord != null)
            {
                // Extract geometry
                ESRI.ArcGIS.Client.Geometry.Geometry geom = ExtractCoordinate(coord.Value);
                if (geom != null)
                {
                    // Create symbol and use style information
                    PointSymbolDescriptor sym = new PointSymbolDescriptor();
                    sym.style = kmlStyle;

                    // Create feature descriptor from geometry and other information
                    PlacemarkDescriptor fd = new PlacemarkDescriptor()
                    {
                        Geometry = geom,
                        Symbol = sym
                    };

                    return fd;
                }
            }

            return null;
        }

        /// <summary>
        /// Extracts a collection of points from a LinearRing definition.
        /// </summary>
        /// <param name="boundary">Outer or Inner boundary XElement object.</param>
        /// <returns>A PointCollection containing MapPoint objects.</returns>
        private static PointCollection ExtractRing(XElement boundary)
        {
			XNamespace kmlNS = boundary.Name.Namespace;
			// Ensure there is a LinearRing element within the boundary
            XElement ring = boundary.Element(kmlNS + "LinearRing");
            if (ring != null)
            {
                // Ensure there is a coordinates element within the linear ring
                XElement coord = ring.Element(kmlNS + "coordinates");
                if (coord != null)
                {
                    return ExtractCoordinates(coord);
                }
            }

            return null;
        }

        /// <summary>
        /// Extracts the X and Y values from a comma delimited string containing multiple coordinates.
        /// </summary>
        /// <param name="coordinates">Comma delimited string containing multiple coordinate groups.</param>
        /// <returns>A PointCollection containing MapPoint objects.</returns>
        private static PointCollection ExtractCoordinates(XElement coordinates)
        {
			IList<MapPoint> pointsList = new List<MapPoint>();

            // Break collection into individual coordinates
            string[] paths = coordinates.Value.Trim().Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string coordinate in paths)
            {
                MapPoint mapPoint = ExtractCoordinate(coordinate);
                if (mapPoint != null)
					pointsList.Add(mapPoint);
            }

			return new ESRI.ArcGIS.Client.Geometry.PointCollection(pointsList);
        }

        /// <summary>
        /// Extracts the X and Y values from a comma delimited string containing a single coordinate.
        /// </summary>
        /// <param name="coordinate">Comma delimited string containing X, Y and Z values.</param>
        /// <returns>A MapPoint object with X and Y coordinate values assigned.</returns>
        private static MapPoint ExtractCoordinate(string coordinate)
        {
            MapPoint mp = null;

            // Ensure string coordinate is intact
            if (!String.IsNullOrEmpty(coordinate))
            {
                // Split input string into an array of strings using comma as the delimiter
                string[] xy = coordinate.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);

                // Make sure X and Y coordinate strings are available
                if (xy.Length >= 2)
                {
                    double x, y;

                    // Create new MapPoint object passing in X and Y values to constructor
                    if (double.TryParse(xy[0], System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out x) && double.TryParse(xy[1], System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out y))
                    {
                        mp = new MapPoint(x, y);
                    }
                }
            }

            return mp;
        }


		/// <summary>
		/// Gets the 'normal' style of a style map.
		/// Getting this style may need recursive download.
		/// When the style is ready -> execute the callback.
		/// </summary>
		/// <remarks>
		/// The 'highlight' style is not used by the KmlLayer.
		/// </remarks>
		/// <param name="styleMap">The style map element to parse.</param>
		/// <param name="xDoc">The xDocument the style map is part of.</param>
		/// <param name="credentials">The credentials.</param>
		/// <param name="callback">The callback to call when the style is downloaded (if needed).</param>
		private void  GetStyleMapAsync(XElement styleMap, XDocument xDoc, ICredentials credentials, Action<KMLStyle> callback)
		{
			XNamespace kmlNS = styleMap.Name.Namespace;
			KMLStyle kmlStyle = null;
			foreach (XElement pair in styleMap.Descendants(kmlNS + "Pair"))
			{
				XElement key = pair.Element(kmlNS + "key");
				if (key != null)
				{
					if (key.Value == "normal")
					{
						XElement style = pair.Element(kmlNS + "Style");
						if (style != null)
						{
							kmlStyle = new KMLStyle();
							GetStyle(style, kmlStyle);
						}
						else
						{
							XElement styleUrl = pair.Element(kmlNS + "styleUrl");
							if (styleUrl != null)
							{
								XAttribute styleIdAttribute = styleMap.Attribute("id");
								string styleId = styleIdAttribute == null ? null : styleIdAttribute.Value;

								// Get the style from the styleUrl. This may need to downloading an external KML file
								GetStyleUrlAsync(styleUrl.Value, xDoc, credentials
									, kmlstyle =>
										{
											//// After obtaining the style (which may have involved recursion and downloading external KML files
											//// to resolve style URLs) be sure to always overwrite the styleId with the name given to this StyleMap.
											if (styleId != null && kmlstyle != null)
												kmlstyle.StyleId = styleId;
											callback(kmlstyle);
										});
								return;
							}
						}
					}
				}
			}

			// execute the callback with the found style (or null if not found)
			callback(kmlStyle);
		}


		private void GetStyleUrlAsync(string styleUrl, XDocument xDoc, System.Net.ICredentials credentials, Action<KMLStyle> callback)
		{
			KMLStyle kmlStyle = new KMLStyle();

			if (!String.IsNullOrEmpty(styleUrl))
			{
				// If the style url begins with a # symbol, then it is a reference to a style
				// defined within the current KML file. Otherwise it is a reference to a style
				// in an external file which must be downloaded and processed.
				if (styleUrl.Substring(0, 1) == "#")
				{
					// Remove first character (which is "#")
					string styleId = styleUrl.Substring(1);

					if (xDoc == null)
					{
						if (featureDefs.styles.ContainsKey(styleId))
							kmlStyle.CopyFrom(featureDefs.styles[styleId]);
					}
					else
					{
						XElement style = xDoc.Descendants().FirstOrDefault(e => e.Name.LocalName == "Style" && (string)e.Attribute("id") == styleId);

						// Make sure the style was found and use first element
						if (style != null)
						{
							GetStyle(style, kmlStyle);
						}
						else
						{
							// See if the styleURL value is associated with a StyleMap node
							style = xDoc.Descendants().FirstOrDefault(e => e.Name.LocalName == "StyleMap" && (string)e.Attribute("id") == styleId);

							// Make sure the style map was found and use first element
							if (style != null)
							{
								GetStyleMapAsync(style, xDoc, credentials, callback);
								return;
							}
						}
					}
				}
				else
				{
					DownloadStyleAsync(styleUrl, credentials, callback);
					return;
				}
			}

			callback(kmlStyle);
		}

        /// <summary>
        /// Constructs a KMLStyle object that represents KML style contained in the input XElement.
        /// </summary>
        /// <param name="style">XElement containing KML style definition.</param>
        /// <param name="kmlStyle">KMLStyle object representing input style.</param>
        private static void GetStyle(XElement style, KMLStyle kmlStyle)
        {
        	XNamespace kmlNS = style.Name.Namespace;
            XAttribute styleId = style.Attribute("id");
            if (styleId != null)
            {
                kmlStyle.StyleId = styleId.Value;
            }

            // If style contains an BalloonStyle, then extract that information
            XElement balloonStyle = style.Element(kmlNS + "BalloonStyle");
            if (balloonStyle != null)
            {
                XElement text = balloonStyle.Element(kmlNS + "text");
                if (text != null)
                {
                    kmlStyle.BalloonText = text.Value;
                }
            }

            // If style contains an IconStyle, then extract that information
            XElement iconStyle = style.Element(kmlNS + "IconStyle");
            if (iconStyle != null)
            {
                XElement icon = iconStyle.Element(kmlNS + "Icon");
                if (icon != null)
                {
                    XElement href = icon.Element(kmlNS + "href");
                    if (href != null)
                    {
                        kmlStyle.IconHref = href.Value;
                    }
                }

                // If the hotspot element is present, make use of it
                XElement hotspot = iconStyle.Element(kmlNS + "hotSpot");
                if (hotspot != null)
                {
                    XAttribute units;
                    XAttribute val;

                    units = hotspot.Attribute("xunits");
                    if (units != null)
                    {
                        try
                        {
                            kmlStyle.IconHotspotUnitsX = (HotSpotUnitType)Enum.Parse(typeof(HotSpotUnitType), units.Value, true);
                            val = hotspot.Attribute("x");
                            if (val != null)
                            {
                                double x;
                                if (double.TryParse(val.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out x))
                                    kmlStyle.IconHotspotX = x;
                            }
                        }
                        catch { }
                    }

                    units = hotspot.Attribute("yunits");
                    if (units != null)
                    {
                        try
                        {
                            kmlStyle.IconHotspotUnitsY = (HotSpotUnitType)Enum.Parse(typeof(HotSpotUnitType), units.Value, true);
                            val = hotspot.Attribute("y");
                            if (val != null)
                            {
                                double y;
                                if (double.TryParse(val.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out y))
                                    kmlStyle.IconHotspotY = y;
                            }
                        }
                        catch { }
                    }
                }

                // If the heading element is present, make use of it
                XElement heading = iconStyle.Element(kmlNS + "heading");
                if (heading != null)
                {
                    double degrees;
                    if (double.TryParse(heading.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out degrees))
                        kmlStyle.IconHeading = degrees;
                }

                // If the scale element is present, make use of it
                XElement scale = iconStyle.Element(kmlNS + "scale");
                if (scale != null)
                {
                    double scaleAmount;
                    if (double.TryParse(scale.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out scaleAmount))
                        kmlStyle.IconScale = scaleAmount;
                }
            }

            // If style contains a LineStyle, then extract that information
            XElement lineStyle = style.Element(kmlNS + "LineStyle");
            if (lineStyle != null)
            {
                XElement color = lineStyle.Element(kmlNS + "color");
                if (color != null)
                {
                    kmlStyle.LineColor = GetColorFromHexString(color.Value);
                }
                XElement width = lineStyle.Element(kmlNS + "width");
                if (width != null)
                {
                    double widthVal;
                    if (double.TryParse(width.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out widthVal))
                        kmlStyle.LineWidth = widthVal;
                }
            }

            // If style contains a PolyStyle, then extract that information
            XElement polyStyle = style.Element(kmlNS + "PolyStyle");
            if (polyStyle != null)
            {
                XElement color = polyStyle.Element(kmlNS + "color");
                if (color != null)
                {
                    kmlStyle.PolyFillColor = GetColorFromHexString(color.Value);
                }
                XElement fill = polyStyle.Element(kmlNS + "fill");
                if (fill != null)
                {
                    kmlStyle.PolyFill = StringToBool(fill.Value);
                }
                XElement outline = polyStyle.Element(kmlNS + "outline");
                if (outline != null)
                {
                    kmlStyle.PolyOutline = StringToBool(outline.Value);
                }
            }
        }

        /// <summary>
        /// Converts a string containing an integer value into a boolean.
        /// </summary>
        /// <param name="s">String containing boolean in numeric format.</param>
        /// <returns>Boolean value extracted from the input string.</returns>
        private static bool StringToBool(string s)
        {
            int intVal;

            // Try to parse the string into an integer. If successful, then treat 0 as false and
            // all other values as true.
            if (int.TryParse(s, out intVal))
            {
                return (intVal == 0) ? false : true;
            }

            // If parsing failed, then return default value for boolean type
            return false;
        }

		private static bool GetBooleanValue(XElement element)
		{
			bool visible;
			if (Boolean.TryParse(element.Value, out visible))
				return visible;
			return StringToBool(element.Value);
		}

        /// <summary>
        /// Converts hexadecimal color notation into equivalent Silverlight Color.
        /// </summary>
        /// <param name="s">Input color string in hexadecimal format.</param>
        /// <returns>Color object representing input string.</returns>
        private static System.Windows.Media.Color GetColorFromHexString(string s)
        {
            if (s.Length == 8)
            {
                // Be advised that the values are not ARGB, but instead ABGR.
                byte a = System.Convert.ToByte(s.Substring(0, 2), 16);
                byte b = System.Convert.ToByte(s.Substring(2, 2), 16);
                byte g = System.Convert.ToByte(s.Substring(4, 2), 16);
                byte r = System.Convert.ToByte(s.Substring(6, 2), 16);
                return System.Windows.Media.Color.FromArgb(a, r, g, b);
            }
            else
            {
                byte b = System.Convert.ToByte(s.Substring(0, 2), 16);
                byte g = System.Convert.ToByte(s.Substring(2, 2), 16);
                byte r = System.Convert.ToByte(s.Substring(4, 2), 16);
                return System.Windows.Media.Color.FromArgb(255, r, g, b);
            }
        }

        private static string GetAtomHref(XElement element)
        {
            XAttribute atomHrefAttr = element.Attribute("href");
            if (atomHrefAttr != null)
            {
                return atomHrefAttr.Value;
            }

            return null;
        }

        private static string GetAtomAuthor(XElement element)
        {
            XElement name = element.Element(atomNS + "name");
            if (name != null)
            {
                return name.Value;
            }

            return null;
        }

		private static double GetDoubleValue(XElement element)
		{
			double ret = 0.0;
			if (element != null)
				double.TryParse(element.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out ret);

			return ret;
		}

        #endregion
    }
}
