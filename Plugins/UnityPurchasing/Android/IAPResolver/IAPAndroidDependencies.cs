using System;
using System.Collections.Generic;
using Unity.SelfDeclaredAndroidDependencies.Editor;

namespace IAPResolver
{
    class IAPAndroidDependencies : AndroidDependencies
    {
        public override string DependantName => "com.unity.purchasing";
        public override List<string> Dependencies =>
            new List<string>()
            {
                "androidx.activity:activity-compose:1.3.1",
                "com.android.billingclient:billing:6.2.1"
            };
        public override List<string> Repositories =>
            new List<string>();

        public override List<string> GradleProperties =>
            new List<string>()
            {
                "android.useAndroidX=true"
            };
    }
}
