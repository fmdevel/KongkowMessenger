using System.Reflection;
using ChatAPI;

#if DEBUG
[assembly: Android.App.Application(Debuggable = true)]
#else
[assembly: Android.App.Application(Debuggable = false)]
#endif

[assembly: AssemblyTitle(Util.APP_NAME)]
[assembly: AssemblyVersion(Util.APP_VERSION)]
[assembly: AssemblyFileVersion(Util.APP_VERSION)]
