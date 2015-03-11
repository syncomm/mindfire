//
// MindFire (c) Gregory S. Hayes 2000-2005 <syncomm@gmail.com>
//
//
// This program is designed to improve reading speed with
// a technique known as Rapid Serial Visual Presentation.
// This technique can stop subvocalization and reduce eye lag.
//

using System;
using System.IO;
using System.Text.RegularExpressions;
using Gtk;
using Glade;

public class MindFire
{
		
		[Widget] Gtk.Label imageCanvas;
		[Widget] Gtk.TextView textView;
		[Widget] Gtk.ScrolledWindow scrolledWindow;
		[Widget] Gtk.CheckMenuItem showTextBox;
		[Widget] Gtk.Statusbar statusBar;
		[Widget] Gtk.Toolbar toolBar;
		[Widget] Gtk.MenuItem startMenuItem;
		[Widget] Gtk.MenuItem stopMenuItem;
		[Widget] Gtk.Window mindFire;
		[Widget] Gtk.Button startButton;
		[Widget] Gtk.Button stopButton;
		[Widget] Gtk.Button prevButton;
		[Widget] Gtk.Button nextButton;
		[Widget] Gtk.Notebook noteBook;
		[Widget] Gtk.EventBox eventBox;
		[Widget] Gtk.HScale slider;
		[Widget] Gtk.Label ftLabel;
		[Widget] Gtk.Label riLabel;
		[Widget] Gtk.Label sgLabel;
		[Widget] Gtk.Label mlLabel;
		[Widget] Gtk.Button speedUp;
		[Widget] Gtk.Button speedDown;
		[Widget] Gtk.ProgressBar speedbar;
		
		private Gdk.Pixbuf playButtonPixbuf;
		private Gtk.Image playButtonImage;
		
		private Gdk.Pixbuf stopButtonPixbuf;
		private Gtk.Image stopButtonImage;
		
		private Gdk.Pixbuf prevButtonPixbuf;
		private Gtk.Image prevButtonImage;
		
		private Gdk.Pixbuf nextButtonPixbuf;
		private Gtk.Image nextButtonImage;
		
		private Gtk.TextTag tag;
		
		private Gdk.Pixbuf icon;
		
		private Gtk.Image aboutDialogImage;
		private Gdk.Pixbuf aboutDialogPixbuf;
		private Gtk.TextBuffer textBuffer;
		
		private Gtk.TextIter endWordIter = new Gtk.TextIter ();
		private Gtk.TextIter startWordIter = new Gtk.TextIter ();
		private Gtk.TextIter endHilightIter = new Gtk.TextIter ();
		private Gtk.TextIter startHilightIter = new Gtk.TextIter ();
		
		private Gdk.Color fontColor = new Gdk.Color (87, 87, 194);
		private Gdk.Color backgroundColor = new Gdk.Color (0, 0, 0);
		private Gdk.Color hilightColor = new Gdk.Color (255, 255, 117);
		
		private Config config;
		
		uint timer;
		uint removeTimer;
		
		private System.DateTime dateTime;
		private int c;
		private int t;
		private int wao = 1;
		private int averageWordLength = 7;
		private int updateSpeedInterval = 3;
		private string fontName = "Sans Bold 24"; 
		private string curWord = "MindFire";
		private bool endPara;
		private bool running = false;
		private bool fileLoaded = false;
		private bool slideLock = false;
		
		private class SignalFuncHelper {
                public SignalFuncHelper (System.EventHandler e)
                {
                        this.e = e;
                }

                public void Func ()
                {
                        this.e (Sender, System.EventArgs.Empty);
                }

                System.EventHandler e;
                public object Sender;
        }
        
		
        public static void Main (string[] args)
        {
                new MindFire (args);
        }

