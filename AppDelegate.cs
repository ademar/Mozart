using System;
using System.Collections.Generic;
using System.Linq;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MonoTouch.AudioToolbox;
using MonoTouch.AudioUnit;
using System.Runtime.InteropServices;
using System.Diagnostics;
using OpenTK.Graphics.ES11;

namespace Mozart
{

	/// The UIApplicationDelegate for the application. This class is responsible for launching the 
	// User Interface of the application, as well as listening (and optionally responding) to 
	// application events from iOS.
	[Register ("AppDelegate")]
	public partial class AppDelegate : UIApplicationDelegate
	{
		const int kAudioUnitSampleFractionBits = 24;
		public const int sampleRate = 44100;

		const int maxFPS = 4096;
		//const int maxFPS = 65536;

		static int drawBufferLen = maxFPS/2;

		UIWindow window;
		MainViewController mainViewController;

		float animationInterval;
	
		void Run ()
		{
			initDrawBuffers ();
			//init sound

			AudioSession.Initialize ();
			AudioSession.Category = AudioSessionCategory.PlayAndRecord;
			AudioSession.AddListener (AudioSessionProperty.AudioRouteChange, audioListener);
			AudioSession.PreferredHardwareIOBufferDuration = .005f;
			AudioSession.SetActive (true);

			SetupRemoteIO ();

			// create a new window instance based on the screen size
			window = new UIWindow (UIScreen.MainScreen.Bounds);
			mainViewController = new MainViewController (drawBuffers,this);
			window.RootViewController = mainViewController;
			window.MakeKeyAndVisible ();

			//-- start the animation
			animationInterval = 1.0f / 20.0f;
			//refresh at 20 hz
			mainViewController.Canvas.setAnimationDelay (animationInterval);
			mainViewController.Canvas.startAnimation ();

		}
		
		//
		// This method is invoked when the application has loaded and is ready to run. In this 
		// method you should instantiate the window, load the UI into it and then make the window
		// visible.
		//
		// You have 17 seconds to return from this method, or iOS will terminate your application.
		//
		public override bool FinishedLaunching (UIApplication app, NSDictionary options)
		{
			Run ();
				
			return true;
		}

		public void DrawView (CanvasView canvasView, TimeSpan time)
		{
			DrawOscilloscope ();
		}

		bool unitIsRunning;
		AudioUnit rioUnit;
		bool unitCreated;
		AudioStreamBasicDescription audioFormat;

		void SetupRemoteIO ()
		{
			AudioComponentDescription desc = new AudioComponentDescription ();
			desc.ComponentType = AudioComponentType.Output;
			desc.ComponentSubType = 0x72696f63;

			desc.ComponentManufacturer = AudioComponentManufacturerType.Apple;

			desc.ComponentFlags = 0;
			desc.ComponentFlagsMask = 0;

			var component = AudioComponent.FindNextComponent (null, desc);

			rioUnit = new AudioUnit (component);

			rioUnit.SetEnableIO (true, AudioUnitScopeType.Input, 1);
			rioUnit.SetRenderCallback (renderDelegate, AudioUnitScopeType.Input, 0);


			audioFormat = new AudioStreamBasicDescription();

			audioFormat.Format = AudioFormatType.LinearPCM;
			audioFormat.SampleRate = sampleRate;

			audioFormat.ChannelsPerFrame = 2;
			audioFormat.FramesPerPacket = 1;
			audioFormat.BitsPerChannel = 8 * sizeof(int);
			audioFormat.BytesPerPacket = sizeof(int);
			audioFormat.BytesPerFrame = sizeof(int);

			audioFormat.FormatFlags = AudioStreamBasicDescription.AudioFormatFlagsAudioUnitCanonical;

			rioUnit.SetAudioFormat (audioFormat, AudioUnitScopeType.Input, 0);
			rioUnit.SetAudioFormat (audioFormat, AudioUnitScopeType.Output, 1);
			rioUnit.Initialize ();

			unitCreated = true;

			FFTBufferManager = new FFTBufferManager (maxFPS, this);
			FFTBufferManager.Setup ();

			rioUnit.Start ();

			unitIsRunning = true;
		}

