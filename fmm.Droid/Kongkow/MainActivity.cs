using System;
using System.IO;
using Android;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using ChatAPI;

namespace fmm
{
    //[IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault }, DataMimeType = "image/*")]
    //[IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault }, DataMimeType = "audio/*")]
    //[IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault }, DataMimeType = "video/*")]
    //[IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault }, DataMimeType = "application/*")]

    [Activity(MainLauncher = true, LaunchMode = Android.Content.PM.LaunchMode.SingleTop)]
    public partial class MainActivity : Activity
    {
        public static bool AnyUnprocessedChat;
        public static bool AnyUnprocessedFeed;
        private static int m_lastTabIndex;
        private HorizontalTabSwipe m_tabView;
        private TabIcon[] m_tabIcons;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            if (!PermissionGranted)
                return;

            Window.RequestFeature(WindowFeatures.NoTitle);
            Initialize();
            SetContentView(Resource.Layout.Kongkow_Main);
            ThemedResId = new[] { Resource.Id.ActivityHeader };
            CreateTabSwipe();
            InitPopupMenu();
            InitSearch();
            MessageService.Start(Application.Context); // Use Application Context instead of Activity Context
            // HandleIntentShare();
        }

        protected override void OnResume()
        {
            base.OnResume();
            if (!IsInitialized)
                return;

            if (!Core.Setting.IsLanguageSpecified)
            {
                SettingActivity.SelectLanguage(this);
                return;
            }
            if (SettingActivity.PendingRecreate)
            {
                SettingActivity.PendingRecreate = false;
                Recreate();
                return;
            }


            var state = Core.Setting.ActivationState;
            if (state == ActivationState.NEED_ACTIVATION)
            {
                StartActivity(typeof(ActivationLoginActivity));
                return;
            }
            if (state == ActivationState.ANSWER_SECURITY_QUESTION || state == ActivationState.NEED_SET_PASSWORD)
            {
                ActivationStartRecovery();
                return;
            }
            if (state == ActivationState.USER_SIGNUP)
            {
                StartActivity(typeof(ActivationSignUpActivity));
                return;
            }
            if (state != ActivationState.ACTIVATED)
                return;

            // check if activity started from notification
            string contactID = Intent.GetStringExtra("Contact");
            if (!string.IsNullOrEmpty(contactID))
            {
                Intent.RemoveExtra("Contact");
                var contact = Core.FindContact(contactID);
                if (contact != null)
                {
                    StartChat(contact);
                    return;
                }
            }

            if (m_searchMode)
                SearchModeOff();
            else
            {
                UpdateActiveIcon();
                var index = ActiveTabIndex;
                if (index >= 0)
                {
                    ClearHandler();
                    PostDelayed(SelectTabDelay, 200);
                }
            }

            var p = m_pendingAction;
            if (p != null)
            {
                m_pendingAction = null;
                p.Invoke();
            }
        }

        private void ActivationStartRecovery()
        {
            if (Core.Owner == null)
            {
                Core.Setting.ActivationState = ActivationState.NEED_ACTIVATION;
                StartActivity(typeof(ActivationLoginActivity));
            }
        }

        private void SelectTabDelay()
        {
            if (this == CurrentActivity)
            {
                UpdateConfig(this);
                m_tabView.SelectTab(m_lastTabIndex, false);
                UpdateTab(m_lastTabIndex);
            }
        }

        protected override void OnPause()
        {
            ClearHandler();
            if (IsInitialized) m_lastTabIndex = ActiveTabIndex;
            base.OnPause();
        }

        public override void OnTrimMemory(TrimMemory level)
        {
            if (IsInitialized && level == TrimMemory.UiHidden)
            {
                var activeTabIndex = ActiveTabIndex;
                if (activeTabIndex != 0)
                    TrimMemory_RecentChat();
                if (activeTabIndex != 1)
                    TrimMemory_Feed();
                if (activeTabIndex != 2)
                    TrimMemory_Contact();

                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            }

            base.OnTrimMemory(level);
        }

        #region " Tab Function "
        public int ActiveTabIndex
        {
            get { return m_tabView.SelectedIndex; }
        }

