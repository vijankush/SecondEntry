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
/*
//=====================================================================================================
//   OpenSource code distributed under GPL3.0 or later (https://opensource.org/licenses/GPL-3.0)
//=====================================================================================================
//
//  This open source indicator was developed to aid new users of price action. This indicator does NOT guarantee
//  any profits and is only supposed to be a tool. It is highly recommended to learn how to count legs/use second entries
//  before live trading with this tool. Past performance is not indicative of future results. May we all achieve consistency!
//
//=====================================================================================================
*/

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators.PAT
{
	public class SecondEntry : Indicator
	{
		#region variables
		private double pivotHigh = 0; 
		private double pivotLow = 9999999;
		private double lastHigh = 0;
		private double lastLow = 9999999;
		private double longRisk = 0;
		private double shortRisk = 0;
		private double longPos = 0;
		private double shortPos = 0;
		private double longTarget = 0;
		private double longStop = 0;
		private double shortTarget = 0;
		private double shortStop = 0;
		private double longPrice = 0;
		private double shortPrice = 0;
		private int legDown = 1;
		private int legUp = 1;
		private int lastBarS = 0;
		private int lastBarL = 0;
		private int upLabel = 0;
		private int downLabel = 0;
		private bool isUpLeg = true;
		private bool isDownLeg = true;
		private bool isLongEntry = true;
		private bool isResetLong = false;
		private bool isResetShort = false;
		private bool isShortEntry = true;
		private Brush riskL;
		private Brush riskS;
		private Brush empty = Brushes.Transparent;
		#endregion
		
		protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description             = @"Second Entry indicator";
                Name                    = "Second Entry";
                Calculate               = Calculate.OnEachTick;
                IsOverlay               = true;
                DisplayInDataBox        = true;
                DrawOnPricePanel        = true;
                DrawHorizontalGridLines = true;
                DrawVerticalGridLines   = true;
                PaintPriceMarkers       = true;
                ScaleJustification      = NinjaTrader.Gui.Chart.ScaleJustification.Right;
                IsSuspendedWhileInactive= true;
                Target                  = 4;
                MaxStop                 = 8;
                EntryPlacement          = 1;
                StopPlacement           = 1;
                Margin                  = 5;
                IsOnly2ndEntries        = true;
                IsSignalOn              = true;
                IsAlertOn               = false;
                IsResetDoubleBotTop     = false;
                IsSwapEntryDrawing      = false;
                AlertSound              = "";
                isLongEntryBrush        = Brushes.Lime;
                isShortEntryBrush       = Brushes.Red;
                FutureSignalTarget      = Brushes.Lime;
                FutureSignalStop        = Brushes.Red;
                FutureSignalEntry       = Brushes.Yellow;
                FutureSignalRiskOver    = Brushes.Magenta;
                FutureSignalRiskUnder   = Brushes.Yellow;
                TextFont                = new NinjaTrader.Gui.Tools.SimpleFont("Arial", 9);
            }
        }
	
		protected override void OnBarUpdate()
        {
            if (CurrentBar < 1) return;

            CountLegs();

            if (IsSignalOn)
            {
                FutureLongSignal();
                FutureShortSignal();
            }
        }
        
        private void DrawEntries()
        {
            if (IsSwapEntryDrawing)
            {
                if (isUpLeg)
                {
                    if (IsOnly2ndEntries && legUp == 2)
                    {
                        Draw.Text(this, "u" + upLabel, false, legUp + "", 0, High[0] + (Margin * TickSize), 0, isLongEntryBrush, TextFont, TextAlignment.Center, empty, empty, 0);
                    }
                    else if (!IsOnly2ndEntries)
                    {
                        Draw.Text(this, "u" + upLabel, false, legUp + "", 0, High[0] + (Margin * TickSize), 0, isLongEntryBrush, TextFont, TextAlignment.Center, empty, empty, 0);
                    }
                    upLabel += 1;
                }
                else if (isDownLeg)
                {
                    if (IsOnly2ndEntries && legDown == 2)
                    {
                        Draw.Text(this, "d" + downLabel, false, legDown + "", 0, Low[0] - (Margin * TickSize), 0, isShortEntryBrush, TextFont, TextAlignment.Center, empty, empty, 0);
                    }
                    else if (!IsOnly2ndEntries)
                    {
                        Draw.Text(this, "d" + downLabel, false, legDown + "", 0, Low[0] - (Margin * TickSize), 0, isShortEntryBrush, TextFont, TextAlignment.Center, empty, empty, 0);
                    }
                    downLabel += 1;
                }
            }
            else
            {
                if (isUpLeg)
                {
                    if (IsOnly2ndEntries && legUp == 2)
                    {
                        Draw.Text(this, "u" + upLabel, false, legUp + "", 0, Low[0] - (Margin * TickSize), 0, isLongEntryBrush, TextFont, TextAlignment.Center, empty, empty, 0);
                    }
                    else if (!IsOnly2ndEntries)
                    {
                        Draw.Text(this, "u" + upLabel, false, legUp + "", 0, Low[0] - (Margin * TickSize), 0, isLongEntryBrush, TextFont, TextAlignment.Center, empty, empty, 0);
                    }
                    upLabel += 1;
                }
                else if (isDownLeg)
                {
                    if (IsOnly2ndEntries && legDown == 2)
                    {
                        Draw.Text(this, "d" + downLabel, false, legDown + "", 0, High[0] + (Margin * TickSize), 0, isShortEntryBrush, TextFont, TextAlignment.Center, empty, empty, 0);
                    }
                    else if (!IsOnly2ndEntries)
                    {
                        Draw.Text(this, "d" + downLabel, false, legDown + "", 0, High[0] + (Margin * TickSize), 0, isShortEntryBrush, TextFont, TextAlignment.Center, empty, empty, 0);
                    }
                    downLabel += 1;
                }
            }
        }

        // Helper method For Future Long/Short Signals
        private void DrawLine(string tag, double price, Brush brush) {
            Draw.Line(this, tag, -2, price, -6, price, brush);
        }

        // Helper method For Future Long/Short Signals
        private void DrawDiamond(string tag, double price, Brush brush) {
            Draw.Diamond(this, tag, false, -6, price, brush);
        }

        // Helper method For Future Long/Short Signals
        private void DrawText(string tag, string text, double price, Brush brush) {
            Draw.Text(this, tag, false, text, -9, price, 0, brush, TextFont, TextAlignment.Center, empty, empty, 0);
        }
		
		private void FutureShortSignal() {
            // Exit early if not in a short entry or if the second entry short is not currently an upleg
            if (!isShortEntry && !(legDown == 2 && isUpLeg)) {
                return;
            }

            if (legDown == 2 && isUpLeg) {
                if (IsAlertOn) {
                Alert("isShortEntryAlert", Priority.High, "Second entry short setting up", AlertSound, 999999, Brushes.Transparent, Brushes.Transparent);
                }

                // Reuse variable to avoid creating unnecessary variables
                shortStop = Math.Max(High[0], High[1]) + (StopPlacement * TickSize);
                shortPrice = Low[1] - (TickSize * EntryPlacement);
                shortTarget = Low[1] - ((Target + EntryPlacement) * TickSize);
                shortRisk = ((shortStop + (TickSize * StopPlacement)) - shortPrice) / TickSize;

                // Use ternary operator to avoid unnecessary if-else statement
                var riskS = shortRisk > MaxStop ? FutureSignalRiskOver : FutureSignalRiskUnder;

                DrawDiamond("isShortEntryD", shortPrice, isShortEntryBrush);
                DrawDiamond("ShortStopD", shortStop, isShortEntryBrush);
                DrawDiamond("ShortTargetD", shortTarget, isShortEntryBrush);
                DrawLine("ShortTarget", shortTarget, FutureSignalTarget);
                DrawLine("ShortStop", shortStop, FutureSignalStop);
                DrawLine("isShortEntry", shortPrice, FutureSignalEntry);
                DrawText("ShortRisk", shortRisk + " ticks", shortStop, riskS);

                // Set isShortEntry to true to indicate that we are now in a short entry
                isShortEntry = true;
            } else //if (isShortEntry)
            {
                shortRisk = (shortStop - shortPrice) / TickSize;
                DrawDiamond("isShortEntryD", shortPrice, isShortEntryBrush);
                DrawDiamond("ShortStopD", shortStop, isShortEntryBrush);
                DrawDiamond("ShortTargetD", shortTarget, isShortEntryBrush);
                DrawLine("ShortTarget", shortTarget, FutureSignalTarget);
                DrawLine("ShortStop", shortStop, FutureSignalStop);
                DrawLine("isShortEntry", shortPrice, FutureSignalEntry);
                DrawText("ShortRisk", shortRisk + " ticks", shortStop, riskS);

                if (Low[0] < shortTarget || High[0] >= shortStop) {
                // Set isShortEntry to false to indicate that we are no longer in a short entry
                isShortEntry = false;

                // Remove all the drawing objects related to the short entry
                RemoveDrawObject("isShortEntryD");
                RemoveDrawObject("ShortStopD");
                RemoveDrawObject("ShortTargetD");
                RemoveDrawObject("ShortTarget");
                RemoveDrawObject("ShortStop");
                RemoveDrawObject("isShortEntry");
                RemoveDrawObject("ShortRisk");

                // Rearm the alert if applicable
                RearmAlert("isShortEntryAlert");
                }
            }
        }
		
		//displays lines for target, entry, and stop for long entries. refer to FutureShortSignal() comments for functionality
        private void FutureLongSignal()
        {
            // Exit early if not in a long entry or if the second entry long is not currently a downleg
            if (!isLongEntry && !(legUp == 2 && isDownLeg))
            {
                return;
            }

            if (legUp == 2 && isDownLeg)
            {
                if (IsAlertOn)
                {
                    Alert("isLongEntryAlert", Priority.High, "Second entry long setting up", AlertSound, 999999, Brushes.Transparent, Brushes.Transparent);
                }

                longPrice = High[1] + (TickSize * EntryPlacement);
                longStop = Math.Min(Low[0], Low[1]) - (StopPlacement * TickSize);
                longTarget = High[1] + ((Target + EntryPlacement) * TickSize);
                longRisk = ((High[1] + (TickSize * EntryPlacement)) - longStop) / TickSize;

                var riskL = longRisk > MaxStop ? FutureSignalRiskOver : FutureSignalRiskUnder;

                DrawDiamond("isLongEntryD", longPrice, isLongEntryBrush);
                DrawDiamond("LongStopD", longStop, isLongEntryBrush);
                DrawDiamond("LongTargetD", longTarget, isLongEntryBrush);
                DrawLine("LongTarget", longTarget, isLongEntryBrush);
                DrawLine("LongStop", longStop, isLongEntryBrush);
                DrawLine("isLongEntry", longPrice, isLongEntryBrush);
                DrawText("LongRisk", longRisk + " ticks", longStop, riskL);

                isLongEntry = true;
            }
            else //if (isLongEntry)
            {
                longRisk = (longPrice - longStop) / TickSize;
                DrawDiamond("isLongEntryD", longPrice, isLongEntryBrush);
                DrawDiamond("LongStopD", longStop, isLongEntryBrush);
                DrawDiamond("LongTargetD", longTarget, isLongEntryBrush);
                DrawText("LongRisk", longRisk + " ticks", longStop, riskL);
                DrawLine("LongTarget", longTarget, FutureSignalTarget);
                DrawLine("LongStop", longStop, FutureSignalStop);
                DrawLine("isLongEntry", longPrice, FutureSignalEntry);

                if (High[0] > longTarget || Low[0] <= longStop)
                {
                    isLongEntry = false;

                    RemoveDrawObject("isLongEntryD");
                    RemoveDrawObject("LongStopD");
                    RemoveDrawObject("LongTargetD");
                    RemoveDrawObject("LongTarget");
                    RemoveDrawObject("LongStop");
                    RemoveDrawObject("isLongEntry");
                    RemoveDrawObject("LongRisk");

                    RearmAlert("isLongEntryAlert");
                }
            }
        }
		
	
		/*
		Method to count legs
		For longs, you must find a higher high, a leg down, a lower high, a leg down and then the next break of a bar is a second entry.
		For shorts, you must find a lower low, a leg up, a higher low, a leg up and then the break below a bar.
		*/
        private void CountLegs()
		{
			if(IsResetDoubleBotTop)
			{
				isResetShort = lastLow <= pivotLow;
				isResetLong = lastHigh >= pivotHigh;
			}
			else
			{
				isResetShort = lastLow < pivotLow;
				isResetLong = lastHigh > pivotHigh;
			}
			if(isDownLeg) //if currently a down leg
			{
				pivotHigh = lastHigh; //sets the highest high to the previous high before the down leg initiates
				if(High[0] >= High[1] + (TickSize*EntryPlacement) && lastBarL != CurrentBar) //looking for a break above previous bar
            	{

					lastBarL = CurrentBar;
					isDownLeg = false;
					isUpLeg = true;  //initiates up leg after break of previous bar
					if(isResetShort) //if a lower low forms
					{
						pivotLow = lastLow;//set the lowest low to the new low and reset leg count;
						legDown = 1;
					}
					else
					{
						legDown+=1; //if there is a higher low that forms, increment leg count.
					}
					DrawEntries();
					
					if(legUp == 2) // if second entry, set prices for target, stop and entry.
					{
						isLongEntry = true;
						longPrice = High[1] + (TickSize*EntryPlacement);
						longTarget = longPrice + ((Target)*TickSize);
						longStop = Math.Min(Low[0],Low[1]) - (StopPlacement*TickSize);
					}
					lastHigh = High[0]; //sets most recent low before up leg
					lastLow = Math.Min(lastLow, Low[0]);
           		}
				else if (High[0] > lastHigh && lastBarL == CurrentBar)
				{
					lastHigh = High[0];
					isDownLeg = false;
					isUpLeg = true;
					if(isResetShort) //if a lower low forms
					{
						pivotLow = lastLow;//set the lowest low to the new low and reset leg count;
						legDown = 1;
					}
					
					lastLow = Math.Min(lastLow, Low[0]);
				}
				else
				{
					lastLow = Math.Min(lastLow, Low[0]); //continuously sets the next low
				}
			}
			else if(isUpLeg) //if currently an up leg
			{
				pivotLow = lastLow; //sets the lowest low to the previous low before the up leg initiates
				if(Low[0] <= Low[1] - (TickSize*EntryPlacement) && CurrentBar != lastBarS) //looking for a break below the previous bar
	            {
					lastBarS = CurrentBar;
					isUpLeg = false;
					isDownLeg = true; //initiates new down leg
					if(isResetLong)// if a higher high has formed
					{
						//lastHigh = Low[0];
						pivotHigh = lastHigh; //set highest high to the higher high
						legUp = 1; //reset leg count
										
					}
					else
					{
						legUp += 1; // if there is a lower high, increment leg count
						
					}
					DrawEntries();
					if(legDown == 2) // if second entry, set prices for target, stop and entry.
					{
						isShortEntry = true;
						shortPrice = Low[1]-(TickSize*EntryPlacement);
						shortTarget = shortPrice - ((Target)*TickSize);
						shortStop = Math.Max(High[0], High[1])+(StopPlacement*TickSize);
					}
					lastLow = Low[0]; // sets most recent low before down leg
					lastHigh = Math.Max(lastHigh,High[0]);
	            }
				else if (Low[0] < lastLow && lastBarS == CurrentBar)
				{
					lastLow = Low[0];
					isUpLeg = false;
					isDownLeg = true;
					if(isResetLong)// if a higher high has formed
					{
						pivotHigh = lastHigh; //set highest high to the higher high
						legUp = 1; //reset leg count				
					}
					
					lastHigh = Math.Max(lastHigh,High[0]);
				}
				else
				{
					lastHigh = Math.Max(lastHigh,High[0]); //continuously sets the next high
				}
			}
		}
		
		#region Properties
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Target(Ticks)", Description="Target in ticks for instrument", Order=1, GroupName="Parameters")]
		public int Target
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Max Stop(Ticks)", Description="Maximum stoploss in ticks", Order=2, GroupName="Parameters")]
		public int MaxStop
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Entry Placement(Ticks)", Description="Ticks above signal bar for entry for instrument", Order=3, GroupName="Parameters")]
		public int EntryPlacement
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Stop Placement(Ticks)", Description="number of ticks a trap/stop would trigger above/below bar", Order=4, GroupName="Parameters")]
		public int StopPlacement
		{ get; set; }
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Margin(Ticks)", Description="distance from bar that signal appears", Order=5, GroupName="Parameters")]
		public int Margin
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Draw Only 2EL/2ES", Description="Only show 2nd entries", Order=6, GroupName="Parameters")]
		public bool IsOnly2ndEntries
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Reset count on DT/DB", Description="Resets the count on double bottoms/tops", Order=6, GroupName="Parameters")]
		public bool IsResetDoubleBotTop
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Longs on top/Shorts on bottom", Description="Swaps the position of the entries", Order=7, GroupName="Parameters")]
		public bool IsSwapEntryDrawing
		{ get; set; }
		
		
		[NinjaScriptProperty]
		[Display(Name="Draw Future Signals", Description="Show future order signals", Order=7, GroupName="Parameters")]
		public bool IsSignalOn
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Alerts for 2E", Description="Toggle Alerts", Order=8, GroupName="Parameters")]
		public bool IsAlertOn
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Alert Sound", Order = 9 , GroupName= "Parameters" )]
		[PropertyEditor ("NinjaTrader.Gui.Tools.FilePathPicker" , Filter= "Any Files (*.*)|*.*" )]
		public string AlertSound
		{ get ; set ; }
		
		[Display(Name	= "Font, size, type, style",
		Description		= "select font, style, size to display on chart",
		GroupName		= "Text",
		Order			= 10)]
		public Gui.Tools.SimpleFont TextFont
		{ get; set; }	
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="Long Entry", Description="Color of Long entry and entry lines", Order=11, GroupName="Colors")]
		public Brush isLongEntryBrush
		{ get; set; }

		[Browsable(false)]
		public string isLongEntryBrushSerializable
		{
			get { return Serialize.BrushToString(isLongEntryBrush); }
			set { isLongEntryBrush = Serialize.StringToBrush(value); }
		}			

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="Short Entry", Description="Color of short entry and short entry lines", Order=12, GroupName="Colors")]
		public Brush isShortEntryBrush
		{ get; set; }

		[Browsable(false)]
		public string isShortEntryBrushSerializable
		{
			get { return Serialize.BrushToString(isShortEntryBrush); }
			set { isShortEntryBrush = Serialize.StringToBrush(value); }
		}			
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="FutureSignalTarget", Description="Target when in a trade", Order=1, GroupName="Future Signal Colors")]
		public Brush FutureSignalTarget
		{ get; set; }

		[Browsable(false)]
		public string FutureSignalTargetSerializable
		{
			get { return Serialize.BrushToString(FutureSignalTarget); }
			set { FutureSignalTarget = Serialize.StringToBrush(value); }
		}			

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="FutureSignalStop", Description="Stop placement when in a trade", Order=2, GroupName="Future Signal Colors")]
		public Brush FutureSignalStop
		{ get; set; }

		[Browsable(false)]
		public string FutureSignalStopSerializable
		{
			get { return Serialize.BrushToString(FutureSignalStop); }
			set { FutureSignalStop = Serialize.StringToBrush(value); }
		}			

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="FutureSignalEntry", Description="Entry Placement when in a trade", Order=3, GroupName="Future Signal Colors")]
		public Brush FutureSignalEntry
		{ get; set; }

		[Browsable(false)]
		public string FutureSignalEntrySerializable
		{
			get { return Serialize.BrushToString(FutureSignalEntry); }
			set { FutureSignalEntry = Serialize.StringToBrush(value); }
		}			

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="FutureSignalRiskOver", Description="Color of the risk when exceeded max stop", Order=4, GroupName="Future Signal Colors")]
		public Brush FutureSignalRiskOver
		{ get; set; }

		[Browsable(false)]
		public string FutureSignalRiskOverSerializable
		{
			get { return Serialize.BrushToString(FutureSignalRiskOver); }
			set { FutureSignalRiskOver = Serialize.StringToBrush(value); }
		}			

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="FutureSignalRiskUnder", Description="Color of risk when within max stop", Order=5, GroupName="Future Signal Colors")]
		public Brush FutureSignalRiskUnder
		{ get; set; }

		[Browsable(false)]
		public string FutureSignalRiskUnderSerializable
		{
			get { return Serialize.BrushToString(FutureSignalRiskUnder); }
			set { FutureSignalRiskUnder = Serialize.StringToBrush(value); }
		}			
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PAT.SecondEntry[] cacheSecondEntry;
		public PAT.SecondEntry SecondEntry(int target, int maxStop, int entryPlacement, int stopPlacement, int margin, bool isOnly2ndEntries, bool isResetDoubleBotTop, bool isSwapEntryDrawing, bool isSignalOn, bool isAlertOn, string alertSound, Brush isLongEntryBrush, Brush isShortEntryBrush, Brush futureSignalTarget, Brush futureSignalStop, Brush futureSignalEntry, Brush futureSignalRiskOver, Brush futureSignalRiskUnder)
		{
			return SecondEntry(Input, target, maxStop, entryPlacement, stopPlacement, margin, isOnly2ndEntries, isResetDoubleBotTop, isSwapEntryDrawing, isSignalOn, isAlertOn, alertSound, isLongEntryBrush, isShortEntryBrush, futureSignalTarget, futureSignalStop, futureSignalEntry, futureSignalRiskOver, futureSignalRiskUnder);
		}

		public PAT.SecondEntry SecondEntry(ISeries<double> input, int target, int maxStop, int entryPlacement, int stopPlacement, int margin, bool isOnly2ndEntries, bool isResetDoubleBotTop, bool isSwapEntryDrawing, bool isSignalOn, bool isAlertOn, string alertSound, Brush isLongEntryBrush, Brush isShortEntryBrush, Brush futureSignalTarget, Brush futureSignalStop, Brush futureSignalEntry, Brush futureSignalRiskOver, Brush futureSignalRiskUnder)
		{
			if (cacheSecondEntry != null)
				for (int idx = 0; idx < cacheSecondEntry.Length; idx++)
					if (cacheSecondEntry[idx] != null && cacheSecondEntry[idx].Target == target && cacheSecondEntry[idx].MaxStop == maxStop && cacheSecondEntry[idx].EntryPlacement == entryPlacement && cacheSecondEntry[idx].StopPlacement == stopPlacement && cacheSecondEntry[idx].Margin == margin && cacheSecondEntry[idx].IsOnly2ndEntries == isOnly2ndEntries && cacheSecondEntry[idx].IsResetDoubleBotTop == isResetDoubleBotTop && cacheSecondEntry[idx].IsSwapEntryDrawing == isSwapEntryDrawing && cacheSecondEntry[idx].IsSignalOn == isSignalOn && cacheSecondEntry[idx].IsAlertOn == isAlertOn && cacheSecondEntry[idx].AlertSound == alertSound && cacheSecondEntry[idx].isLongEntryBrush == isLongEntryBrush && cacheSecondEntry[idx].isShortEntryBrush == isShortEntryBrush && cacheSecondEntry[idx].FutureSignalTarget == futureSignalTarget && cacheSecondEntry[idx].FutureSignalStop == futureSignalStop && cacheSecondEntry[idx].FutureSignalEntry == futureSignalEntry && cacheSecondEntry[idx].FutureSignalRiskOver == futureSignalRiskOver && cacheSecondEntry[idx].FutureSignalRiskUnder == futureSignalRiskUnder && cacheSecondEntry[idx].EqualsInput(input))
						return cacheSecondEntry[idx];
			return CacheIndicator<PAT.SecondEntry>(new PAT.SecondEntry(){ Target = target, MaxStop = maxStop, EntryPlacement = entryPlacement, StopPlacement = stopPlacement, Margin = margin, IsOnly2ndEntries = isOnly2ndEntries, IsResetDoubleBotTop = isResetDoubleBotTop, IsSwapEntryDrawing = isSwapEntryDrawing, IsSignalOn = isSignalOn, IsAlertOn = isAlertOn, AlertSound = alertSound, isLongEntryBrush = isLongEntryBrush, isShortEntryBrush = isShortEntryBrush, FutureSignalTarget = futureSignalTarget, FutureSignalStop = futureSignalStop, FutureSignalEntry = futureSignalEntry, FutureSignalRiskOver = futureSignalRiskOver, FutureSignalRiskUnder = futureSignalRiskUnder }, input, ref cacheSecondEntry);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PAT.SecondEntry SecondEntry(int target, int maxStop, int entryPlacement, int stopPlacement, int margin, bool isOnly2ndEntries, bool isResetDoubleBotTop, bool isSwapEntryDrawing, bool isSignalOn, bool isAlertOn, string alertSound, Brush isLongEntryBrush, Brush isShortEntryBrush, Brush futureSignalTarget, Brush futureSignalStop, Brush futureSignalEntry, Brush futureSignalRiskOver, Brush futureSignalRiskUnder)
		{
			return indicator.SecondEntry(Input, target, maxStop, entryPlacement, stopPlacement, margin, isOnly2ndEntries, isResetDoubleBotTop, isSwapEntryDrawing, isSignalOn, isAlertOn, alertSound, isLongEntryBrush, isShortEntryBrush, futureSignalTarget, futureSignalStop, futureSignalEntry, futureSignalRiskOver, futureSignalRiskUnder);
		}

		public Indicators.PAT.SecondEntry SecondEntry(ISeries<double> input , int target, int maxStop, int entryPlacement, int stopPlacement, int margin, bool isOnly2ndEntries, bool isResetDoubleBotTop, bool isSwapEntryDrawing, bool isSignalOn, bool isAlertOn, string alertSound, Brush isLongEntryBrush, Brush isShortEntryBrush, Brush futureSignalTarget, Brush futureSignalStop, Brush futureSignalEntry, Brush futureSignalRiskOver, Brush futureSignalRiskUnder)
		{
			return indicator.SecondEntry(input, target, maxStop, entryPlacement, stopPlacement, margin, isOnly2ndEntries, isResetDoubleBotTop, isSwapEntryDrawing, isSignalOn, isAlertOn, alertSound, isLongEntryBrush, isShortEntryBrush, futureSignalTarget, futureSignalStop, futureSignalEntry, futureSignalRiskOver, futureSignalRiskUnder);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PAT.SecondEntry SecondEntry(int target, int maxStop, int entryPlacement, int stopPlacement, int margin, bool isOnly2ndEntries, bool isResetDoubleBotTop, bool isSwapEntryDrawing, bool isSignalOn, bool isAlertOn, string alertSound, Brush isLongEntryBrush, Brush isShortEntryBrush, Brush futureSignalTarget, Brush futureSignalStop, Brush futureSignalEntry, Brush futureSignalRiskOver, Brush futureSignalRiskUnder)
		{
			return indicator.SecondEntry(Input, target, maxStop, entryPlacement, stopPlacement, margin, isOnly2ndEntries, isResetDoubleBotTop, isSwapEntryDrawing, isSignalOn, isAlertOn, alertSound, isLongEntryBrush, isShortEntryBrush, futureSignalTarget, futureSignalStop, futureSignalEntry, futureSignalRiskOver, futureSignalRiskUnder);
		}

		public Indicators.PAT.SecondEntry SecondEntry(ISeries<double> input , int target, int maxStop, int entryPlacement, int stopPlacement, int margin, bool isOnly2ndEntries, bool isResetDoubleBotTop, bool isSwapEntryDrawing, bool isSignalOn, bool isAlertOn, string alertSound, Brush isLongEntryBrush, Brush isShortEntryBrush, Brush futureSignalTarget, Brush futureSignalStop, Brush futureSignalEntry, Brush futureSignalRiskOver, Brush futureSignalRiskUnder)
		{
			return indicator.SecondEntry(input, target, maxStop, entryPlacement, stopPlacement, margin, isOnly2ndEntries, isResetDoubleBotTop, isSwapEntryDrawing, isSignalOn, isAlertOn, alertSound, isLongEntryBrush, isShortEntryBrush, futureSignalTarget, futureSignalStop, futureSignalEntry, futureSignalRiskOver, futureSignalRiskUnder);
		}
	}
}

#endregion
