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
				AlertOn										= true;
				AlertSound									= @"secondEntry.wav";
				FirstEntrySound								= @"firstEntry.wav";
				ShowFirstPivot								= true;
				TrendFIlterOn								= true;
				MinBars 									= 15;
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
		// remove 1st entry of new high by end of bar
		// add buttons
		// add short entries
		
		protected override void OnBarUpdate()
		{
			if ( CurrentBar > MinBars ) { 
				LastBar = CurrentBar -1; 
			} else { return; }
			
			///**************	find new high  ***************
			
			if (High[0] >= MAX(High, MinBars)[1] ) { 
				RemoveDrawObject("NewHigh"+LastBar); 
				Draw.Diamond(this, "NewHigh"+CurrentBar, false, 0, High[0], PivotColor);
				NewHigBarnum = CurrentBar;
				NewHighPrice = High[0];
				FoundFirstEntry = false;
				SecondEntrySetupBar = false;
				FirstEntryBarnum = 0;
				FoundSecondEntry = false;
				return; /// MARK:- FIX - marking first entry prior to new high
			}
			
			///**************	find first entry  ***************
	
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
			
			///**************	find second entry  ***************
			 
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
					DrawSecondEntryLine();
					if (High[0] > High[1] ) {
						Draw.TriangleUp(this, "2EL"+CurrentBar, false, 0, Low[0] - 2 * TickSize, TextColor);
						FoundSecondEntry = true;
						NewHighPrice = 0.0;
						RemoveDrawObject("SecondEntryLine"+CurrentBar);
						RemoveDrawObject("SecondEntryLineTxt"+CurrentBar); 
						if ( AlertOn ) {
							Alert("secondEntry"+CurrentBar, Priority.High, "Second Entry", 
							NinjaTrader.Core.Globals.InstallDir+@"\sounds\"+ AlertSound,10, 
							Brushes.Black, Brushes.Yellow);  
						}
					}
				}
			}
		}

		// add line of prior high + 1 tick when searching for 2nd entry
		// problem, marking live without lower low
		private void DrawSecondEntryLine() {
			if (IsFirstTickOfBar) {
				LineName = "SecondEntryLine";
				if ( Debug ) 
				{ 
					Print("Sec Entry Line " + Time[0].ToShortDateString() + " \t" + Time[0].ToShortTimeString() 
					+ " \t" + " Barnum: " + CurrentBar 
					+ " \t" + " Ticks: " + Bars.TickCount.ToString());
				}
				RemoveDrawObject("SecondEntryLine"+LastBar); 
				double EntryPrice = High[1] + TickSize;
				Draw.Line(this, "SecondEntryLine"+CurrentBar, 2, EntryPrice, -2, EntryPrice, TextColor);
				RemoveDrawObject("SecondEntryLineTxt"+LastBar); 
				Draw.Text(this, "SecondEntryLineTxt"+CurrentBar, EntryPrice.ToString(), -6, EntryPrice, TextColor);
			}
		}
		
		#region Properties
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="Tex tColor", Order=1, GroupName="Parameters")]
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
		[Display(Name="Pivot Color", Order=1, GroupName="Parameters")]
		public Brush PivotColor
		{ get; set; }

		[Browsable(false)]
		public string PivotColorSerializable
		{
			get { return Serialize.BrushToString(PivotColor); }
			set { PivotColor = Serialize.StringToBrush(value); }
		}	
		
		[NinjaScriptProperty]
		[Display(Name="AlertOn", Order=2, GroupName="Parameters")]
		public bool AlertOn
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="2nd entry Sound", Order=3, GroupName="Parameters")]
		public string AlertSound
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="First Entry Sound", Order=4, GroupName="Parameters")]
		public string FirstEntrySound
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="ShowFirstPivot", Order=5, GroupName="Parameters")]
		public bool ShowFirstPivot
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="TrendFIlterOn", Order=6, GroupName="Parameters")]
		public bool TrendFIlterOn
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Swing Lookback", Order=7, GroupName="Parameters")]
		public int MinBars
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
		public PatsSecondEntry PatsSecondEntry(Brush textColor, Brush pivotColor, bool alertOn, string alertSound, string firstEntrySound, bool showFirstPivot, bool trendFIlterOn, int minBars)
		{
			return PatsSecondEntry(Input, textColor, pivotColor, alertOn, alertSound, firstEntrySound, showFirstPivot, trendFIlterOn, minBars);
		}

		public PatsSecondEntry PatsSecondEntry(ISeries<double> input, Brush textColor, Brush pivotColor, bool alertOn, string alertSound, string firstEntrySound, bool showFirstPivot, bool trendFIlterOn, int minBars)
		{
			if (cachePatsSecondEntry != null)
				for (int idx = 0; idx < cachePatsSecondEntry.Length; idx++)
					if (cachePatsSecondEntry[idx] != null && cachePatsSecondEntry[idx].TextColor == textColor && cachePatsSecondEntry[idx].PivotColor == pivotColor && cachePatsSecondEntry[idx].AlertOn == alertOn && cachePatsSecondEntry[idx].AlertSound == alertSound && cachePatsSecondEntry[idx].FirstEntrySound == firstEntrySound && cachePatsSecondEntry[idx].ShowFirstPivot == showFirstPivot && cachePatsSecondEntry[idx].TrendFIlterOn == trendFIlterOn && cachePatsSecondEntry[idx].MinBars == minBars && cachePatsSecondEntry[idx].EqualsInput(input))
						return cachePatsSecondEntry[idx];
			return CacheIndicator<PatsSecondEntry>(new PatsSecondEntry(){ TextColor = textColor, PivotColor = pivotColor, AlertOn = alertOn, AlertSound = alertSound, FirstEntrySound = firstEntrySound, ShowFirstPivot = showFirstPivot, TrendFIlterOn = trendFIlterOn, MinBars = minBars }, input, ref cachePatsSecondEntry);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PatsSecondEntry PatsSecondEntry(Brush textColor, Brush pivotColor, bool alertOn, string alertSound, string firstEntrySound, bool showFirstPivot, bool trendFIlterOn, int minBars)
		{
			return indicator.PatsSecondEntry(Input, textColor, pivotColor, alertOn, alertSound, firstEntrySound, showFirstPivot, trendFIlterOn, minBars);
		}

		public Indicators.PatsSecondEntry PatsSecondEntry(ISeries<double> input , Brush textColor, Brush pivotColor, bool alertOn, string alertSound, string firstEntrySound, bool showFirstPivot, bool trendFIlterOn, int minBars)
		{
			return indicator.PatsSecondEntry(input, textColor, pivotColor, alertOn, alertSound, firstEntrySound, showFirstPivot, trendFIlterOn, minBars);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PatsSecondEntry PatsSecondEntry(Brush textColor, Brush pivotColor, bool alertOn, string alertSound, string firstEntrySound, bool showFirstPivot, bool trendFIlterOn, int minBars)
		{
			return indicator.PatsSecondEntry(Input, textColor, pivotColor, alertOn, alertSound, firstEntrySound, showFirstPivot, trendFIlterOn, minBars);
		}

		public Indicators.PatsSecondEntry PatsSecondEntry(ISeries<double> input , Brush textColor, Brush pivotColor, bool alertOn, string alertSound, string firstEntrySound, bool showFirstPivot, bool trendFIlterOn, int minBars)
		{
			return indicator.PatsSecondEntry(input, textColor, pivotColor, alertOn, alertSound, firstEntrySound, showFirstPivot, trendFIlterOn, minBars);
		}
	}
}

#endregion