        private void CreateTabSwipe()
        {
            m_tabIcons = new TabIcon[]
            {
                new TabIcon(this, Resource.Drawable.chat_header, Resource.Drawable.chat_header_h, Resource.Drawable.chat_header_new),
                new TabIcon(this, Resource.Drawable.feed_header,Resource.Drawable.feed_header_h,  Resource.Drawable.feed_header_new),
                new TabIcon(this, Resource.Drawable.contact_header, Resource.Drawable.contact_header_h, Resource.Drawable.contact_header)
#if BUILD_PARTNER
                , new TabIcon(this, Resource.Drawable.pos_header, Resource.Drawable.pos_header_h, Resource.Drawable.pos_header_new)
#endif
            };

            var header = new TabHeader(this, m_tabIcons);
#if BUILD_PARTNER
            m_tabView = new HorizontalTabSwipe(this, header, 3);
#else
            m_tabView = new HorizontalTabSwipe(this, header, 0);
#endif

            m_tabView.OnCreateTab = CreateTab;
            m_tabView.OnSelectedIndexChanged = SelectedIndexChanged;
            m_tabView.OnTabCreated = TabCreated;
            FindViewById<LinearLayout>(Resource.Id.MainLayout).AddView(m_tabView, ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
        }


        private View CreateTab(int index)
        {
#if BUILD_PARTNER
            return LayoutInflater.Inflate(index == 3 ? Resource.Layout.POS_Tab : Resource.Layout.Kongkow_ListViewer, null);
#else
            return LayoutInflater.Inflate(Resource.Layout.Kongkow_ListViewer, null);
#endif
        }

        private void TabCreated(int index)
        {
            if (index == 0)
                Initialize_RecentChat();
            else if (index == 1)
                Initialize_Feed();
            else if (index == 2)
                Initialize_Contact();
#if BUILD_PARTNER
            else if (index == 3)
                Initialize_Pos();
#endif
        }

        private void UpdateActiveIcon()
        {
            for (int i = 0; i < m_tabIcons.Length; i++)
                m_tabIcons[i].UpdateActiveIcon();
            m_tabView.Header.SetBackgroundColor(Core.Setting.Themes);
        }

        private void SelectedIndexChanged(int index)
        {
            if (!m_searchMode)
            {
                UpdateTab(index);
                //SyncContact(false, false);
            }
        }

        private void UpdateTab(int index)
        {
            if (index == 0)
                Update_RecentChat();
            else if (index == 1)
                Update_Feed();
            else if (index == 2)
                Update_Contact();
#if BUILD_PARTNER
            else if (index == 3)
                Update_Pos();
#endif
        }
        #endregion


        #region " Menu "
        private void InitPopupMenu()
        {
            var popupMenu = FindViewById<ImageView>(Resource.Id.MainPopUpMenu);
            popupMenu.Click += (object sender, EventArgs e) =>
            {
                if (Core.Owner == null) return;
                //    var menus = new string[] {
                //        Resources.GetString(Resource.String.EditProfile), "Broadcast", "Setting",
                //        Resources.GetString(Resource.String.AddContact),
                //        Resources.GetString(Resource.String.AddContactServer),
                //        Resources.GetString(Resource.String.AddGroup),
                //        "Logout" };
                //    PopupMenu(menus, PopupMenuClick, popupMenu);
                //};
                var menus = new string[] {
                    Resources.GetString(Resource.String.EditProfile), "Broadcast", "Setting",
                    Resources.GetString(Resource.String.AddContact),
                    Resources.GetString(Resource.String.AddContactServer),
                    "Logout" };
                PopupMenu(menus, PopupMenuClick, popupMenu);
            };
        }

        private void PopupMenuClick(int index)
        {
            switch (index)
            {
                case 0:
                    CurrentContact = Core.Owner;
                    StartActivity(typeof(ViewProfileActivity));
                    break;
                case 1:
                    var contacts = Core.GetSortedContacts();
                    if (contacts.Length > 0)
                        StartActivity(typeof(BroardcastActivity));
                    else
                        PopupError(Resources.GetString(Resource.String.ErrContact));
                    break;
                case 2:
                    StartActivity(typeof(SettingActivity));
                    break;
                case 3:
                    AddContact(false);
                    break;
                case 4:
                    AddContact(true);
                    break;
                case 5:
                    Logout();
                    break;
            }
        }

        public override void OnCreateContextMenu(IContextMenu menu, View v, IContextMenuContextMenuInfo menuInfo)
        {
            if (ActiveTabIndex == 0)
                OnCreateContextMenu_RecentChat(menu, v, menuInfo);
            else if (ActiveTabIndex == 2)
                OnCreateContextMenu_Contact(menu, v, menuInfo);
        }

        public override bool OnContextItemSelected(IMenuItem item)
        {
            if (ActiveTabIndex == 0)
                OnContextItemSelected_RecentChat(item);
            else if (ActiveTabIndex == 2)
                OnContextItemSelected_Contact(item);

            return true;
        }
        #endregion

        #region " Search Function "
        private bool m_searchMode;
        private int m_originSearchTab;
        private void InitSearch()
        {
            var tbSearch = FindViewById<EditText>(Resource.Id.tbSearch);
            var btnSearch = FindViewById<ImageView>(Resource.Id.Search);
            var mainTitle = FindViewById<TextView>(Resource.Id.MainTitle);
            mainTitle.Text = Util.APP_NAME;
            var mainPopup = FindViewById<ImageView>(Resource.Id.MainPopUpMenu);
            btnSearch.Click += (sender, e) =>
            {
                if (m_searchMode)
                {
                    SearchModeOff();
                }
                else
                {
                    m_originSearchTab = ActiveTabIndex;
                    if (m_originSearchTab != 2)
                        m_tabView.SelectTab(2, false);
                    m_searchMode = true; // Activate searchMode after selecting tab 2, will ignore TabUpdate on SelectedIndexChanged
                    m_tabView.Header.Visibility = ViewStates.Gone;
                    mainTitle.Visibility = ViewStates.Gone;
                    tbSearch.Visibility = ViewStates.Visible;
                    btnSearch.SetImageResource(Resource.Drawable.ic_cross);
                    mainPopup.Visibility = ViewStates.Gone;
                    Util.SetFocusAndShowSoftKeyboard(tbSearch);
                }
            };
            tbSearch.TextChanged += (sender, e) =>
            {
                if (ActiveTabIndex == 2)
                {
                    var criteria = Util.GuardValue(tbSearch.Text).Trim();
                    if (criteria.Length > 0 && criteria[0] == '@')
                        criteria = criteria.Substring(1);
                    RefreshList_Contact(Core.FindContacts(criteria));
                }
            };
        }

        private void SearchModeOff()
        {
            m_tabView.Header.Visibility = ViewStates.Visible;
            //m_searchMode = false; // Deactivate searchMode after selecting tab 0, TabUpdate will active on SelectedIndexChanged
            var tbSearch = FindViewById<EditText>(Resource.Id.tbSearch);
            tbSearch.Text = string.Empty;
            tbSearch.Visibility = ViewStates.Gone;
            FindViewById<TextView>(Resource.Id.MainTitle).Visibility = ViewStates.Visible;
            var btnSearch = FindViewById<ImageView>(Resource.Id.Search);
            btnSearch.SetImageResource(Resource.Drawable.ic_search);
            FindViewById<ImageView>(Resource.Id.MainPopUpMenu).Visibility = ViewStates.Visible;
            Util.SetHideSoftKeyboard(tbSearch);
            ClearHandler();
            PostDelayed(SelectTab0, 400);
        }

        private void SelectTab0()
        {
            m_searchMode = false;
            if (this == CurrentActivity)
            {
                m_tabView.SelectTab(m_originSearchTab, false);
                if (m_originSearchTab == 2)
                    RefreshList_Contact(); // BUG FIX
            }
        }

        public override void OnBackPressed()
        {
            if (m_searchMode)
                SearchModeOff();
            else
                base.OnBackPressed();
        }
        #endregion

        #region " Global Handler And Initialization "  
        private bool IsInitialized { get { return PermissionGranted && Core.OnLogin != null; } }
        internal static void Initialize()
        {
            if (Core.OnLogin == null)
                Init();
        }

        private static void Init()
        {
            Core.OnLogin = OnLogin;
            Core.OnUnprocessedIncomingChat += LocalNotification.NotifyChatArrive;
            Core.OnUnprocessedIncomingChat += OnUnprocessedIncomingChat;
            Core.OnFeed += OnFeed;
            Contact.OnUpdate = OnUpdate;
            string privateDataDir = Path.GetDirectoryName(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments));
            string publicDataDir = Path.Combine(Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDocuments).AbsolutePath, Util.APP_NAME); // Path.Combine(Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryPictures).AbsolutePath, Util.APP_NAME); //Path.Combine(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath, Util.APP_NAME);
            Core.Initialize(privateDataDir, publicDataDir);
#if BUILD_PARTNER
            POS_Activity.Initialize();
#endif
#if DEBUG
            Network.Initialize("202.78.200.17");
#else
            Network.Initialize("kongkow.flash-machine.com");
#endif
        }

        private static Action m_pendingAction;
        private static void RegisterPending(Action action)
        {
            var a = CurrentActivity;
            if (a == null)
                m_pendingAction = action;
            else
                a.RunOnUiThread(action);
        }

        private static void OnLogin(LoginInfo info)
        {
            var status = info.Status;
            if (status == LoginStatus.SUCCESS_BUT_OBSOLETE)
                RegisterPending(() => NavigateToMarket(false));

            else if (status == LoginStatus.FAIL_UNSUPPORTED_PROTOCOL)
                RegisterPending(() => NavigateToMarket(true));

            else if (status == LoginStatus.FAIL_SESSION_EXPIRED)
                Logout();

            else if (status == LoginStatus.FAIL_ACCOUNT_HAS_BEEN_BANNED)
            {
                if (CurrentActivity == null)
                {
                    Process.KillProcess(Process.MyPid());
                    return;
                }
                CurrentActivity.RunOnUiThread(() => HandleBanned((BannedInfo)info));
            }

            else if (status == LoginStatus.SUCCESS)
                RegisterPending(RemindSecurityQuestion);
        }

        private static void RemindSecurityQuestion()
        {
            if (Core.Setting.ShouldRemindSecurityQuestion())
            {
                if (Core.Setting.ShouldAskPassword(TimeSpan.FromHours(3)))
                    CurrentActivity.AskPassword(CheckSecurityQuestion, false);
                else
                    CheckSecurityQuestion();
            }
        }

        private static void CheckSecurityQuestion()
        {
            Core.OnActivation = ActivationResult;
            Core.ActivationGetSecurityQuestion(Core.Owner.Username);
        }

        private static bool ActivationResult(bool networkSent, ActivationType type, byte resultCode, object extraInfo)
        {
            Core.OnActivation = null;
            if (CurrentActivity != null && networkSent && type == ActivationType.GetSecurityQuestion)
            {
                var sq = (Core.SecurityQuestion)extraInfo;
                if (sq != null)
                {
                    CurrentActivity.RunOnUiThread(() =>
                    {
                        if (sq.SelectedQuestionIndex1 < 0 || sq.SelectedQuestionIndex2 < 0)
                            ActivationUpdateRecoveryOptionsActivity.Start(sq.QuestionList1, sq.QuestionList2, true);
                        else
                            ActivationViewRecoveryOptionsActivity.Start(sq);
                    });
                    return true;
                }
            }
            return false;
        }

        private static void HandleBanned(BannedInfo info)
        {
            string message;
            bool permanent = info.Time == long.MaxValue;

            if (Core.Setting.Language == Language.ID)
            {
                message = permanent ? "Akun telah ditangguhkan secara permanen" : "Akun Anda dikunci sementara";
                if (!string.IsNullOrEmpty(info.By))
                    message += " oleh @" + info.By;

                message += string.IsNullOrEmpty(info.Reason) ? " karena adanya aktivitas tidak wajar" : "\r\n\r\nAlasan: " + info.Reason;
                if (!permanent && info.Time > DateTime.UtcNow.Ticks)
                    message += "\r\nBerlaku hingga " + info.ToLocalTimeString();
            }
            else
            {
                message = permanent ? "Account permanently suspended" : "Account temporarily suspended";
                if (!string.IsNullOrEmpty(info.By))
                    message += " by @" + info.By;

                message += string.IsNullOrEmpty(info.Reason) ? " due to unusual activity" : "\r\n\r\nReason: " + info.Reason;
                if (!permanent && info.Time > DateTime.UtcNow.Ticks)
                    message += "\r\nUntil " + info.ToLocalTimeString();
            }
            CurrentActivity.PopupError(message, () => { Process.KillProcess(Process.MyPid()); });
        }

        private static void OnFeed(Feed contact)
        {
            var a = CurrentActivity as MainActivity;
            if (a != null)
                a.RunOnUiThread(() =>
                {
                    if (a.ActiveTabIndex == 1)
                    {
                        a.RefreshList_Feed();
                        AnyUnprocessedFeed = false;
                    }
                    else
                    {
                        a.m_tabIcons[1].State = TabState.InactiveNotif;
                        AnyUnprocessedFeed = true;
                    }
                });
            else
                AnyUnprocessedFeed = true;
        }

        private static void OnUnprocessedIncomingChat(ChatMessage obj)
        {
            var a = CurrentActivity as MainActivity;
            if (a != null)
                CurrentActivity.RunOnUiThread(() =>
                {
                    if (a.ActiveTabIndex == 0)
                    {
                        a.RefreshList_RecentChat();
                        AnyUnprocessedChat = false;
                    }
                    else
                    {
                        a.m_tabIcons[0].State = TabState.InactiveNotif;
                        AnyUnprocessedChat = true;
                    }
                });
            else
                AnyUnprocessedChat = true;
        }

