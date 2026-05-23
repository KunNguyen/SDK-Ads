//
//  SolarEngineSDKUnityBridge.m
//  SolarEngineSDK
//
//  Created by MVP on 2022/1/24.
//

#import <Foundation/Foundation.h>
#import <SolarEngineSDK/SolarEngineSDK.h>

#if  __has_include(<SolarEngineSDK/SESDKForCN.h>)
    #import <SolarEngineSDK/SESDKForCN.h>
#endif
#if  __has_include(<SolarEngineSDK/SESDKForUS.h>)
    #import <SolarEngineSDK/SESDKForUS.h>
#endif

typedef void (*SEBridgeCallback)(int errorCode, const char * data);
typedef void (*SEBridgeInitCallback)(int errorCode);

NSString * const SEKeyFlutterEventType                                   = @"_event_type";

NSString * const SEKeyFlutterEventNameIAP                                = @"_appPur";
NSString * const SEKeyFlutterEventNameAdImpresstion                      = @"_appImp";
NSString * const SEKeyFlutterEventNameAdClick                            = @"_appClick";
NSString * const SEKeyFlutterEventNameAppAttr                            = @"_appAttr";
NSString * const SEKeyFlutterEventNameRegister                           = @"_appReg";
NSString * const SEKeyFlutterEventNameLogin                              = @"_appLogin";
NSString * const SEKeyFlutterEventNameOrder                              = @"_appOrder";
NSString * const SEKeyFlutterEventNameCustom                             = @"_custom_event";


NSString * const SEKeyFlutterKeyCustomProperties                         = @"_customProperties";
NSString * const SEKeyFlutterKeyCustomEventName                          = @"_customEventName";


@interface SEWrapperManager : NSObject

@property (nonatomic,copy)NSString *sub_lib_version;
@property (nonatomic,copy)NSString *sdk_type;

+ (SEWrapperManager *)sharedInstance;

@end

@implementation SEWrapperManager

+ (SEWrapperManager *)sharedInstance {
    static SEWrapperManager * instance = nil;
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        instance = [[self alloc] init];
    });
    return instance;
}

- (instancetype)init
{
    self = [super init];
    if (self) {
        _sdk_type = @"unity";
    }
    return self;
}

@end


id seTrimValue(id __nullable value){
    
    if (!value || [value isEqual:[NSNull null]]) {
        return nil;
    }

    return value;
}

static SEIAPEventAttribute *buildIAPAttribute(const char *IAPAttribute) {
    
    NSString *jsonString = [NSString stringWithUTF8String:IAPAttribute];
    NSData *data = [jsonString dataUsingEncoding:NSUTF8StringEncoding];

    NSError *error = nil;
    NSDictionary *dict = [NSJSONSerialization JSONObjectWithData:data options:0 error:&error];
    if (error) {
        NSString *msg = [NSString stringWithFormat:@"%@ is not an invalid JSON data", jsonString];
        NSLog(@"trackIAPWithAttributes, error :%@",msg);
        return nil;
    }
    
    NSString *productID     = [dict objectForKey:SEIAPEventProductID];
    NSString *productName   = [dict objectForKey:SEIAPEventProductName];
    NSString *orderId       = [dict objectForKey:SEIAPEventOrderID];
    NSString *currencyType  = [dict objectForKey:SEIAPEventCurrency];
    NSString *payType       = [dict objectForKey:SEIAPEventPayType];
    NSString *failReason    = [dict objectForKey:SEIAPEventFailReason];
    
    NSNumber *payStatus     = [dict objectForKey:SEIAPEventPaystatus];
    NSNumber *productCount  = [dict objectForKey:SEIAPEventProductCount];
    NSNumber *payAmount     = [dict objectForKey:SEIAPEventProductPayAmount];
    NSDictionary *customProperties = [dict objectForKey:@"_customProperties"];

    SEIAPEventAttribute *attribute = [[SEIAPEventAttribute alloc] init];
    attribute.productID = seTrimValue(productID);
    attribute.productName = seTrimValue(productName);
    attribute.orderId = seTrimValue(orderId);
    attribute.currencyType = seTrimValue(currencyType);
    attribute.payType = seTrimValue(payType);
    attribute.payStatus = (SolarEngineIAPStatus)[seTrimValue(payStatus) integerValue];
    attribute.failReason = seTrimValue(failReason);
    attribute.payAmount = [seTrimValue(payAmount) doubleValue];
    attribute.productCount = [seTrimValue(productCount) integerValue];
    attribute.customProperties = seTrimValue(customProperties);

    return attribute;

}

static SEAdImpressionEventAttribute *buildAdImpressionAttribute(const char *adImpressionAttribute) {
    NSString *jsonString = [NSString stringWithUTF8String:adImpressionAttribute];
    NSData *data = [jsonString dataUsingEncoding:NSUTF8StringEncoding];
    NSError *error = nil;
    NSDictionary *dict = [NSJSONSerialization JSONObjectWithData:data options:0 error:&error];
    
    if (error) {
        NSString *msg = [NSString stringWithFormat:@"%@ is not an invalid JSON data", jsonString];
        NSLog(@"trackAdImpressionWithAttributes, error :%@", msg);
        return nil;
    }

    NSString *adNetworkPlatform = [dict objectForKey:SEAdImpressionPropertyAdPlatform];
    NSString *adNetworkAppID = [dict objectForKey:SEAdImpressionPropertyAppID];
    NSString *adNetworkPlacementID = [dict objectForKey:SEAdImpressionPropertyPlacementID];
    NSString *currency = [dict objectForKey:SEAdImpressionPropertyCurrency];
    
    NSNumber *adType = [dict objectForKey:SEAdImpressionPropertyAdType];
    NSNumber *ecpm = [dict objectForKey:SEAdImpressionPropertyEcpm];
    NSString *mediationPlatform = [dict objectForKey:SEAdImpressionPropertyMediationPlatform];
    NSNumber *rendered = [dict objectForKey:SEAdImpressionPropertyRendered];
    NSDictionary *customProperties = [dict objectForKey:@"_customProperties"];

    SEAdImpressionEventAttribute *attribute = [[SEAdImpressionEventAttribute alloc] init];
    attribute.adType = (SolarEngineAdType)[seTrimValue(adType) integerValue];
    attribute.adNetworkPlatform = seTrimValue(adNetworkPlatform);
    attribute.adNetworkAppID = seTrimValue(adNetworkAppID);
    attribute.adNetworkPlacementID = seTrimValue(adNetworkPlacementID);
    attribute.currency = seTrimValue(currency);
    attribute.mediationPlatform = seTrimValue(mediationPlatform);
    attribute.ecpm = [seTrimValue(ecpm) doubleValue];
    attribute.rendered = [seTrimValue(rendered) boolValue];
    attribute.customProperties = seTrimValue(customProperties);
    return attribute;

}


