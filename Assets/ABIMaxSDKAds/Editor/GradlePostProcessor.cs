using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

public class GradlePostProcessor
{
     [PostProcessBuild]
     public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
     {
          if (target == BuildTarget.Android)
          {
               string unityLibraryGradlePath = Path.Combine(pathToBuiltProject, "unityLibrary/build.gradle");

               if (File.Exists(unityLibraryGradlePath))
               {
                    string gradleContent = File.ReadAllText(unityLibraryGradlePath);

                    // Extract ndkPath value using regex
                    Regex ndkPathRegex = new Regex(@"ndkPath\s+\""(.*?)\""");
                    Match match = ndkPathRegex.Match(gradleContent);

                    if (match.Success)
                    {
                         string ndkPath = match.Groups[1].Value;
                         Debug.Log($"Extracted NDK Path: {ndkPath}");

                         // Replace android.ndkDirectory with the extracted ndkPath
                         gradleContent = gradleContent.Replace("android.ndkDirectory", $"\"{ndkPath}\"");

                         // Save the modified gradle file
                         File.WriteAllText(unityLibraryGradlePath, gradleContent);
                         Debug.Log("Replaced android.ndkDirectory with the extracted ndkPath in unityLibrary/build.gradle.");
                    }
                    else
                    {
                         Debug.LogError("ndkPath not found in the Gradle file.");
                    }
               }
               else
               {
                    Debug.LogError("unityLibrary/build.gradle not found.");
               }
          }
     }
}