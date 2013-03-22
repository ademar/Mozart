using System;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using System.Drawing;
using System.Linq;
using MonoTouch.CoreAnimation;
using MonoTouch.ObjCRuntime;
using OpenTK.Graphics.ES11;
using MonoTouch.OpenGLES;
using OpenTK.Platform.iPhoneOS;
using OpenTK.Graphics;

namespace Mozart
{
	public class CanvasView : UIView
	{
		float animationInterval;

		int backingWidth;

		int backingHeight;

		void SetupView ()
		{
			GL.Viewport(0, 0, backingWidth, backingHeight);

			GL.MatrixMode (All.Projection);
			GL.LoadIdentity ();
			GL.Ortho(0, backingWidth, 0, backingHeight, -1.0f, 1.0f);
			GL.MatrixMode (All.Modelview);

			// Clears the view with black
			GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
			
			GL.EnableClientState(All.VertexArray);
		}

		AppDelegate appDelegate;

		uint vieFrameBuffer;
		uint viewRenderBuffers;

		void DestroyFrameBuffer ()
		{
			GL.Oes.DeleteFramebuffers (1, ref vieFrameBuffer);
			vieFrameBuffer = 0;
			GL.Oes.DeleteRenderbuffers (1, ref viewRenderBuffers);
			viewRenderBuffers = 0;
		}

		void createFrameBuffer ()
		{
			GL.Oes.GenFramebuffers (1, ref vieFrameBuffer);
			GL.Oes.GenRenderbuffers (1, ref viewRenderBuffers);
			
			GL.Oes.BindFramebuffer (All.FramebufferOes, vieFrameBuffer);
			GL.Oes.BindRenderbuffer (All.RenderbufferOes, viewRenderBuffers);

			Context.EAGLContext.RenderBufferStorage ((uint) All.RenderbufferOes, (CAEAGLLayer) Layer);
			GL.Oes.FramebufferRenderbuffer (All.FramebufferOes,
			                                All.ColorAttachment0Oes,
			                                All.RenderbufferOes,
			                                viewRenderBuffers);
			
			GL.Oes.GetRenderbufferParameter (All.RenderbufferOes, All.RenderbufferWidthOes, ref backingWidth);
			GL.Oes.GetRenderbufferParameter (All.RenderbufferOes, All.RenderbufferHeightOes, ref backingHeight);
			

			var status = GL.Oes.CheckFramebufferStatus (All.FramebufferOes);
			if (status != All.FramebufferCompleteOes) {
				Console.Error.WriteLine("failed to make complete framebuffer object {0}",
				                        GL.Oes.CheckFramebufferStatus (All.FramebufferOes));
			}

		}

		iPhoneOSGraphicsContext Context;

		public CanvasView (RectangleF frame,AppDelegate appDelegate):base(frame)
		{
			this.appDelegate = appDelegate;

			CAEAGLLayer eaglLayer = (CAEAGLLayer) Layer;
			eaglLayer.Opaque = true;
			eaglLayer.DrawableProperties = NSDictionary.FromObjectsAndKeys (
				new NSObject []{NSNumber.FromBoolean(false),          EAGLColorFormat.RGBA8},
			new NSObject []{EAGLDrawableProperty.RetainedBacking, EAGLDrawableProperty.ColorFormat}
			);

			Context = (iPhoneOSGraphicsContext) ((IGraphicsContextInternal) GraphicsContext.CurrentContext).Implementation;
			
			Context.MakeCurrent(null);

			createFrameBuffer ();

			animationInterval = 1.0f / 60.0f;
			
			SetupView();
			DrawView();
		}

		DateTime animationStarted;

		public void setAnimationDelay (float animationInterval)
		{
			this.animationInterval = animationInterval;
		}

		public void startAnimation(){

			NSTimer.CreateScheduledTimer (animationInterval, this, new MonoTouch.ObjCRuntime.Selector ("drawView"), null, true);

			animationStarted = DateTime.Now;
		}

		public float[][] drawBuffers;

		[Export ("layerClass")]
		public static Class LayerClass ()
		{
			return new Class (typeof (CAEAGLLayer));
		}

		//updates the openGL view when the timer fires
		[Export("drawView")]
		public  void DrawView ()
		{
			Context.MakeCurrent(null);
			GL.Oes.BindFramebuffer (All.FramebufferOes, vieFrameBuffer);

			appDelegate.DrawView (this, DateTime.Now - animationStarted);

			GL.Oes.BindRenderbuffer (All.RenderbufferOes, viewRenderBuffers);
			Context.EAGLContext.PresentRenderBuffer ((uint) All.RenderbufferOes);
		}
	}

}