        public MindFire (string[] args) 
        {
                Application.Init();
                Glade.XML gxml = new Glade.XML (null, "gui.glade", "mindFire", null);
                gxml.Autoconnect (this);
                
                playButtonPixbuf = new Gdk.Pixbuf (null, "media-play.png");
				playButtonImage = new Gtk.Image ();
				playButtonImage.Pixbuf = playButtonPixbuf;
				startButton.Add (playButtonImage);
				startButton.Sensitive = false;
                startButton.Clicked += new EventHandler (StartRsvp);
                startButton.ShowAll ();
                
                stopButtonPixbuf = new Gdk.Pixbuf (null, "media-stop.png");
				stopButtonImage = new Gtk.Image ();
				stopButtonImage.Pixbuf = stopButtonPixbuf;
				stopButton.Add (stopButtonImage);
                stopButton.Sensitive = false;
                stopButton.Clicked += new EventHandler (StopRsvp);
                stopButton.ShowAll ();
                
                prevButtonPixbuf = new Gdk.Pixbuf (null, "media-prev.png");
				prevButtonImage = new Gtk.Image ();
				prevButtonImage.Pixbuf = prevButtonPixbuf;
				prevButton.Add (prevButtonImage);
				prevButton.Sensitive = false;
                prevButton.Clicked += new EventHandler (PreviousWord);
                prevButton.ShowAll ();
                
                nextButtonPixbuf = new Gdk.Pixbuf (null, "media-next.png");
				nextButtonImage = new Gtk.Image ();
				nextButtonImage.Pixbuf = nextButtonPixbuf;
				nextButton.Add (nextButtonImage);
				nextButton.Sensitive = false;
                nextButton.Clicked += new EventHandler (NextWord);
                nextButton.ShowAll ();
                
                speedUp.Clicked += new EventHandler (SpeedUp);
                speedDown.Clicked += new EventHandler (SpeedDown);
                
                startMenuItem.Sensitive = false;
                stopMenuItem.Sensitive = false;
                slider.ValueChanged += new EventHandler (SliderMoved);
                noteBook.SwitchPage += new SwitchPageHandler (NoteBookChanged);
                icon = new Gdk.Pixbuf (null, "mindFireMonkey.png");
                mindFire.Icon = icon;
                
                dateTime = System.DateTime.Now;
                
                config = new Config ();
                                
                noteBook.SetTabLabelPacking(noteBook.GetNthPage (0), true, true, Gtk.PackType.Start);
				ftLabel.ModifyFg(Gtk.StateType.Active, new Gdk.Color (127, 127, 127));
				riLabel.ModifyFg(Gtk.StateType.Active, new Gdk.Color (127, 127, 127));
				sgLabel.ModifyFg(Gtk.StateType.Active, new Gdk.Color (127, 127, 127));
                mlLabel.ModifyFg(Gtk.StateType.Active, new Gdk.Color (127, 127, 127));
                
                statusBar.Push (0, "Ready");
                ShowWord(curWord);               
                Application.Run();
        }
                
        public void OnWindowDeleteEvent (object o, DeleteEventArgs args) 
        {
                Application.Quit ();
                args.RetVal = true;
        }
        
		public void OnQuitActivate (object o, EventArgs args)
		{
				Application.Quit ();
		}
		
		public void OnOpenActivate (object o, EventArgs args)
		{	
			string filename = null;
			//FileSelection openFileDialog = new FileSelection ("Open File");
			Gtk.FileFilter txtFileFilter = new Gtk.FileFilter ();
			txtFileFilter.AddPattern ("*.txt");
			txtFileFilter.Name = "Plain Text";
			Gtk.FileChooserDialog openFileDialog = new Gtk.FileChooserDialog ("Open file", mindFire, FileChooserAction.Open, Stock.Cancel, ResponseType.Cancel, Stock.Open, ResponseType.Ok);
			//openFileDialog.Complete ("*.txt");
			//openFileDialog.HideFileopButtons ();
			openFileDialog.AddFilter(txtFileFilter);
			openFileDialog.Run ();
			filename = openFileDialog.Filename;
			if (File.Exists(filename)) { 
				// Check mimetype
				OpenFile (filename); 
			}
			openFileDialog.Destroy ();
		}
		
		public void PreviousWord (object o, EventArgs args)
		{
			if (startWordIter.Equal(Gtk.TextIter.Zero)) {
				startWordIter = textBuffer.StartIter;
			}
			endWordIter = startWordIter;
			
			while (startWordIter.BackwardChar () && ! startWordIter.StartsWord ()) {
				
			}
			
			curWord = startWordIter.GetText(endWordIter);
			
			curWord = Regex.Replace(curWord, @"--", "", RegexOptions.Multiline);
			curWord = curWord.TrimStart(new char[5] {' ', '\n', '\t', '\r', '-'});	
			curWord = curWord.TrimEnd(new char[5] {' ', '\n', '\t', '\r', '-'});
			curWord = Regex.Replace(curWord, @"\s+", " ", RegexOptions.Multiline);
			//Console.WriteLine("Word: \"" + curWord +"\"");
			ShowWord(curWord);
			HilightWord();
			slideLock = true;
			slider.Value = endWordIter.Offset;
			slideLock = false;
			
			
		}
		
