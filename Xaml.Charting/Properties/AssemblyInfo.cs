// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// AssemblyInfo.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
// For full terms and conditions of the license, see http://www.ultrachart.com/ultrachart-eula/
// 
// This source code is protected by international copyright law. Unauthorized
// reproduction, reverse-engineering, or distribution of all or any portion of
// this source code is strictly prohibited.
// 
// This source code contains confidential and proprietary trade secrets of
// ulc software Services Ltd., and should at no time be copied, transferred, sold,
// distributed or made available without express written permission.
// *************************************************************************************
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Windows;
using System.Windows.Markup;

// WPF: By default, we dont encrypt methods, strings, constants. These are turned on selectively for sensitive code
[assembly: Obfuscation(Feature = "encryptmethod;encryptstrings;encryptconstants", 
    Exclude = true, StripAfterObfuscation = true)]

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Ecng.Xaml.Charting")]
[assembly: AssemblyConfiguration("")]

[assembly: AssemblyDescription("Ultrachart - high performance real-time WPF and Silverlight charts")]

[assembly: XmlnsDefinition("http://schemas.ulcsoftware.co.uk/ultrachart", "Ecng.Xaml.Charting")]
[assembly: XmlnsDefinition("http://schemas.ulcsoftware.co.uk/ultrachart", "Ecng.Xaml.Charting.Common")]
[assembly: XmlnsDefinition("http://schemas.ulcsoftware.co.uk/ultrachart", "Ecng.Xaml.Charting.Common.Extensions")]
[assembly: XmlnsDefinition("http://schemas.ulcsoftware.co.uk/ultrachart", "Ecng.Xaml.Charting.ChartModifiers")]
[assembly: XmlnsDefinition("http://schemas.ulcsoftware.co.uk/ultrachart", "Ecng.Xaml.Charting.Model.DataSeries")]
[assembly: XmlnsDefinition("http://schemas.ulcsoftware.co.uk/ultrachart", "Ecng.Xaml.Charting.Rendering.Common")]
[assembly: XmlnsDefinition("http://schemas.ulcsoftware.co.uk/ultrachart", "Ecng.Xaml.Charting.Utility.Mouse")]
[assembly: XmlnsDefinition("http://schemas.ulcsoftware.co.uk/ultrachart", "Ecng.Xaml.Charting.Visuals")]
[assembly: XmlnsDefinition("http://schemas.ulcsoftware.co.uk/ultrachart", "Ecng.Xaml.Charting.Visuals.Annotations")]
[assembly: XmlnsDefinition("http://schemas.ulcsoftware.co.uk/ultrachart", "Ecng.Xaml.Charting.Visuals.Axes")]
[assembly: XmlnsDefinition("http://schemas.ulcsoftware.co.uk/ultrachart", "Ecng.Xaml.Charting.Visuals.PointMarkers")]
[assembly: XmlnsDefinition("http://schemas.ulcsoftware.co.uk/ultrachart", "Ecng.Xaml.Charting.Visuals.RenderableSeries")]
[assembly: XmlnsDefinition("http://schemas.ulcsoftware.co.uk/ultrachart", "Ecng.Xaml.Charting.Rendering.HighSpeedRasterizer")]
[assembly: XmlnsDefinition("http://schemas.ulcsoftware.co.uk/ultrachart", "Ecng.Xaml.Charting.Rendering.HighQualityRasterizer")]

[assembly: XmlnsPrefix("http://schemas.ulcsoftware.co.uk/ultrachart", "s")]

#if SAFECODE
// Required for Bloomberg and other partial trust environments
[assembly: AllowPartiallyTrustedCallers()]
#endif

//In order to begin building localizable applications, set 
//<UICulture>CultureYouAreCodingWith</UICulture> in your .csproj file
//inside a <PropertyGroup>.  For example, if you are using US english
//in your source files, set the <UICulture> to en-US.  Then uncomment
//the NeutralResourceLanguage attribute below.  Update the "en-US" in
//the line below to match the UICulture setting in the project file.

//[assembly: NeutralResourcesLanguage("en-US", UltimateResourceFallbackLocation.Satellite)]


[assembly: ThemeInfo(
    ResourceDictionaryLocation.None, //where theme specific resource dictionaries are located
    //(used if a resource is not found in the page, 
    // or application resource dictionaries)
    ResourceDictionaryLocation.SourceAssembly //where the generic resource dictionary is located
    //(used if a resource is not found in the page, 
    // app, or any theme specific resource dictionaries)
)]

//[assembly: CLSCompliant(true)]

