#import <Foundation/Foundation.h>
#import <StoreKit/StoreKit.h>

extern "C" {
    void _RequestStoreReview() {
        if (@available(iOS 14.0, *)) {
            dispatch_async(dispatch_get_main_queue(), ^{
                NSSet<UIScene *> *scenes = [[UIApplication sharedApplication] connectedScenes];
                for (UIScene *scene in scenes) {
                    if (scene.activationState == UISceneActivationStateForegroundActive && [scene isKindOfClass:[UIWindowScene class]]) {
                        [SKStoreReviewController requestReviewInScene:(UIWindowScene *)scene];
                        return;
                    }
                }
            });
        } else if (@available(iOS 10.3, *)) {
            dispatch_async(dispatch_get_main_queue(), ^{
                [SKStoreReviewController requestReview];
            });
        }
    }
}
