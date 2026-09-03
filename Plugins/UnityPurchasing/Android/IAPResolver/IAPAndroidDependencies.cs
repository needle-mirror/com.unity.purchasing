using System;
using System.Collections.Generic;
using Unity.SelfDeclaredAndroidDependencies.Editor;

namespace IAPResolver
{
    class IAPAndroidDependencies : AndroidDependencies
    {
        public override string DependantName  => "com.unity.purchasing";
        public override List<string> Dependencies =>
            new List<string>()
            {
                "com.android.billingclient:billing:9.0.0",
                // Not IAP's: Unity's androidx chain pulls kotlin-stdlib-jdk7/jdk8:1.6.21, which duplicate
                // classes now in kotlin-stdlib 1.8+. 1.8.22 are empty stubs; drop at androidx.lifecycle 2.7.0.
                "org.jetbrains.kotlin:kotlin-stdlib-jdk7:1.8.22",
                "org.jetbrains.kotlin:kotlin-stdlib-jdk8:1.8.22"
            };
        public override List<string> Repositories =>
            new List<string>();

        public override List<string> GradleProperties =>
            new List<string>()
            {
                "android.useAndroidX=true",
                "android.enableJetifier=true"
            };
    }
}
