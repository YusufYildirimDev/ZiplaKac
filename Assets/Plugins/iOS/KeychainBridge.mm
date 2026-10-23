#import <Foundation/Foundation.h>
#import <Security/Security.h>

extern "C" {
    void _KeychainSetString(const char* key, const char* value) {
        if (!key || !value) return;
        NSString *keyStr = [NSString stringWithUTF8String:key];
        NSString *valStr = [NSString stringWithUTF8String:value];
        NSData *valData = [valStr dataUsingEncoding:NSUTF8StringEncoding];

        NSString *service = [[NSBundle mainBundle] bundleIdentifier];
        if (!service) service = @"com.yusufyldrm.ziplakac";

        NSDictionary *query = @{
            (__bridge id)kSecClass: (__bridge id)kSecClassGenericPassword,
            (__bridge id)kSecAttrService: service,
            (__bridge id)kSecAttrAccount: keyStr
        };

        // Delete existing item first
        SecItemDelete((__bridge CFDictionaryRef)query);

        // Add new item with AccessibleAfterFirstUnlock
        NSMutableDictionary *newQuery = [query mutableCopy];
        [newQuery setObject:valData forKey:(__bridge id)kSecValueData];
        [newQuery setObject:(__bridge id)kSecAttrAccessibleAfterFirstUnlock forKey:(__bridge id)kSecAttrAccessible];

        SecItemAdd((__bridge CFDictionaryRef)newQuery, NULL);
    }

    char* _KeychainGetString(const char* key) {
        if (!key) return NULL;
        NSString *keyStr = [NSString stringWithUTF8String:key];

        NSString *service = [[NSBundle mainBundle] bundleIdentifier];
        if (!service) service = @"com.yusufyldrm.ziplakac";

        NSDictionary *query = @{
            (__bridge id)kSecClass: (__bridge id)kSecClassGenericPassword,
            (__bridge id)kSecAttrService: service,
            (__bridge id)kSecAttrAccount: keyStr,
            (__bridge id)kSecReturnData: (__bridge id)kCFBooleanTrue,
            (__bridge id)kSecMatchLimit: (__bridge id)kSecMatchLimitOne
        };

        CFTypeRef result = NULL;
        OSStatus status = SecItemCopyMatching((__bridge CFDictionaryRef)query, &result);
        if (status == errSecSuccess && result != NULL) {
            NSData *data = (__bridge_transfer NSData*)result;
            NSString *str = [[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding];
            if (str) {
                const char* utf8 = [str UTF8String];
                char* res = (char*)malloc(strlen(utf8) + 1);
                strcpy(res, utf8);
                return res;
            }
        }
        return NULL;
    }

    void _KeychainDelete(const char* key) {
        if (!key) return;
        NSString *keyStr = [NSString stringWithUTF8String:key];

        NSString *service = [[NSBundle mainBundle] bundleIdentifier];
        if (!service) service = @"com.yusufyldrm.ziplakac";

        NSDictionary *query = @{
            (__bridge id)kSecClass: (__bridge id)kSecClassGenericPassword,
            (__bridge id)kSecAttrService: service,
            (__bridge id)kSecAttrAccount: keyStr
        };
        SecItemDelete((__bridge CFDictionaryRef)query);
    }

    char* _GetIOSDeviceLanguage() {
        NSArray *languages = [NSLocale preferredLanguages];
        if (languages != nil && [languages count] > 0) {
            NSString *preferredLang = [languages objectAtIndex:0];
            if (preferredLang != nil) {
                const char* utf8 = [preferredLang UTF8String];
                char* res = (char*)malloc(strlen(utf8) + 1);
                strcpy(res, utf8);
                return res;
            }
        }
        return strdup("en");
    }
}
