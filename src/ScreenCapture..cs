using System;
using Atomus.Control;
using System.Windows.Forms;
using System.Drawing;
using Atomus.Diagnostics;

namespace Atomus.Drawing
{
    public class ScreenCapture : IAction
    {
        private IAction browser;
        private AtomusControlEventHandler beforeActionEventHandler;
        private AtomusControlEventHandler afterActionEventHandler;

        private System.Windows.Media.Matrix matrix;
        private Rectangle totalScreen;
        private Bitmap screenBitmap;

        private Bitmap[,] tmpBitmaps;

        private Color backGroundColor;
        private bool isCaptureCursor;

        #region Init
        public ScreenCapture()
        {
            this.backGroundColor = Color.Empty;

            this.isCaptureCursor = false;
        }
        #endregion

        #region Dictionary
        #endregion

        #region Spread
        #endregion

        #region IO
        object IAction.ControlAction(ICore sender, AtomusControlArgs e)
        {
            int[] colRow;

            try
            {
                this.beforeActionEventHandler?.Invoke(this, e);

                switch (e.Action)
                {
                    case "SetControl":
                        this.browser = (IAction)sender;

                        return true;

                    case "SetBackGroundColor":
                        if (e.Value is Color)
                        {
                            this.backGroundColor = (Color)e.Value;
                            return true;
                        }

                        return false;

                    case "bool":
                        if (e.Value is Color)
                        {
                            this.isCaptureCursor = (bool)e.Value;
                            return true;
                        }

                        return false;

                    case "GetScreenImage":
                        if (e.Value != null)
                        {
                            if (e.Value is Rectangle)
                                return this.GetScreenImage((Rectangle)e.Value);

                            return false;
                        }
                        else
                            return this.GetScreenImage(this.ScreenRectangle, Color.Empty);

                    case "GetScreenImages":

                        if (e.Value != null)
                        {
                            if (e.Value is int[])
                            {
                                colRow = (int[])e.Value;
                                return this.GetScreenImages(colRow[0], colRow[1]);
                            }
                            return this.GetScreenImages(1, 1); ;
                        }
                        else
                            return this.GetScreenImages(1, 1); ;


                    case "GetScreenRectangle":
                        return this.ScreenRectangle;

                    default:
                        throw new AtomusException("'{0}'은 처리할 수 없는 Action 입니다.".Translate(e.Action));
                }
            }
            finally
            {
                this.afterActionEventHandler?.Invoke(this, e);
            }
        }

        private Bitmap GetScreenImage()
        {
            return this.GetScreenImage(this.ScreenRectangle);
        }

        private Bitmap GetScreenImage(Rectangle captureRectangle)
        {
            return this.GetScreenImage(captureRectangle, Color.Empty);
        }

        private Bitmap GetScreenImage(Color backGroupdColor)
        {
            return this.GetScreenImage(this.ScreenRectangle, backGroupdColor);
        }

