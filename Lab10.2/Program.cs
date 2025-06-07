using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using System.Drawing;

namespace Lab10._2
{
    public class Program
    {
        public static void Main()
        {
            var nativeWindowSettings = new NativeWindowSettings()
            {
                ClientSize = new Vector2i(800, 600),
                Title = "Bezier Surface",
                APIVersion = new Version(4, 6),
                Profile = ContextProfile.Core,
            };

            using var window = new BezierSurfaceWindow(GameWindowSettings.Default, nativeWindowSettings);
            window.Run();
        }
    }
}
