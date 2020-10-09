using System;

namespace UnityEngine.Purchasing
{
    public class FakeUDPExtension : IUDPExtensions
    {
        public object GetUserInfo()
        {
            Type udpUserInfo = UserInfoInterface.GetClassType();
            if (udpUserInfo == null)
            {
                return null;
            }

            object userInfo = Activator.CreateInstance(udpUserInfo);

            var channelProp = UserInfoInterface.GetChannelProp();
            channelProp.SetValue(userInfo, "Fake_Channel", null);
            var userIdProp = UserInfoInterface.GetIdProp();
            userIdProp.SetValue(userInfo, "Fake_User_Id_123456", null);
            var loginTokenProp = UserInfoInterface.GetIdProp();
            loginTokenProp.SetValue(userInfo, "Fake_Login_Token", null);
            return userInfo;
        }

        public string GetLastInitializationError()
        {
            return "Fake Initialization Error";
        }

        public string GetLastPurchaseError()
        {
            return "Fake Purchase Error";
        }

        public void EnableDebugLog(bool enable)
        {
            return;
        }
    }
}