		public void NextWord (object o, EventArgs args)
		{
			if(!running) {
				if (endWordIter.Equal(Gtk.TextIter.Zero)) {
					endWordIter = textBuffer.StartIter;
				}
				startWordIter = endWordIter;
				curWord = GetNextWord ();
				if (curWord == "") { return; }
				HilightWord();
				ShowWord(curWord);
				slideLock = true;
				slider.Value = endWordIter.Offset;
				slideLock = false;
			}
		}
		
		public void StopRsvp (object o, EventArgs args)
		{
			stopButton.Sensitive = false;
			stopMenuItem.Sensitive = false;
			startButton.Sensitive = true;
			startMenuItem.Sensitive = true;
			
			running = false;
			c = 0;
			t = 0; 
			//Console.WriteLine(t);
			
			statusBar.Push(0, "Stopped");
		}
		
		public void OpenFile (string filename)
		{
			string text;
			
			TextReader reader = File.OpenText(filename);
			try {
				statusBar.Push(0, "Opening file " + filename);
				text = reader.ReadToEnd ();
			} finally {
				if (reader != null) {
					((IDisposable)reader).Dispose ();
				}
			}
			
			statusBar.Push(0, filename);
			textBuffer = textView.Buffer;
			textBuffer.Text = text;
			
			//endWordIter = textBuffer.StartIter;
			endWordIter = Gtk.TextIter.Zero;
			startWordIter = Gtk.TextIter.Zero;
			endHilightIter = Gtk.TextIter.Zero;
			startHilightIter = Gtk.TextIter.Zero;
			curWord = "";
			slider.SetRange (0, textBuffer.CharCount);			
			MakeHilightTag ();
			
			if (endWordIter.Equal(Gtk.TextIter.Zero)) {
				endWordIter = textBuffer.StartIter;
			}
			startWordIter = endWordIter;
			curWord = GetNextWord();
			ShowWord(curWord);
			HilightWord();
						
			fileLoaded = true;
			startButton.Sensitive = true;
			prevButton.Sensitive = true;
			nextButton.Sensitive = true;
		}
		
		public void OnFontActivate (object o, EventArgs args)
		{
			string fontname = null;
			FontSelectionDialog openFontSelection = new FontSelectionDialog ("RSVP Font");
			openFontSelection.SetFontName (fontName);
			openFontSelection.Run ();
			fontname = openFontSelection.FontName;
			fontName = fontname;
			ShowWord(curWord);
			openFontSelection.Destroy ();
		}
		
		public void OnShowTextBoxActivate (object o, EventArgs args)
		{
			if(!showTextBox.Active) {
				noteBook.Hide ();
			} else {
				noteBook.Show ();
			}	
		}
		
		public void OnAboutActivate (object o, EventArgs args)
		{
			Dialog aboutDialog = new Dialog ("About MindFire", mindFire, Gtk.DialogFlags.DestroyWithParent);
			aboutDialogPixbuf = new Gdk.Pixbuf (null, "aboutDialogImage.png");
			aboutDialogImage = new Gtk.Image ();
			aboutDialogImage.Pixbuf = aboutDialogPixbuf;
			aboutDialog.VBox.PackStart (aboutDialogImage, false, false, 0);
			
			Label copyrightLabel = new Label ("<span weight=\"bold\" size=\"small\">(c) 2005 Gregory S. Hayes\nAll Rights Reserved</span>");
			copyrightLabel.UseMarkup = true;
			copyrightLabel.Justify = Gtk.Justification.Center;
			aboutDialog.VBox.PackStart(copyrightLabel, false, false, 0);
			
			aboutDialog.VBox.ShowAll ();
			
			aboutDialog.AddButton ("Close",ResponseType.Close);
			aboutDialog.Run ();
			aboutDialog.Destroy ();
		}	
		
