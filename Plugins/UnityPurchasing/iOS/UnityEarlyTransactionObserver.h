#if !MAC_APPSTORE
#import <StoreKit/StoreKit.h>
#if !UNITY_XCODE_PROJECT_TYPE_SWIFT
#import "LifeCycleListener.h"
#endif

@protocol UnityEarlyTransactionObserverDelegate <NSObject>

- (void)promotionalPurchaseAttempted:(SKPayment *)payment;

@end

#if UNITY_XCODE_PROJECT_TYPE_SWIFT
@interface UnityEarlyTransactionObserver : NSObject<SKPaymentTransactionObserver> {
#else
@interface UnityEarlyTransactionObserver : NSObject<SKPaymentTransactionObserver, LifeCycleListener> {
#endif
    NSMutableSet *m_QueuedPayments;
}

@property BOOL readyToReceiveTransactionUpdates;

// The delegate exists so that the observer can notify it of attempted promotional purchases.
@property(nonatomic, weak) id<UnityEarlyTransactionObserverDelegate> delegate;

+ (UnityEarlyTransactionObserver*)defaultObserver;

- (void)initiateQueuedPayments;

@end
#endif
