using System;
using System.IO;
using System.Collections.Generic;

#if BUILD_PARTNER
using ChatAPI.Connector;
#endif
namespace ChatAPI
{
    public partial class ContactPOS : Contact
    {
#if BUILD_PARTNER
        public static ContactPOS Current;
        public static bool RegistrationNeeded;
        public static void SetCurrent(ContactPOS contact)
        {
            if (contact != null)
            {
                if (Current != contact)
                {
                    Current = contact;
                    Core.Setting.Write("SID", contact.ID);
                }
                contact.GetAgenInfo(false);
            }
        }
#endif

        public ContactPOS(string id, string name, string status, DP dp, string extraInfo, long joinDate) : base(id, name, status, dp, extraInfo, null, 1, joinDate)
        {
#if BUILD_PARTNER
            ProductProviderPrepaid = new List<ListProvider>();
            ProductProviderPostpaid = new List<ListProvider>();
            ProductProviderTicket = new List<ListProvider>();
            ProductProviderTransBank = new List<ListProvider>();
#endif
        }

#if BUILD_PARTNER

        public readonly List<ListProvider> ProductProviderPrepaid;
        public readonly List<ListProvider> ProductProviderPostpaid;
        public readonly List<ListProvider> ProductProviderTicket;
        public readonly List<ListProvider> ProductProviderTransBank;

        public ListProvider GetListProvider(int category)
        {
            if (category <= 100)
                return GetListProvider(ProductProviderPrepaid, category);
            else if (category <= 200)
                return GetListProvider(ProductProviderPostpaid, category);
            else if (category <= 300)
                return GetListProvider(ProductProviderTicket, category);
            else if (category <= 350)
                return GetListProvider(ProductProviderTransBank, category);

            return null;
        }

        private static ListProvider GetListProvider(List<ListProvider> list, int category)
        {
            foreach (ListProvider item in list)
                if (item.Type.Category == category)
                    return item;

            var type = TypeProduct.GetType(category);
            if (type == null)
                return null;

            var listProv = new ListProvider(type);
            list.Add(listProv);
            return listProv;
        }


        #region " POS Functions "
        public Agen AgenInfo;

        private TrxHistoryDB m_histoty;
        public TrxHistoryDB GetHistory()
        {
            if (m_histoty == null)
            {
                var contactDir = Core.GetContactDir(this.ID);
                if (!Directory.Exists(contactDir))
                    Directory.CreateDirectory(contactDir);
                m_histoty = new TrxHistoryDB(Path.Combine(contactDir, "history.fdt"));
            }
            return m_histoty;
        }

        private int m_lastCheckInfo;
        private int m_lastGetProduct;
        internal static bool CanRequest(int duration, ref int lastRequest)
        {
            return CanRequest(duration, ref lastRequest, false);
        }
        internal static bool CanRequest(int duration, ref int lastRequest, bool force)
        {
            var timeNow = Environment.TickCount;
            if (!force && lastRequest != 0 && timeNow - lastRequest < duration)
                return false;

            lastRequest = timeNow;
            return true;
        }

        public delegate void OnAgenInfoDelegate(ContactPOS contact);
        public static OnAgenInfoDelegate OnAgenInfo;
        public void GetAgenInfo(bool force)
        {
            if (CanRequest(20 * 1000, ref m_lastCheckInfo, force))
                SendPos(5, null, 9000);
        }

        public delegate void OnRegisterDelegate(ContactPOS contact, int result, string message); // -1=Silahkan melakukan request perdaftaran, 0=SUKSES, 1=Gagal sudah terdaftar, 2=Gagal coba lagi kemudian, 3=Gagal PIN tidak aman, 4=Lainnya, lihat message
        public static OnRegisterDelegate OnRegister;
        public void Register(string name, string address, string postalCode, string pin)
        {
            SendPos(44, new string[] { name, address, postalCode, pin }, 18000);
        }

        public delegate void OnChangePinDelegate(ContactPOS contact, int result); // 0=SUKSES, 1=Gagal Pin lama salah, 2=Gagal coba lagi kemudian, 3=Gagal PIN tidak aman
        public static OnChangePinDelegate OnChangePin;
        public void ChangePin(string newPin, string oldPin)
        {
            SendPos(6, new string[] { newPin, oldPin }, 18000);
        }