		public void ShowWord (string word)
		{
			imageCanvas.ModifyFg (Gtk.StateType.Normal, fontColor );
			eventBox.ModifyBg (Gtk.StateType.Normal, backgroundColor );
			imageCanvas.ModifyFont(Pango.FontDescription.FromString(fontName));                             
			imageCanvas.Text = word;
			imageCanvas.UseMarkup = false;
            imageCanvas.CanFocus = false;
			imageCanvas.ShowAll ();
		}
		
		public void OnFontColorActivate (object o, EventArgs args)
		{
			ColorSelectionDialog fontColorDialog = new ColorSelectionDialog ("RSVP Color");
			fontColorDialog.ColorSelection.CurrentColor = fontColor;
			fontColorDialog.ColorSelection.HasPalette = true;
			fontColorDialog.Run ();
			fontColor = fontColorDialog.ColorSelection.CurrentColor;
			ShowWord (curWord);
			fontColorDialog.Destroy ();
		}
		public void OnBackgroundColorActivate (object o, EventArgs args)
		{
			ColorSelectionDialog backgroundColorDialog = new ColorSelectionDialog ("RSVP Background Color");
			backgroundColorDialog.ColorSelection.CurrentColor = backgroundColor;
			backgroundColorDialog.ColorSelection.HasPalette = true;
			backgroundColorDialog.Run ();
			backgroundColor = backgroundColorDialog.ColorSelection.CurrentColor;
			ShowWord (curWord);
			backgroundColorDialog.Destroy ();
		}
		
		public void StartRsvp (object o, EventArgs args)
		{
			stopButton.Sensitive = true;
			stopMenuItem.Sensitive = true;
			startButton.Sensitive = false;
			startMenuItem.Sensitive = false;
			
			if (endWordIter.Equal(Gtk.TextIter.Zero)) {
					endWordIter = textBuffer.StartIter;
			}
			running = true;
			removeTimer = 0;
			statusBar.Push (0, "Reading...");
			DoRsvp ();	
		}
		
		public bool DoRsvp () 
		{
			if ( timer != 0 && removeTimer != 0 && timer <= removeTimer) {
				Console.WriteLine("Removed stray timer");
				return false;
			}
			
			removeTimer = timer;
			
			uint time;
			curWord = GetNextWord ();
			if (curWord == "") { return false; }
			ShowWord(curWord);
			HilightWord();
			slideLock = true;
			slider.Value = endWordIter.Offset;
			slideLock = false;
			time  = calcTimeForWord(curWord, wao, endPara);
			//time = 127;
			
			if ( running ) {
				timer = GLib.Timeout.Add (time, new GLib.TimeoutHandler(DoRsvp));
				//t = DateTime.Now.Second
			}
			
			return false;
		}
		
		public uint calcTimeForWord (string text, int numwords, bool endp)
		{
			//string workText;
			int modify = 0;
			int speed = config.RsvpSpeed;
			
			
			//workText = text;
			modify = ((text.Length - (config.AverageWordLength * numwords)) * 
				speed ) / (config.AverageWordLength * numwords);
			if (modify < 0) {
				modify = 0;
			}
			
			speed = speed + modify;
			//Console.WriteLine(text + " " + numwords + " " + endp + " " + speed + " " + modify);
			
			return Convert.ToUInt32 (speed);
		}
		
