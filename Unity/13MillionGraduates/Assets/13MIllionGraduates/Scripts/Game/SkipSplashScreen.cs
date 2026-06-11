using System;
using System.Reflection;
using UnityEngine;

/// <summary>
/// 反射方式强行跳过 Unity 开场 Splash Screen。
/// 注意：Unity 6 Personal 版可能仍然无效，引擎层有限制。
/// </summary>
public static class SkipSplashScreen
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    private static void BeforeSplash()
    {
#if !UNITY_EDITOR
        TryStopSplash();
#endif
    }

    private static void TryStopSplash()
    {
        // 方案1：反射调用 UnityEngine.Rendering.SplashScreen.Stop（尝试所有重载）
        var renderingAsm = typeof(Application).Assembly;
        var splashType = renderingAsm.GetType("UnityEngine.Rendering.SplashScreen");
        if (splashType != null)
        {
            // Stop(StopBehavior)
            var stopMethod = splashType.GetMethod("Stop", BindingFlags.Public | BindingFlags.Static);
            if (stopMethod != null)
            {
                try { stopMethod.Invoke(null, new object[] { 0 }); } catch { }
            }

            // Stop() 无参
            var stopNoArg = splashType.GetMethod("Stop", Type.EmptyTypes);
            if (stopNoArg != null)
            {
                try { stopNoArg.Invoke(null, null); } catch { }
            }
        }

        // 方案2：直接走内置 UnitySplashScreen 类
        var unitySplashType = renderingAsm.GetType("UnityEditor.UnitySplashScreen");
        if (unitySplashType == null)
            unitySplashType = renderingAsm.GetType("UnityEngine.UnitySplashScreen");

        if (unitySplashType != null)
        {
            var stop = unitySplashType.GetMethod("Stop", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            // 有些版本是 Finish() 或 Hide()
            var finish = unitySplashType.GetMethod("Finish", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            var hide = unitySplashType.GetMethod("Hide", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            try { stop?.Invoke(null, null); } catch { }
            try { finish?.Invoke(null, null); } catch { }
            try { hide?.Invoke(null, null); } catch { }
        }

        // 方案3：异步延时再试一次（绕过初始化时序检查）
        System.Threading.ThreadPool.QueueUserWorkItem(_ =>
        {
            System.Threading.Thread.Sleep(50);
            try
            {
                var t = typeof(Application).Assembly.GetType("UnityEngine.Rendering.SplashScreen");
                t?.GetMethod("Stop", BindingFlags.Public | BindingFlags.Static)
                 ?.Invoke(null, new object[] { 0 });
            }
            catch { }
        });
    }
}
