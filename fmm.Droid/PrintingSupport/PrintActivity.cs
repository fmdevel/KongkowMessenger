using System;
using System.Collections.Generic;

using Android.App;
using Android.Views;
using Android.Content;
using Android.OS;
using Android.Graphics;
using Android.Widget;
using Android.Print;
using Android.Bluetooth;

namespace fmm.PrintingSupport
{
    public abstract class PrintActivity : Activity
    {
        const int REQUEST_ENABLE_BT = 0x1000;
        protected static List<BTDevice> BTDevices = new List<BTDevice>();
        protected static BluetoothSocket BTSocket;
        protected BTBroadcastReceiver BTReceiver;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            if (IsBTEnabled())
            {
                RegisterBTReceiver();
                CloseBTSocket();
                StartBTDiscovery();
            }
        }

        protected override void OnResume()
        {
            base.OnResume();
            RegisterBTReceiver();
        }

        protected override void OnPause()
        {
            base.OnPause();
            if (BTReceiver != null)
            {
                UnregisterReceiver(BTReceiver);
                BTReceiver = null;
            }
        }

        private void RegisterBTReceiver()
        {
            if (BTReceiver == null)
            {
                BTReceiver = new BTBroadcastReceiver();
                RegisterReceiver(BTReceiver, new IntentFilter(BluetoothDevice.ActionFound));
            }
        }

        protected static void CloseBTSocket()
        {
            if (BTSocket != null)
            {
                try
                {
                    BTSocket.Close();
                }
                catch { }
                BTSocket = null;
            }
        }

        protected static void CancelBTDiscovery()
        {
            var btAdapter = BluetoothAdapter.DefaultAdapter;
            if (btAdapter != null && btAdapter.IsDiscovering)
                btAdapter.CancelDiscovery();
        }

        protected static void StartBTDiscovery()
        {
            var btAdapter = BluetoothAdapter.DefaultAdapter;
            if (btAdapter != null)
            {
                var hack = btAdapter.IsDiscovering; // Quick dirty hack
                if (!btAdapter.IsDiscovering)
                    btAdapter.StartDiscovery();
            }
        }

        protected static bool IsBTEnabled()
        {
            var btAdapter = BluetoothAdapter.DefaultAdapter;
            return btAdapter != null && btAdapter.IsEnabled;
        }

        protected void ActivateBT()
        {
            try
            {
                StartActivityForResult(new Intent(BluetoothAdapter.ActionRequestEnable), REQUEST_ENABLE_BT);
            }
            catch { }
        }

        protected static int UpdateBTBondedDevices()
        {
            var btAdapter = BluetoothAdapter.DefaultAdapter;
            if (btAdapter != null)
            {
                try
                {
                    var devs = btAdapter.BondedDevices;
                    foreach (BluetoothDevice device in devs)
                        UpdateBTDevices(device, false);

                    return devs.Count;
                }
                catch { }
            }
            return 0;
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (requestCode == REQUEST_ENABLE_BT)
            {
                if (resultCode == Result.Ok)
                {
                    Toast.MakeText(this, "Mencari perangkat Bluetooth ...", ToastLength.Long).Show();
                    if (UpdateBTBondedDevices() > 0)
                        RefreshPopupList();

                    CloseBTSocket();
                    //CancelBTDiscovery();
                    StartBTDiscovery();
                }
            }
        }

        protected void PrintTo(BTDevice dev)
        {
            var device = dev.Device;
            if (!IsBTEnabled())
            {
                ActivateBT();
                return;
            }
            if (dev == null || dev.Device == null)
                return;

            CloseBTSocket();
            CancelBTDiscovery();

            Toast.MakeText(this, "Menghubungkan ke " + device.Name, ToastLength.Short).Show();
            ChatAPI.Util.StartThread(() => BTConnect(dev));
        }

        private void BTConnect(BTDevice dev)
        {
            var device = dev.Device;
            try
            {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.IceCreamSandwichMr1)
                {
                    var uuids = device.GetUuids();
                    if (uuids != null)
                    {
                        for (int i = 0; i < uuids.Length; i++)
                        {
                            BTSocket = doConnect(device, uuids[i].Uuid);
                            if (BTSocket != null)
                                break;
                        }
                    }
                }
                if (BTSocket == null)
                {
                    var common_uuid = Java.Util.UUID.FromString("00001101-0000-1000-8000-00805F9B34FB");
                    BTSocket = doConnect(device, common_uuid);
                    if (BTSocket == null)
                        BTSocket = doConnectReflection(device, common_uuid);
                }
            }
            catch { }

