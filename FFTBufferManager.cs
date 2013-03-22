using System;
using System.Collections.Generic;
using System.Linq;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MonoTouch.AudioToolbox;
using MonoTouch.AudioUnit;
using System.Drawing;
using MonoTouch.CoreGraphics;
using System.Runtime.InteropServices;
using MonoTouch;

namespace Mozart
{
	unsafe class  FFTBufferManager
	{
		public  bool NeedsNewAudioData {
			get;
			set;
		}

		public int maxFPS; 

		int mAudioBufferSize;
		AppDelegate appDelegate;

		public FFTBufferManager (int maxFPS, AppDelegate appDelegate)
		{
			this.appDelegate = appDelegate;
			this.maxFPS = maxFPS;

			mAudioBufferSize = maxFPS;
			NeedsNewAudioData = true;
			mAudioBuffer = new int[mAudioBufferSize];
		}

		int mAudioBufferCurrentIndex;
		public  int[] mAudioBuffer;

		void memcpy (int mAudioBufferCurrentIndex, IntPtr data, int bytesToCopy)
		{
			unsafe {
				int* src = (int*)data;
				fixed(int* buff = mAudioBuffer) {
					for (int i = 0; i < bytesToCopy / sizeof(int); i++) {
						buff [mAudioBufferCurrentIndex + i] = src [i];
					}
				}
			}
		}

		public bool hasNewFFTData;

		public void GrabAudioData (AudioBuffers data)
		{
			if (mAudioBufferSize < data [0].DataByteSize)
				return;
			
			var audioBuffer = data [0];

			var bytesToCopy = Math.Min (audioBuffer.DataByteSize, mAudioBufferSize - mAudioBufferCurrentIndex);

			memcpy (mAudioBufferCurrentIndex, audioBuffer.Data, bytesToCopy);
			
			mAudioBufferCurrentIndex += bytesToCopy / sizeof(Int32);

			if (mAudioBufferCurrentIndex >= mAudioBufferSize / sizeof(Int32)) {

				//lock (syncLock) {
					NeedsNewAudioData = false;
					hasNewFFTData = true;
				//}
			}

		}

		//private readonly object syncLock = new object ();

		public bool ComputeFFT (float[] fftData)
		{
			if (hasNewFFTData) {
				FourierAnalysis (fftData);
				//lock (syncLock) {
					hasNewFFTData = false;
					NeedsNewAudioData = true;
				//}

				mAudioBufferCurrentIndex = 0;

				return true;
			} else if (!NeedsNewAudioData) {
				//lock (syncLock) {
					NeedsNewAudioData = true;
				//}


			}

			return false;
		}

		void FourierAnalysis (float[] l_fftData)
		{
			unsafe {
				fixed (int* p = mAudioBuffer) {
					fixed (Mozart.Accelerate.DSPSplitComplex* pA = &A) {
						fixed (float* pfftData = l_fftData) {

							Accelerate.vDSP_vflt32 (p, 1, outputBuffer, 1, (ulong)mAudioBufferSize);

							Accelerate.vDSP_vmul (outputBuffer, 1, hammingWindow, 1,outputBuffer, 1, (ulong)mAudioBufferSize);

							Accelerate.vDSP_ctoz ((Mozart.Accelerate.DSPComplex*)outputBuffer, 2, pA, 1, (ulong)nOver2);

							Accelerate.vDSP_fft_zrip (fftSetup, pA, 1, log2n, Accelerate.FFT_FORWARD);

							//Zero out the nyquist value
							A.imagp [0] = 0.0f;

							Accelerate.vDSP_zvmags (pA, 1, outputBuffer, 1, (ulong)nOver2);

							float max;
							int maxi;

							Accelerate.vDSP_maxvi(outputBuffer,1,&max,&maxi,(ulong)nOver2);

							//normalize
							var n = 1.0f/max;
							Accelerate.vDSP_vsmul (outputBuffer, 1, &n, outputBuffer, 1, (ulong)nOver2);

							copy (l_fftData);
						}
					}
				}

			}
		}

		public void copy (float[] l_fftData)
		{
			for (int i = 0; i < l_fftData.Length; i++) {
				l_fftData [i] = outputBuffer [i];
			}
		}

		unsafe void* fftSetup;
		Mozart.Accelerate.DSPSplitComplex A;
		int nOver2;
		float* outputBuffer,hammingWindow;

		ulong log2n;

		float[] realData {
			get;
			set;
		}

		public void Setup ()
		{
			unsafe {

				nOver2 = mAudioBufferSize / 2;
				log2n = (ulong)Math.Round(Math.Log (mAudioBufferSize, 2));

				fftSetup = Accelerate.vDSP_create_fftsetup (log2n, 0);

				var sizeOfFloat = Marshal.SizeOf (typeof(float));

				A.imagp = (float*)Marshal.AllocHGlobal (nOver2 * sizeOfFloat);
				A.realp = (float*)Marshal.AllocHGlobal (nOver2 * sizeOfFloat);

				outputBuffer = (float*)Marshal.AllocHGlobal (mAudioBufferSize * sizeOfFloat);
				hammingWindow = (float*)Marshal.AllocHGlobal (mAudioBufferSize * sizeOfFloat);

				Accelerate.vDSP_hamm_window(hammingWindow, mAudioBufferSize, 0);

			}

		}
                               
	}

}

