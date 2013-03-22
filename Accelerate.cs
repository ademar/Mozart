using System;
using System.Runtime.InteropServices;

namespace Mozart
{
	public unsafe static class Accelerate
	{
		[StructLayout(LayoutKind.Sequential)]
		public struct DSPComplex
		{
			public float               real;
			public float               imag;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct DSPSplitComplex
		{
			public float *             realp;
			public float *             imagp;
		}

		public const int FFT_FORWARD = 1;
		public const int FFT_INVERSE = -1;

		[DllImport ("/System/Library/Frameworks/Accelerate.framework/Accelerate",EntryPoint="vDSP_vfltu32")]
		unsafe public extern static void vDSP_vfltu32 (uint*       A,
		                                               int   __vDSP_I,
		                                               float*       __vDSP_C,
		                                               int   __vDSP_K,
		                                               ulong   __vDSP_N);

		[DllImport ("/System/Library/Frameworks/Accelerate.framework/Accelerate",EntryPoint="vDSP_vflt32")]
		unsafe public extern static void vDSP_vflt32 (int*       A,
		                                               int   __vDSP_I,
		                                               float*       __vDSP_C,
		                                               int   __vDSP_K,
		                                               ulong   __vDSP_N);
		
		[DllImport ("/System/Library/Frameworks/Accelerate.framework/Accelerate",EntryPoint="vDSP_vflt16")]
		unsafe public extern static void vDSP_vflt16 (short*       A,
		                                              int   __vDSP_I,
		                                              float*       __vDSP_C,
		                                              int   __vDSP_K,
		                                              ulong   __vDSP_N);
		
		[DllImport ("/System/Library/Frameworks/Accelerate.framework/Accelerate",EntryPoint="vDSP_create_fftsetup")]
		unsafe public extern static void* vDSP_create_fftsetup (ulong __vDSP_log2n,
		                                                         int __vDSP_radix);

		[DllImport ("/System/Library/Frameworks/Accelerate.framework/Accelerate",EntryPoint="vDSP_ctoz")]
		unsafe public extern static void vDSP_ctoz (
			DSPComplex*   __vDSP_C,
				int        __vDSP_strideC,
				DSPSplitComplex *  __vDSP_Z,
				int        __vDSP_strideZ,
			ulong        __vDSP_size);

		[DllImport ("/System/Library/Frameworks/Accelerate.framework/Accelerate",EntryPoint="vDSP_fft_zrip")]
		unsafe public extern static void 
			vDSP_fft_zrip (
				void*           __vDSP_setup,
				DSPSplitComplex *  __vDSP_ioData,
				int        __vDSP_stride,
				ulong        __vDSP_log2n,
				int       __vDSP_direction);

		[DllImport ("/System/Library/Frameworks/Accelerate.framework/Accelerate",EntryPoint="vDSP_ztoc")]
		unsafe public extern static void 
			vDSP_ztoc (
				 DSPSplitComplex *  __vDSP_Z,
				int              __vDSP_strideZ,
				DSPComplex[]               __vDSP_C,
				int              __vDSP_strideC,
				ulong              __vDSP_size);

		[DllImport ("/System/Library/Frameworks/Accelerate.framework/Accelerate",EntryPoint="vDSP_vsmul")]
		unsafe public extern static void 
			vDSP_vsmul (

				float*    __vDSP_input1,
				int    __vDSP_stride1,
				 float *  __vDSP_input2,

				float*          __vDSP_result,
				int    __vDSP_strideResult,
				ulong    __vDSP_size);

		[DllImport ("/System/Library/Frameworks/Accelerate.framework/Accelerate",EntryPoint="vDSP_vmul")]
		unsafe public extern static void 
			vDSP_vmul (

				float*    __vDSP_input1,
				int    __vDSP_stride1,
				float *  __vDSP_input2,
				int    __vDSP_stride2,
				float*          __vDSP_result,
				int    __vDSP_strideResult,
				ulong    __vDSP_size);

		[DllImport ("/System/Library/Frameworks/Accelerate.framework/Accelerate",EntryPoint="vDSP_zvmags")]
		unsafe public extern static void 
			vDSP_zvmags (
				DSPSplitComplex *  __vDSP_A,
				int        __vDSP_I,
				float *            __vDSP_C,
				int        __vDSP_K,
				ulong        __vDSP_N);

		[DllImport ("/System/Library/Frameworks/Accelerate.framework/Accelerate",EntryPoint="vDSP_zvabs")]
		unsafe public extern static void 
			vDSP_zvabs (
				DSPSplitComplex *  __vDSP_A,
				int        __vDSP_I,
				float *            __vDSP_C,
				int        __vDSP_K,
				ulong        __vDSP_N);

		[DllImport ("/System/Library/Frameworks/Accelerate.framework/Accelerate",EntryPoint="vDSP_hamm_window")]
		unsafe public extern static void vDSP_hamm_window (float* hammingWindow, int mAudioBufferSize, int i);

		[DllImport ("/System/Library/Frameworks/Accelerate.framework/Accelerate",EntryPoint="vDSP_vdbcon")]
		unsafe public extern static void vDSP_vdbcon (float *        __vDSP_A,
		                                              int    __vDSP_I,
		                                              float *        __vDSP_B,
		                                              float *        __vDSP_C,
		                                              int    __vDSP_K,
		                                              ulong    __vDSP_N,
		                                              uint   __vDSP_F);

		[DllImport ("/System/Library/Frameworks/Accelerate.framework/Accelerate",EntryPoint="vDSP_maxvi")]
		unsafe public extern static void vDSP_maxvi (
			float *__vDSP_A,
			int __vDSP_I,
			float *__vDSP_C,
			int *__vDSP_IC,
			ulong __vDSP_N
		);
	}
}