static SEAdClickEventAttribute *buildAdClickAttribute(const char *adClickAttribute) {
    
    NSString *jsonString = [NSString stringWithUTF8String:adClickAttribute];
    NSData *data = [jsonString dataUsingEncoding:NSUTF8StringEncoding];
    NSError *error = nil;
    NSDictionary *dict = [NSJSONSerialization JSONObjectWithData:data options:0 error:&error];
    
    if (error) {
        NSString *msg = [NSString stringWithFormat:@"%@ is not an invalid JSON data", jsonString];
        NSLog(@"trackAdClickWithAttributes, error :%@", msg);
        return nil;
    }

    NSString *adNetworkPlatform = [dict objectForKey:SEAdImpressionPropertyAdPlatform];
    NSNumber *adType = [dict objectForKey:SEAdImpressionPropertyAdType];
    NSString *adNetworkPlacementID = [dict objectForKey:SEAdImpressionPropertyPlacementID];
    NSString *mediationPlatform = [dict objectForKey:SEAdImpressionPropertyMediationPlatform];
    NSDictionary *customProperties = [dict objectForKey:@"_customProperties"];
    
    SEAdClickEventAttribute *attribute = [[SEAdClickEventAttribute alloc] init];
    attribute.adType = (SolarEngineAdType)[seTrimValue(adType) integerValue];
    attribute.adNetworkPlatform = seTrimValue(adNetworkPlatform);
    attribute.adNetworkPlacementID = seTrimValue(adNetworkPlacementID);
    attribute.mediationPlatform = seTrimValue(mediationPlatform);
    attribute.customProperties = seTrimValue(customProperties);
    
    return attribute;
}

static SERegisterEventAttribute *buildRegisterAttribute(const char *registerAttribute) {
    
    NSString *jsonString = [NSString stringWithUTF8String:registerAttribute];
    NSData *data = [jsonString dataUsingEncoding:NSUTF8StringEncoding];
    NSError *error = nil;
    NSDictionary *dict = [NSJSONSerialization JSONObjectWithData:data options:0 error:&error];
    
    if (error) {
        NSString *msg = [NSString stringWithFormat:@"%@ is not an invalid JSON data", jsonString];
        NSLog(@"trackRegisterWithAttributes, error :%@", msg);
        return nil;
    }

    NSString *type = [dict objectForKey:SERegisterPropertyType];
    NSString *status = [dict objectForKey:SERegisterPropertyStatus];
    NSDictionary *customProperties = [dict objectForKey:@"_customProperties"];
    SERegisterEventAttribute *attribute = [[SERegisterEventAttribute alloc] init];
    attribute.registerType = seTrimValue(type);
    attribute.registerStatus = seTrimValue(status);
    attribute.customProperties = seTrimValue(customProperties);
    
    return attribute;

}
static SELoginEventAttribute *buildLoginAttribute(const char *loginAttribute) {
    
    NSString *jsonString = [NSString stringWithUTF8String:loginAttribute];
    NSData *data = [jsonString dataUsingEncoding:NSUTF8StringEncoding];
    NSError *error = nil;
    NSDictionary *dict = [NSJSONSerialization JSONObjectWithData:data options:0 error:&error];
    
    if (error) {
        NSString *msg = [NSString stringWithFormat:@"%@ is not an invalid JSON data", jsonString];
        NSLog(@"trackLoginWithAttributes, error :%@", msg);
        return nil;
    }

    NSString *type = [dict objectForKey:SELoginPropertyType];
    NSString *status = [dict objectForKey:SELoginPropertyStatus];
    NSDictionary *customProperties = [dict objectForKey:@"_customProperties"];
    
    SELoginEventAttribute *attribute = [[SELoginEventAttribute alloc] init];
    attribute.loginType = seTrimValue(type);
    attribute.loginStatus = seTrimValue(status);
    attribute.customProperties = seTrimValue(customProperties);

    return attribute;
}
static SEOrderEventAttribute *buildOrderAttribute(const char *orderAttribute) {
    
    NSString *jsonString = [NSString stringWithUTF8String:orderAttribute];
    NSData *data = [jsonString dataUsingEncoding:NSUTF8StringEncoding];
    NSError *error = nil;
    NSDictionary *dict = [NSJSONSerialization JSONObjectWithData:data options:0 error:&error];
    
    if (error) {
        NSString *msg = [NSString stringWithFormat:@"%@ is not an invalid JSON data", jsonString];
        NSLog(@"trackOrderWithAttributes, error :%@", msg);
        return nil;
    }

    NSString *orderID = [dict objectForKey:SEOrderPropertyID];
    NSNumber *payAmount = [dict objectForKey:SEOrderPropertyPayAmount];
    NSString *currencyType = [dict objectForKey:SEOrderPropertyCurrencyType];
    NSString *payType = [dict objectForKey:SEOrderPropertyPayType];
    NSString *status = [dict objectForKey:SEOrderPropertyStatus];
    NSDictionary *customProperties = [dict objectForKey:@"_customProperties"];
    
    SEOrderEventAttribute *attribute = [[SEOrderEventAttribute alloc] init];
    attribute.orderID = seTrimValue(orderID);
    attribute.payAmount = [seTrimValue(payAmount) doubleValue];
    attribute.currencyType = seTrimValue(currencyType);
    attribute.payType = seTrimValue(payType);
    attribute.status = seTrimValue(status);
    attribute.customProperties = seTrimValue(customProperties);

    return attribute;
}
static SEAppAttrEventAttribute *buildAppAttrAttribute(const char *AppAttrAttribute) {
    
    NSString *jsonString = [NSString stringWithUTF8String:AppAttrAttribute];
    NSData *data = [jsonString dataUsingEncoding:NSUTF8StringEncoding];
    NSError *error = nil;
    NSDictionary *dict = [NSJSONSerialization JSONObjectWithData:data options:0 error:&error];
    
    if (error) {
        NSString *msg = [NSString stringWithFormat:@"%@ is not an invalid JSON data", jsonString];
        NSLog(@"trackOrderWithAttributes, error :%@", msg);
        return nil;
    }

    NSString *adNetwork         = [dict objectForKey:SEAppAttrPropertyAdNetwork];
    NSString *subChannel        = [dict objectForKey:SEAppAttrPropertySubChannel];
    NSString *adAccountID       = [dict objectForKey:SEAppAttrPropertyAdAccountID];
    NSString *adAccountName     = [dict objectForKey:SEAppAttrPropertyAdAccountName];
    NSString *adCampaignID      = [dict objectForKey:SEAppAttrPropertyAdCampaignID];
    NSString *adCampaignName    = [dict objectForKey:SEAppAttrPropertyAdCampaignName];
    NSString *adOfferID         = [dict objectForKey:SEAppAttrPropertyAdOfferID];
    NSString *adOfferName       = [dict objectForKey:SEAppAttrPropertyAdOfferName];
    NSString *adCreativeID      = [dict objectForKey:SEAppAttrPropertyAdCreativeID];
    NSString *adCreativeName    = [dict objectForKey:SEAppAttrPropertyAdCreativeName];
    NSString *attributionPlatform = [dict objectForKey:SEAppAttrPropertyAttributionPlatform];

    NSDictionary *customProperties  = [dict objectForKey:@"_customProperties"];

    SEAppAttrEventAttribute *appAttr = [[SEAppAttrEventAttribute alloc] init];
    appAttr.adNetwork = seTrimValue(adNetwork);
    appAttr.subChannel = seTrimValue(subChannel);
    appAttr.adAccountID = seTrimValue(adAccountID);
    appAttr.adAccountName = seTrimValue(adAccountName);
    appAttr.adCampaignID = seTrimValue(adCampaignID);
    appAttr.adCampaignName = seTrimValue(adCampaignName);
    appAttr.adOfferID = seTrimValue(adOfferID);
    appAttr.adOfferName = seTrimValue(adOfferName);
    appAttr.adCreativeID = seTrimValue(adCreativeID);
    appAttr.adCreativeName = seTrimValue(adCreativeName);
    appAttr.attributionPlatform = seTrimValue(attributionPlatform);

    appAttr.customProperties = seTrimValue(customProperties);
    
    return appAttr;
}

