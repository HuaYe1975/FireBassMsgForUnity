﻿using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Collections;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif
using System.IO;
using System.Linq;

public class PostBuildProcessor : MonoBehaviour
{
#if UNITY_IOS
    /*
    This methods are per platform post export methods. They can be added additionally to the post process attributes in the Advanced Features Settings on UCB using
    - PostBuildProcessor.OnPostprocessBuildiOS
    - PostBuildProcessor.OnPostprocessBuildAndroid
    depending on the platform they should be executed.
    Here is the basic order of operations (e.g. with iOS operation)
    - Unity Cloud Build Pre-export methods run
    - Export process happens
    - Methods marked the built-in PostProcessBuildAttribute are called
    - Unity Cloud Build Post-export methods run
    - [unity ends]
    - (iOS) Xcode processes project
    - Done!
    More information can be found on http://forum.unity3d.com/threads/solved-ios-build-failed-pushwoosh-dependency.293192/
    */
    public static void OnPostprocessBuildiOS(string exportPath)
    {
    }
#endif

#if UNITY_IOS

    private static void AddFrameworks(string path)
    {
        string projPath = path + "/Unity-iPhone.xcodeproj/project.pbxproj";

        PBXProject proj = new PBXProject();
        proj.ReadFromString(File.ReadAllText(projPath));

        string target = proj.TargetGuidByName("Unity-iPhone");

        // Set a custom link flag
		proj.AddBuildProperty (target, "OTHER_LDFLAGS", "-ObjC");
		proj.AddBuildProperty (target, "OTHER_LDFLAGS", "-lz");
		proj.AddBuildProperty (target, "OTHER_LDFLAGS", "-lstdc++");

        // add frameworks
		proj.AddFrameworkToProject(target, "AdSupport.framework", true);
		proj.AddFrameworkToProject(target, "AddressBook.framework", true);

		// add customs
		proj.AddFileToBuild(target, proj.AddFile("usr/lib/libsqlite3.tbd", "Frameworks/libsqlite3.tbd", PBXSourceTree.Sdk));

		string[] plistFiles = Directory.GetFiles(Application.dataPath, "GoogleService-Info.plist", SearchOption.AllDirectories);
		foreach (string customPlistPath in plistFiles)
		{
			proj.AddFileToBuild(target, proj.AddFile(customPlistPath, "GoogleService-Info.plist", PBXSourceTree.Source)); 
		}

        File.WriteAllText(projPath, proj.WriteToString());
    }

    private static void MergePLists(string path)
    {
        // Add url schema to plist file
        string plistPath = path + "/Info.plist";
        Debug.Log("Open main plist at : " + plistPath);

        PlistDocument plist = new PlistDocument();
        plist.ReadFromString(File.ReadAllText(plistPath));

        // Get root
        PlistElementDict rootDict = plist.root;

        string[] plistFiles = Directory.GetFiles(Application.dataPath, "*.plist", SearchOption.AllDirectories);
        foreach (string customPlistPath in plistFiles)
        {
            if (customPlistPath == plistPath)
            {
                continue;
            }
            if (customPlistPath.Contains("/Editor/"))
            {
                continue;
            }

            Debug.Log("add keys from custom plist at path : " + customPlistPath);
            PlistDocument customPlist = new PlistDocument();
            customPlist.ReadFromString(File.ReadAllText(customPlistPath));
            PlistElementDict customRootDict = customPlist.root;
            foreach (var pair in customRootDict.values)
            {
                if (rootDict.values.ContainsKey(pair.Key))
                {
                    rootDict.values[pair.Key] = pair.Value;
                }
                else
                {
                    rootDict.values.Add(pair.Key, pair.Value);
                }
            }
        }

        // Write to file
        File.WriteAllText(plistPath, plist.WriteToString());
    }

    // a normal post process method which is executed by Unity
    [PostProcessBuild]
    public static void OnPostprocessBuild(BuildTarget buildTarget, string path)
    {

        Debug.Log("OnPostprocessBuildiOS");

        AddFrameworks(path);

        //MergePLists(path);
    }
#endif
}