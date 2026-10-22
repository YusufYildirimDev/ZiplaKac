#import <Foundation/Foundation.h>
#import <AppTrackingTransparency/AppTrackingTransparency.h>
#import <AdSupport/AdSupport.h>

typedef void (*ATTCallback)(int status);

extern "C" {
    int _GetTrackingAuthorizationStatus() {
        if (@available(iOS 14, *)) {
            return (int)[ATTrackingManager trackingAuthorizationStatus];
        }
        return 3; // Authorized on iOS < 14
    }

    void _RequestTrackingAuthorization(ATTCallback callback) {
        if (@available(iOS 14, *)) {
            [ATTrackingManager requestTrackingAuthorizationWithCompletionHandler:^(ATTrackingManagerAuthorizationStatus status) {
                if (callback != NULL) {
                    callback((int)status);
                }
            }];
        } else {
            if (callback != NULL) {
                callback(3); // Authorized
            }
        }
    }
}
