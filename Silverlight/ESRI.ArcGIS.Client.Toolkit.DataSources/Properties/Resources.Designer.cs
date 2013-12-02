﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.225
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ESRI.ArcGIS.Client.Toolkit.DataSources.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("ESRI.ArcGIS.Client.Toolkit.DataSources.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Image type &apos;{0}&apos; is not supported in Silverlight..
        /// </summary>
        internal static string FeatureDefinition_ImageTypeNotSupported {
            get {
                return ResourceManager.GetString("FeatureDefinition_ImageTypeNotSupported", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Url is not set..
        /// </summary>
        internal static string Generic_UrlNotSet {
            get {
                return ResourceManager.GetString("Generic_UrlNotSet", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error in reading the RSS feed..
        /// </summary>
        internal static string GeoRss_ReadingFeedFailed {
            get {
                return ResourceManager.GetString("GeoRss_ReadingFeedFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Layer does not support setting the GraphicsSource Property..
        /// </summary>
        internal static string GraphicsLayer_GraphicsSourceCannotBeSetOnLayer {
            get {
                return ResourceManager.GetString("GraphicsLayer_GraphicsSourceCannotBeSetOnLayer", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Intensity cannot be less than one..
        /// </summary>
        internal static string HeatMapLayer_IntensityLessThanOne {
            get {
                return ResourceManager.GetString("HeatMapLayer_IntensityLessThanOne", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to KML layer parsing document failed..
        /// </summary>
        internal static string KmlLayer_DocumentParsingFailed {
            get {
                return ResourceManager.GetString("KmlLayer_DocumentParsingFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to GroundOverlays.
        /// </summary>
        internal static string KmlLayer_GroundOverlaysSublayer {
            get {
                return ResourceManager.GetString("KmlLayer_GroundOverlaysSublayer", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Placemarks.
        /// </summary>
        internal static string KmlLayer_PlacemarksSublayer {
            get {
                return ResourceManager.GetString("KmlLayer_PlacemarksSublayer", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unable to read KML source..
        /// </summary>
        internal static string KmlLayer_XDocumentReadFailed {
            get {
                return ResourceManager.GetString("KmlLayer_XDocumentReadFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to A security exception occurred while trying to connect to the &apos;{0}&apos; service. Make sure you have a cross domain policy file available at the root for your server that allows for requests from this application. If not, use a proxy page (handler) to broker communication..
        /// </summary>
        internal static string MapService_SecurityException {
            get {
                return ResourceManager.GetString("MapService_SecurityException", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Request canceled..
        /// </summary>
        internal static string WebRequest_Canceled {
            get {
                return ResourceManager.GetString("WebRequest_Canceled", resourceCulture);
            }
        }
    }
}
