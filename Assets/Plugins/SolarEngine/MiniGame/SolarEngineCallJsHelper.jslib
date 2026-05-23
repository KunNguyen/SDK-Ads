mergeInto(LibraryManager.library, {

    _createRequestSign: function (data) {
        try {
            var jsonObject;
            var _data = UTF8ToString(data);
            jsonObject = _data ? JSON.parse(_data) : {};
            var value = window.SolarEngineHelper.createRequestSign(jsonObject);
            var bufferSize = lengthBytesUTF8(value) + 1;
            var buffer = _malloc(bufferSize);
            stringToUTF8(value, buffer, bufferSize);
            return buffer;
        } catch (error) {
            console.log(error);

        }
    },

    _createSign: function (data) {
        try {
            var jsonObject;
            var _data = UTF8ToString(data);
            jsonObject = _data ? JSON.parse(_data) : {};
            //var value = GameGlobal.SolarEngineHelper.createSign(jsonObject);
            
        
             var value = window.SolarEngineHelper.createSign(jsonObject);

            var bufferSize = lengthBytesUTF8(value) + 1;
            var buffer = _malloc(bufferSize);
            stringToUTF8(value, buffer, bufferSize);
            return buffer;
        } catch (error) {
            console.log(error);

        }
    },

})