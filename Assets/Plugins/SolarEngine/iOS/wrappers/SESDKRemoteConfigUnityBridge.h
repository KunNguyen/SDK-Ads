//
//  SolarEngineSDKUnityBridge.h
//  SolarEngineSDK
//
//  Created by MVP on 2022/1/24.
//

#import <Foundation/Foundation.h>

extern "C" {
    void __iOSSESDKSetDefaultConfig (const char * config);
    void __iOSSESDKSetRemoteConfigEventProperties(const char *properties);
    void __iOSSESDKSetRemoteConfigUserProperties(const char *properties);
    char * __iOSSESDKFastFetchRemoteConfig(const char *key);
    void __iOSSESDKAsyncFetchRemoteConfig(const char *key);


}
    

NS_ASSUME_NONNULL_END
