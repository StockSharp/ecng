// *************************************************************************************
// ULTRACHART� � Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// PaletteProviderBase.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows.Media;

namespace Ecng.Xaml.Charting.Visuals.RenderableSeries
{
    /// <summary>
    /// Base class for custom Palette Providers, which may be used to override bar, candle or OHLC colors for individual points during rendering
    /// </summary>
    public abstract class PaletteProviderBase : IPaletteProvider
    {
        /// <summary>
        /// When called for an X,Y point, override the color on the attached <see cref="BaseRenderableSeries" />.
        /// Return null to keep the default series color
        /// Return a value to override the series color
        /// </summary>
        /// <param name="series">The source <see cref="IRenderableSeries" /></param>
        /// <param name="xValue">The X-value of the data-point</param>
        /// <param name="yValue">The Y-value of the data-point</param>
        /// <returns>
        /// The overriden color. Return null to keep the default
        /// </returns>
        public virtual Color? GetColor(IRenderableSeries series, double xValue, double yValue)
        {
            return null;
        }

        /// <summary>
        /// When called for an OHLC point, override the color on the attached <see cref="BaseRenderableSeries" />.
        /// Return null to keep the default series color
        /// Return a value to override the series color
        /// </summary>
        /// <param name="series">The source <see cref="IRenderableSeries" /></param>
        /// <param name="xValue">The x value.</param>
        /// <param name="openValue">The open value.</param>
        /// <param name="highValue">The high value.</param>
        /// <param name="lowValue">The low value.</param>
        /// <param name="closeValue">The close value.</param>
        /// <returns></returns>
        public virtual Color? OverrideColor(IRenderableSeries series, double xValue, double openValue, double highValue, double lowValue, double closeValue)
        {
            return null;
        }

        /// <summary>
        /// When called for an bubble point, override the color on the attached <see cref="FastBubbleRenderableSeries" />.
        /// Return null to keep the default series color
        /// Return a value to override the series color
        /// </summary>
        /// <param name="series">The source <see cref="IRenderableSeries" /></param>
        /// <param name="xValue">The x value.</param>
        /// <param name="yValue">The y value.</param>
        /// <param name="zValue">The z value.</param>
        /// <returns></returns>
        public virtual Color? OverrideColor(IRenderableSeries series, double xValue, double yValue, double zValue)
        {
            return null;
        }
    }
}