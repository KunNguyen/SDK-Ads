//
//  SolarEngineSDKUnityBridge.m
//  SolarEngineSDK
//
//  Created by MVP on 2022/1/24.
//

#include <string.h>
#include <stdlib.h>

#import <Foundation/Foundation.h>
#import <SolarEngineSDK/SolarEngineSDK.h>
#import <SolarEngineSDK/SERemoteConfigManager.h>

typedef void (*FetchRemoteConfigCallback)(const char * result);

static char *reteinStr(const char *str) {
    if (str == NULL) {
        return NULL;
    }

    char *ret = (char *)malloc(strlen(str) + 1);
    strcpy(ret, str);
    return ret;
}


extern "C" {

    void __iOSSESDKSetDefaultConfig (const char * config) {
        if (config == NULL) {
            NSLog(@"Error: _iOSSESDKSetDefaultConfig: config must not be empty.");
                    return;
        }
        
        NSArray *defaultConfig = nil;
        NSString *_config = [NSString stringWithUTF8String:config];
        if (![_config isEqualToString:@"null"]) {
            NSData *data = [_config dataUsingEncoding:NSUTF8StringEncoding];
            NSError *error = nil;
            defaultConfig = [NSJSONSerialization JSONObjectWithData:data options:0 error:&error];
            if (error) {
                NSString *msg = [NSString stringWithFormat:@"%@ is not an invalid JSON data",_config];
                NSLog(@"__iOSSESDKSetRemoteConfigUserProperties, error :%@",msg);
                return;
            }
        }
        
        [[SERemoteConfigManager sharedInstance] setDefaultConfig:defaultConfig];
        
    }

    void __iOSSESDKSetRemoteConfigEventProperties(const char *properties) {
        NSDictionary *dict = nil;
        if (properties != NULL) {
            NSString *_properties = [NSString stringWithUTF8String:properties];
            if (![_properties isEqualToString:@"null"]) {
                NSData *data = [_properties dataUsingEncoding:NSUTF8StringEncoding];
                NSError *error = nil;
                dict = [NSJSONSerialization JSONObjectWithData:data options:0 error:&error];
                if (error) {
                    NSString *msg = [NSString stringWithFormat:@"%@ is not an invalid JSON data",_properties];
                    NSLog(@"__iOSSESDKSetRemoteConfigUserProperties, error :%@",msg);
                    return;
                }
            }
        }

        [[SERemoteConfigManager sharedInstance] setRemoteConfigEventProperties:dict];
    }

    void __iOSSESDKSetRemoteConfigUserProperties(const char *properties) {
        NSDictionary *dict = nil;
        if (properties != NULL) {
            NSString *_properties = [NSString stringWithUTF8String:properties];
            if (![_properties isEqualToString:@"null"]) {
                NSData *data = [_properties dataUsingEncoding:NSUTF8StringEncoding];
                NSError *error = nil;
                dict = [NSJSONSerialization JSONObjectWithData:data options:0 error:&error];
                if (error) {
                    NSString *msg = [NSString stringWithFormat:@"%@ is not an invalid JSON data",_properties];
                    NSLog(@"__iOSSESDKSetRemoteConfigUserProperties, error :%@",msg);
                    return;
                }
            }
        }

        [[SERemoteConfigManager sharedInstance] setRemoteConfigUserProperties:dict];
    }

    char * __iOSSESDKFastFetchRemoteConfig(const char *key) {
        NSString *keyString = [NSString stringWithUTF8String:key];
        id data = [[SERemoteConfigManager sharedInstance] fastFetchRemoteConfig:keyString];
        NSString *msg = [NSString stringWithFormat:@"%@", data];
        
        if ([data isKindOfClass:[NSDictionary class]] || [data isKindOfClass:[NSArray class]]) {
            NSData *d = [NSJSONSerialization dataWithJSONObject:data options:0 error:nil];
            if (d) {
                msg = [[NSString alloc] initWithData:d encoding:NSUTF8StringEncoding];
            }
        }
        
        return reteinStr([msg cStringUsingEncoding:NSUTF8StringEncoding]);
    }
    
    void __iOSSESDKAsyncFetchRemoteConfig(const char * key , FetchRemoteConfigCallback callback) {
        NSString *keyString = [NSString stringWithUTF8String:key];
        [[SERemoteConfigManager sharedInstance] asyncFetchRemoteConfig:keyString completionHandler:^(id  _Nonnull data) {
            NSString *msg = [NSString stringWithFormat:@"%@", data];
            if ([data isKindOfClass:[NSDictionary class]] || [data isKindOfClass:[NSArray class]]) {
                NSData *d = [NSJSONSerialization dataWithJSONObject:data options:0 error:nil];
                if (d) {
                    msg = [[NSString alloc] initWithData:d encoding:NSUTF8StringEncoding];
                }
            }
            callback([msg cStringUsingEncoding:NSUTF8StringEncoding]);
        }];
    }

    char * __iOSSESDKFastFetchAllRemoteConfig() {
        NSDictionary *dict = [[SERemoteConfigManager sharedInstance] fastFetchRemoteConfig];
        NSData *data = [NSJSONSerialization dataWithJSONObject:dict options:0 error:nil];
        if (data == nil) {
            return NULL;
        }
        NSString *dataString = [[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding];
        return reteinStr([dataString cStringUsingEncoding:NSUTF8StringEncoding]);
    }

    void __iOSSESDKAsyncFetchAllRemoteConfig(FetchRemoteConfigCallback callback) {
                
        [[SERemoteConfigManager sharedInstance] asyncFetchRemoteConfigWithCompletionHandler:^(NSDictionary * _Nonnull dict) {
            
            if (dict) {
                NSData *data = [NSJSONSerialization dataWithJSONObject:dict options:0 error:nil];
                if (data != nil) {
                    NSString *dataString = [[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding];
                    callback(reteinStr([dataString cStringUsingEncoding:NSUTF8StringEncoding]));
                } else {
                    callback(NULL);
                }
            } else {
                callback(NULL);
            }


        }];
    }

}