		public string GetNextWord ()
		{
			Gtk.TextIter prevCharIter = new Gtk.TextIter ();
			Gtk.TextIter nextCharIter = new Gtk.TextIter ();
			Gtk.TextIter prevWordEndIter = new Gtk.TextIter ();
			
			string text;
			string word;
			bool bail = false;
			
			endPara = false;
			wao = 1;
			
			startWordIter = endWordIter;
			// Need to trap last word see gnomeRSVP
			if(!endWordIter.ForwardWordEnd ()) { StopRsvp (null, null); }
			do
			{
				prevCharIter = endWordIter;
				prevCharIter.BackwardChar ();
				nextCharIter = endWordIter;
				nextCharIter.ForwardChar ();
				
				if (Regex.IsMatch(endWordIter.Char, @"^\S$", RegexOptions.None)  
				  && Regex.IsMatch(prevCharIter.Char, @"^\s$", RegexOptions.None) ) {
				  	
				  	text = startWordIter.GetText(endWordIter);
				  	text = Regex.Replace(text, @"--", "", RegexOptions.Multiline);
				  	text = text.TrimStart(new char[4] {' ', '\n', '\t', '-'});
				  	text = Regex.Replace(text, @"\s+", " ", RegexOptions.Multiline);
									  	
				  	if (text.Length <= config.WordGroupSize) {
				  		if ( endWordIter.InsideSentence() ) {
				  			prevWordEndIter = prevCharIter;
				  			wao++;
				  		}
				  	} else {
				  		bail = true;
				  		nextCharIter.BackwardChar ();
				  	}
				} else if (endWordIter.EndsSentence()) {
					bail = true;
					do
					{

					} while (endWordIter.EndsSentence() && endWordIter.ForwardChar());
					nextCharIter.BackwardChar();
					if (!endWordIter.IsEnd) {
						if(nextCharIter.Char == "\n") {
							endPara = true;
						}
					}
				}
				
				endWordIter = nextCharIter;
			} while (!bail && !endWordIter.IsEnd);
			
			word = startWordIter.GetText(endWordIter);
			word = Regex.Replace(word, @"--", "", RegexOptions.Multiline);
			word = word.TrimStart(new char[4] {' ', '\n', '\t', '-'});
			word = Regex.Replace(word, @"\s+$", "", RegexOptions.Multiline);
			word = Regex.Replace(word, @"\s+", " ", RegexOptions.Multiline);
						
			if (word.Length > config.WordGroupSize && !prevWordEndIter.Equal(Gtk.TextIter.Zero)) {
				wao--;
				endWordIter = prevWordEndIter;
			}
			
			
			word = startWordIter.GetText(endWordIter);
			
			word = Regex.Replace(word, @"--", "", RegexOptions.Multiline);
			word = word.TrimStart(new char[4] {' ', '\n', '\t', '-'});
			word = Regex.Replace(word, @"\s+$", "", RegexOptions.Multiline);
			word = Regex.Replace(word, @"\s+", " ", RegexOptions.Multiline);
						
			if (endWordIter.IsEnd) {
				StopRsvp (null, null);
			}
			
			return(word);
		}
		
		public void HilightWord ()
		{
			Gtk.TextIter prevEndCharIter = new Gtk.TextIter ();
			//Gtk.TextIter nextCharIter = new Gtk.TextIter ();
			
			if (! (endHilightIter.Equal(Gtk.TextIter.Zero)) )
				textBuffer.RemoveTag("hilight", startHilightIter, endHilightIter);
						
			startHilightIter = startWordIter;
			endHilightIter = endWordIter;
			prevEndCharIter = endWordIter;
			prevEndCharIter.BackwardChar ();
			while ( Regex.IsMatch(startHilightIter.Char, @"[\s-]", RegexOptions.None) ) 
			{
				startHilightIter.ForwardChar ();	
			}
			
			while ( Regex.IsMatch(prevEndCharIter.Char, @"\s", RegexOptions.None) &&
				Regex.IsMatch(endHilightIter.Char, @"\S", RegexOptions.None) ) 
			{
				endHilightIter.BackwardChar ();
				prevEndCharIter.BackwardChar ();	
			}
			
			textBuffer.ApplyTag( tag, startHilightIter, endHilightIter );
			textView.ScrollToIter(endWordIter, 0, false, 0, 0);
			
			
			if (!running) 
				statusBar.Push(0, "Stopped at position " + startWordIter.Offset);
		}
		
		public void MakeHilightTag ()
		{
			try {
				tag.BackgroundGdk = hilightColor;
			}
			
			catch (System.NullReferenceException) {
				tag = new Gtk.TextTag ("hilight");
				textBuffer.TagTable.Add(tag);
				tag.BackgroundGdk = hilightColor;
			}
			
		}
		
		public void SpeedUp (object o, EventArgs args)
		{
			if (speedDown.Sensitive == false) {
				speedDown.Sensitive = true;
			}
			if (config.RsvpSpeed >= 27) {
				config.RsvpSpeed -= 9;
			} else {
				speedUp.Sensitive = false;
				speedbar.Fraction = 1.0;
			}
			//speedbar.Fraction = Convert.ToDouble (config.RsvpSpeed) / 297.00;
			//Console.WriteLine ("Speed: " + (288.00 - config.RsvpSpeed) / 288.00);
			speedbar.Fraction = (297.00 - config.RsvpSpeed) / 297.00;
			
		}
		
