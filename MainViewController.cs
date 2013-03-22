using System;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using System.Drawing;

namespace Mozart
{
	public partial class MainViewController : UIViewController
	{
		float[][] drawBuffers;

		AppDelegate appDelegate;
		
		public MainViewController (float[][] drawBuffers,AppDelegate appDelegate)
		{
			this.appDelegate = appDelegate;
			this.drawBuffers = drawBuffers;

		}

		public CanvasView Canvas;

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();

			Canvas = new CanvasView(View.Frame, appDelegate);

			Canvas.drawBuffers= drawBuffers;
			Canvas.BackgroundColor = UIColor.White;

			View.AddSubview(Canvas);

		}
		
		public override bool ShouldAutorotateToInterfaceOrientation (UIInterfaceOrientation toInterfaceOrientation)
		{
			return false;
		}
		
		public override void DidReceiveMemoryWarning ()
		{
			// Releases the view if it doesn't have a superview.
			base.DidReceiveMemoryWarning ();

		}

	}
}