        private Bitmap GetScreenImage(Rectangle captureRectangle, Color backGroupdColor)
        {
            Rectangle rectangle;

            if (this.screenBitmap == null)
                this.screenBitmap = new Bitmap(captureRectangle.Width, captureRectangle.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            using (Graphics _Graphics = Graphics.FromImage(this.screenBitmap))
            {
                _Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

                if (backGroupdColor != Color.Empty)
                    _Graphics.Clear(backGroupdColor);

                _Graphics.CopyFromScreen(captureRectangle.X, captureRectangle.Y, 0, 0, captureRectangle.Size, CopyPixelOperation.SourceCopy);

                if (this.isCaptureCursor)
                {
                    rectangle = new Rectangle();

                    Cursor.Current = Cursors.Default;

                    rectangle.Size = Cursor.Current.Size;
                    rectangle.Location = new Point(Cursor.Position.X - captureRectangle.Location.X, Cursor.Position.Y - captureRectangle.Location.Y);

                    Cursor.Current.Draw(_Graphics, rectangle);
                }

                _Graphics.Flush();
            }

            return this.screenBitmap;
        }

        private Bitmap[,] GetScreenImages(int colCount, int rowCount)
        {
            bool isNew;
            Rectangle screenRectangle;
            int width;
            int height;

            isNew = false;

            screenRectangle = this.ScreenRectangle;

            width = screenRectangle.Width / colCount;
            height = screenRectangle.Height / rowCount;

            if (this.tmpBitmaps == null)
            {
                this.tmpBitmaps = new Bitmap[colCount, rowCount];
                isNew = true;
            }

            if (!(this.tmpBitmaps.GetLength(0) == colCount && this.tmpBitmaps.GetLength(1) == rowCount))
            {
                for (int x = 0; x < this.tmpBitmaps.GetLength(0); x++)
                {
                    for (int y = 0; y < this.tmpBitmaps.GetLength(1); y++)
                    {
                        this.tmpBitmaps[x, y].Dispose();
                    }
                }

                this.tmpBitmaps = new Bitmap[colCount, rowCount];
                isNew = true;
            }

            if (isNew)
                for (int x = 0; x < this.tmpBitmaps.GetLength(0); x++)
                {
                    for (int y = 0; y < this.tmpBitmaps.GetLength(1); y++)
                    {
                        this.tmpBitmaps[x, y] = new Bitmap(width, height);
                    }
                }

            return this.GetScreenImages(this.tmpBitmaps);
        }

        private Bitmap[,] GetScreenImages(Bitmap[,] targetBitmaps)
        {
            Rectangle screenRectangle;
            Rectangle captureRectangle;
            Point location;
            Size size;
            Bitmap bitmap;

            screenRectangle = this.ScreenRectangle;

            size = new Size(screenRectangle.Width / targetBitmaps.GetLength(0), screenRectangle.Height / targetBitmaps.GetLength(1));

            bitmap = this.GetScreenImage();

            location = new Point();
            captureRectangle = new Rectangle
            {
                Size = size
            };

            for (int x = 0; x < targetBitmaps.GetLength(0); x++)
            {
                for (int y = 0; y < targetBitmaps.GetLength(1); y++)
                {
                    location.X = (x * size.Width);
                    location.Y = (y * size.Height);

                    captureRectangle.Location = location;

                    using (Graphics graphics = Graphics.FromImage(targetBitmaps[x, y]))
                    {
                        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                        graphics.DrawImage(bitmap, 0, 0, captureRectangle, GraphicsUnit.Pixel);
                    }
                }
            }

            return targetBitmaps;
        }
        #endregion

        #region Event
        event AtomusControlEventHandler IAction.AfterActionEventHandler
        {
            add
            {
                this.beforeActionEventHandler += value;
            }
            remove
            {
                this.beforeActionEventHandler -= value;
            }
        }
        event AtomusControlEventHandler IAction.BeforeActionEventHandler
        {
            add
            {
                this.afterActionEventHandler += value;
            }
            remove
            {
                this.afterActionEventHandler -= value;
            }
        }
        #endregion

        #region "ETC"
        private double Factor
        {
            get
            {
                if (this.matrix != null)
                    return this.matrix.M11;

                try
                {
                    var source = new System.Windows.Interop.HwndSource(new System.Windows.Interop.HwndSourceParameters());

                    this.matrix = source.CompositionTarget.TransformToDevice;

                    return this.matrix.M11;
                }
                catch (Exception exception)
                {
                    DiagnosticsTool.MyTrace(exception);
                    return this.matrix.M11;
                }
            }
        }

        private Rectangle ScreenRectangle
        {
            get
            {
                double factor;

                factor = this.Factor;

                if (!this.totalScreen.Equals(Rectangle.Empty))
                    return this.totalScreen;

                this.totalScreen = new Rectangle();

                foreach (Screen screen in Screen.AllScreens)
                {
                    if (this.totalScreen == null)
                        this.totalScreen = new Rectangle(new Point((int)(screen.Bounds.Location.X * factor), (int)(screen.Bounds.Location.Y * factor))
                                                        , new Size(screen.Bounds.Size.Width, screen.Bounds.Size.Height));
                    else
                    {
                        if (screen.Equals(Screen.PrimaryScreen))
                            this.totalScreen = Rectangle.Union(this.totalScreen, new Rectangle(new Point(screen.Bounds.Location.X, screen.Bounds.Location.Y)
                                                                                                , new Size(screen.Bounds.Size.Width, screen.Bounds.Size.Height)));
                        else
                            this.totalScreen = Rectangle.Union(this.totalScreen, new Rectangle(new Point((int)(screen.Bounds.Location.X / factor), (int)(screen.Bounds.Location.Y / factor))
                                                                                                , new Size((int)(screen.Bounds.Size.Width / factor), (int)(screen.Bounds.Size.Height / factor))));
                    }
                }

                return this.totalScreen;
            }
        }
        #endregion
    }