static SECustomEventAttribute *buildCustomEventAttribute(const char *customAttribute) {
    
    NSString *jsonString = [NSString stringWithUTF8String:customAttribute];
    NSData *data = [jsonString dataUsingEncoding:NSUTF8StringEncoding];
    NSError *error = nil;
    NSDictionary *dict = [NSJSONSerialization JSONObjectWithData:data options:0 error:&error];
    
    if (error) {
        NSString *msg = [NSString stringWithFormat:@"%@ is not an invalid JSON data", jsonString];
        NSLog(@"trackCustomWithAttributes, error :%@", msg);
        return nil;
    }

    NSString *eventName             = [dict objectForKey:@"_custom_event_name"];
    NSDictionary *customProperties  = [dict objectForKey:@"_customProperties"];
    NSDictionary *preProperties     = [dict objectForKey:@"_pre_properties"];
    
    SECustomEventAttribute *attribute = [[SECustomEventAttribute alloc] init];
    attribute.eventName = seTrimValue(eventName);
    attribute.customProperties = seTrimValue(customProperties);
    attribute.presetProperties = seTrimValue(preProperties);
    
    return attribute;
}

extern "C" {

char* convertNSStringToCString(const NSString* nsString)
{
    if (nsString == NULL)
        return NULL;

    const char* nsStringUtf8 = [nsString UTF8String];
    //create a null terminated C string on the heap so that our string's memory isn't wiped out right after method's return
    char* cString = (char*)malloc(strlen(nsStringUtf8) + 1);
    strcpy(cString, nsStringUtf8);

    return cString;
}

void __iOSSolarEngineSDKSetPresetEvent(const char *presetEventName, const char *properties)
{
    NSDictionary *dict = nil;
    if (properties != NULL) {
        NSString *_properties = [NSString stringWithUTF8String:properties];
        if (![_properties isEqualToString:@"null"]) {
            NSData *data = [_properties dataUsingEncoding:NSUTF8StringEncoding];
            NSError *error = nil;
            dict = [NSJSONSerialization JSONObjectWithData:data options:0 error:&error];
            if (error) {
                NSString *msg = [NSString stringWithFormat:@"%@ is not an invalid JSON data",_properties];
                NSLog(@"setPresetEvent, error :%@",msg);
                return;
            }
        }
    }
    
    NSString *_presetEventName = [NSString stringWithUTF8String:presetEventName];
    if ([_presetEventName isEqualToString:@"SEPresetEventTypeAppInstall"]) {
        [[SolarEngineSDK sharedInstance] setPresetEvent:SEPresetEventTypeAppInstall withProperties:dict];
    } else if ([_presetEventName isEqualToString:@"SEPresetEventTypeAppStart"]) {
        [[SolarEngineSDK sharedInstance] setPresetEvent:SEPresetEventTypeAppStart withProperties:dict];
    } else if ([_presetEventName isEqualToString:@"SEPresetEventTypeAppEnd"]) {
        [[SolarEngineSDK sharedInstance] setPresetEvent:SEPresetEventTypeAppEnd withProperties:dict];
    } else if ([_presetEventName isEqualToString:@"SEPresetEventTypeAppAll"]) {
        [[SolarEngineSDK sharedInstance] setPresetEvent:SEPresetEventTypeAppAll withProperties:dict];
    }
}

void __iOSSolarEngineSDKPreInit(const char * appKey, const char * SEUserId) {
 
    NSLog(@"__iOSSolarEngineSDKPreInit called");
    NSString *_appKey = [NSString stringWithUTF8String:appKey];
    
    [[SolarEngineSDK sharedInstance] preInitWithAppKey:_appKey];

}

void __iOSSolarEngineSDKInit(const char * appKey, const char * SEUserId, const char *seConfig, const char *rcConfig) {
    
    NSLog(@"__iOSSolarEngineSDKInit called");
    NSString *_appKey = [NSString stringWithUTF8String:appKey];
    
    NSString *_seConfig = [NSString stringWithUTF8String:seConfig];
    NSData *data = [_seConfig dataUsingEncoding:NSUTF8StringEncoding];
    NSError *error = nil;
    NSDictionary *seDict = [NSJSONSerialization JSONObjectWithData:data options:0 error:&error];
    if (error) {
        NSString *msg = [NSString stringWithFormat:@"%@ is not an invalid JSON data",_seConfig];
        NSLog(@"__iOSSolarEngineSDKInit, seConfig error :%@",msg);
        return;
    }
    
    SEConfig *config = [[SEConfig alloc] init];
    config.isDebugModel = [seDict[@"isDebugModel"] boolValue];
    config.logEnabled = [seDict[@"logEnabled"] boolValue];
    config.setCoppaEnabled = [seDict[@"isCoppaEnabled"] boolValue];
    config.setKidsAppEnabled = [seDict[@"isKidsAppEnabled"] boolValue];
    config.enable2GReporting = [seDict[@"isEnable2GReporting"] boolValue];
    config.isGDPRArea = [seDict[@"isGDPRArea"] boolValue];
    
    if (seDict[@"enableIPV6"]) {
        config.enableIPV6 = [seDict[@"enableIPV6"] boolValue];
    }
    config.attAuthorizationWaitingInterval = [seDict[@"attAuthorizationWaitingInterval"] intValue];
#if  __has_include(<SolarEngineSDK/SESDKForCN.h>)
        if (seDict[@"caid"]) {
            config.caid = seDict[@"caid"];
        }
#endif
    
#if  __has_include(<SolarEngineSDK/SESDKForUS.h>)
        if (seDict[@"odmInfoEnable"]) {
            config.enableODMInfo = [seDict[@"odmInfoEnable"] boolValue];
        }
#endif


    config.enableDelayDeeplink = [seDict[@"delayDeeplinkEnable"] boolValue];

    NSString *sub_lib_version = seDict[@"sub_lib_version"];
    if ([sub_lib_version isKindOfClass:[NSString class]]) {
        [SEWrapperManager sharedInstance].sub_lib_version = sub_lib_version;
    }
    
    NSString *sdk_type = seDict[@"sdk_type"];
    if ([sdk_type isKindOfClass:[NSString class]]) {
        [SEWrapperManager sharedInstance].sdk_type = sdk_type;
    }
    
    NSDictionary *rcDict = nil;
    if (rcConfig != NULL) {
        NSString *_rcConfig = [NSString stringWithUTF8String:rcConfig];
        NSData *data = [_rcConfig dataUsingEncoding:NSUTF8StringEncoding];
        NSError *error = nil;
        rcDict = [NSJSONSerialization JSONObjectWithData:data options:0 error:&error];
        if (error) {
            NSString *msg = [NSString stringWithFormat:@"%@ is not an invalid JSON data",_rcConfig];
            NSLog(@"__iOSSolarEngineSDKInit, rcConfig error :%@",msg);
            return;
        }
        
        SERemoteConfig *remoteConfig = [[SERemoteConfig alloc] init];
        remoteConfig.enable = [rcDict[@"enable"] boolValue];
        remoteConfig.logEnabled = config.logEnabled;
        
        remoteConfig.mergeType = (SERCMergeType)[rcDict[@"mergeType"] integerValue];

        remoteConfig.customIDProperties = rcDict[@"customIDProperties"];
        remoteConfig.customIDUserProperties = rcDict[@"customIDUserProperties"];
        remoteConfig.customIDEventProperties = rcDict[@"customIDEventProperties"];
        
        config.remoteConfig = remoteConfig;
    }
    
    NSDictionary *customDomainDict = seDict[@"customDomain"];
    if ([customDomainDict isKindOfClass:[NSDictionary class]]) {
        
        SECustomDomain *customDomain = [[SECustomDomain alloc] init];
        customDomain.enable             = [customDomainDict[@"enable"] boolValue];
        customDomain.receiverDomain     = customDomainDict[@"receiverDomain"];
        customDomain.ruleDomain         = customDomainDict[@"ruleDomain"];
        customDomain.ruleTcpHost        = customDomainDict[@"tcpRuleHost"];
        customDomain.receiverTcpHost    = customDomainDict[@"tcpReceiverHost"];
        customDomain.gatewayTcpHost     = customDomainDict[@"tcpGatewayHost"];
        config.customDomain = customDomain;
    }
    
    [[SolarEngineSDK sharedInstance] startWithAppKey:_appKey config:config];
}

void __iOSSESDKSetInitCompletedCallback(SEBridgeInitCallback callback) {
    
    [[SolarEngineSDK sharedInstance] setInitCompletedCallback:^(int code) {
        if (callback) {
            callback(code);
        }
    }];
}

void __iOSSESDKRequestTrackingAuthorizationWithCompletionHandler(SEBridgeInitCallback callback) {

    [[SolarEngineSDK sharedInstance] requestTrackingAuthorizationWithCompletionHandler:^(NSUInteger status) {
                
        if (callback) {
            callback((int)status);
        }
    }];
}


void __iOSSESDKSetAttributionDataCallback(SEBridgeCallback callback) {
    
    [[SolarEngineSDK sharedInstance] setAttributionCallback:^(int code, NSDictionary * _Nullable attribution) {
            NSString *attData = nil;
            if ([attribution isKindOfClass:NSDictionary.class]) {
                NSData *data = [NSJSONSerialization dataWithJSONObject:attribution options:0 error:nil];
                if (data) {
                    attData = [[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding];
                }
            }
        
        if (callback) {
            callback(code,convertNSStringToCString(attData));
        }
    }];
}

char* __iOSSolarEngineSDKGetAttribution(void)
{
    NSDictionary *attribution = [[SolarEngineSDK sharedInstance] getAttributionData];
    if (![attribution isKindOfClass:NSDictionary.class]) {
        return nil;
    }
    NSData *data = [NSJSONSerialization dataWithJSONObject:attribution options:0 error:nil];
    if (data == nil) {
        return nil;
    }
    NSString *dataString = [[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding];
    return convertNSStringToCString(dataString);
}


void __iOSSolarEngineSDKSetGDPRArea(bool isGDPRArea) {
    [[SolarEngineSDK sharedInstance] setGDPRArea:isGDPRArea];
}

void __iOSSolarEngineSDKTrack(const char *eventName, const char *attributes)
{
    NSString *_eventName = [NSString stringWithUTF8String:eventName];
    NSString *_attributes = [NSString stringWithUTF8String:attributes];

    NSData *data = [_attributes dataUsingEncoding:NSUTF8StringEncoding];
    NSError *error = nil;
    NSDictionary *dict = nil;
    
    if (data){
        dict = [NSJSONSerialization JSONObjectWithData:data options:0 error:&error];
        if (error) {
            NSString *msg = [NSString stringWithFormat:@"%@ is not an invalid JSON data",_attributes];
            NSLog(@"track, error :%@",msg);
            return;
        }
    }

    [[SolarEngineSDK sharedInstance] track:_eventName withProperties:dict];
}

void __iOSSolarEngineSDKTrackCustomEventWithPreAttributes(const char *eventName, const char *customAttributes, const char *preAttributes)
{
    NSString *_eventName = [NSString stringWithUTF8String:eventName];
    NSString *_customAttributes = [NSString stringWithUTF8String:customAttributes];
    NSString *_preAttributes = [NSString stringWithUTF8String:preAttributes];

    NSData *customData = [_customAttributes dataUsingEncoding:NSUTF8StringEncoding];
    NSError *error = nil;
    NSDictionary *customProperties = nil;
    if (customData) {
        customProperties = [NSJSONSerialization JSONObjectWithData:customData options:0 error:&error];
        if (error) {
            NSString *msg = [NSString stringWithFormat:@"%@ is not an invalid JSON data",_customAttributes];
            NSLog(@"track, error :%@",msg);
            return;
        }
    }
    
    NSData *preData = [_preAttributes dataUsingEncoding:NSUTF8StringEncoding];
    NSError *preError = nil;
    NSDictionary *preProperties = nil;
    if (preData) {
        preProperties = [NSJSONSerialization JSONObjectWithData:preData options:0 error:&error];
        if (preError) {
            NSString *msg = [NSString stringWithFormat:@"%@ is not an invalid JSON data",_preAttributes];
            NSLog(@"track, error :%@",msg);
            return;
        }
    }
    [[SolarEngineSDK sharedInstance] track:_eventName withCustomProperties:customProperties withPresetProperties:preProperties];
}

void __iOSSolarEngineSDKTrackAppReEngagement(const char *attributes)
{       
    NSLog(@"__iOSSolarEngineSDKTrackAppReEngagement called");
    NSDictionary *customProperties = nil;
    if (attributes != NULL) {
        NSString *jsonString = [NSString stringWithUTF8String:attributes];
        NSData *data = [jsonString dataUsingEncoding:NSUTF8StringEncoding];
        NSError *error = nil;
        customProperties = [NSJSONSerialization JSONObjectWithData:data options:0 error:&error];
        if (error) {
            NSString *msg = [NSString stringWithFormat:@"%@ is not an invalid JSON data", jsonString];
            NSLog(@"trackAppReEngagement, error :%@",msg);
            customProperties = nil;
        }        
    }
    [[SolarEngineSDK sharedInstance] trackAppReEngagement:customProperties];
}

void __iOSSolarEngineSDKTrackFirstEventWithAttributes(const char *firstEventAttribute) {
    
    NSLog(@"__iOSSolarEngineSDKTrackFirstEventWithAttributes called");

    NSString *jsonString = [NSString stringWithUTF8String:firstEventAttribute];
    NSData *data = [jsonString dataUsingEncoding:NSUTF8StringEncoding];

    NSError *error = nil;
    NSDictionary *dict = [NSJSONSerialization JSONObjectWithData:data options:0 error:&error];
    if (error) {
        NSString *msg = [NSString stringWithFormat:@"%@ is not an invalid JSON data", jsonString];
        NSLog(@"TrackFirstEventWithAttributes, error :%@",msg);
        return;
    }
    
    NSString *eventType = [dict objectForKey:SEKeyFlutterEventType];
    NSString *first_event_check_id  =  [dict objectForKey:@"_first_event_check_id"];

    SEEventBaseAttribute *attribute = nil;
    
    if ([eventType isEqualToString:SEKeyFlutterEventNameIAP]) {
        attribute = buildIAPAttribute(firstEventAttribute);
    } else if ([eventType isEqualToString:SEKeyFlutterEventNameAdImpresstion]) {
        attribute = buildAdImpressionAttribute(firstEventAttribute);
    } else if ([eventType isEqualToString:SEKeyFlutterEventNameAdClick]) {
        attribute = buildAdClickAttribute(firstEventAttribute);
    } else if ([eventType isEqualToString:SEKeyFlutterEventNameAppAttr]) {
        attribute = buildAppAttrAttribute(firstEventAttribute);
    } else if ([eventType isEqualToString:SEKeyFlutterEventNameRegister]) {
        attribute = buildRegisterAttribute(firstEventAttribute);
    } else if ([eventType isEqualToString:SEKeyFlutterEventNameLogin]) {
        attribute = buildLoginAttribute(firstEventAttribute);
    } else if ([eventType isEqualToString:SEKeyFlutterEventNameOrder]) {
        attribute = buildOrderAttribute(firstEventAttribute);
    } else if ([eventType isEqualToString:SEKeyFlutterEventNameCustom]) {
        attribute = buildCustomEventAttribute(firstEventAttribute);
    }

    if (attribute) {
        if ([first_event_check_id isKindOfClass:NSString.class]) {
            attribute.firstCheckId = first_event_check_id;
        }
        [[SolarEngineSDK sharedInstance] trackFirstEvent:attribute];
    } else {
        NSLog(@"TrackFirstEventWithAttributes attribute is nil");
    }
}

void __iOSSolarEngineSDKTrackIAPWithAttributes(const char *IAPAttribute)
{
    NSString *jsonString = [NSString stringWithUTF8String:IAPAttribute];
    NSData *data = [jsonString dataUsingEncoding:NSUTF8StringEncoding];

    NSError *error = nil;
    NSDictionary *dict = [NSJSONSerialization JSONObjectWithData:data options:0 error:&error];
    if (error) {
        NSString *msg = [NSString stringWithFormat:@"%@ is not an invalid JSON data", jsonString];
        NSLog(@"trackIAPWithAttributes, error :%@",msg);
        return;
    }

    SEIAPEventAttribute *attribute = buildIAPAttribute(IAPAttribute);
    [[SolarEngineSDK sharedInstance] trackIAPWithAttributes:attribute];
}

void __iOSSolarEngineSDKTrackAdImpressionWithAttributes(const char *adImpressionAttribute)
{
    NSString *jsonString = [NSString stringWithUTF8String:adImpressionAttribute];
    NSData *data = [jsonString dataUsingEncoding:NSUTF8StringEncoding];
    NSError *error = nil;
    NSDictionary *dict = [NSJSONSerialization JSONObjectWithData:data options:0 error:&error];
    
    if (error) {
        NSString *msg = [NSString stringWithFormat:@"%@ is not an invalid JSON data", jsonString];
        NSLog(@"trackAdImpressionWithAttributes, error :%@", msg);
        return;
    }

    SEAdImpressionEventAttribute *attribute = buildAdImpressionAttribute(adImpressionAttribute);
    [[SolarEngineSDK sharedInstance] trackAdImpressionWithAttributes:attribute];
}

void __iOSSolarEngineSDKTrackAdClickWithAttributes(const char *adClickAttribute) {
    NSString *jsonString = [NSString stringWithUTF8String:adClickAttribute];
    NSData *data = [jsonString dataUsingEncoding:NSUTF8StringEncoding];
    NSError *error = nil;
    NSDictionary *dict = [NSJSONSerialization JSONObjectWithData:data options:0 error:&error];
    
    if (error) {
        NSString *msg = [NSString stringWithFormat:@"%@ is not an invalid JSON data", jsonString];
        NSLog(@"trackAdClickWithAttributes, error :%@", msg);
        return;
    }
    
    SEAdClickEventAttribute *attribute = buildAdClickAttribute(adClickAttribute);
    [[SolarEngineSDK sharedInstance] trackAdClickWithAttributes:attribute];
}

void __iOSSolarEngineSDKTrackRegisterWithAttributes(const char *registerAttribute) {
    NSString *jsonString = [NSString stringWithUTF8String:registerAttribute];
    NSData *data = [jsonString dataUsingEncoding:NSUTF8StringEncoding];
    NSError *error = nil;
    NSDictionary *dict = [NSJSONSerialization JSONObjectWithData:data options:0 error:&error];
    
    if (error) {
        NSString *msg = [NSString stringWithFormat:@"%@ is not an invalid JSON data", jsonString];
        NSLog(@"trackRegisterWithAttributes, error :%@", msg);
        return;
    }

    SERegisterEventAttribute *attribute = buildRegisterAttribute(registerAttribute);
    [[SolarEngineSDK sharedInstance] trackRegisterWithAttributes:attribute];
}

void __iOSSolarEngineSDKTrackLoginWithAttributes(const char *loginAttribute) {
    NSString *jsonString = [NSString stringWithUTF8String:loginAttribute];
    NSData *data = [jsonString dataUsingEncoding:NSUTF8StringEncoding];
    NSError *error = nil;
    NSDictionary *dict = [NSJSONSerialization JSONObjectWithData:data options:0 error:&error];
    
    if (error) {
        NSString *msg = [NSString stringWithFormat:@"%@ is not an invalid JSON data", jsonString];
        NSLog(@"trackLoginWithAttributes, error :%@", msg);
        return;
    }

    SELoginEventAttribute *attribute = buildLoginAttribute(loginAttribute);
    [[SolarEngineSDK sharedInstance] trackLoginWithAttributes:attribute];
}

void __iOSSolarEngineSDKTrackOrderWithAttributes(const char *orderAttribute) {
    NSString *jsonString = [NSString stringWithUTF8String:orderAttribute];
    NSData *data = [jsonString dataUsingEncoding:NSUTF8StringEncoding];
    NSError *error = nil;
    NSDictionary *dict = [NSJSONSerialization JSONObjectWithData:data options:0 error:&error];
    
    if (error) {
        NSString *msg = [NSString stringWithFormat:@"%@ is not an invalid JSON data", jsonString];
        NSLog(@"trackOrderWithAttributes, error :%@", msg);
        return;
    }

    SEOrderEventAttribute *attribute = buildOrderAttribute(orderAttribute);
    [[SolarEngineSDK sharedInstance] trackOrderWithAttributes:attribute];
}

void __iOSSolarEngineSDKTrackAppAttrWithAttributes(const char *AppAttrAttribute) {
    NSString *jsonString = [NSString stringWithUTF8String:AppAttrAttribute];
    NSData *data = [jsonString dataUsingEncoding:NSUTF8StringEncoding];
    NSError *error = nil;
    NSDictionary *dict = [NSJSONSerialization JSONObjectWithData:data options:0 error:&error];
    
    if (error) {
        NSString *msg = [NSString stringWithFormat:@"%@ is not an invalid JSON data", jsonString];
        NSLog(@"trackOrderWithAttributes, error :%@", msg);
        return;
    }

    SEAppAttrEventAttribute *appAttr = buildAppAttrAttribute(AppAttrAttribute);
    [[SolarEngineSDK sharedInstance] trackAppAttrWithAttributes:appAttr];
}


void __iOSSolarEngineSDKSetVisitorID(const char *visitorID)
{
    NSString *_visitorID = [NSString stringWithUTF8String:visitorID];
    [[SolarEngineSDK sharedInstance] setVisitorID:_visitorID];
}

char* __iOSSolarEngineSDKVisitorID(void)
{
    NSString *visitorID = [[SolarEngineSDK sharedInstance] visitorID];
    return convertNSStringToCString(visitorID);
}

char* __iOSSolarEngineSDKGetDistinctId(void) {
    
    NSString *visitorID = [[SolarEngineSDK sharedInstance] getDistinctId];
    return convertNSStringToCString(visitorID);
}

char* __iOSSolarEngineSDKGetPresetProperties(void){
    NSDictionary *presetProperties = [[SolarEngineSDK sharedInstance] getPresetProperties];
    
    NSData *data = [NSJSONSerialization dataWithJSONObject:presetProperties options:0 error:nil];
    if (data == nil) {
        return NULL;
    }
    NSString *dataString = [[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding];
    
    return convertNSStringToCString(dataString);
}

void __iOSSolarEngineSDKLoginWithAccountID(const char *accountID)
{
    NSString *_accountID = [NSString stringWithUTF8String:accountID];
    [[SolarEngineSDK sharedInstance] loginWithAccountID:_accountID];
}

char* __iOSSolarEngineSDKAccountID(void)
{
    NSString *accountID = [[SolarEngineSDK sharedInstance] accountID];
    return convertNSStringToCString(accountID);
}

void __iOSSolarEngineSDKLogout(void)
{
    [[SolarEngineSDK sharedInstance] logout];
}

void __iOSSolarEngineSDKSetSuperProperties(const char *properties)
{
    NSString *_properties = [NSString stringWithUTF8String:properties];
    NSData *data = [_properties dataUsingEncoding:NSUTF8StringEncoding];
    NSError *error = nil;
    NSDictionary *dict = [NSJSONSerialization JSONObjectWithData:data options:0 error:&error];
    if (error) {
        NSString *msg = [NSString stringWithFormat:@"%@ is not an invalid JSON data",_properties];
        NSLog(@"setSuperProperties, error :%@",msg);
        return;
    }
    if (![dict isKindOfClass:NSDictionary.class]) {
        NSString *msg = [NSString stringWithFormat:@"%@ is not an dict",_properties];
        NSLog(@"setSuperProperties, error :%@",msg);
        return;
    }
    [[SolarEngineSDK sharedInstance] setSuperProperties:dict];
}

void __iOSSolarEngineSDKUnsetSuperProperty(const char *property)
{
    NSString *_property = [NSString stringWithUTF8String:property];
    [[SolarEngineSDK sharedInstance] unsetSuperProperty:_property];
}

void __iOSSolarEngineSDKClearSuperProperties(void)
{
    [[SolarEngineSDK sharedInstance] clearSuperProperties];
}

void __iOSSolarEngineSDKReportEventImmediately(void)
{
    [[SolarEngineSDK sharedInstance] reportEventImmediately];
}

void __iOSSolarEngineSDKUserInit(const char *properties)
{
    NSString *_properties = [NSString stringWithUTF8String:properties];

    NSData *data = [_properties dataUsingEncoding:NSUTF8StringEncoding];

    NSError *error = nil;
    NSDictionary *dict = [NSJSONSerialization JSONObjectWithData:data options:0 error:&error];

    if (error) {
        NSString *msg = [NSString stringWithFormat:@"%@ is not an invalid JSON data",_properties];
        NSLog(@"userInit, error :%@",msg);
        return;
    }
    if (![dict isKindOfClass:NSDictionary.class]) {
        NSString *msg = [NSString stringWithFormat:@"%@ is not an dict",_properties];
        NSLog(@"userInit, error :%@",msg);
        return;
    }
    
    [[SolarEngineSDK sharedInstance] userInit:dict];
}

void __iOSSolarEngineSDKUserUpdate(const char *properties)
{
    NSString *_properties = [NSString stringWithUTF8String:properties];
    NSData *data = [_properties dataUsingEncoding:NSUTF8StringEncoding];

    NSError *error = nil;
    NSDictionary *dict = [NSJSONSerialization JSONObjectWithData:data options:0 error:&error];

    if (error) {
        NSString *msg = [NSString stringWithFormat:@"%@ is not an invalid JSON data",_properties];
        NSLog(@"userUpdate, error :%@", msg);
        return;
    }
    if (![dict isKindOfClass:NSDictionary.class]) {
        NSString *msg = [NSString stringWithFormat:@"%@ is not an dict",_properties];
        NSLog(@"userUpdate, error :%@", msg);
        return;
    }
    [[SolarEngineSDK sharedInstance] userUpdate:dict];
}

void __iOSSolarEngineSDKUserAdd(const char *properties)
{
    NSString *_properties = [NSString stringWithUTF8String:properties];

    NSData *data = [_properties dataUsingEncoding:NSUTF8StringEncoding];

    NSError *error = nil;
    NSDictionary *dict = [NSJSONSerialization JSONObjectWithData:data options:0 error:&error];

    if (error) {
        NSString *msg = [NSString stringWithFormat:@"%@ is not an invalid JSON data",_properties];
        NSLog(@"userAdd, error :%@",msg);
        return;
    }
    if (![dict isKindOfClass:NSDictionary.class]) {
        NSString *msg = [NSString stringWithFormat:@"%@ is not an dict",_properties];
        NSLog(@"userAdd, error :%@",msg);
        return;
    }
    [[SolarEngineSDK sharedInstance] userAdd:dict];
}

void __iOSSolarEngineSDKUserUnset(const char *keys)
{
    NSString *_keys = [NSString stringWithUTF8String:keys];

    NSData *data = [_keys dataUsingEncoding:NSUTF8StringEncoding];

    NSError *error = nil;
    NSArray *array = [NSJSONSerialization JSONObjectWithData:data options:0 error:&error];

    if (error) {
        NSString *msg = [NSString stringWithFormat:@"%@ is not an invalid JSON data",_keys];
        NSLog(@"userUnset, error :%@", msg);
        return;
    }
    
    if (![array isKindOfClass:NSArray.class]) {
        NSString *msg = [NSString stringWithFormat:@"%@ is not an array",_keys];
        NSLog(@"userUnset, error :%@", msg);
        return;
    }
    
    [[SolarEngineSDK sharedInstance] userUnset:array];
}

void __iOSSolarEngineSDKUserAppend(const char *properties)
{
    NSString *_properties = [NSString stringWithUTF8String:properties];
    NSData *data = [_properties dataUsingEncoding:NSUTF8StringEncoding];

    NSError *error = nil;
    NSDictionary *dict = [NSJSONSerialization JSONObjectWithData:data options:0 error:&error];

    if (error) {
        NSString *msg = [NSString stringWithFormat:@"%@ is not an invalid JSON data",_properties];
        NSLog(@"userAppend, error :%@", msg);
        return;
    }
    if (![dict isKindOfClass:NSDictionary.class]) {
        NSString *msg = [NSString stringWithFormat:@"%@ is not an dict",_properties];
        NSLog(@"userAppend, error :%@", msg);
        return;
    }
    [[SolarEngineSDK sharedInstance] userAppend:dict];
}

void __iOSSolarEngineSDKUserDelete(int deleteType)
{
    
    [[SolarEngineSDK sharedInstance] userDelete:deleteType];
}

void __iOSSolarEngineSDKEventStart(const char *eventName)
{
    NSString *_eventName = [NSString stringWithUTF8String:eventName];
    [[SolarEngineSDK sharedInstance] eventStart:_eventName];
}

void __iOSSolarEngineSDKEventFinish(const char *eventJSONStr)
{
    NSString *_eventJSONStr = [NSString stringWithUTF8String:eventJSONStr];
    NSData *data = [_eventJSONStr dataUsingEncoding:NSUTF8StringEncoding];

    NSError *error = nil;
    NSDictionary *dict = [NSJSONSerialization JSONObjectWithData:data options:0 error:&error];

    if (error) {
        NSString *msg = [NSString stringWithFormat:@"%@ is not an invalid JSON data",_eventJSONStr];
        NSLog(@"eventFinish, error :%@",msg);
        return;
    }
    
    if (![dict isKindOfClass:NSDictionary.class]) {
        NSString *msg = [NSString stringWithFormat:@"%@ is not an dict",_eventJSONStr];
        NSLog(@"eventFinish, error :%@", msg);
        return;
    }
    
    NSString *_eventName = [dict objectForKey:@"eventName"];
    NSDictionary *_attributes = [dict objectForKey:@"attributes"];
    [[SolarEngineSDK sharedInstance] eventFinish:_eventName properties:_attributes];
}

void __iOSSolarEngineSDKEventFinishNew(const char *eventName, const char *properties)
{
    NSString *_eventName = [NSString stringWithUTF8String:eventName];
    NSDictionary *_properties = nil;
    if (properties != NULL){
        NSString *_eventJSONStr = [NSString stringWithUTF8String:properties];
        if (_eventJSONStr && ![_eventJSONStr isEqualToString:@"null"]){
            NSData *data = [_eventJSONStr dataUsingEncoding:NSUTF8StringEncoding];
            
            NSError *error = nil;
            NSDictionary *dict = [NSJSONSerialization JSONObjectWithData:data options:0 error:&error];
            
            if (error) {
                dict = nil;
                NSString *msg = [NSString stringWithFormat:@"%@ is not an invalid JSON data",_eventJSONStr];
                NSLog(@"eventFinish, error :%@",msg);
            }
            
            if (![dict isKindOfClass:NSDictionary.class]) {
                dict = nil;
                NSString *msg = [NSString stringWithFormat:@"%@ is not an dict",_eventJSONStr];
                NSLog(@"eventFinish, error :%@", msg);
            }
            _properties = dict;
        }
    }
    
    [[SolarEngineSDK sharedInstance] eventFinish:_eventName properties:_properties];
}


void __iOSSESDKupdatePostbackConversionValue(int conversionValue, SEBridgeCallback callback) {
    
    [[SolarEngineSDK sharedInstance] updatePostbackConversionValue:conversionValue completionHandler:^(NSError * _Nonnull error) {
        if (callback) {
            callback((int)error.code,convertNSStringToCString(error.description));
        }
    }];
}

void __iOSSESDKupdateConversionValueCoarseValue(int fineValue, const char *  coarseValue, SEBridgeCallback callback) {
    
    NSString *_coarseValue = nil;
    if (coarseValue != NULL) {
        _coarseValue = [NSString stringWithUTF8String:coarseValue];
    }

    [[SolarEngineSDK sharedInstance] updatePostbackConversionValue:fineValue coarseValue:_coarseValue completionHandler:^(NSError * _Nonnull error) {
        if (callback) {
            callback((int)error.code,convertNSStringToCString(error.description));
        }
    }];
}

void __iOSSESDKupdateConversionValueCoarseValueLockWindow(int fineValue, const char *  coarseValue, bool lockWindow, SEBridgeCallback callback) {
    
    NSString *_coarseValue = nil;
    if (coarseValue != NULL) {
        _coarseValue = [NSString stringWithUTF8String:coarseValue];
    }
    [[SolarEngineSDK sharedInstance] updatePostbackConversionValue:fineValue coarseValue:_coarseValue lockWindow:lockWindow completionHandler:^(NSError * _Nonnull error) {
        if (callback) {
            callback((int)error.code,convertNSStringToCString(error.description));
        }
    }];
    
}


void __iOSSolarEngineSDKDeeplinkParseCallback(SEBridgeCallback callback) {
    
    [[SolarEngineSDK sharedInstance] setDeepLinkCallback:^(int code, SEDeeplinkInfo * _Nullable deeplinkInfo) {
                
        NSString *dData = nil;
        if (code == 0){
            
            NSMutableDictionary *deeplinkData = [NSMutableDictionary dictionary];
            
            if (deeplinkInfo.from) {
                [deeplinkData setObject:deeplinkInfo.from forKey:@"from"];
            }
            if (deeplinkInfo.sedpLink) {
                [deeplinkData setObject:deeplinkInfo.sedpLink forKey:@"sedpLink"];
            }
            if (deeplinkInfo.turlId) {
                [deeplinkData setObject:deeplinkInfo.turlId forKey:@"turlId"];
            }
            if (deeplinkInfo.customParams) {
                [deeplinkData setObject:deeplinkInfo.customParams forKey:@"customParams"];
            }
            
            NSData *data = [NSJSONSerialization dataWithJSONObject:deeplinkData options:0 error:nil];
            if (data) {
                dData = [[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding];
            }
            
        }
        
        
        if (callback) {
            callback(code,convertNSStringToCString(dData));
        }
    }];
    
}


void __iOSSolarEngineSDKDelayDeeplinkParseCallback(SEBridgeCallback callback) {
    
    [[SolarEngineSDK sharedInstance] setDelayDeeplinkDeepLinkCallbackWithSuccess:^(SEDelayDeeplinkInfo * _Nullable deeplinkInfo) {
        
        NSString *dData = nil;

        NSMutableDictionary *deeplinkData = [NSMutableDictionary dictionary];
        
        if (deeplinkInfo.sedpUrlscheme) {
            [deeplinkData setObject:deeplinkInfo.sedpUrlscheme forKey:@"sedpUrlscheme"];
        }
        if (deeplinkInfo.sedpLink) {
            [deeplinkData setObject:deeplinkInfo.sedpLink forKey:@"sedpLink"];
        }
        if (deeplinkInfo.turlId) {
            [deeplinkData setObject:deeplinkInfo.turlId forKey:@"turlId"];
        }
        
        NSData *data = [NSJSONSerialization dataWithJSONObject:deeplinkData options:0 error:nil];
        if (data) {
            dData = [[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding];
        }
        
        if (callback) {
            callback(0,convertNSStringToCString(dData));
        }
        
    } fail:^(NSError * _Nullable error) {
        
        if (callback) {
            callback((int)error.code,convertNSStringToCString(nil));
        }

    }];
}


}



