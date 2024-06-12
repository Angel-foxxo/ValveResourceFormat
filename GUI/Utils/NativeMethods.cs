using System.Drawing;
using System.Runtime.InteropServices;

namespace GUI.Utils;

#pragma warning disable SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time - this requires unsafe code
class NativeMethods
{
    public const int SHCNE_ASSOCCHANGED = 0x8000000;
    public const int SHCNF_FLUSH = 0x1000;

    [DllImport("shell32.dll", CharSet = CharSet.Auto, ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static extern int SetWindowTheme(IntPtr hwnd, string pszSubAppName, string pszSubIdList);

    [StructLayout(LayoutKind.Sequential)]
    internal struct Win32Rect
    {
        internal int left;
        internal int top;
        internal int right;
        internal int bottom;
        static internal Win32Rect Empty
        {
            get
            {
                return new Win32Rect(0, 0, 0, 0);
            }
        }

        internal Win32Rect(int _left, int _top, int _right, int _bottom)
        {
            left = _left; top = _top; right = _right; bottom = _bottom;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct COMBOBOXINFO
    {
        internal int cbSize;
        internal Win32Rect rcItem;
        internal Win32Rect rcButton;
        internal int stateButton;
        internal IntPtr hwndCombo;
        internal IntPtr hwndItem;
        internal IntPtr hwndList;

        internal COMBOBOXINFO(int size)
        {
            cbSize = size;
            rcItem = Win32Rect.Empty;
            rcButton = Win32Rect.Empty;
            stateButton = 0;
            hwndCombo = IntPtr.Zero;
            hwndItem = IntPtr.Zero;
            hwndList = IntPtr.Zero;
        }
    };

    [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static extern bool GetComboBoxInfo(IntPtr hwnd, [In, Out] ref NativeMethods.COMBOBOXINFO cbInfo);

    [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.UserDirectories)]
    public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);

    [DllImport("dwmapi.dll", CharSet = CharSet.Auto, ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, int[] attrValue, int attrSize);

    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public readonly Rectangle ToRectangle()
        {
            return Rectangle.FromLTRB(Left, Top, Right, Bottom);
        }
    }

    [DllImport("dwmapi.dll", CharSet = CharSet.Auto, ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);

    [DllImport("Gdi32.dll", CharSet = CharSet.Auto, ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.UserDirectories)]
    public static extern IntPtr CreateRoundRectRgn
    (
        int nLeftRect,     // x-coordinate of upper-left corner
        int nTopRect,      // y-coordinate of upper-left corner
        int nRightRect,    // x-coordinate of lower-right corner
        int nBottomRect,   // y-coordinate of lower-right corner
        int nWidthEllipse, // height of ellipse
        int nHeightEllipse // width of ellipse
    );

    [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.UserDirectories)]
    public static extern IntPtr GetDC(IntPtr hwnd);

    [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.UserDirectories)]
    public static extern IntPtr ReleaseDC(IntPtr hwnd, IntPtr hdc);
}
