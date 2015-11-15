﻿// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// EllipsePointMarker.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Ecng.Xaml.Charting.Rendering.Common;

namespace Ecng.Xaml.Charting.Visuals.PointMarkers
{
    /// <summary>
    /// Allows a Ellipse to be rendered at each data-point location using the following XAML syntax
    /// </summary>
    /// <remarks><see cref="BasePointMarker"/> derived types use fast bitmap rendering to draw data-points to the screen. This means that
    /// traditional WPF style tooltips won't work. For that we need to use the HitTest API. Please see the HitTest sections of the user manual
    /// for more information</remarks>
    /// <seealso cref="BasePointMarker"></seealso>
    /// <seealso cref="SquarePointMarker"></seealso>
    /// <seealso cref="TrianglePointMarker"></seealso>
    /// <seealso cref="EllipsePointMarker"></seealso>
    /// <seealso cref="CrossPointMarker"></seealso>
    /// <seealso cref="SpritePointMarker"></seealso>
    /// <example>
    /// <code title="EllipsePointmarker usage" description="Shows how to instantiate a EllipsePointMarker inline in XAML. Note when templating or styling a series, you will need to use the BaseRenderableSeries.PointMarkerTemplate property instead" lang="xaml">
    /// &lt;s:FastLineRenderableSeries&gt;
    ///    &lt;s:FastLineRenderableSeries.PointMarker&gt;
    ///       &lt;s:EllipsePointMarker Width="7" Height="7" Fill="Yellow" Stroke="White" StrokeThickness="1"/&gt;
    ///    &lt;/s:FastLineRenderableSeries.PointMarker&gt;
    /// &lt;/s:FastLineRenderableSeries&gt;
    /// </code>
    /// </example>
    public class EllipsePointMarker : BasePointMarker
    {
        private float _width;
        private float _height;

        /// <summary>
        /// When overridden in a derived class, draws the point markers at specified collection of <see cref="Point" /> centers
        /// </summary>
        /// <param name="context">The RenderContext to draw with</param>
        /// <param name="centers">The Centres of the point markers</param>
        /// <param name="pen">The default Stroke pen (if current pen is not set)</param>
        /// <param name="brush">The default Fill brush (if current brush is not set)</param>
        /// <seealso cref="IRenderContext2D" />
        ///   <seealso cref="IPen2D" />
        ///   <seealso cref="IBrush2D" />
        protected override void DrawInternal(IRenderContext2D context, IEnumerable<Point> centers, IPen2D pen, IBrush2D brush)
        {
            context.DrawEllipses(pen, brush, centers, _width, _height);
        }

        protected override void DrawInternal(IRenderContext2D context, double x, double y, IPen2D pen, IBrush2D brush)
        {
            context.DrawEllipse(pen, brush, new Point(x, y), _width, _height);
        }

        protected override void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            _width = (float) Width;
            _height = (float) Height;
            base.OnPropertyChanged(d, e);
        }
    }
}
