using System;
using System.Threading;

namespace ChatAPI
{
    public static partial class Core
    {
        public delegate bool ActivationResult(bool networkSent, ActivationType type, byte resultCode, object extraInfo);
        public static ActivationResult OnActivation;
        private static ManualResetEvent m_activationWaitEvent;

        private static NSerializer CreateActivationRequest(string username, ActivationType type)
        {
            var buf = new NSerializer(TypeHeader.ACTIVATION);
            buf.Add(username);
            buf.Add((byte)type);
            buf.Add(Network.PROTOCOL_MAJOR_VERSION);
            buf.Add(Network.PROTOCOL_MINOR_VERSION);
            return buf;
        }

        private static bool ActivationWaitTransmit(NSerializer buf)
        {
            int retryTimes = 4;
        retry:
            Network.Connect(null);
            if (!Network.IsAvailable)
            {
                if (--retryTimes == 0)
                    return false;

                Thread.Sleep(500);
                goto retry;
            }

            m_activationWaitEvent = new ManualResetEvent(false);
            Network.Send(buf);
            var result = m_activationWaitEvent.WaitOne(12000, false);
            m_activationWaitEvent = null;
            return result;
        }

        private static bool ActivationTestServerResponse()
        {
            var buf = CreateActivationRequest(string.Empty, ActivationType.Ping);
            if (!ActivationWaitTransmit(buf))
            {
                Network.Close(); // LOL!! force close connection
                if (!ActivationWaitTransmit(buf)) // don't give up, lets try again
                    return false;
            }
            return true;
        }

        public static void ActivationCheckUsernameAvailability(string username)
        {
            Util.StartThread(() =>
            {
                if (ActivationTestServerResponse())
                    Network.Send(CreateActivationRequest(username, ActivationType.CheckUsernameAvailability));
                else if (OnActivation != null)
                    OnActivation.Invoke(false, ActivationType.CheckUsernameAvailability, 255, null);
            });
        }

        public static void ActivationSignUp(string name, string username, string password)
        {
            CryptoKey = null;
            CryptoKeyPassword = null;
            Util.StartThread(() =>
            {
                if (ActivationTestServerResponse())
                {
                    var buf = CreateActivationRequest(username, ActivationType.SignUp);
                    buf.Add(name);
                    buf.Add(Crypto.EncryptPassword(password));
                    buf.Add((byte)Setting.Language);
                    Network.Send(buf);
                }
                else if (OnActivation != null)
                    OnActivation.Invoke(false, ActivationType.SignUp, 255, null);
            });
        }

        public static void ActivationUseExisting(string username, string password)
        {
            CryptoKey = null;
            CryptoKeyPassword = null;
            Util.StartThread(() =>
            {
                if (ActivationTestServerResponse())
                {
                    var buf = CreateActivationRequest(username, ActivationType.UseExistingAccount);
                    buf.Add(Crypto.EncryptPassword(password));
                    buf.Add((byte)Setting.Language);
                    Network.Send(buf);
                }
                else if (OnActivation != null)
                    OnActivation.Invoke(false, ActivationType.UseExistingAccount, 255, null);
            });
        }

        public static void ActivationRecovery(string username, int loginToken, string recoveryCode, string password)
        {
            Util.StartThread(() =>
            {
                if (ActivationTestServerResponse())
                {
                    var buf = CreateActivationRequest(username, ActivationType.Recovery);
                    buf.Add(recoveryCode);
                    buf.Add(string.IsNullOrEmpty(password) ? Util.EmptyBytes : Crypto.EncryptPassword(password));
                    buf.Add(loginToken);
                    Network.Send(buf);
                }
                else if (OnActivation != null)
                    OnActivation.Invoke(false, ActivationType.Recovery, 255, null);
            });
        }

        public static void ActivationGetSecurityQuestion(string username)
        {
            Util.StartThread(() =>
            {
                if (ActivationTestServerResponse())
                {
                    var buf = CreateActivationRequest(username, ActivationType.GetSecurityQuestion);
                    buf.Add((byte)Setting.Language);
                    Network.Send(buf);
                }
                else if (OnActivation != null)
                    OnActivation.Invoke(false, ActivationType.GetSecurityQuestion, 255, null);
            });
        }

        public static void ActivationAnswerSecurityQuestion(string username, string answer1, string answer2)
        {
            Util.StartThread(() =>
            {
                if (ActivationTestServerResponse())
                {
                    var buf = CreateActivationRequest(username, ActivationType.AnswerSecurityQuestion);
                    buf.Add(answer1);
                    buf.Add(answer2);
                    Network.Send(buf);
                }
                else if (OnActivation != null)
                    OnActivation.Invoke(false, ActivationType.AnswerSecurityQuestion, 255, null);
            });
        }

        public static void ActivationUpdateSecurityQuestion(string username, byte selectedQuestionIndex1, string answer1, byte selectedQuestionIndex2, string answer2)
        {
            Util.StartThread(() =>
            {
                if (ActivationTestServerResponse())
                {
                    var buf = CreateActivationRequest(username, ActivationType.UpdateSecurityQuestion);
                    var s = new Serializer();
                    s.Add(selectedQuestionIndex1);
                    s.Add(answer1);
                    s.Add(selectedQuestionIndex2);
                    s.Add(answer2);
                    buf.SecureAdd(s, CryptoKey);
                    Network.Send(buf);
                }
                else if (OnActivation != null)
                    OnActivation.Invoke(false, ActivationType.UpdateSecurityQuestion, 255, null);
            });
        }

