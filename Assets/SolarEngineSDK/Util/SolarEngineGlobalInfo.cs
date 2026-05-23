using System;
using System.IO;

using UnityEngine;

namespace SolarEngine
{
    public class SolarEngineGlobalInfo
    {
        
        public enum MainLand
        {
            None,
            China=1,
            Non_China=2
        }
        private static MainLand _mainLand;
        
        static string  mainlandpath="Assets/SolarEngine/MainLand";
        static string streamingAssetsPath =Path.Combine(Application.streamingAssetsPath,"SolarEngine") ;
       static string fileName = "se_mainland.txt";

       private static string sourcepath = Path.Combine(mainlandpath,fileName);

        public static void setMainLand(MainLand mainLand)
        {
            if(!Directory.Exists(mainlandpath))
                Directory.CreateDirectory(mainlandpath);
            string  filePath =  Path.Combine(mainlandpath, fileName);
             try
                {
                    // 创建文件并写入内容
                    using (StreamWriter writer = new StreamWriter(filePath, false))
                    {
                        writer.WriteLine((int)mainLand);
                    }
                
                }
                catch (System.Exception e)
                {
                  Debug.LogError($"{Analytics.SolorEngine}Error writing file: {e.Message}");
                }
        }
        public static MainLand getMainLand()
        {
#if UNITY_EDITOR
            string  filePath =  Path.Combine(mainlandpath, fileName);
            #else
            string  filePath =  Path.Combine(streamingAssetsPath, fileName);
#endif

            try
            {
                // 检查文件是否存在
                if (File.Exists(filePath))
                {
                    // 读取文件内容
                    string content = File.ReadAllText(filePath);
                   _mainLand= (MainLand)int.Parse(content);
                }
                else
                {
                    _mainLand=MainLand.None;
                    Debug.Log($"{Analytics.SolorEngine} file not found :{filePath}");
                 
                    
                }
            }
            catch (System.Exception e)
            {
                _mainLand=MainLand.None;
              Debug.LogError($"{Analytics.SolorEngine}Error reading file: {e.Message}");
              
            }
            return _mainLand;
        }
        
        public static void copyMainLand()
        {
            string _mainland = Path.Combine(streamingAssetsPath,fileName);
            try
            {
                if(!Directory.Exists(streamingAssetsPath))
                    Directory.CreateDirectory(streamingAssetsPath);
                if(File.Exists(_mainland))
                    File.Delete(_mainland);
                File.Copy(sourcepath, _mainland);
                Debug.Log($"{Analytics.SolorEngine} copyMainLand Success");
              
            }
            catch (Exception e)
            {
                Debug.LogError($"{Analytics.SolorEngine} copyMainLand Failed: {e.Message}");
            }
            
        }
        public static void deleteMainLand()
        { 
            try
            {
                string _mainland = Path.Combine(streamingAssetsPath,fileName);
                if(File.Exists(_mainland))
                    File.Delete(_mainland);
                Debug.Log($"{Analytics.SolorEngine} deleteMainLand Success");
            }
            catch (Exception e)
            {
                Debug.LogError($"{Analytics.SolorEngine} deleteMainLand Failed: {e.Message}");
            }
        }
     

    }
}