#if BUILD_PARTNER
        public static bool AnyUnprocessedPosUpdate;
        private static void OnUpdatePos()
        {
            var a = CurrentActivity as MainActivity;
            if (a != null)
                a.RunOnUiThread(() =>
                {
                    if (a.ActiveTabIndex == 3)
                    {
                        a.Refresh_Pos();
                        AnyUnprocessedPosUpdate = false;
                    }
                    else
                    {
                        a.m_tabIcons[3].State = TabState.InactiveNotif;
                        AnyUnprocessedPosUpdate = true;
                    }
                });
            else
                AnyUnprocessedPosUpdate = true;
        }
#endif

        private static void OnUpdate(Contact contact, ChatAPI.Notification typeUpdate)
        {
            if (contact == Core.Owner)
                return;

#if BUILD_PARTNER
            if (contact == ContactPOS.Current)
            {
                OnUpdatePos();
                return;
            }
#endif

            if (typeUpdate == ChatAPI.Notification.USER_UPDATE_EXTRAINFO || typeUpdate == ChatAPI.Notification.USER_UPDATE_BANNER)
                return;

            var chat = CurrentActivity as PrivateChatActivity;
            if (chat != null)
            {
                chat.OnUpdateInternal(contact, typeUpdate);
                return;
            }

            if (typeUpdate != ChatAPI.Notification.USER_TYPING)
            {
                var main = CurrentActivity as MainActivity;
                if (main != null)
                {
                    main.OnUpdateInternal(contact, typeUpdate);
                    return;
                }
            }
        }

        internal void OnUpdateInternal(Contact contact, ChatAPI.Notification typeUpdate)
        {
            RunOnUiThread(() =>
            {
                int index = ActiveTabIndex;
                if (index == 0)
                    Update_RecentChat(contact);
                else if (index == 1)
                {
                    if (typeUpdate == ChatAPI.Notification.USER_LAST_SEEN || typeUpdate == ChatAPI.Notification.USER_ONLINESTATE ||
                    typeUpdate == ChatAPI.Notification.USER_UPDATE_NAME || typeUpdate == ChatAPI.Notification.USER_UPDATE_USERNAME)
                        Update_Feed(contact);
                }
                else if (index == 2)
                    Update_Contact(contact);
            });
        }

        private static void NavigateToMarket(bool forceStop)
        {
            CurrentActivity.PopupInfo(CurrentActivity.Resources.GetString(Resource.String.UpdateAvailable), () =>
            {
                var packageName = Application.Context.PackageName;
                if (!TryNavigate("market://details?id=" + packageName))
                    TryNavigate("http://play.google.com/store/apps/details?id=" + packageName);
                if (forceStop)
                    System.Environment.Exit(0);
            });
        }
        #endregion

        #region " Intent Share Functions "
        //private void HandleIntentShare()
        //{
        //    var intent = Intent;
        //    var type = intent.Type;
        //    if (string.IsNullOrEmpty(type))
        //        return;
        //    if (intent.Action != Intent.ActionSend)
        //        return;


        //    if (type.StartsWith("image/"))
        //    {
        //        return;
        //    }
        //    if (type.StartsWith("audio/"))
        //    {
        //        return;
        //    }
        //    if (type.StartsWith("video/"))
        //    {
        //        return;
        //    }
        //    if (type.StartsWith("application/"))
        //    {
        //        return;
        //    }
        //}
        #endregion
    }
}