        public delegate void OnGetProductDelegate(ContactPOS contact, Product[] result);
        public static OnGetProductDelegate OnGetProduk;
        private bool m_produkCacheLoaded;
        private int m_produkHash;
        public void GetProduct()
        {
            if (!m_produkCacheLoaded)
            {
                m_produkCacheLoaded = true;
                if (OnGetProduk != null)
                {
                    var f = Path.Combine(Core.GetContactDir(ID), "pcache");
                    if (File.Exists(f))
                    {
                        var raw = StringEncoder.UTF8.GetString(File.ReadAllBytes(f));
                        m_produkHash = Crypto.hashV2(raw);
                        OnGetProduk.Invoke(this, ParseGetProduct(raw));
                    }
                }
            }

            if (CanRequest(1 * 60 * 1000, ref m_lastGetProduct))
                SendPos(49, new string[] { m_produkHash.ToString() }, 9000);
        }

        public delegate void OnTrxDelegate(ContactPOS contact, TrxResult result);
        public static OnTrxDelegate OnTrx;
        public void Trx(TrxData trx, string productCode, string pin, string hpCustomer)
        {
            SendPos(27, new string[] { trx.TrxId.ToString(), productCode, trx.Destination, pin, hpCustomer }, 18000);
        }

        public void CheckStatus(TrxData trx)
        {
            SendPos(28, new string[] { trx.TrxId.ToString(), trx.Date.Ticks.ToString() }, 9000);
        }

        public delegate void OnCheckBillDelegate(ContactPOS contact, TrxResult result);
        public static OnCheckBillDelegate OnCheckBill;
        public void CheckBill(InqData inq, string pin)
        {
            string productCode = inq.Product.Category == 301 ? "Transfer Bank" : inq.Product.Code;
            SendPos(29, new string[] { string.IsNullOrEmpty(inq.Period) ? productCode : productCode + "." + inq.Period, inq.Destination, pin, inq.TrxId.ToString() }, 18000);
        }

        public delegate void OnAdminInfoDelegate(ContactPOS contact, string result);
        public static OnAdminInfoDelegate OnAdminInfo;
        public void TransferDep(string target, string amount, string pin)
        {
            SendPos(30, new string[] { target, amount, pin }, 18000);
        }

        public void RequestTicket(string amount, string pin)
        {
            SendPos(31, new string[] { amount, pin }, 18000);
        }

        public void SetActive(string target, bool active, string pin)
        {
            SendPos(32, new string[] { target, active ? "1" : "0", pin }, 18000);
        }

        public void RegisterDownline(string name, string hp, string address, string postalCode, string pin)
        {
            SendPos(33, new string[] { name, hp, address, postalCode, pin }, 18000);
        }

        // vendorType= 1: KAI
        // bookType= 1: GetSchedule, 2: Book, 3=GetBookInfo
        public delegate void OnBookDelegate(ContactPOS contact, BookResult result);
        public static OnBookDelegate OnBook;
        public void Book(long trxId, int vendorType, int bookType, string data)
        {
            SendPos(45, new string[] { trxId.ToString(), vendorType.ToString(), bookType.ToString(), data }, 24000);
        }

        public IProgress PosQueue;
        private void SendPos(byte type, string[] data, int timeOut)
        {
            PosQueue = null;
            var r = new NSerializer(TypeHeader.POS);
            r.Add(this.ID);
            r.Add(type);
            if (data != null)
                r.Add(Util.CommonJoin(data));

            var p = new PosProgress(this, type);
            if (!Network.Enqueue(r, timeOut, p))
                if (type != 5 && type != 47) // Not AgenInfo or GetProduct
                    PosQueue = p;
        }

