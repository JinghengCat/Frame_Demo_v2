using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


public enum LoadState
{
    UnLoaded,
    Loading,
    Loaded
}

public class ResourceContainer
{

    public AssetBundle bundle;
    public bool autoRelease = true;
    public LoadState loadState = LoadState.UnLoaded;
    public string bundlePath;
    public ResourceContainer[] Dependencies;
    public int LeftSecToRelease
    {
        get; set;
    } = 5;

    #region Ref Count
    private int m_RefCount = 0;
    public int RefCount { get { return m_RefCount; } }
    public void AddRefCount()
    {
        this.m_RefCount++;
        if (Dependencies != null)
        {
            for (int i = 0; i < Dependencies.Length; i++)
            {
                Dependencies[i].AddRefCount();
            }
        }
    }

    private bool HasReference()
    {
        if (m_RefCount > 0)
        {
            return true;
        }
        else
        {
            if (m_RefCount < 0)
            {
                Debug.LogErrorFormat("RefCount Logic Error");
            }
            return false;
        }
    }
    public void MinusRefCount()
    {
        m_RefCount--;
        if (Dependencies != null)
        {
            for (int i = 0; i < Dependencies.Length; i++)
            {
                Dependencies[i].MinusRefCount();
            }
        }
        HasReference();
    }

    public void MinusRefCount(int count)
    {
        m_RefCount -= count;
        if (Dependencies != null)
        {
            for (int i = 0; i < Dependencies.Length; i++)
            {
                Dependencies[i].MinusRefCount(count);
            }
        }
        HasReference();
    }

    public void ResetContainer()
    {
        LeftSecToRelease = 5;
        if (Dependencies != null)
        {
            for (int i = 0; i < Dependencies.Length; i++)
            {
                Dependencies[i].LeftSecToRelease = 5;
            }
        }
    }
    #endregion

    public T GetAsset<T>(string assetName) where T : UnityEngine.Object
    {
#if UNITY_EDITOR
        if (Global.Instance.useAssetBundle)
        {
            return GetAssetInBundle<T>(assetName);
        }
        else
        {
            return GetAssetInEditor<T>(assetName);
        }
#else
            return GetAssetInBundle<T>(assetName);
#endif

    }

    private T GetAssetInBundle<T>(string assetName) where T : UnityEngine.Object
    {
        if (loadState == LoadState.Loaded)
        {
            var asset = bundle.LoadAsset<T>(assetName) as GameObject;
            return asset as T;
        }
        else
        {
            Debug.LogErrorFormat("GetAssetInBundle Error: Bundle is not loaded");
            return null;
        }
    }




#if UNITY_EDITOR
    private T GetAssetInEditor<T>(string assetName) where T : UnityEngine.Object
    {
        var theType = typeof(T);
        if (theType == typeof(GameObject))
        {
            return GetGameObject(assetName) as T;
        }

        return null;
    }

    private GameObject GetGameObject(string assetName)
    {
        var path = (string.Format("Assets/AssetBundle/{0}", bundlePath));
        Debug.LogFormat(path);
        GameObject asset = AssetDatabase.LoadAssetAtPath(path, typeof(GameObject)) as GameObject;
        return asset;
    }

#endif
}
