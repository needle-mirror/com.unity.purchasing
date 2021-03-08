
namespace UnityEngine.Purchasing.Default {
    public class Factory {

        public static IWindowsIAP Create(bool mocked) {
            ICurrentApp app;
            if (mocked) {
                app = new UnibillCurrentAppSimulator();
            }
            else
            {
                app = new CurrentApp();
            }
            
            return new WinRTStore(app);
        }
    }
}