		public void audioListener (AudioSessionProperty prop, int size, IntPtr data)
		{

			if (prop == AudioSessionProperty.AudioRouteChange) {

				var audioInputAvailable = AudioSession.AudioInputAvailable;

				if (unitIsRunning && !audioInputAvailable) {

					rioUnit.Stop ();

				} else if (!unitIsRunning && audioInputAvailable) {

					if (!unitCreated) {

						SetupRemoteIO ();

					}
				}
			}
		}

		FFTBufferManager FFTBufferManager;

		void Silence (AudioBuffers data,int numFrames)
		{
			for (var i = 0; i < data.Count; i++) {

				var _data = data [i].Data;

				for (var j=0; j < data [i].DataByteSize; j++)
				{
					System.Runtime.InteropServices.Marshal.WriteByte(_data,j,0);
				}
			}
		}

		AudioUnitStatus renderDelegate (AudioUnitRenderActionFlags actionFlags, AudioTimeStamp timeStamp, uint busNumber, uint numberFrames, AudioBuffers data)
		{
			var err = rioUnit.Render (ref actionFlags, timeStamp, 1, numberFrames, data);

			if (err != AudioUnitStatus.OK) {
				return err;
			}
		
			if (FFTBufferManager == null)
				return AudioUnitStatus.OK;
			
			if (FFTBufferManager.NeedsNewAudioData) {
				FFTBufferManager.GrabAudioData (data); 
			}

			Silence (data,(int)numberFrames);

			return AudioUnitStatus.OK;
		}

		public bool hasNewFFTData;

		float[] fftData = new float[drawBufferLen];
		float[][] drawBuffers = new float[kNumDrawBuffers][];
		float[] osciLine =  new float[2*drawBufferLen];

		unsafe void DrawOscilloscope ()
		{
			if (FFTBufferManager == null) {
				return;
			}

			if (FFTBufferManager.hasNewFFTData) {

				hasNewFFTData = FFTBufferManager.ComputeFFT (fftData) 
					& !float.IsNaN (fftData [0]) ;

			} else {
				hasNewFFTData = false;
			}

			if (hasNewFFTData) {

				hasNewFFTData = false;
				drawBuffers[0] = fftData;
				cycleOscilloscopeLines ();

			}

			GL.Clear ((uint) All.ColorBufferBit);
			GL.BlendFunc(All.Alpha,All.One);

			GL.PushMatrix ();

			GL.Translate(1.0f, 0.0f,0);
			GL.Scale (UIScreen.MainScreen.Bounds.Width, UIScreen.MainScreen.Bounds.Height,1);

			GL.Disable(All.Texture);
			GL.DisableClientState(All.TextureCoordArray);
			GL.DisableClientState(All.ColorArray);
			GL.Disable(All.LineSmooth);

			GL.LineWidth(2.0f);

			// Draw a line for each stored line in our buffer (the lines are stored and fade over time)
			for (var drawBuffer_i=0; drawBuffer_i<kNumDrawBuffers; drawBuffer_i++) {

				var drawBuffer_ptr = drawBuffers [drawBuffer_i];

				// If we're drawing the newest line, draw it in solid green. Otherwise, draw it in a faded green.
				if (drawBuffer_i == 0)
					GL.Color4 (0.0f, 1.0f, 0.0f, 1.0f);
				else
					GL.Color4 (0.0f, 1.0f, 0.0f, (0.24f * (1.0f - ((float)drawBuffer_i / (float)kNumDrawBuffers))));

				var c = 0;
				var b = 0;

				for (var i=0.0f; i<drawBufferLen; i=i+1.0f) {
				
					osciLine [c++] = (drawBuffer_ptr [b]) ;/// 128.0f
					osciLine [c++] = i / drawBufferLen;

					b++;
				}

				GL.VertexPointer (2, All.Float, 0, osciLine);
				GL.DrawArrays (All.LineStrip, 0, drawBufferLen);
			}

			GL.PopMatrix ();
			GL.PopMatrix ();
	
		}
		const int kNumDrawBuffers = 12;

		void initDrawBuffers ()
		{
			for (var i =0; i<kNumDrawBuffers; i++)
				drawBuffers [i] = new float[drawBufferLen];
		}

		void cycleOscilloscopeLines ()
		{
			// Cycle the lines in our draw buffer so that they age and fade. The oldest line is discarded.
			int drawBuffer_i;
			for (drawBuffer_i=10; drawBuffer_i>=0; drawBuffer_i--)
				drawBuffers [drawBuffer_i + 1] = drawBuffers [drawBuffer_i];
		}
	}
}