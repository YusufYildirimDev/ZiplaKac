#import <Foundation/Foundation.h>
#import <UserNotifications/UserNotifications.h>

extern "C" {
    void _RequestNotificationPermission() {
        if (@available(iOS 10.0, *)) {
            UNUserNotificationCenter *center = [UNUserNotificationCenter currentNotificationCenter];
            [center requestAuthorizationWithOptions:(UNAuthorizationOptionAlert | UNAuthorizationOptionSound | UNAuthorizationOptionBadge)
                                  completionHandler:^(BOOL granted, NSError * _Nullable error) {
                // Permission handled
            }];
        }
    }

    void _ScheduleLocalNotification(const char* title, const char* message, int delaySeconds, const char* identifier) {
        if (@available(iOS 10.0, *)) {
            UNUserNotificationCenter *center = [UNUserNotificationCenter currentNotificationCenter];
            
            NSString *nId = [NSString stringWithUTF8String:identifier];
            [center removePendingNotificationRequestsWithIdentifiers:@[nId]];

            UNMutableNotificationContent *content = [[UNMutableNotificationContent alloc] init];
            content.title = [NSString stringWithUTF8String:title];
            content.body = [NSString stringWithUTF8String:message];
            content.sound = [UNNotificationSound defaultSound];

            UNTimeIntervalNotificationTrigger *trigger = [UNTimeIntervalNotificationTrigger triggerWithTimeInterval:delaySeconds repeats:NO];
            UNNotificationRequest *request = [UNNotificationRequest requestWithIdentifier:nId content:content trigger:trigger];

            [center addNotificationRequest:request withCompletionHandler:nil];
        }
    }

    void _CancelAllNotifications() {
        if (@available(iOS 10.0, *)) {
            UNUserNotificationCenter *center = [UNUserNotificationCenter currentNotificationCenter];
            [center removeAllPendingNotificationRequests];
        }
    }
}