        internal void HandlePos(byte type, Deserializer buf)
        {
            switch (type)
            {
                case 5:
                    var a = ParseAgenInfo(buf);
                    if (a != null)
                    {
                        AgenInfo = a;
                        if (OnAgenInfo != null)
                            OnAgenInfo.Invoke(this);
                    }
                    break;
                case 6:
                    if (OnChangePin != null) OnChangePin.Invoke(this, ParseChangePin(buf));
                    break;
                case 27:
                case 28:
                    if (OnTrx != null) OnTrx.Invoke(this, ParseTrx(buf));
                    break;
                case 29:
                    if (OnCheckBill != null) OnCheckBill.Invoke(this, ParseTrx(buf));
                    break;
                case 30:
                case 31:
                    if (OnAdminInfo != null) OnAdminInfo.Invoke(this, ParseString(buf));
                    break;
                case 44:
                    if (OnRegister != null) HandleRegister(buf);
                    break;
                case 45:
                    if (OnBook != null) OnBook.Invoke(this, ParseBook(buf));
                    break;
                case 49:
                    if (OnGetProduk != null) OnGetProduk.Invoke(this, ParseGetProduct(buf));
                    break;
            }
        }

        public delegate void OnTrxSentDelegate(ContactPOS contact);
        public static OnTrxSentDelegate OnTrxSent;

        private class PosProgress : IProgress
        {
            public ContactPOS Contact;
            public byte Type;
            public PosProgress(ContactPOS contact, byte type)
            {
                Contact = contact;
                Type = type;
            }

            void IProgress.SetProgress(bool success, int current, int max)
            {
                if (success && current == max)
                {
                    if (Type == 27)
                        if (OnTrxSent != null) OnTrxSent.Invoke(Contact);
                }
            }

            void IProgress.TimedOut()
            {
                Contact.HandlePos(Type, null);
            }
        }

        private static Agen ParseAgenInfo(Deserializer buf)
        {
            string data = null;
            if (buf == null || !buf.Extract(ref data))
                return null;

            return new Agen(Util.CommonSplit(data));
        }

        private void HandleRegister(Deserializer buf)
        {
            string raw = null;
            if (buf == null || !buf.Extract(ref raw))
            {
                OnRegister.Invoke(this, 2, null);
                return;
            }

            var data = Util.CommonSplit(raw);
            var result = Convert.ToInt32(data[0]);
            if (result == -1)
            {
                RegistrationNeeded = true;
                return;
            }
            else if (result == 0 || result == 1)
                RegistrationNeeded = false;

            OnRegister.Invoke(this, result, data[1]);
        }

        private static int ParseChangePin(Deserializer buf)
        {
            string data = null;
            if (buf == null || !buf.Extract(ref data))
                return 2;

            return Convert.ToInt32(data);
        }

        private Product[] ParseGetProduct(Deserializer buf)
        {
            string raw = null;
            if (buf == null || !buf.Extract(ref raw) || raw.Length == 0)
                return null;

            var hash = Crypto.hashV2(raw);
            if (hash != m_produkHash)
            {
                m_produkHash = hash;
                File.WriteAllBytes(Path.Combine(Core.GetContactDir(ID), "pcache"), StringEncoder.UTF8.GetBytes(raw));
            }
            return ParseGetProduct(raw);
        }

        private static Product[] ParseGetProduct(string raw)
        {
            var data = raw.Split(new[] { ',' }, StringSplitOptions.None);
            if (data.Length > 0)
            {
                int productCount = 0;
                int.TryParse(data[0], out productCount);
                if (productCount > 0 && productCount + 1 < data.Length)
                {
                    productCount = productCount / 6;
                    var products = new Product[productCount];
                    for (int i = 0; i < productCount; i++)
                        products[i] = new Product(data, i * 6 + 1);

                    ProductPrefix.Parse(data, 1 + (productCount * 6), products);
                    return products;
                }
            }
            return null;
        }

        private static TrxResult ParseTrx(Deserializer buf)
        {
            string data = null;
            if (buf == null || !buf.Extract(ref data))
                return null;

            return new TrxResult(Util.CommonSplit(data));
        }

        private static BookResult ParseBook(Deserializer buf)
        {
            string data = null;
            if (buf == null || !buf.Extract(ref data))
                return null;

            return new BookResult(Util.CommonSplit(data));
        }

        private static string ParseString(Deserializer buf)
        {
            string data = null;
            if (buf == null || !buf.Extract(ref data))
                return null;

            return data;
        }
        #endregion

#endif
    }
}