        internal static void SetActivationResult(NDeserializer buf)
        {
            byte type = 0;
            if (!buf.Extract(ref type))
                return; // Reject SPAM!!

            byte resultCode = 0;
            if (!buf.Extract(ref resultCode))
                return; // Reject SPAM!!

            var wait = m_activationWaitEvent;
            if (resultCode == (byte)LoginStatus.FAIL_UNSUPPORTED_PROTOCOL)
            {
                OnActivation = null; // Since App need to close
                if (wait != null) wait.Set();
                SetLoginResult(LoginStatus.FAIL_UNSUPPORTED_PROTOCOL, buf);
                return;
            }

            if (wait != null) wait.Set();
            if (type == (byte)ActivationType.Ping)
                return;

            if (type == (byte)ActivationType.SignUp || type == (byte)ActivationType.UseExistingAccount)
            {
                SecurityQuestion sq = null;
                if (resultCode == 0)
                {
                    if (!VerifySignUpOrUseExisting(buf))
                        return;

                    sq = ParseSecurityQuestion(buf, 0);
                }

                else if (resultCode == (byte)LoginStatus.FAIL_ACCOUNT_HAS_BEEN_BANNED)
                {
                    SetLoginResult(LoginStatus.FAIL_ACCOUNT_HAS_BEEN_BANNED, buf);
                    return;
                }

                if (OnActivation != null)
                    OnActivation.Invoke(true, (ActivationType)type, resultCode, sq);
            }

            else if (type == (byte)ActivationType.Recovery)
            {
                if (resultCode == 0)
                {
                    byte[] cryptoKeyPassword = null;
                    if (buf.Extract(ref cryptoKeyPassword) && cryptoKeyPassword.Length == 32 + 4)
                        SavePassword(cryptoKeyPassword);
                }
                if (OnActivation != null)
                    OnActivation.Invoke(true, ActivationType.Recovery, resultCode, null);
            }

            else if (type == (byte)ActivationType.CheckUsernameAvailability)
            {
                string id = null;
                if (buf.Extract(ref id) && OnActivation != null)
                    OnActivation.Invoke(true, ActivationType.CheckUsernameAvailability, resultCode, id);

            }

            else if (type == (byte)ActivationType.GetSecurityQuestion)
            {
                if (OnActivation != null)
                    OnActivation.Invoke(true, ActivationType.GetSecurityQuestion, resultCode, ParseSecurityQuestion(buf, resultCode));
            }

            else if (type == (byte)ActivationType.AnswerSecurityQuestion)
            {
                if (OnActivation != null)
                    OnActivation.Invoke(true, ActivationType.AnswerSecurityQuestion, resultCode, new AnswerSecurityQuestionResult(buf, resultCode));
            }

            else if (type == (byte)ActivationType.UpdateSecurityQuestion)
            {
                if (OnActivation != null)
                    OnActivation.Invoke(true, ActivationType.UpdateSecurityQuestion, resultCode, null);
            }
        }

        private static bool VerifySignUpOrUseExisting(Deserializer buf)
        {
            string ownerId = null, username = null, name = null;
            byte[] cryptoKey = null, cryptoKeyPassword = null;
            uint accType = 0;
            long joinDate = 0;

            if (buf.Extract(ref ownerId) &&
                buf.Extract(ref username) &&
                buf.Extract(ref name) &&
                buf.Extract(ref cryptoKey) && cryptoKey.Length == 32 + 4 &&
                buf.Extract(ref cryptoKeyPassword) && cryptoKeyPassword.Length == 32 + 4 &&
                buf.Extract(ref accType) &&
                buf.Extract(ref joinDate))
            {
                SaveOwner(ownerId, name, username, cryptoKey, cryptoKeyPassword, accType, joinDate);
                Login();
                return true;
            }
            return false;
        }

        private static SecurityQuestion ParseSecurityQuestion(Deserializer buf, byte resultCode)
        {
            Deserializer securedBuffer = buf;
            if (resultCode == 0)
            {
                if (!buf.SecureExtract(ref securedBuffer, CryptoKey))
                    return null; // Something wrong here. User already logged out or server update
            }
            return new SecurityQuestion(securedBuffer);
        }

        public class SecurityQuestion
        {
            public string[] QuestionList1;
            public string[] QuestionList2;
            public int SelectedQuestionIndex1;
            public int SelectedQuestionIndex2;
            public string Answer1;
            public string Answer2;

            internal SecurityQuestion(Deserializer buf)
            {
                this.SelectedQuestionIndex1 = -1;
                this.SelectedQuestionIndex2 = -1;

                string questionList1 = null, questionList2 = null;
                byte selectedQuestionIndex1 = 255, selectedQuestionIndex2 = 255;
                if (buf.Extract(ref questionList1) && buf.Extract(ref selectedQuestionIndex1) && buf.Extract(ref this.Answer1) &&
                    buf.Extract(ref questionList2) && buf.Extract(ref selectedQuestionIndex2) && buf.Extract(ref this.Answer2))
                {
                    this.QuestionList1 = Util.CommonSplit(questionList1);
                    this.QuestionList2 = Util.CommonSplit(questionList2);
                    if (selectedQuestionIndex1 != 255) this.SelectedQuestionIndex1 = selectedQuestionIndex1;
                    if (selectedQuestionIndex2 != 255) this.SelectedQuestionIndex2 = selectedQuestionIndex2;
                };
            }
        }

        public class AnswerSecurityQuestionResult
        {
            public bool Answer1IsCorrect;
            public bool Answer2IsCorrect;
            public int LoginToken;
            public string RecoveryCode;

            internal AnswerSecurityQuestionResult(Deserializer buf, byte resultCode)
            {
                byte resultCode2 = 0;
                if (buf.Extract(ref resultCode2) && buf.Extract(ref this.LoginToken) && buf.Extract(ref this.RecoveryCode))
                {
                    Answer1IsCorrect = resultCode == 0;
                    Answer2IsCorrect = resultCode2 == 0;
                }
            }
        }
    }
}