#import <Foundation/Foundation.h>
#import <objc/message.h>
#import <stdatomic.h>

// Reflection-only bridge to Firebase Analytics (FIRAnalytics) and FirebaseCore
// (FIRApp). Firebase is optional, so nothing here links against it: classes
// are looked up by name, every selector is guarded with respondsToSelector:, and
// each field degrades to NULL when unavailable (Firebase absent or too old for
// the API; the session id and app id also until FIRApp is configured).

// Defined in UnityPurchasing.m.
extern char* UnityPurchasingMakeHeapAllocatedStringCopy(NSString* string);

typedef void (*UnityPurchasingFirebaseSessionIdCallback)(const char* sessionId, bool timedOut);

// App instance id: +[FIRAnalytics appInstanceID], added in Firebase iOS SDK 3.13.0.
// Requires FIRApp as well, matching the two-class check in Android's
// FirebaseAnalyticsClient.
char* unityPurchasingFirebaseAppInstanceId(void)
{
    Class analytics = NSClassFromString(@"FIRAnalytics");
    SEL selector = NSSelectorFromString(@"appInstanceID");
    if (NSClassFromString(@"FIRApp") == nil
        || analytics == nil || ![analytics respondsToSelector:selector]) {
        return NULL;
    }
    id result = ((id (*)(id, SEL))objc_msgSend)(analytics, selector);
    return [result isKindOfClass:[NSString class]] ? UnityPurchasingMakeHeapAllocatedStringCopy(result) : NULL;
}

// +isDefaultAppConfigured is not public API, but unlike +defaultApp it does
// not log an error when nothing is configured. Where it is missing, ask
// +defaultApp.
static BOOL UnityPurchasingFirebaseDefaultAppConfigured(Class appClass)
{
    SEL configuredSelector = NSSelectorFromString(@"isDefaultAppConfigured");
    if ([appClass respondsToSelector:configuredSelector]) {
        return ((BOOL (*)(id, SEL))objc_msgSend)(appClass, configuredSelector);
    }
    SEL defaultAppSelector = NSSelectorFromString(@"defaultApp");
    return [appClass respondsToSelector:defaultAppSelector]
        && ((id (*)(id, SEL))objc_msgSend)(appClass, defaultAppSelector) != nil;
}

// Whether the default FIRApp is configured.
bool unityPurchasingFirebaseIsDefaultAppConfigured(void)
{
    Class appClass = NSClassFromString(@"FIRApp");
    if (appClass == nil) {
        return false;
    }
    return UnityPurchasingFirebaseDefaultAppConfigured(appClass);
}

// Firebase app id: [FIRApp defaultApp].options.googleAppID. NULL until the
// app has called [FIRApp configure].
char* unityPurchasingFirebaseAppId(void)
{
    Class appClass = NSClassFromString(@"FIRApp");
    SEL defaultAppSelector = NSSelectorFromString(@"defaultApp");
    if (appClass == nil || ![appClass respondsToSelector:defaultAppSelector]) {
        return NULL;
    }
    if (!UnityPurchasingFirebaseDefaultAppConfigured(appClass)) {
        return NULL;
    }
    id app = ((id (*)(id, SEL))objc_msgSend)(appClass, defaultAppSelector);
    SEL optionsSelector = NSSelectorFromString(@"options");
    if (app == nil || ![app respondsToSelector:optionsSelector]) {
        return NULL;
    }
    id options = ((id (*)(id, SEL))objc_msgSend)(app, optionsSelector);
    SEL appIdSelector = NSSelectorFromString(@"googleAppID");
    if (options == nil || ![options respondsToSelector:appIdSelector]) {
        return NULL;
    }
    id appId = ((id (*)(id, SEL))objc_msgSend)(options, appIdSelector);
    return [appId isKindOfClass:[NSString class]] ? UnityPurchasingMakeHeapAllocatedStringCopy(appId) : NULL;
}

// Mimics [FIROptions defaultOptions].googleAppID, GOOGLE_APP_ID from the plist.
// That call logs an error when no plist is bundled, so the file is first looked
// for where Firebase looks: the main bundle, then FirebaseCore's own bundle.
// Requires FirebaseCore to be linked, so a leftover plist alone yields nothing.
char* unityPurchasingFirebaseBundledAppId(void)
{
    Class appClass = NSClassFromString(@"FIRApp");
    if (appClass == nil) {
        return NULL;
    }
    BOOL bundled = NO;
    for (NSBundle* bundle in @[ [NSBundle mainBundle], [NSBundle bundleForClass:appClass] ]) {
        if ([bundle pathForResource:@"GoogleService-Info" ofType:@"plist"] != nil) {
            bundled = YES;
            break;
        }
    }
    Class optionsClass = NSClassFromString(@"FIROptions");
    SEL defaultOptionsSelector = NSSelectorFromString(@"defaultOptions");
    SEL appIdSelector = NSSelectorFromString(@"googleAppID");
    if (!bundled || optionsClass == nil || ![optionsClass respondsToSelector:defaultOptionsSelector]) {
        return NULL;
    }
    id options = ((id (*)(id, SEL))objc_msgSend)(optionsClass, defaultOptionsSelector);
    if (options == nil || ![options respondsToSelector:appIdSelector]) {
        return NULL;
    }
    id appId = ((id (*)(id, SEL))objc_msgSend)(options, appIdSelector);
    return [appId isKindOfClass:[NSString class]] ? UnityPurchasingMakeHeapAllocatedStringCopy(appId) : NULL;
}

// Session id: +[FIRAnalytics sessionIDWithCompletion:], added in Firebase
// iOS SDK 9.6.0; older SDKs get NULL. The completion may fire on any queue;
// the C# side marshals the string during the callback.
//
// The callback fires exactly once: with the result, or with NULL and timedOut
// set after a timeout. Firebase can drop the completion, and the C# side hands every
// caller the same pending request until this callback fires.
static const int64_t kUnityPurchasingFirebaseSessionIdTimeoutSeconds = 2;

void unityPurchasingFirebaseSessionId(UnityPurchasingFirebaseSessionIdCallback callback)
{
    Class analytics = NSClassFromString(@"FIRAnalytics");
    SEL selector = NSSelectorFromString(@"sessionIDWithCompletion:");
    if (analytics == nil || ![analytics respondsToSelector:selector]) {
        callback(NULL, false);
        return;
    }
    // Shared by the completion and the timeout; whichever flips it first delivers.
    __block atomic_bool delivered = false;
    void (^completion)(int64_t, NSError* _Nullable) = ^(int64_t sessionID, NSError* _Nullable error) {
        if (atomic_exchange(&delivered, true)) {
            return;
        }
        // sessionID can be 0 with a nil error (firebase-ios-sdk#15258) — treat as unavailable.
        if (error != nil || sessionID == 0) {
            callback(NULL, false);
            return;
        }
        callback([NSString stringWithFormat:@"%lld", sessionID].UTF8String, false);
    };
    dispatch_after(dispatch_time(DISPATCH_TIME_NOW, kUnityPurchasingFirebaseSessionIdTimeoutSeconds * (int64_t)NSEC_PER_SEC),
                   dispatch_get_global_queue(QOS_CLASS_UTILITY, 0), ^{
        if (!atomic_exchange(&delivered, true)) {
            callback(NULL, true);
        }
    });
    ((void (*)(id, SEL, id))objc_msgSend)(analytics, selector, completion);
}