		public void SpeedDown (object o, EventArgs args)
		{
			if (speedUp.Sensitive == false) {
				speedUp.Sensitive = true;
			}
			if (config.RsvpSpeed <= 288) {
				config.RsvpSpeed += 9;
			} else {
				speedDown.Sensitive = false;
				speedbar.Fraction = 0.0;
			}
			//Console.WriteLine ("Speed: " + (297.00 - config.RsvpSpeed) / 297.00);
			speedbar.Fraction = (297.00 - config.RsvpSpeed) / 297.00;
		}
		
		public void SliderMoved (object o, EventArgs args)
		{
			
			if (!slideLock) {
				startWordIter.Offset = Convert.ToInt32(slider.Value);
				endWordIter = startWordIter;
				curWord = GetNextWord();
				ShowWord(curWord);
				HilightWord();
			}
			
		}
		
		public void NoteBookChanged (object o, SwitchPageArgs args)
		{
			switch (noteBook.Page)
			{
				case 0:
					noteBook.SetTabLabelPacking(noteBook.GetNthPage (0), true, true, Gtk.PackType.Start);
					noteBook.SetTabLabelPacking(noteBook.GetNthPage (1), false, true, Gtk.PackType.Start);
					noteBook.SetTabLabelPacking(noteBook.GetNthPage (2), false, true, Gtk.PackType.Start);
					noteBook.SetTabLabelPacking(noteBook.GetNthPage (3), false, true, Gtk.PackType.Start);
					riLabel.ModifyFg(Gtk.StateType.Active, new Gdk.Color (127, 127, 127));
					break;
				case 1:
					noteBook.SetTabLabelPacking(noteBook.GetNthPage (0), false, true, Gtk.PackType.Start);
					noteBook.SetTabLabelPacking(noteBook.GetNthPage (1), true, true, Gtk.PackType.Start);
					noteBook.SetTabLabelPacking(noteBook.GetNthPage (2), false, true, Gtk.PackType.Start);
					noteBook.SetTabLabelPacking(noteBook.GetNthPage (3), false, true, Gtk.PackType.Start);
					
					break;
				case 2:
					noteBook.SetTabLabelPacking(noteBook.GetNthPage (0), false, true, Gtk.PackType.Start);
					noteBook.SetTabLabelPacking(noteBook.GetNthPage (1), false, true, Gtk.PackType.Start);
					noteBook.SetTabLabelPacking(noteBook.GetNthPage (2), true, true, Gtk.PackType.Start);
					noteBook.SetTabLabelPacking(noteBook.GetNthPage (3), false, true, Gtk.PackType.Start);
					
					break;
				case 3:
					noteBook.SetTabLabelPacking(noteBook.GetNthPage (0), false, true, Gtk.PackType.Start);
					noteBook.SetTabLabelPacking(noteBook.GetNthPage (1), false, true, Gtk.PackType.Start);
					noteBook.SetTabLabelPacking(noteBook.GetNthPage (2), false, true, Gtk.PackType.Start);
					noteBook.SetTabLabelPacking(noteBook.GetNthPage (3), true, true, Gtk.PackType.Start);
					
					break;
			}
		}
		
}

public class Config
{
		private int averageWordLength = 7;
		private int updateSpeedInterval = 3;
		private int wordGroupSize = 7;
		private int rsvpSpeed = 127;
		private string fontName = "Sans Bold 24";
		
		public string ToXML ()
		{
			return null;
		}
		
		public object FromXML ()
		{
			return this;
		}
		
		public int AverageWordLength
		{
			get {
				return averageWordLength;
			}
			set {
				averageWordLength = value;
			}
		}
		
		public int UpdateSpeedInterval
		{
			get {
				return updateSpeedInterval;
			}
			set {
				updateSpeedInterval = value;
			}
		}
		
		public int WordGroupSize
		{
			get {
				return wordGroupSize;
			}
			set {
				wordGroupSize = value;
			}
		}
		
		public int RsvpSpeed
		{
			get {
				return rsvpSpeed;
			}
			set {
				rsvpSpeed = value;
			}
		}
		
		
		
} 

