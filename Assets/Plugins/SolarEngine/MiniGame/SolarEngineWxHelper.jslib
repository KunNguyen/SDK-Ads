mergeInto(LibraryManager.library, {

_setDebug:function(){
     try {
    GameGlobal.SDK.setDebug(true)
      } catch (error) {
          console.log("__setDebug: ", error);
      }
 },


  _init:function(tencentAdvertisingGameSDKInitParams){
   try{
          var _tencentAdvertisingGameSDKInitParams = UTF8ToString(tencentAdvertisingGameSDKInitParams);
          var jsonObject = {};
          if (_tencentAdvertisingGameSDKInitParams != "") jsonObject = JSON.parse(_tencentAdvertisingGameSDKInitParams);
                 
          GameGlobal.dnSDK = new SDK({
          
             user_action_set_id: parseInt(jsonObject.user_action_set_id),
           
           secret_key: jsonObject.secret_key.toString(),
           appid: jsonObject.appid.toString(),
           auto_track:jsonObject.tencentSdkIsAutoTrack,
          });
      }catch (error) {
          console.log("__init : ", error);
      }
 },

  _setOpenId: function (openid) {
      try {
           var openidStr = UTF8ToString(openid);
           GameGlobal.dnSDK.setOpenId(openidStr);
       } catch (error) {
           console.log("__setOpenId: ", error);
       }
  },
    _setUnionId: function (unionid) {
      try {
             var unionidStr = UTF8ToString(unionid);
             GameGlobal.dnSDK.setUnionId(unionidStr);
         } catch (error) {
             console.log("__setUnionId : ", error);
         }
    },
  _onPurchase: function (purchaseValue) {
     try {
          
           GameGlobal.dnSDK.onPurchase(purchaseValue);
       } catch (error) {
           console.log("__onPurchase: ", error);
       }
  },
  _onRegister: function () {
      try {
          GameGlobal.dnSDK.onRegister();
      } catch (error) {
          console.log("__onRegister : ", error);
      }
  },
  _onReActive: function (backFlowDay,customProperties) {
    try {
          var _customProperties = UTF8ToString(customProperties);
          var jsonObject = _customProperties? JSON.parse(_customProperties) : {};
          var assignedData = Object.assign({
              backFlowDay: backFlowDay
          }, jsonObject)
      
          GameGlobal.dnSDK.track('RE_ACTIVE', assignedData);
      } catch (error) {
          console.log("__onReActive : ", error);
      }
  },
  _onAddToWishlist: function (type,customProperties) {
   try {
          var typeStr = UTF8ToString(type);
          var _customProperties = UTF8ToString(customProperties);
          var jsonObject = _customProperties? JSON.parse(_customProperties) : {};
          var assignedData = Object.assign({
              type: typeStr
          }, jsonObject)
      
          GameGlobal.dnSDK.track('ADD_TO_WISHLIST', assignedData);
      } catch (error) {
          console.log("__onAddToWishlist : ", error);
      }
  },
  _onShare: function (target,customProperties) {
  try {
         var _target = UTF8ToString(target);
         var _customProperties = UTF8ToString(customProperties);
         var jsonObject = _customProperties? JSON.parse(_customProperties) : {};
         var assignedData = Object.assign({
             target: _target
         }, jsonObject);
 
         GameGlobal.dnSDK.track('SHARE', assignedData);
     } catch (error) {
         console.log("__onShare: ", error);
     }
  },
  _onCreateRole: function (roleName) {
    try {
           var roleNameStr = UTF8ToString(roleName);
           GameGlobal.dnSDK.onCreateRole(roleNameStr);
       } catch (error) {
           console.log("__onCreateRole: ", error);
       }
  },
  _onTutorialFinish: function () {
    try {
          GameGlobal.dnSDK.onTutorialFinish();
      } catch (error) {
          console.log("__onTutorialFinish: ", error);
      }
  },
  _onUpdateLevel: function (level, customProperties) {
    try {
           var _customProperties = UTF8ToString(customProperties);
           var jsonObject = _customProperties? JSON.parse(_customProperties) : {};
           var assignedData = Object.assign({
               level: level,
           }, jsonObject);
   
           GameGlobal.dnSDK.track('UPDATE_LEVEL', assignedData);
       } catch (error) {
           console.log("__onUpdateLevel: ", error);
       }
  },
  _onViewContent: function (item,customProperties) {
 try {
         var _item = UTF8ToString(item);
 
         var _customProperties = UTF8ToString(customProperties);
         var jsonObject = _customProperties? JSON.parse(_customProperties) : {};
         var assignedData = Object.assign({
             item: _item
         }, jsonObject);
 
         GameGlobal.dnSDK.track('VIEW_CONTENT', assignedData);
     } catch (error) {
         console.log("_onViewContent: ", error);
     }
  },
  _onAppStart: function () {
    try {
          GameGlobal.dnSDK.onAppStart();
      } catch (error) {
          console.log("_onAppStart: ", error);
      }
  },
  
});