    //public Image CaptureWindow(IntPtr handle, Rectangle _CaptureRectangle, Color _BackGroupdColor)
    //{
    //    // get te hDC of the target window
    //    IntPtr hdcSrc = User32.GetWindowDC(handle);
    //
    //    //// get the size
    //    //User32.RECT windowRect = new User32.RECT();
    //    //User32.GetWindowRect(handle, ref windowRect);
    //
    //    //int width = windowRect.right - windowRect.left;
    //    //int height = windowRect.bottom - windowRect.top;
    //
    //    // create a device context we can copy to
    //    IntPtr hdcDest = GDI32.CreateCompatibleDC(hdcSrc);
    //
    //    // create a bitmap we can copy it to,
    //    // using GetDeviceCaps to get the width/height
    //    //IntPtr hBitmap = GDI32.CreateCompatibleBitmap(hdcSrc, width, height);
    //    IntPtr hBitmap = GDI32.CreateCompatibleBitmap(hdcSrc, _CaptureRectangle.Width, _CaptureRectangle.Height);
    //
    //    // select the bitmap object
    //    IntPtr hOld = GDI32.SelectObject(hdcDest, hBitmap);
    //
    //    // bitblt over
    //    //GDI32.BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, GDI32.SRCCOPY);
    //    GDI32.BitBlt(hdcDest, 0, 0, _CaptureRectangle.Width, _CaptureRectangle.Height, hdcSrc, _CaptureRectangle.X, _CaptureRectangle.Y, GDI32.SRCCOPY);
    //
    //    // restore selection
    //    GDI32.SelectObject(hdcDest, hOld);
    //    // clean up 
    //    GDI32.DeleteDC(hdcDest);
    //    User32.ReleaseDC(handle, hdcSrc);
    //    // get a .NET image object for it
    //    Image img = Image.FromHbitmap(hBitmap);
    //    // free up the Bitmap object
    //    GDI32.DeleteObject(hBitmap);
    //    return img;
    //}
    //
    //public Image CaptureScreen()
    //{
    //    return CaptureWindow(User32.GetDesktopWindow());
    //}
    //
    //class User32
    //{
    //    [StructLayout(LayoutKind.Sequential)]
    //    public struct RECT
    //    {
    //        public int left;
    //        public int top;
    //        public int right;
    //        public int bottom;
    //    }
    //    [DllImport("user32.dll")]
    //    public static extern IntPtr GetDesktopWindow();
    //    [DllImport("user32.dll")]
    //    public static extern IntPtr GetWindowDC(IntPtr hWnd);
    //    [DllImport("user32.dll")]
    //    public static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDC);
    //    [DllImport("user32.dll")]
    //    public static extern IntPtr GetWindowRect(IntPtr hWnd, ref RECT rect);
    //}
    //class GDI32
    //{
    //
    //    public const int SRCCOPY = 0x00CC0020; // BitBlt dwRop parameter
    //
    //    [DllImport("gdi32.dll")]
    //    public static extern bool BitBlt(IntPtr hObject, int nXDest, int nYDest,
    //        int nWidth, int nHeight, IntPtr hObjectSource,
    //        int nXSrc, int nYSrc, int dwRop);
    //    [DllImport("gdi32.dll")]
    //    public static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth,
    //        int nHeight);
    //    [DllImport("gdi32.dll")]
    //    public static extern IntPtr CreateCompatibleDC(IntPtr hDC);
    //    [DllImport("gdi32.dll")]
    //    public static extern bool DeleteDC(IntPtr hDC);
    //    [DllImport("gdi32.dll")]
    //    public static extern bool DeleteObject(IntPtr hObject);
    //    [DllImport("gdi32.dll")]
    //    public static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);
    //}
}
