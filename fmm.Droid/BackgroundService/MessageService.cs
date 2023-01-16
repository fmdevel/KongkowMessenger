using System;
using Android.App;
using Android.Content;
using Android.OS;

namespace fmm
{
    [Service(Enabled = true)]
    public class MessageService : Service
    {
        public override void OnCreate()
        {
            base.OnCreate();
            if (Activity.VerifyPermission()) MainActivity.Initialize();
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            if (Activity.VerifyPermission())
            {
                ScheduleStart(this, 11 * 60 * 1000); // Schedule after 11 minutes
                MainActivity.Initialize();
            }
            else Stop(this, intent);
            return StartCommandResult.Sticky;
        }

        internal static void Stop(Service service, Intent intent)
        {
            try
            {
                service.StopSelf();
            }
            catch { }
            if (intent != null)
                try { service.StopService(intent); }
                catch { }
        }

        public static void ScheduleStart(Context context, long triggerTime)
        {
            triggerTime += SystemClock.ElapsedRealtime();
            AlarmManager manager = (AlarmManager)context.GetSystemService(AlarmService);
            Intent intent = new Intent(context, typeof(AlarmReceiver));
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                PendingIntent pendingintent = PendingIntent.GetBroadcast(context, 0, intent, Build.VERSION.SdkInt >= (BuildVersionCodes)31 ? PendingIntentFlags.Immutable : 0);
                manager.Cancel(pendingintent);
                manager.SetAndAllowWhileIdle(AlarmType.ElapsedRealtimeWakeup, triggerTime, pendingintent);
            }
            else
            {
                manager.Set(AlarmType.ElapsedRealtimeWakeup, triggerTime, PendingIntent.GetBroadcast(context, 0, intent, PendingIntentFlags.UpdateCurrent));
            }
        }

        public override IBinder OnBind(Intent intent)
        {
            return null;
        }

        public static void Start(Context context)
        {
            ScheduleStart(context, 11 * 60 * 1000); // Schedule after 11 minutes
            context.StartService(new Intent(context, typeof(MessageService)));
        }
    }

    [BroadcastReceiver(Enabled = true, Exported = true)]
    [IntentFilter(new[] { Intent.ActionBootCompleted, Intent.ActionReboot, Android.Net.ConnectivityManager.ConnectivityAction })]
    public class GlobalBroadcastReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                MessageService.ScheduleStart(context.ApplicationContext, 11 * 1000); // Schedule after 11 seconds
            else
                MessageService.Start(context.ApplicationContext); // Switch to App Context.
        }
    }

    [BroadcastReceiver(Enabled = true)]
    public class AlarmReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            context.StartService(new Intent(context, typeof(MessageService)));
        }
    }
}