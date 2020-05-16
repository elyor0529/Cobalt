using Vanara.PInvoke;

namespace Cobalt.Engine.Infos
{
    public struct BasicWindowInfo
    {
        public BasicWindowInfo(HWND handle, string title)
        {
            Handle = handle;
            Title = title;
        }

        public HWND Handle { get; }
        public string Title { get; }
    }
}