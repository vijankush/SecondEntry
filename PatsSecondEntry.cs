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
		
		private bool Debug = false;
		
		// Long Entries
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
		private int SecondEntryBarnum = 0;
		private long startTime = 0;
		private	long endTime = 0;
		
		// Long trades
		private double SecodEntryLongTarget = 0.00;
		private double SecodEntryLongStop = 0.0;
		private bool 	FailedSecondEntry = false;
		private bool 	In2EfailTrade = false;
		private int 	LongTradeCount = 0;
		private int 	LongTradeLossCount = 0;
		
		// Short Entries
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
		private int 	TicksToRecalc = 0;
		private double 	Padding = 0;
		
		// Short trades
		private double 	SecodEntryShortTarget = 0.00;
		private double 	SecodEntryShortStop = 0.0;
		private bool 	FailedSecondEntryShort = false;
		private bool 	In2EfailShortTrade = false;
		private int		ShortTradeCount = 0;
		private int 	ShortTradeLossCount = 0;
		
		// failed 2nd entry 
		private int		ShortFailedTradeCount = 0;
		private int		LongFailedTradeCount = 0;
		
		// buttons
		private System.Windows.Controls.Button myBuyButton;
		private System.Windows.Controls.Button mySellButton;
		private System.Windows.Controls.Grid   myGrid;
		private bool BuyButtonIsOn = true;
		private bool SellButtonIsOn = true;
			
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
				FailedEntrySound							= @"FailedEntrySound.wav"; 
				MinBars 									= 15;
				//LongsOn										= true;
				//ShortOn										= true;
				DotPadding 									= 2;
				ShowDots									= true;
				TargetTicks 								= 8;
				ShowFail2ndEntries							= true;
				ShowStats									= true;
				NoteFont									= new SimpleFont("Arial", 12); 
				ShowStopsTargets							= true;
				StatsBkgColor								= Brushes.WhiteSmoke;
				StatsBkgOpacity								= 90;
				StartTime						= DateTime.Parse("08:00", System.Globalization.CultureInfo.InvariantCulture);
				EndTime						= DateTime.Parse("15:00", System.Globalization.CultureInfo.InvariantCulture);
			}
			else if (State == State.Configure)
			{ 
				ClearOutputWindow();
				startTime = long.Parse(StartTime.ToString("HHmmss"));
			 	endTime = long.Parse(EndTime.ToString("HHmmss"));
			}
			// Once the NinjaScript object has reached State.Historical, our custom control can now be added to the chart
			else if (State == State.Historical)
			  {
				  Print("State.Historical");
			    // Because we're dealing with UI elements, we need to use the Dispatcher which created the object
			    // otherwise we will run into threading errors...
			    // e.g, "Error on calling 'OnStateChange' method: You are accessing an object which resides on another thread."
			    // Furthermore, we will do this operation Asynchronously to avoid conflicts with internal NT operations
			    ChartControl.Dispatcher.InvokeAsync((() =>
			    {
			        // Grid already exists
			        if (UserControlCollection.Contains(myGrid))
			          return;
			 
			        // Add a control grid which will host our custom buttons
			        myGrid = new System.Windows.Controls.Grid
			        {
			          Name = "MyCustomGrid",
			          // Align the control to the top right corner of the chart
			          HorizontalAlignment = HorizontalAlignment.Right,
			          VerticalAlignment = VerticalAlignment.Bottom,
			        };
			 
			        // Define the two columns in the grid, one for each button
			        System.Windows.Controls.ColumnDefinition column1 = new System.Windows.Controls.ColumnDefinition();
			        System.Windows.Controls.ColumnDefinition column2 = new System.Windows.Controls.ColumnDefinition();
			 
			        // Add the columns to the Grid
			        myGrid.ColumnDefinitions.Add(column1);
			        myGrid.ColumnDefinitions.Add(column2);
			 
			        // Define the custom Buy Button control object
			        myBuyButton = new System.Windows.Controls.Button
			        {
			          Name = "MyBuyButton",
			          Content = "LONG",
			          Foreground = Brushes.White,
			          Background = Brushes.LimeGreen
			        };
			 
			        // Define the custom Sell Button control object
			        mySellButton = new System.Windows.Controls.Button
			        {
			          Name = "MySellButton",
			          Content = "SHORT",
			          Foreground = Brushes.White,
			          Background = Brushes.Red
			        };
			 
			        // Subscribe to each buttons click event to execute the logic we defined in OnMyButtonClick()
			        myBuyButton.Click += OnMyButtonClick;
			        mySellButton.Click += OnMyButtonClick;
			 
			        // Define where the buttons should appear in the grid
			        System.Windows.Controls.Grid.SetColumn(myBuyButton, 0);
			        System.Windows.Controls.Grid.SetColumn(mySellButton, 1);
			 
			        // Add the buttons as children to the custom grid
			        myGrid.Children.Add(myBuyButton);
			        myGrid.Children.Add(mySellButton);
			 
			        // Finally, add the completed grid to the custom NinjaTrader UserControlCollection
			        UserControlCollection.Add(myGrid);
			 
			    }));
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
			
			// get bar size for re draw
			int Bsize = BarsPeriod.Value;
			double pctSize = (double)Bsize * 0.95;
			TicksToRecalc = (int)pctSize;
			// if ( Debug ) { Print(Bsize + " pct " + TicksToRecalc); }
			Padding = (double)DotPadding * TickSize;
			
			if (ToTime(Time[0]) < startTime  || ToTime(Time[0]) > endTime) { return; }
			
			if ( BuyButtonIsOn ) { 
				///**************	find new high or within 1 tick of high  ***************
				
				if (High[0] >= MAX(High, MinBars)[1] - TickSize ) { 
					RemoveDrawObject("NewHigh"+LastBar); 
					if ( ShowDots ) { Draw.Diamond(this, "NewHigh"+CurrentBar, false, 0, High[0] + Padding, PivotColor); }
					NewHigBarnum = CurrentBar;
					NewHighPrice = High[0];
					FoundFirstEntry = false;
					SecondEntrySetupBar = false;
					FirstEntryBarnum = 0;
					FoundSecondEntry = false;
		// return; 
				}
				
				///**************	find first entry long  ***************
		
				BarsSinceHigh = CurrentBar - NewHigBarnum;
				if (BarsSinceHigh >= 2) {SeekingFirstEntry = true;}
				double DistanceToHigh = NewHighPrice - High[0];
				if (DistanceToHigh > 1.0 && High[0] > High[1]  && SeekingFirstEntry && !FoundFirstEntry ) {
					Draw.Text(this, "1stEntry"+CurrentBar, "1", 0, Low[0] - Padding * 2, TextColor);
					SeekingFirstEntry = false;
					FoundFirstEntry = true;
					FirstEntryBarnum = CurrentBar;
					FailedSecondEntry = false;
					if ( AlertOn ) {
						Alert("FirstEntry"+CurrentBar, Priority.High, "First Entry", 
						NinjaTrader.Core.Globals.InstallDir+@"\sounds\"+ FirstEntrySound,10, 
						Brushes.Black, Brushes.Yellow);  
					}
				}
				
				// end of bar if low is lower than prior print, remove the text, add a 1 below
				if( FirstEntryBarnum == CurrentBar && Bars.TickCount >= TicksToRecalc ) {
					RemoveDrawObject("1stEntry"+CurrentBar);
					Draw.Text(this, "1stEntry"+CurrentBar, "1", 0, Low[0] - Padding * 2, TextColor);
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
							Draw.TriangleUp(this, "2EL"+CurrentBar, false, 0, Low[0] -Padding * 2, TextColor);
							FoundSecondEntry = true;
							SecondEntryBarnum = CurrentBar;
							SecodEntryLongTarget = High[1] + TickSize + ((double)TargetTicks * TickSize);
							SecodEntryLongStop = Low[0] - TickSize;
							LongTradeCount  += 1;
							
							if ( ShowStopsTargets ) {
								Draw.Text(this, "tgt" + CurrentBar, "-", 0, SecodEntryLongTarget, TextColor);
								Draw.Text(this, "stop" + CurrentBar, "-", 0, SecodEntryLongStop, ShortTextColor);
							}
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
				if( SecondEntryBarnum == CurrentBar && Bars.TickCount >= TicksToRecalc ) {
					// RemoveDrawObject("SecondEntryLine"+CurrentBar);
					RemoveDrawObject("2EL"+CurrentBar);
					Draw.TriangleUp(this, "2EL"+CurrentBar, false, 0, Low[0] - Padding * 2, TextColor);
					
					SecodEntryLongStop = Low[0] - TickSize;
					RemoveDrawObject("stop"+CurrentBar);
					Draw.Text(this, "stop" + CurrentBar, "-", 0, SecodEntryLongStop, Brushes.Red);
				}
			}
			
			///******************************************************************************************
			///**************************	Failed 2nd Entry Long	*************************************
			///**************************	Results In Short Entry	*************************************
			///******************************************************************************************
			
			// first check for target hit
			if (FoundSecondEntry &&  High[1] > SecodEntryLongTarget ) {
				FailedSecondEntry = true;
				return;
			}
					
			if ( FoundSecondEntry && ShowFail2ndEntries && IsFirstTickOfBar && !FailedSecondEntry ) {
				int BarsSinceEntry = CurrentBar - SecondEntryBarnum;
				if ( BarsSinceEntry >= 0 && BarsSinceEntry <= 6 )  {
					
					// check for failed 2nd entry
					if ( Low[0] <= SecodEntryLongStop  ) {
						FailedSecondEntry = true;
						LongTradeLossCount += 1;
						Draw.Text(this, "FailedSecondEntry" + CurrentBar, "-----X", 0, SecodEntryLongStop, ShortTextColor);
						//RemoveDrawObject("FailedSecondEntry"+LastBar);
						//Draw.Line(this, "FailedSecondEntry", 2, SecodEntryLongStop, -5, SecodEntryLongStop, Brushes.Red);
						LongFailedTradeCount += 1;
						// alert
						if ( AlertOn ) {
							Alert("FailedSecondEntry"+CurrentBar, Priority.High, " FailedSecond Entry", 
							NinjaTrader.Core.Globals.InstallDir+@"\sounds\"+ FailedEntrySound,10, 
							Brushes.Black, Brushes.Yellow); 	
						}
					} else {
						FailedSecondEntry = false;
					}
				}
				
			}
			
			///******************************************************************************************
			///*********************************	Short	*********************************************
			///******************************************************************************************
			
			if ( SellButtonIsOn ) { 
			
				///**************	find new low or within 1 tick of low  ***************
				
				if (Low[0] <= MIN(Low, MinBars)[1] + TickSize ) { 
					RemoveDrawObject("NewLow"+LastBar); 
					//Draw.Diamond(this, "NewLow"+CurrentBar, false, 0, Low[0], PivotColor);
					if ( ShowDots ) { Draw.Dot(this, "NewLow"+CurrentBar, false, 0, Low[0] - Padding, ShortPivotColor);}
					NewLowBarnum = CurrentBar;
					NewLowPrice = Low[0];
					FoundFirstEntryShort = false;
					SecondEntrySetupBarShort = false;
					FirstEntryBarnumShort = 0;
					FoundSecondEntryShort = false;
					FailedSecondEntryShort = false;
					//return;
				}
				
				///**************	find first entry short  ***************
		
				BarsSinceLow = CurrentBar - NewLowBarnum;
				if (BarsSinceLow >= 2) {SeekingFirstEntryShort = true;}
				double DistanceToLow = Low[0] - NewLowPrice;
				if (DistanceToLow > 1.0 && Low[0] < Low[1]  && SeekingFirstEntryShort && !FoundFirstEntryShort ) {
					Draw.Text(this, "1stEntryShort"+CurrentBar, "1", 0, High[0] + Padding * 2, ShortTextColor);
					SeekingFirstEntryShort = false;
					FoundFirstEntryShort = true;
					FirstEntryBarnumShort = CurrentBar;
					if ( AlertOn ) {
						Alert("FirstEntryShort"+CurrentBar, Priority.High, "First Entry Short", 
						NinjaTrader.Core.Globals.InstallDir+@"\sounds\"+ FirstEntrySound,10, 
						Brushes.Black, Brushes.Yellow);  
					}
				}
				
				// end of bar if high is higher than prior print, remove the text, add a 1 below
				if( FirstEntryBarnumShort == CurrentBar && Bars.TickCount >= TicksToRecalc ) {
					RemoveDrawObject("1stEntryShort"+CurrentBar);
					Draw.Text(this, "1stEntryShort"+CurrentBar, "1", 0, High[0] + Padding * 2, ShortTextColor);
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
							Draw.TriangleDown(this, "2ES"+CurrentBar, false, 0, High[0] + Padding * 2, ShortTextColor);
							FoundSecondEntryShort = true;
							SecondEntryBarnumShort = CurrentBar;
							
							SecodEntryShortTarget = Low[1] - TickSize - ((double)TargetTicks * TickSize);
							SecodEntryShortStop = High[0] + TickSize;
							ShortTradeCount  += 1;
							
							if ( ShowStopsTargets ) {
								Draw.Text(this, "tgtS" + CurrentBar, "-", 0, SecodEntryShortTarget, TextColor);
								Draw.Text(this, "stopS" + CurrentBar, "-", 0, SecodEntryShortStop, ShortTextColor);
							}
							
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
			
			}
			// end of bar if high is higher than prior print, remove the triangle, add a triangle below
			if( SecondEntryBarnumShort == CurrentBar && Bars.TickCount >= TicksToRecalc ) {
				RemoveDrawObject("2ES"+CurrentBar);
				Draw.TriangleDown(this, "2ES"+CurrentBar, false, 0, High[0] + Padding * 2, ShortTextColor); 
			}
			
			///******************************************************************************************
			///**************************	Failed 2nd Entry Short	*************************************
			///**************************	Results In Long Entry	*************************************
			///******************************************************************************************
			
			// first check for target hit
			if (FoundSecondEntryShort &&  Low[1] < SecodEntryShortTarget ) {
				FailedSecondEntryShort = true;
				return;
			}
			
			if ( FoundSecondEntryShort && ShowFail2ndEntries && IsFirstTickOfBar && !FailedSecondEntryShort ) {
				int BarsSinceEntry = CurrentBar - SecondEntryBarnumShort;
				if ( BarsSinceEntry >= 0 && BarsSinceEntry <= 6 )  {
					
					// check for failed 2nd entry
					if ( High[0] >= SecodEntryShortStop  ) {
						FailedSecondEntryShort = true;
						ShortTradeLossCount += 1;
						Draw.Text(this, "FailedSecondEntryShort" + CurrentBar, "-----X", 0, SecodEntryShortStop, TextColor);
						//RemoveDrawObject("FailedSecondEntry"+LastBar);
						//Draw.Line(this, "FailedSecondEntry", 2, SecodEntryLongStop, -5, SecodEntryLongStop, Brushes.Red);
						ShortFailedTradeCount += 1; 
						// alert
						if ( AlertOn ) {
							Alert("FailedSecondEntryShort"+CurrentBar, Priority.High, " Failed Second Entry Short", 
							NinjaTrader.Core.Globals.InstallDir+@"\sounds\"+ FailedEntrySound,10, 
							Brushes.Black, Brushes.Yellow); 	
						}
					} else {
						FailedSecondEntryShort = false;
					}
				}
			}
			
			 ShowStatistics();
		}

		#region Helpers
		
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
				Draw.Line(this, LineName+CurrentBar, 1, EntryPrice, -2, EntryPrice, lineColor);
				RemoveDrawObject(LineName+"Txt"+LastBar); 
				Draw.Text(this, LineName+"Txt"+CurrentBar, EntryPrice.ToString(), -4, EntryPrice, lineColor);
			}
		}
		
		private void ShowStatistics() { 
			if (ShowStats) {
				string AllStats = "S t a t i s t i c s\nLong Trade Count " + LongTradeCount;
				int LongWins = LongTradeCount - LongTradeLossCount;
				AllStats += "\nLong Wins " + LongWins;
				double WinPctLong = ((double)LongWins / (double)LongTradeCount) * 100;
				AllStats += "\n" + WinPctLong.ToString("N1") + "% Win Long";
				AllStats += "\n"; 
				
				AllStats += "\nShort Trade Count " + ShortTradeCount;
				int ShortWins = ShortTradeCount - ShortTradeLossCount;
				AllStats += "\nShort Wins " + ShortWins;
				double WinPctShort = ((double)ShortWins / (double)ShortTradeCount) * 100;
				AllStats += "\n" + WinPctShort.ToString("N1") + "% Win Short";
				AllStats += "\n"; 
				
				AllStats += "\nLong Failed 2nd Entry Trade Count " + LongFailedTradeCount;
				AllStats += "\nShort Failed 2nd Entry Trade Count " + ShortFailedTradeCount;
				AllStats += "\n";
				
				Draw.TextFixed(this, "AllStats", AllStats, StatsLocation, TextColor, 
					NoteFont, Brushes.Transparent, StatsBkgColor, StatsBkgOpacity); 
			}
		}
			
		#endregion
		
		#region Button Logic
		// Define a custom event method to handle our custom task when the button is clicked
		private void OnMyButtonClick(object sender, RoutedEventArgs rea)
		{
		  System.Windows.Controls.Button button = sender as System.Windows.Controls.Button;
		  if (button != null) {
			  
			  if ( button.Name == "MyBuyButton") {
				 BuyButtonToggle( b: button); 
			  } else {
				  if ( SellButtonIsOn ) {
					  mySellButton.Background = Brushes.DarkRed;
					  mySellButton.Foreground = Brushes.Black;
				  } else {
					  mySellButton.Background = Brushes.Red;
					  mySellButton.Foreground = Brushes.White;
				  }
					SellButtonIsOn = !SellButtonIsOn;
				  	Print(button.Name + " Clicked and SellButtonIsOn is " + SellButtonIsOn);
			  }
		  }
		}
		
		private void BuyButtonToggle(System.Windows.Controls.Button b) {
			if ( BuyButtonIsOn ) {
				  myBuyButton.Background = Brushes.DarkGreen;
				  myBuyButton.Foreground = Brushes.Black;
			  } else {
				  myBuyButton.Background = Brushes.LimeGreen;
				  myBuyButton.Foreground = Brushes.White;
			  }
				BuyButtonIsOn = !BuyButtonIsOn;
				Print(b.Name + " Clicked and BuyButtonIsOn is " + BuyButtonIsOn);
		}
		#endregion
		
		#region Properties
		
		[NinjaScriptProperty]
		[Display(Name="Sound Alerts On", Order=1, GroupName="Parameters")]
		public bool AlertOn
		{ get; set; }
		/*
		[NinjaScriptProperty]
		[Display(Name="Longs On", Order=11, GroupName="Parameters")]
		public bool LongsOn
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Short On", Order=12, GroupName="Parameters")]
		public bool ShortOn
		{ get; set; }
		*/
		[NinjaScriptProperty]
		[Display(Name="Show Failed 2nd Entries", Order=2, GroupName="Parameters")]
		public bool ShowFail2ndEntries
		{ get; set; }
		
		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="Start Time", Order=3, GroupName="Parameters")]
		public DateTime StartTime
		{ get; set; }
		
		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="End Time", Order=4, GroupName="Parameters")]
		public DateTime EndTime
		{ get; set; }
		
		// ----------------------   colors   ---------------------------------------
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="Long Text Color", Order=1, GroupName="Colors")]
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
		[Display(Name="High Pivot Color", Order=2, GroupName="Colors")]
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
		[Display(Name="Short Text Color", Order=3, GroupName="Colors")]
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
		[Display(Name="Low Pivot Color", Order=4, GroupName="Colors")]
		public Brush ShortPivotColor
		{ get; set; }

		[Browsable(false)]
		public string ShortPivotColorSerializable
		{
			get { return Serialize.BrushToString(ShortPivotColor); }
			set { ShortPivotColor = Serialize.StringToBrush(value); }
		}
		
		// ----------------------   swings   ---------------------------------------
		[NinjaScriptProperty]
		[Display(Name="Swing Markers", Order=1, GroupName="Swings")]
		public bool ShowDots
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Swing Padding", Order=2, GroupName="Swings")]
		public int DotPadding
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Swing Lookback", Order=3, GroupName="Swings")]
		public int MinBars
		{ get; set; }
		
		// ----------------------   sound   ---------------------------------------
		[NinjaScriptProperty]
		[Display(Name="2nd entry Sound", Order=1, GroupName="Sound")]
		public string AlertSound
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="First Entry Sound", Order=2, GroupName="Sound")]
		public string FirstEntrySound
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Failed Entry Sound", Order=3, GroupName="Sound")]
		public string FailedEntrySound
		{ get; set; }
		
		// ----------------------   statistics   ---------------------------------------
		[NinjaScriptProperty]
		[Display(Name="Show Statistics", Order=1, GroupName="Statistics")]
		public bool ShowStats
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Show Targets + Stops", Order=2, GroupName="Statistics")]
		public bool ShowStopsTargets
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Target in Ticks", Order=3, GroupName="Statistics")]
		public int TargetTicks
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Font", Description="Font", Order=4, GroupName="Statistics")]
		public SimpleFont NoteFont
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Location", Description="Stats Location", Order=5, GroupName="Statistics")]
		public TextPosition StatsLocation
		{ get; set; }
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="Background Color", Order=6, GroupName="Statistics")]
		public Brush StatsBkgColor
		{ get; set; }

		[Browsable(false)]
		public string StatsBkgColorSerializable
		{
			get { return Serialize.BrushToString(StatsBkgColor); }
			set { StatsBkgColor = Serialize.StringToBrush(value); }
		}
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Background Opacity", Order=7, GroupName="Statistics")]
		public int StatsBkgOpacity
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
		public PatsSecondEntry PatsSecondEntry(bool alertOn, bool showFail2ndEntries, DateTime startTime, DateTime endTime, Brush textColor, Brush pivotColor, Brush shortTextColor, Brush shortPivotColor, bool showDots, int dotPadding, int minBars, string alertSound, string firstEntrySound, string failedEntrySound, bool showStats, bool showStopsTargets, int targetTicks, SimpleFont noteFont, TextPosition statsLocation, Brush statsBkgColor, int statsBkgOpacity)
		{
			return PatsSecondEntry(Input, alertOn, showFail2ndEntries, startTime, endTime, textColor, pivotColor, shortTextColor, shortPivotColor, showDots, dotPadding, minBars, alertSound, firstEntrySound, failedEntrySound, showStats, showStopsTargets, targetTicks, noteFont, statsLocation, statsBkgColor, statsBkgOpacity);
		}

		public PatsSecondEntry PatsSecondEntry(ISeries<double> input, bool alertOn, bool showFail2ndEntries, DateTime startTime, DateTime endTime, Brush textColor, Brush pivotColor, Brush shortTextColor, Brush shortPivotColor, bool showDots, int dotPadding, int minBars, string alertSound, string firstEntrySound, string failedEntrySound, bool showStats, bool showStopsTargets, int targetTicks, SimpleFont noteFont, TextPosition statsLocation, Brush statsBkgColor, int statsBkgOpacity)
		{
			if (cachePatsSecondEntry != null)
				for (int idx = 0; idx < cachePatsSecondEntry.Length; idx++)
					if (cachePatsSecondEntry[idx] != null && cachePatsSecondEntry[idx].AlertOn == alertOn && cachePatsSecondEntry[idx].ShowFail2ndEntries == showFail2ndEntries && cachePatsSecondEntry[idx].StartTime == startTime && cachePatsSecondEntry[idx].EndTime == endTime && cachePatsSecondEntry[idx].TextColor == textColor && cachePatsSecondEntry[idx].PivotColor == pivotColor && cachePatsSecondEntry[idx].ShortTextColor == shortTextColor && cachePatsSecondEntry[idx].ShortPivotColor == shortPivotColor && cachePatsSecondEntry[idx].ShowDots == showDots && cachePatsSecondEntry[idx].DotPadding == dotPadding && cachePatsSecondEntry[idx].MinBars == minBars && cachePatsSecondEntry[idx].AlertSound == alertSound && cachePatsSecondEntry[idx].FirstEntrySound == firstEntrySound && cachePatsSecondEntry[idx].FailedEntrySound == failedEntrySound && cachePatsSecondEntry[idx].ShowStats == showStats && cachePatsSecondEntry[idx].ShowStopsTargets == showStopsTargets && cachePatsSecondEntry[idx].TargetTicks == targetTicks && cachePatsSecondEntry[idx].NoteFont == noteFont && cachePatsSecondEntry[idx].StatsLocation == statsLocation && cachePatsSecondEntry[idx].StatsBkgColor == statsBkgColor && cachePatsSecondEntry[idx].StatsBkgOpacity == statsBkgOpacity && cachePatsSecondEntry[idx].EqualsInput(input))
						return cachePatsSecondEntry[idx];
			return CacheIndicator<PatsSecondEntry>(new PatsSecondEntry(){ AlertOn = alertOn, ShowFail2ndEntries = showFail2ndEntries, StartTime = startTime, EndTime = endTime, TextColor = textColor, PivotColor = pivotColor, ShortTextColor = shortTextColor, ShortPivotColor = shortPivotColor, ShowDots = showDots, DotPadding = dotPadding, MinBars = minBars, AlertSound = alertSound, FirstEntrySound = firstEntrySound, FailedEntrySound = failedEntrySound, ShowStats = showStats, ShowStopsTargets = showStopsTargets, TargetTicks = targetTicks, NoteFont = noteFont, StatsLocation = statsLocation, StatsBkgColor = statsBkgColor, StatsBkgOpacity = statsBkgOpacity }, input, ref cachePatsSecondEntry);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PatsSecondEntry PatsSecondEntry(bool alertOn, bool showFail2ndEntries, DateTime startTime, DateTime endTime, Brush textColor, Brush pivotColor, Brush shortTextColor, Brush shortPivotColor, bool showDots, int dotPadding, int minBars, string alertSound, string firstEntrySound, string failedEntrySound, bool showStats, bool showStopsTargets, int targetTicks, SimpleFont noteFont, TextPosition statsLocation, Brush statsBkgColor, int statsBkgOpacity)
		{
			return indicator.PatsSecondEntry(Input, alertOn, showFail2ndEntries, startTime, endTime, textColor, pivotColor, shortTextColor, shortPivotColor, showDots, dotPadding, minBars, alertSound, firstEntrySound, failedEntrySound, showStats, showStopsTargets, targetTicks, noteFont, statsLocation, statsBkgColor, statsBkgOpacity);
		}

		public Indicators.PatsSecondEntry PatsSecondEntry(ISeries<double> input , bool alertOn, bool showFail2ndEntries, DateTime startTime, DateTime endTime, Brush textColor, Brush pivotColor, Brush shortTextColor, Brush shortPivotColor, bool showDots, int dotPadding, int minBars, string alertSound, string firstEntrySound, string failedEntrySound, bool showStats, bool showStopsTargets, int targetTicks, SimpleFont noteFont, TextPosition statsLocation, Brush statsBkgColor, int statsBkgOpacity)
		{
			return indicator.PatsSecondEntry(input, alertOn, showFail2ndEntries, startTime, endTime, textColor, pivotColor, shortTextColor, shortPivotColor, showDots, dotPadding, minBars, alertSound, firstEntrySound, failedEntrySound, showStats, showStopsTargets, targetTicks, noteFont, statsLocation, statsBkgColor, statsBkgOpacity);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PatsSecondEntry PatsSecondEntry(bool alertOn, bool showFail2ndEntries, DateTime startTime, DateTime endTime, Brush textColor, Brush pivotColor, Brush shortTextColor, Brush shortPivotColor, bool showDots, int dotPadding, int minBars, string alertSound, string firstEntrySound, string failedEntrySound, bool showStats, bool showStopsTargets, int targetTicks, SimpleFont noteFont, TextPosition statsLocation, Brush statsBkgColor, int statsBkgOpacity)
		{
			return indicator.PatsSecondEntry(Input, alertOn, showFail2ndEntries, startTime, endTime, textColor, pivotColor, shortTextColor, shortPivotColor, showDots, dotPadding, minBars, alertSound, firstEntrySound, failedEntrySound, showStats, showStopsTargets, targetTicks, noteFont, statsLocation, statsBkgColor, statsBkgOpacity);
		}

		public Indicators.PatsSecondEntry PatsSecondEntry(ISeries<double> input , bool alertOn, bool showFail2ndEntries, DateTime startTime, DateTime endTime, Brush textColor, Brush pivotColor, Brush shortTextColor, Brush shortPivotColor, bool showDots, int dotPadding, int minBars, string alertSound, string firstEntrySound, string failedEntrySound, bool showStats, bool showStopsTargets, int targetTicks, SimpleFont noteFont, TextPosition statsLocation, Brush statsBkgColor, int statsBkgOpacity)
		{
			return indicator.PatsSecondEntry(input, alertOn, showFail2ndEntries, startTime, endTime, textColor, pivotColor, shortTextColor, shortPivotColor, showDots, dotPadding, minBars, alertSound, firstEntrySound, failedEntrySound, showStats, showStopsTargets, targetTicks, noteFont, statsLocation, statsBkgColor, statsBkgOpacity);
		}
	}
}

#endregion