            RunOnUiThread(() =>
            {
                if (BTSocket == null)
                {
                    Toast.MakeText(this, "Gagal menghubungkan perangkat", ToastLength.Short).Show();
                    StartBTDiscovery();
                }
                else
                {
                    dev.Connected = true;
                    ClosePopupList();
                    CreatePopup(null, "Pilih Ukuran Kertas", "80 mm", BTPrint80, "58 mm", BTPrint58, null).Show();
                }
            });
        }

        private void BTPrint58()
        {
            BTPrint(384);
        }

        private void BTPrint80()
        {
            BTPrint(568); // 576 is better for some printer
        }

        private void BTPrint(int scaleWidth)
        {
            var bmp = OnBTPrint();
            if (bmp != null)
                BTPrinter.Print(bmp, BTSocket.OutputStream, scaleWidth);
        }

        private static BluetoothSocket doConnect(BluetoothDevice device, Java.Util.UUID uuid, bool secure)
        {
            BluetoothSocket sock = null;
            try
            {
                if (secure)
                    sock = device.CreateRfcommSocketToServiceRecord(uuid);
                else
                    sock = device.CreateInsecureRfcommSocketToServiceRecord(uuid);

                sock.Connect();
                return sock;
            }
            catch
            {
                if (sock != null)
                {
                    try
                    {
                        sock.Close();
                    }
                    catch { }
                }
            }
            return null;
        }

        private static BluetoothSocket doConnectReflection(BluetoothDevice device, Java.Util.UUID uuid, bool secure)
        {
            BluetoothSocket sock = null;
            try
            {
                var type = Java.Lang.Class.FromType(typeof(BluetoothDevice));
                var m = type.GetMethod(secure ? "createRfcommSocket" : "createInsecureRfcommSocket", Java.Lang.Integer.Type);
                sock = (BluetoothSocket)m.Invoke(device, Java.Lang.Integer.ValueOf(1));
                sock.Connect();
                return sock;
            }
            catch
            {
                if (sock != null)
                {
                    try
                    {
                        sock.Close();
                    }
                    catch { }
                }
            }
            return null;
        }

        private static BluetoothSocket doConnect(BluetoothDevice device, Java.Util.UUID uuid)
        {
            var sock = doConnect(device, uuid, false);
            if (sock != null)
                return sock;

            return doConnect(device, uuid, true);
        }

        private static BluetoothSocket doConnectReflection(BluetoothDevice device, Java.Util.UUID uuid)
        {
            var sock = doConnectReflection(device, uuid, false);
            if (sock != null)
                return sock;

            return doConnectReflection(device, uuid, true);
        }

        protected abstract PrintDocumentAdapter OnPrint();
        protected abstract Bitmap OnBTPrint();

        private static void UpdateBTDevices(BluetoothDevice device, bool online)
        {
            if (device == null)
                return;
            try
            {
                var addr = device.Address;
                for (int i = 0; i < BTDevices.Count; i++)
                {
                    var d = BTDevices[i];
                    if (d.Device.Equals(device) || d.Device.Address == addr)
                    {
                        BTDevices[i].Update(device, online);
                        return;
                    }
                }
                BTDevices.Add(new BTDevice(device, online));
            }
            catch { }
            finally
            {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.IceCreamSandwichMr1)
                    device.FetchUuidsWithSdp();
            }
        }


        public class BTBroadcastReceiver : BroadcastReceiver
        {
            public override void OnReceive(Context context, Intent intent)
            {
                if (intent.Action == BluetoothDevice.ActionFound)
                {
                    UpdateBTDevices((BluetoothDevice)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice), true);
                    var a = CurrentActivity as PrintActivity;
                    if (a != null)
                        a.RunOnUiThread(a.RefreshPopupList); //a.RunOnUiThread(a.OnBTDevicesChanged);
                }
            }
        }

        protected class BTDevice : Java.Lang.Object
        {
            public BluetoothDevice Device;
            public bool Online;
            public bool Connected;
            public BTDevice(BluetoothDevice device, bool online)
            { Device = device; Online = online; }
            public void Update(BluetoothDevice device, bool online)
            { Device = device; Online |= online; }

            public static int Comparer(BTDevice a, BTDevice b)
            {
                if (!a.Connected)
                {
                    if (b.Connected) return 1;
                }
                else if (!b.Connected)
                    return -1;

                if (!a.Online)
                {
                    if (b.Online) return 1;
                }
                else if (!b.Online)
                    return -1;

                return string.Compare(a.Device.Name, b.Device.Name, true);
            }
        }

        #region " Device List Popup "

        private ViewGroup m_popup;
        private bool m_popupVisible;
        protected void RefreshPopupList()
        {
            if (m_popup == null || !m_popupVisible)
                return;

            var list = m_popup.FindViewById<ViewGroup>(Resource.Id.listPrinter);
            if (!IsBTEnabled())
            {
                list.RemoveAllViews();
                return;
            }

            int printerCount = (list.ChildCount + 1) / 2;
            while (printerCount < BTDevices.Count)
            {
                if (printerCount > 0)
                    list.AddView(CreateSpace());

                list.AddView(CreatePrinterView());
                printerCount++;
            }
            int count = Math.Min(printerCount, BTDevices.Count);
            if (count > 0)
            {
                var devs = BTDevices.ToArray();
                Array.Sort(devs, BTDevice.Comparer);
                for (int i = 0; i < count; i++)
                    AssociatePrinterView((ViewGroup)list.GetChildAt(i * 2), devs[i]);
            }
        }

        private void PrinterView_Click(object sender, EventArgs e)
        {
            var view = (View)sender;
            var device = view.Tag as BTDevice;
            if (device == null)
            {
                device = ((View)view.Parent).Tag as BTDevice;
                if (device == null)
                    return;
            }
            PrintTo(device);
        }

        protected void ShowPopupList()
        {
            if (m_popupVisible)
                return;

            if (m_popup == null)
                InitPopup();

            m_popupVisible = true;
            var parent = m_popup.Parent as ViewGroup;
            if (parent != null)
                parent.RemoveView(m_popup);

            UpdateBTBondedDevices();
            RefreshPopupList();
            var dialog = CreatePopup("Pilih Printer", m_popup, null, null, null, () => { m_popupVisible = false; });
            m_popup.Tag = dialog;
            dialog.Show();
        }

        private ViewGroup CreatePrinterView()
        {
            var icon = new ImageView(this);
            icon.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);

            var text = new TextView(this);
            text.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            text.SetPadding(12, 0, 0, 0);
            text.SetTextSize(Android.Util.ComplexUnitType.Dip, 18);

            var view = new LinearLayout(this);
            view.Orientation = Orientation.Horizontal;
            view.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            view.SetPadding(24, 12, 12, 12);
            view.SetGravity(GravityFlags.CenterVertical);
            var iconSize = UIUtil.DpToPx(32);
            view.AddView(icon, iconSize, iconSize);
            view.AddView(text);
            view.Click += PrinterView_Click;
            return view;
        }

        private View CreateSpace()
        {
            var space = new View(this);
            space.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 2);
            space.SetBackgroundColor(new Color(0xDD, 0xDD, 0xE0));
            return space;
        }

        private static void AssociatePrinterView(ViewGroup view, BTDevice dev)
        {
            view.Tag = dev;
            var icon = (ImageView)view.GetChildAt(0);
            if (dev.Connected || dev.Online)
                icon.SetImageResource(Resource.Drawable.ic_print);
            else
                icon.SetImageDrawable(null);

            ((TextView)view.GetChildAt(1)).Text = dev.Device.Name;
        }

        private void InitPopup()
        {
            m_popup = (ViewGroup)LayoutInflater.From(this).Inflate(Resource.Layout.PrinterList, null);
            m_popup.FindViewById(Resource.Id.bluetooth).Click += ScanBT;
            m_popup.FindViewById(Resource.Id.plugin).Click += PrintPlugin;
        }

        protected void ClosePopupList()
        {
            if (m_popup != null && m_popupVisible)
            {
                var a = m_popup.Tag as AlertDialog;
                if (a != null)
                    a.Dismiss();
            }
        }

        private void PrintPlugin(object sender, EventArgs e)
        {
            try
            {
                ClosePopupList();
                ((PrintManager)Application.Context.GetSystemService(PrintService)).Print(ChatAPI.Util.APP_NAME, OnPrint(), null);
            }
            catch { }
        }

        private void ScanBT(object sender, EventArgs e)
        {
            if (!IsBTEnabled())
            {
                ActivateBT();
                return;
            }

            Toast.MakeText(this, "Mencari perangkat Bluetooth ...", ToastLength.Long).Show();
            if (UpdateBTBondedDevices() > 0)
                RefreshPopupList();

            CloseBTSocket();
            StartBTDiscovery();
        }

        #endregion

    }
}