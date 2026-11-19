using System.Runtime.InteropServices;
using static DesktopNotesSwitcher.UiText;

namespace DesktopNotesSwitcher;

internal sealed record DesktopSnapshot(Guid CurrentId, Guid[] DesktopIds);

// The COM declarations are adapted from Markus Scholtes' VirtualDesktop (MIT).
// https://github.com/MScholtes/VirtualDesktop/blob/master/VirtualDesktop11-24H2.cs
internal sealed class VirtualDesktops : IDisposable
{
    private static readonly Guid ShellClass = new("C2F03A33-21F5-47FA-B4BB-156362A2F239");
    private static readonly Guid DesktopService = new("C5E0CDCA-7B6E-41B2-9FC4-D93975CC467B");
    private static readonly Guid PinnedService = new("B5A399E7-1C87-46B8-88E9-FC5747B171BD");

    private IServiceProvider? _shell;
    private IDesktopManager? _manager;
    private IViewCollection? _views;
    private IPinnedViews? _pinned;

    private void Connect()
    {
        if (_manager is not null)
            return;
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 26100))
            throw new PlatformNotSupportedException(T("UnsupportedWindows"));

        var shellType = Type.GetTypeFromCLSID(ShellClass)!;
        _shell = (IServiceProvider)Activator.CreateInstance(shellType)!;
        _views = Query<IViewCollection>(typeof(IViewCollection).GUID);
        _pinned = Query<IPinnedViews>(PinnedService);
        _manager = Query<IDesktopManager>(DesktopService);
    }

    private T Query<T>(Guid service) where T : class
    {
        Guid iid = typeof(T).GUID;
        return (T)_shell!.QueryService(ref service, ref iid);
    }

    public DesktopSnapshot Read()
    {
        Connect();
        IDesktop current = _manager!.GetCurrentDesktop();
        Guid currentId;
        try
        {
            currentId = current.GetId();
        }
        finally
        {
            Marshal.ReleaseComObject(current);
        }

        _manager.GetDesktops(out IObjectArray desktops);
        try
        {
            desktops.GetCount(out uint count);
            var ids = new Guid[count];
            Guid iid = typeof(IDesktop).GUID;
            for (uint i = 0; i < count; i++)
            {
                desktops.GetAt(i, ref iid, out object item);
                try
                {
                    ids[i] = ((IDesktop)item).GetId();
                }
                finally
                {
                    Marshal.ReleaseComObject(item);
                }
            }

            return new DesktopSnapshot(currentId, ids);
        }
        finally
        {
            Marshal.ReleaseComObject(desktops);
        }
    }

    public void SwitchTo(Guid id)
    {
        Connect();
        IDesktop target = _manager!.FindDesktop(ref id);
        try
        {
            _manager.SwitchDesktop(target);
        }
        finally
        {
            Marshal.ReleaseComObject(target);
        }
    }

    public bool Pin(nint window)
    {
        Connect();
        int result = _views!.GetViewForHwnd(window, out nint view);
        if (result < 0 || view == 0)
            return false;
        try
        {
            if (!_pinned!.IsViewPinned(view))
                _pinned.PinView(view);
            return _pinned.IsViewPinned(view);
        }
        finally
        {
            Marshal.Release(view);
        }
    }

    public void Dispose()
    {
        Release(ref _manager);
        Release(ref _pinned);
        Release(ref _views);
        Release(ref _shell);
    }

    private static void Release<T>(ref T? instance) where T : class
    {
        if (instance is null)
            return;
        Marshal.ReleaseComObject(instance);
        instance = null;
    }

    [ComImport, Guid("6D5140C1-7436-11CE-8034-00AA006009FA")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IServiceProvider
    {
        [return: MarshalAs(UnmanagedType.IUnknown)]
        object QueryService(ref Guid service, ref Guid iid);
    }

    [ComImport, Guid("3F07F4BE-B107-441A-AF0F-39D82529072C")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IDesktop
    {
        void IsViewVisible();
        Guid GetId();
    }

    // Unused entries are declared only to maintain the native vtable order.
    [ComImport, Guid("53F5CA0B-158F-4124-900C-057158060B27")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IDesktopManager
    {
        void GetCount();
        void MoveViewToDesktop();
        void CanViewMoveDesktops();
        IDesktop GetCurrentDesktop();
        void GetDesktops(out IObjectArray desktops);
        void GetAdjacentDesktop();
        void SwitchDesktop(IDesktop desktop);
        void SwitchDesktopAndMoveForegroundView();
        void CreateDesktop();
        void MoveDesktop();
        void RemoveDesktop();
        IDesktop FindDesktop(ref Guid id);
    }

    [ComImport, Guid("92CA9DCD-5622-4BBA-A805-5E9F541BD8C9")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IObjectArray
    {
        void GetCount(out uint count);
        void GetAt(uint index, ref Guid iid, [MarshalAs(UnmanagedType.Interface)] out object item);
    }

    [ComImport, Guid("1841C6D7-4F9D-42C0-AF41-8747538F10E5")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IViewCollection
    {
        void GetViews();
        void GetViewsByZOrder();
        void GetViewsByAppUserModelId();
        [PreserveSig] int GetViewForHwnd(nint hwnd, out nint view);
    }

    [ComImport, Guid("4CE81583-1E4C-4632-A621-07A53543148F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPinnedViews
    {
        void IsAppIdPinned();
        void PinAppId();
        void UnpinAppId();
        [return: MarshalAs(UnmanagedType.Bool)]
        bool IsViewPinned(nint view);
        void PinView(nint view);
    }
}
