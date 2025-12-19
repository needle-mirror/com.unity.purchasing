#import <Foundation/Foundation.h>
#import <objc/runtime.h>

void unityPurchasing_TransactionObserved(const char *transactionId,
                                         const char *productId,
                                         const char *productJsonRepresentation,
                                         double transactionUnixTime,
                                         const char *transactionJsonRepresentation,
                                         const char *signatureJws) {
    // Find attribution class
    Class attributionClass = NSClassFromString(@"UnityAds.UnityAds");
    if (!attributionClass) {
        attributionClass = NSClassFromString(@"UnityAds");
    }

    if (!attributionClass) {
        return;
    }
    
    // Check if attribution SDK is initialized
    SEL isInitializedSelector = NSSelectorFromString(@"isInitialized");
    if ([attributionClass respondsToSelector:isInitializedSelector]) {
        NSMethodSignature *isInitSig = [attributionClass methodSignatureForSelector:isInitializedSelector];
        NSInvocation *isInitInvocation = [NSInvocation invocationWithMethodSignature:isInitSig];
        [isInitInvocation setSelector:isInitializedSelector];
        [isInitInvocation setTarget:attributionClass];
        [isInitInvocation invoke];
        
        BOOL isInitialized = NO;
        [isInitInvocation getReturnValue:&isInitialized];
        
        if (!isInitialized) {
            return;
        }
    }

    // Create selector
    SEL selector = NSSelectorFromString(@"recordTransactionInternal:productIdentifier:productJsonRepresentation:transactionDate:transactionJsonRepresentation:jwsRepresentation:");

    if (![attributionClass respondsToSelector:selector]) {
        return;
    }
    
    // Convert C strings to Objective-C objects
    NSString *txId = transactionId ? [NSString stringWithUTF8String:transactionId] : @"";
    NSString *pId = productId ? [NSString stringWithUTF8String:productId] : @"";
    NSString *jws = signatureJws ? [NSString stringWithUTF8String:signatureJws] : @"";
    
    if (txId.length == 0 || pId.length == 0) {
        return;
    }
    
    // Convert base64-encoded JSON strings to NSData
    NSData *pJsonData = nil;
    if (productJsonRepresentation) {
        NSString *pJsonStr = [NSString stringWithUTF8String:productJsonRepresentation];
        pJsonData = [[NSData alloc] initWithBase64EncodedString:pJsonStr options:0];
    }
    if (!pJsonData) pJsonData = [NSData data];

    NSData *tJsonData = nil;
    if (transactionJsonRepresentation) {
        NSString *tJsonStr = [NSString stringWithUTF8String:transactionJsonRepresentation];
        tJsonData = [[NSData alloc] initWithBase64EncodedString:tJsonStr options:0];
    }
    if (!tJsonData) tJsonData = [NSData data];
    
    // Convert unix time to NSDate
    NSDate *date = (transactionUnixTime > 0) ? [NSDate dateWithTimeIntervalSince1970:transactionUnixTime] : [NSDate date];
    
    // Use NSInvocation for reflection call
    NSMethodSignature *signature = [attributionClass methodSignatureForSelector:selector];
    NSInvocation *invocation = [NSInvocation invocationWithMethodSignature:signature];
    [invocation setSelector:selector];
    [invocation setTarget:attributionClass];
    
    // Set arguments (indices 0 and 1 are self and _cmd)
    [invocation setArgument:&txId atIndex:2];
    [invocation setArgument:&pId atIndex:3];
    [invocation setArgument:&pJsonData atIndex:4];
    [invocation setArgument:&date atIndex:5];
    [invocation setArgument:&tJsonData atIndex:6];
    [invocation setArgument:&jws atIndex:7];
    
    // Invoke
    [invocation invoke];
}