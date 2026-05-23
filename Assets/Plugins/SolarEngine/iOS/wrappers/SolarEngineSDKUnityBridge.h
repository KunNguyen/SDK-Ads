//
//  SolarEngineSDKUnityBridge.h
//  SolarEngineSDK
//
//  Created by MVP on 2022/1/24.
//

#import <Foundation/Foundation.h>

extern "C" {

void __iOSSolarEngineSDKInit(const char * appKey, const char * SEUserId, const char *seConfig, const char *rcConfig);

void __iOSSolarEngineSDKSetGDPRArea(bool isGDPRArea);

void __iOSSolarEngineSDKSetPresetEvent(const char *presetEventName, const char *properties);

void __iOSSolarEngineSDKTrack(const char *eventName, const char *attributes);

void __iOSSolarEngineSDKTrackIAPWithAttributes(const char *IAPAttribute);

void __iOSSolarEngineSDKTrackAdImpressionWithAttributes(const char *adImpressionAttribute);

void __iOSSolarEngineSDKTrackAdClickWithAttributes(const char *adClickAttribute);

void __iOSSolarEngineSDKTrackRegisterWithAttributes(const char *registerAttribute);

void __iOSSolarEngineSDKTrackLoginWithAttributes(const char *loginAttribute);

void __iOSSolarEngineSDKTrackOrderWithAttributes(const char *orderAttribute);

void __iOSSolarEngineSDKSetVisitorID(const char *visitorID);

char* __iOSSolarEngineSDKVisitorID(void);

char* __iOSSolarEngineSDKVisitorID(void);

void __iOSSolarEngineSDKLoginWithAccountID(const char *accountID);

char* __iOSSolarEngineSDKAccountID(void);

void __iOSSolarEngineSDKLogout(void);

void __iOSSolarEngineSDKSetSuperProperties(const char *properties);

void __iOSSolarEngineSDKUnsetSuperProperty(const char *property);

void __iOSSolarEngineSDKClearSuperProperties(void);

void __iOSSolarEngineSDKUserInit(const char *properties);

void __iOSSolarEngineSDKUserUpdate(const char *properties);

void __iOSSolarEngineSDKUserAdd(const char *properties);

void __iOSSolarEngineSDKUserUnset(const char *keys);

void __iOSSolarEngineSDKUserAppend(const char *properties);

void __iOSSolarEngineSDKUserDelete(int deleteType);

void __iOSSolarEngineSDKEventStart(const char *eventName);

void __iOSSolarEngineSDKEventFinish(const char *eventJSONStr);

}
    

NS_ASSUME_NONNULL_END
