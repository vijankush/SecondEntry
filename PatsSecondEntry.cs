#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class PatsSecondEntry : Indicator
	{
		private int LastBar = 0;
		private int NewHigBarnum = 0;
		private double NewHighPrice = 0.0;
		private int BarsSinceHigh = 0;
		private bool SeekingFirstEntry = false;
		private bool FoundFirstEntry = false;
		private int FirstEntryBarnum = 0;
		private int BarsSinceFirstEntry = 0;
		private bool SecondEntrySetupBar = false;
		private bool FoundSecondEntry = false;
		private string LineName = "na";
		private bool Debug = false;
		private int SecondEntryBarnum = 0;
		
		// Lows
		private int 	BarsSinceLow = 0;
		private int 	NewLowBarnum = 0;
		private double	NewLowPrice = 0.0;
		private bool	FoundFirstEntryShort = false;
		private bool	SecondEntrySetupBarShort = false;
		private int		FirstEntryBarnumShort = 0;
		private bool	FoundSecondEntryShort = false;
		private bool 	SeekingFirstEntryShort = false;
		private int 	BarsSinceFirstEntryShort = 0;
		private int 	SecondEntryBarnumShort = 0;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "PATS Second Entry";
				Calculate									= Calculate.OnPriceChange;
				IsOverlay									= true;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
				TextColor									= Brushes.DimGray;
				PivotColor									= Brushes.WhiteSmoke;
				ShortTextColor								= Brushes.Red;
				ShortPivotColor								= Brushes.WhiteSmoke;
				AlertOn										= true;
				AlertSound									= @"secondEntry.wav";
				FirstEntrySound								= @"firstEntry.wav";
				ShowFirstPivot								= true;
				TrendFIlterOn								= true;
				MinBars 									= 15;
				LongsOn										= true;
				ShortOn										= true;
			}
			else if (State == State.Configure)
			{ 
				ClearOutputWindow();
			}
		}

		// MARK:- TODO -
		// [ X ] add line of prior high + 1 tick when searching for 2nd entry 
		// [ X ] to enable 2nd entry, must break low of 1st entry, must be lower than high of 1st entry
		// [ X ] if lower low at close, reprint 1 
		// [ X ] add short entries
		// [   ] add buttons
		
		protected override void OnBarUpdate()
		{
			if ( CurrentBar > MinBars ) { 
				LastBar = CurrentBar -1; 
			} else { return; }
			
			///**************	find new high or within 1 tick of high  ***************
			
			if (High[0] >= MAX(High, MinBars)[1] - TickSize ) { 
				RemoveDrawObject("NewHigh"+LastBar); 
				Draw.Diamond(this, "NewHigh"+CurrentBar, false, 0, High[0], PivotColor);
				NewHigBarnum = CurrentBar;
				NewHighPrice = High[0];
				FoundFirstEntry = false;
				SecondEntrySetupBar = false;
				FirstEntryBarnum = 0;
				FoundSecondEntry = false;
				return; 
			}
			
			///**************	find first entry long  ***************
	
			BarsSinceHigh = CurrentBar - NewHigBarnum;
			if (BarsSinceHigh >= 2) {SeekingFirstEntry = true;}
			double DistanceToHigh = NewHighPrice - High[0];
			if (DistanceToHigh > 1.0 && High[0] > High[1]  && SeekingFirstEntry && !FoundFirstEntry ) {
				Draw.Text(this, "1stEntry"+CurrentBar, "1", 0, Low[0] - 2 * TickSize, TextColor);
				SeekingFirstEntry = false;
				FoundFirstEntry = true;
				FirstEntryBarnum = CurrentBar;
				if ( AlertOn ) {
					Alert("FirstEntry"+CurrentBar, Priority.High, "First Entry", 
					NinjaTrader.Core.Globals.InstallDir+@"\sounds\"+ FirstEntrySound,10, 
					Brushes.Black, Brushes.Yellow);  
				}
			}
			
			// end of bar if low is lower than prior print, remove the text, add a 1 below
			if( FirstEntryBarnum == CurrentBar && Bars.TickCount >= 1900 ) {
				RemoveDrawObject("1stEntry"+CurrentBar);
				Draw.Text(this, "1stEntry"+CurrentBar, "1", 0, Low[0] - 2 * TickSize, TextColor);
			}
			
			// find pullback from first entry
//			if ( FoundFirstEntry && High[0] < High[1] ) {
//				 SecondEntrySetupBar = true;
//			}
			
			// must break low of 1st entry +  must be lower than high of 1st entry
			int FirstEntryBarsAgo = CurrentBar - FirstEntryBarnum;
			if ( FoundFirstEntry && Low[0] < Low[FirstEntryBarsAgo] 
					&& Close[0] < High[FirstEntryBarsAgo] ) {
				 	SecondEntrySetupBar = true;
			}
			
			///**************	find second entry long ***************
			 
			if (IsFirstTickOfBar && FirstEntryBarnum != 0) {
				BarsSinceFirstEntry = CurrentBar - FirstEntryBarnum;
				if ( Debug ) 
					{ Print( Time[0].ToShortDateString() + " \t" + Time[0].ToShortTimeString() 
					+ " \t" + "BarNum: " + CurrentBar 
					+ " \t" + "BarsSinceFirstEntry: " + BarsSinceFirstEntry );
					}
			}
			
			if (BarsSinceFirstEntry >=2 && SecondEntrySetupBar && FoundFirstEntry && !FoundSecondEntry) {
				if( DistanceToHigh > 2.0 ) {
					double EntryPrice = High[1] + TickSize;
					LineName = "SecondEntryLine";
					DrawSecondEntryLine(EntryPrice, LineName);
					if (High[0] > High[1] ) {
						Draw.TriangleUp(this, "2EL"+CurrentBar, false, 0, Low[0] - 2 * TickSize, TextColor);
						FoundSecondEntry = true;
						SecondEntryBarnum = CurrentBar;
						NewHighPrice = 0.0;
						RemoveDrawObject(LineName+CurrentBar);
						RemoveDrawObject(LineName+"Txt"+CurrentBar); 
						if ( AlertOn ) {
							Alert("secondEntry"+CurrentBar, Priority.High, "Second Entry", 
							NinjaTrader.Core.Globals.InstallDir+@"\sounds\"+ AlertSound,10, 
							Brushes.Black, Brushes.Yellow);  
						}
					}
				}
			}
			
			// end of bar if low is lower than prior print, remove the triangle, add a triangle below
			if( SecondEntryBarnum == CurrentBar && Bars.TickCount >= 1900 ) {
				RemoveDrawObject("SecondEntryLine"+CurrentBar);
				Draw.TriangleUp(this, "2EL"+CurrentBar, false, 0, Low[0] - 2 * TickSize, TextColor);
			}
			
			///******************************************************************************************
			///*********************************	Short	*********************************************
			///******************************************************************************************
			
			if ( !ShortOn ) { return; }
			
			///**************	find new low or within 1 tick of low  ***************
			
			if (Low[0] <= MIN(Low, MinBars)[1] + TickSize ) { 
				RemoveDrawObject("NewLow"+LastBar); 
				//Draw.Diamond(this, "NewLow"+CurrentBar, false, 0, Low[0], PivotColor);
				Draw.Dot(this, "NewLow"+CurrentBar, false, 0, Low[0], ShortPivotColor);
				NewLowBarnum = CurrentBar;
				NewLowPrice = Low[0];
				FoundFirstEntryShort = false;
				SecondEntrySetupBarShort = false;
				FirstEntryBarnumShort = 0;
				FoundSecondEntryShort = false;
				return;
			}
			
			///**************	find first entry short  ***************
	
			BarsSinceLow = CurrentBar - NewLowBarnum;
			if (BarsSinceLow >= 2) {SeekingFirstEntryShort = true;}
			double DistanceToLow = Low[0] - NewLowPrice;
			if (DistanceToLow > 1.0 && Low[0] < Low[1]  && SeekingFirstEntryShort && !FoundFirstEntryShort ) {
				Draw.Text(this, "1stEntryShort"+CurrentBar, "1", 0, High[0] + 2 * TickSize, ShortTextColor);
				SeekingFirstEntryShort = false;
				FoundFirstEntryShort = true;
				FirstEntryBarnumShort = CurrentBar;
				if ( AlertOn ) {
					Alert("FirstEntryShort"+CurrentBar, Priority.High, "First Entry Short", 
					NinjaTrader.Core.Globals.InstallDir+@"\sounds\"+ FirstEntrySound,10, 
					Brushes.Black, Brushes.Yellow);  
				}
			}
			
			// end of bar if low is lower than prior print, remove the text, add a 1 below
			if( FirstEntryBarnumShort == CurrentBar && Bars.TickCount >= 1900 ) {
				RemoveDrawObject("1stEntryShort"+CurrentBar);
				Draw.Text(this, "1stEntryShort"+CurrentBar, "1", 0, High[0] + 2 * TickSize, ShortTextColor);
			}
			
			// must break high of 1st entry +  must be higher than low of 1st entry
			int FirstEntryBarsAgoShort = CurrentBar - FirstEntryBarnumShort;
			if ( FoundFirstEntryShort && High[0] > High[FirstEntryBarsAgoShort] 
					&& Close[0] > Low[FirstEntryBarsAgoShort] ) {
				 	SecondEntrySetupBarShort = true;
			}
					
			
			///**************	find second entry short ***************
			 
			if (IsFirstTickOfBar && FirstEntryBarnumShort != 0) {
				BarsSinceFirstEntryShort = CurrentBar - FirstEntryBarnumShort;
				if ( Debug ) 
					{ Print( Time[0].ToShortDateString() + " \t" + Time[0].ToShortTimeString() 
					+ " \t" + "BarNum: " + CurrentBar 
					+ " \t" + "BarsSinceFirstEntryShort: " + BarsSinceFirstEntryShort );
					}
			}
			
			if (BarsSinceFirstEntryShort >=2 && SecondEntrySetupBarShort && FoundFirstEntryShort && !FoundSecondEntryShort) {
				if( DistanceToLow > 2.0 ) {
					double EntryPrice = Low[1] - TickSize;
					LineName = "SecondEntryLineShort";
					DrawSecondEntryLine(EntryPrice, LineName);
					if (Low[0] < Low[1] ) {
						Draw.TriangleDown(this, "2ES"+CurrentBar, false, 0, High[0] + 2 * TickSize, ShortTextColor);
						FoundSecondEntryShort = true;
						SecondEntryBarnumShort = CurrentBar;
						NewLowPrice = 0.0;
						RemoveDrawObject(LineName+CurrentBar);
						RemoveDrawObject(LineName+"Txt"+CurrentBar); 
						if ( AlertOn ) {
							Alert("secondEntryShort"+CurrentBar, Priority.High, "Second Entry Short", 
							NinjaTrader.Core.Globals.InstallDir+@"\sounds\"+ AlertSound,10, 
							Brushes.Black, Brushes.Yellow);  
						}
					}
				}
			}
			
			// end of bar if low is lower than prior print, remove the triangle, add a triangle below
			if( SecondEntryBarnumShort == CurrentBar && Bars.TickCount >= 1900 ) {
				RemoveDrawObject("SecondEntryLine"+CurrentBar);
				Draw.TriangleDown(this, "2ES"+CurrentBar, false, 0, High[0] + 2 * TickSize, ShortTextColor);
			}
			
			
			
		}

		// add line of prior high + 1 tick when searching for 2nd entry
		// problem, marking live without lower low
		private void DrawSecondEntryLine(double EntryPrice, string LineName  ) {
			if (IsFirstTickOfBar) {
				Brush lineColor	= TextColor;
				if ( LineName == "SecondEntryLineShort" ) {
					//change color to red	
					lineColor	= ShortTextColor;
				}
				if ( Debug ) 
				{ 
					Print("Sec Entry Line " + Time[0].ToShortDateString() + " \t" + Time[0].ToShortTimeString() 
					+ " \t" + " Barnum: " + CurrentBar 
					+ " \t" + " Ticks: " + Bars.TickCount.ToString());
				}
				RemoveDrawObject(LineName+LastBar);  
				Draw.Line(this, LineName+CurrentBar, 2, EntryPrice, -3, EntryPrice, lineColor);
				RemoveDrawObject(LineName+"Txt"+LastBar); 
				Draw.Text(this, LineName+"Txt"+CurrentBar, EntryPrice.ToString(), -6, EntryPrice, lineColor);
			}
		}
		
		#region Properties
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="Text Color", Order=1, GroupName="Parameters")]
		public Brush TextColor
		{ get; set; }

		[Browsable(false)]
		public string TextColorSerializable
		{
			get { return Serialize.BrushToString(TextColor); }
			set { TextColor = Serialize.StringToBrush(value); }
		}			

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="Pivot Color", Order=2, GroupName="Parameters")]
		public Brush PivotColor
		{ get; set; }

		[Browsable(false)]
		public string PivotColorSerializable
		{
			get { return Serialize.BrushToString(PivotColor); }
			set { PivotColor = Serialize.StringToBrush(value); }
		}	
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="Short Text Color", Order=3, GroupName="Parameters")]
		public Brush ShortTextColor
		{ get; set; }

		[Browsable(false)]
		public string ShortTextColorSerializable
		{
			get { return Serialize.BrushToString(ShortTextColor); }
			set { ShortTextColor = Serialize.StringToBrush(value); }
		}
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="Short Pivot Color", Order=4, GroupName="Parameters")]
		public Brush ShortPivotColor
		{ get; set; }

		[Browsable(false)]
		public string ShortPivotColorSerializable
		{
			get { return Serialize.BrushToString(ShortPivotColor); }
			set { ShortPivotColor = Serialize.StringToBrush(value); }
		}
		
		[NinjaScriptProperty]
		[Display(Name="AlertOn", Order=5, GroupName="Parameters")]
		public bool AlertOn
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="2nd entry Sound", Order=6, GroupName="Parameters")]
		public string AlertSound
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="First Entry Sound", Order=7, GroupName="Parameters")]
		public string FirstEntrySound
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="ShowFirstPivot", Order=8, GroupName="Parameters")]
		public bool ShowFirstPivot
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="TrendFIlterOn", Order=9, GroupName="Parameters")]
		public bool TrendFIlterOn
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Swing Lookback", Order=10, GroupName="Parameters")]
		public int MinBars
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Longs On", Order=11, GroupName="Parameters")]
		public bool LongsOn
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Short On", Order=12, GroupName="Parameters")]
		public bool ShortOn
		{ get; set; }
		
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PatsSecondEntry[] cachePatsSecondEntry;
		public PatsSecondEntry PatsSecondEntry(Brush textColor, Brush pivotColor, Brush shortTextColor, Brush shortPivotColor, bool alertOn, string alertSound, string firstEntrySound, bool showFirstPivot, bool trendFIlterOn, int minBars, bool longsOn, bool shortOn)
		{
			return PatsSecondEntry(Input, textColor, pivotColor, shortTextColor, shortPivotColor, alertOn, alertSound, firstEntrySound, showFirstPivot, trendFIlterOn, minBars, longsOn, shortOn);
		}

		public PatsSecondEntry PatsSecondEntry(ISeries<double> input, Brush textColor, Brush pivotColor, Brush shortTextColor, Brush shortPivotColor, bool alertOn, string alertSound, string firstEntrySound, bool showFirstPivot, bool trendFIlterOn, int minBars, bool longsOn, bool shortOn)
		{
			if (cachePatsSecondEntry != null)
				for (int idx = 0; idx < cachePatsSecondEntry.Length; idx++)
					if (cachePatsSecondEntry[idx] != null && cachePatsSecondEntry[idx].TextColor == textColor && cachePatsSecondEntry[idx].PivotColor == pivotColor && cachePatsSecondEntry[idx].ShortTextColor == shortTextColor && cachePatsSecondEntry[idx].ShortPivotColor == shortPivotColor && cachePatsSecondEntry[idx].AlertOn == alertOn && cachePatsSecondEntry[idx].AlertSound == alertSound && cachePatsSecondEntry[idx].FirstEntrySound == firstEntrySound && cachePatsSecondEntry[idx].ShowFirstPivot == showFirstPivot && cachePatsSecondEntry[idx].TrendFIlterOn == trendFIlterOn && cachePatsSecondEntry[idx].MinBars == minBars && cachePatsSecondEntry[idx].LongsOn == longsOn && cachePatsSecondEntry[idx].ShortOn == shortOn && cachePatsSecondEntry[idx].EqualsInput(input))
						return cachePatsSecondEntry[idx];
			return CacheIndicator<PatsSecondEntry>(new PatsSecondEntry(){ TextColor = textColor, PivotColor = pivotColor, ShortTextColor = shortTextColor, ShortPivotColor = shortPivotColor, AlertOn = alertOn, AlertSound = alertSound, FirstEntrySound = firstEntrySound, ShowFirstPivot = showFirstPivot, TrendFIlterOn = trendFIlterOn, MinBars = minBars, LongsOn = longsOn, ShortOn = shortOn }, input, ref cachePatsSecondEntry);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PatsSecondEntry PatsSecondEntry(Brush textColor, Brush pivotColor, Brush shortTextColor, Brush shortPivotColor, bool alertOn, string alertSound, string firstEntrySound, bool showFirstPivot, bool trendFIlterOn, int minBars, bool longsOn, bool shortOn)
		{
			return indicator.PatsSecondEntry(Input, textColor, pivotColor, shortTextColor, shortPivotColor, alertOn, alertSound, firstEntrySound, showFirstPivot, trendFIlterOn, minBars, longsOn, shortOn);
		}

		public Indicators.PatsSecondEntry PatsSecondEntry(ISeries<double> input , Brush textColor, Brush pivotColor, Brush shortTextColor, Brush shortPivotColor, bool alertOn, string alertSound, string firstEntrySound, bool showFirstPivot, bool trendFIlterOn, int minBars, bool longsOn, bool shortOn)
		{
			return indicator.PatsSecondEntry(input, textColor, pivotColor, shortTextColor, shortPivotColor, alertOn, alertSound, firstEntrySound, showFirstPivot, trendFIlterOn, minBars, longsOn, shortOn);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PatsSecondEntry PatsSecondEntry(Brush textColor, Brush pivotColor, Brush shortTextColor, Brush shortPivotColor, bool alertOn, string alertSound, string firstEntrySound, bool showFirstPivot, bool trendFIlterOn, int minBars, bool longsOn, bool shortOn)
		{
			return indicator.PatsSecondEntry(Input, textColor, pivotColor, shortTextColor, shortPivotColor, alertOn, alertSound, firstEntrySound, showFirstPivot, trendFIlterOn, minBars, longsOn, shortOn);
		}

		public Indicators.PatsSecondEntry PatsSecondEntry(ISeries<double> input , Brush textColor, Brush pivotColor, Brush shortTextColor, Brush shortPivotColor, bool alertOn, string alertSound, string firstEntrySound, bool showFirstPivot, bool trendFIlterOn, int minBars, bool longsOn, bool shortOn)
		{
			return indicator.PatsSecondEntry(input, textColor, pivotColor, shortTextColor, shortPivotColor, alertOn, alertSound, firstEntrySound, showFirstPivot, trendFIlterOn, minBars, longsOn, shortOn);
		}
	}
}

#endregion
